using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using TGServerService.ChatCommands;
using TGServerService.ChatProviders;
using TGServiceInterface;
using TGServiceInterface.Components;

namespace TGServerService
{
	partial class ServerInstance : ITGChat
	{

		IList<ITGChatProvider> ChatProviders;
		object ChatLock = new object();

		public void InitChat()
		{
			var infos = InitProviderInfos();
			ChatProviders = new List<ITGChatProvider>(infos.Count);
			foreach (var info in infos)
			{
				ITGChatProvider chatProvider;
				try
				{
					switch (info.Provider)
					{
						case ChatProvider.Discord:
							chatProvider = new DiscordChatProvider(info);
							break;
						case ChatProvider.IRC:
							chatProvider = new IRCChatProvider(info);
							break;
						default:
							Service.WriteError(String.Format("Invalid chat provider: {0}", info.Provider), EventID.InvalidChatProvider);
							continue;
					}
				}
				catch (Exception e)
				{
					Service.WriteError(String.Format("Failed to start chat provider {0}! Error: {1}", info.Provider, e.ToString()), EventID.ChatProviderStartFail);
					continue;
				}
				chatProvider.OnChatMessage += ChatProvider_OnChatMessage;
				var res = chatProvider.Connect();
				if (res != null)
					Service.WriteWarning(String.Format("Unable to connect to chat! Provider {0}, Error: {1}", chatProvider.GetType().ToString(), res), EventID.ChatConnectFail);
				ChatProviders.Add(chatProvider);
			}
		}

		private void ChatProvider_OnChatMessage(ITGChatProvider ChatProvider, string speaker, string channel, string message, bool isAdmin, bool isAdminChannel)
		{
			var splits = message.Trim().Split(' ');

			if (splits.Length == 1 && splits[0] == "")
			{
				ChatProvider.SendMessageDirect("Hi!", channel);
				return;
			}

			var asList = new List<string>(splits);

			Command.OutputProcVar.Value = (m) => ChatProvider.SendMessageDirect(m, channel);
			ChatCommand.CommandInfo.Value = new CommandInfo()
			{
				IsAdmin = isAdmin,
				IsAdminChannel = isAdminChannel,
				Speaker = speaker,
				Server = this,
			};
			Service.WriteInfo(String.Format("Chat Command from {0} ({2}): {1}", speaker, String.Join(" ", asList), channel), EventID.ChatCommand);
			if (ServerChatCommands == null)
				LoadServerChatCommands();
			new RootChatCommand(ServerChatCommands).DoRun(asList);
		}

		//cleanup and save
		void DisposeChat()
		{
			var infosList = new List<IList<string>>();

			foreach (var ChatProvider in ChatProviders)
			{
				infosList.Add(ChatProvider.ProviderInfo().DataFields);
				ChatProvider.Dispose();
			}
			ChatProviders = null;

			var rawdata = new JavaScriptSerializer().Serialize(infosList);
			var Config = Properties.Settings.Default;

			Config.ChatProviderData = Helpers.EncryptData(rawdata, out string entrp);
			Config.ChatProviderEntropy = entrp;
		}

		public IList<ChatSetupInfo> ProviderInfos()
		{
			var infosList = new List<ChatSetupInfo>();
			foreach (var chatProvider in ChatProviders)
				infosList.Add(chatProvider.ProviderInfo());
			return infosList;
		}

		//public api
		IList<ChatSetupInfo> InitProviderInfos()
		{
			lock (ChatLock)
			{
				var Config = Properties.Settings.Default;
				var rawdata = Config.ChatProviderData;
				if (rawdata == "NEEDS INITIALIZING")
					return new List<ChatSetupInfo>() { new IRCSetupInfo(), new DiscordSetupInfo() };

				string plaintext;
				try
				{
					plaintext = Helpers.DecryptData(rawdata, Config.ChatProviderEntropy);
					
					var lists = new JavaScriptSerializer().Deserialize<List<List<string>>>(plaintext);
					var output = new List<ChatSetupInfo>(lists.Count);
					var foundirc = 0;
					var founddiscord = 0;
					foreach (var l in lists)
					{
						var info = new ChatSetupInfo(l);
						if (info.Provider == ChatProvider.Discord)
							++founddiscord;
						else if (info.Provider == ChatProvider.IRC)
							++foundirc;
						output.Add(info);
					}

					if (foundirc != 1 || founddiscord != 1)
						throw new Exception();
					
					return output;
				}
				catch
				{
					Config.ChatProviderData = "NEEDS INITIALIZING";
				}
			}
			//if we get here we want to retry
			return InitProviderInfos();
		}

		//public api
		public string SetProviderInfo(ChatSetupInfo info)
		{
			try
			{
				lock (ChatLock)
				{
					foreach (var ChatProvider in ChatProviders)
						if (info.Provider == ChatProvider.ProviderInfo().Provider)
							return ChatProvider.SetProviderInfo(info);
					return "Error: Invalid provider: " + info.Provider.ToString();
				}
			}
			catch (Exception e)
			{
				return e.ToString();
			}
		}

		//public api
		public bool Connected(ChatProvider providerType)
		{
			foreach (var I in ChatProviders)
				if (I.ProviderInfo().Provider == providerType)
					return I.Connected();
			return false;
		}

		/// <summary>
		/// Reconnect servers that are enabled and disconnected
		/// </summary>
		void ChatConnectivityCheck()
		{
			foreach (ChatProvider I in Enum.GetValues(typeof(ChatProvider)))
				if(!Connected(I))
					Reconnect(I);
		}

		//public api
		public string Reconnect(ChatProvider providerType)
		{
			foreach (var I in ChatProviders)
				if (I.ProviderInfo().Provider == providerType)
					return I.Reconnect();
			return "Could not find specified provider!";
		}
		
		/// <summary>
		/// Broadcast a message to appropriate channels based on the message type
		/// </summary>
		/// <param name="msg">The message to send</param>
		/// <param name="mt">The message type</param>
		public void SendMessage(string msg, MessageType mt)
		{
			lock (ChatLock)
			{
				foreach (var ChatProvider in ChatProviders)
					try
					{
						ChatProvider.SendMessage(msg, mt);
					}catch(Exception e)
					{
						Service.WriteWarning(String.Format("Chat broadcast failed (Provider: {3}) (Flags: {0}) (Message: {1}): {2}", mt, msg, e.ToString(), ChatProvider.ProviderInfo().Provider), EventID.ChatBroadcastFail);
					}
			}
		}
	}
}
