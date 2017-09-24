using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Script.Serialization;
using TGServiceInterface;

namespace TGServerService
{
	/// <summary>
	/// Type of chat message, these may be OR'd together
	/// </summary>
	[Flags]
	enum ChatMessageType
	{
		AdminInfo = 1,
		GameInfo = 2,
		WatchdogInfo = 4,
		DeveloperInfo = 8,
	}
	interface ITGChatProvider : IDisposable
	{
		/// <summary>
		/// Sets info for the provider
		/// </summary>
		/// <param name="info">The info to set</param>
		/// <returns>null on success, error message on failure</returns>
		string SetProviderInfo(TGChatSetupInfo info);

		/// <summary>
		/// Gets the info of the provider
		/// </summary>
		/// <returns>The info for the chat provider</returns>
		TGChatSetupInfo ProviderInfo();

		/// <summary>
		/// Called with chat message info
		/// </summary>
		event OnChatMessage OnChatMessage;

		/// <summary>
		/// Connects the chat provider if it's enabled
		/// </summary>
		/// <returns>null on success, error message on failure</returns>
		string Connect();
		/// <summary>
		/// Forces a reconnection of the chat provider if it's enabled
		/// </summary>
		/// <returns>null on success, error message on failure</returns>
		string Reconnect();

		/// <summary>
		/// Checks if the chat provider is connected
		/// </summary>
		/// <returns>true if the provider is connected, false otherwise</returns>
		bool Connected();

		/// <summary>
		/// Disconnects the chat provider
		/// </summary>
		void Disconnect();

		/// <summary>
		/// Send a message to a channel
		/// </summary>
		/// <param name="message">The message to send</param>
		/// <param name="channel">The channel to send to</param>
		/// <returns>null on success, error message on failure</returns>
		string SendMessageDirect(string message, string channel);

		/// <summary>
		/// Broadcast a message to appropriate channels based on the message type
		/// </summary>
		/// <param name="msg">The message to send</param>
		/// <param name="mt">The message type</param>
		void SendMessage(string msg, ChatMessageType mt);
	}

	/// <summary>
	/// Callback for the chat provider recieving a message
	/// </summary>
	/// <param name="ChatProvider">The chat provider the message came from</param>
	/// <param name="speaker">The username of the speaker</param>
	/// <param name="channel">The name of the channel</param>
	/// <param name="message">The message text</param>
	/// <param name="tagged">true if the bot was mentioned in the first word, false otherwise</param>
	delegate void OnChatMessage(ITGChatProvider ChatProvider, string speaker, string channel, string message, bool isAdmin, bool isAdminChannel);

	partial class TGStationServer : ITGChat
	{

		IList<ITGChatProvider> ChatProviders;
		object ChatLock = new object();

		public void InitChat()
		{
			var infos = InitProviderInfos();
			ChatProviders = new List<ITGChatProvider>(infos.Count);
			foreach (var info in infos)
			{
				ITGChatProvider ChatProvider;
				try
				{
					switch (info.Provider)
					{
						case TGChatProvider.Discord:
							ChatProvider = new TGDiscordChatProvider(info);
							break;
						case TGChatProvider.IRC:
							ChatProvider = new TGIRCChatProvider(info);
							break;
						default:
							TGServerService.WriteError(String.Format("Invalid chat provider: {0}", info.Provider), TGServerService.EventID.InvalidChatProvider);
							continue;
					}
				}
				catch (Exception e)
				{
					TGServerService.WriteError(String.Format("Failed to start chat provider {0}! Error: {1}", info.Provider, e.ToString()), TGServerService.EventID.ChatProviderStartFail);
					continue;
				}
				ChatProvider.OnChatMessage += ChatProvider_OnChatMessage;
				var res = ChatProvider.Connect();
				if (res != null)
					TGServerService.WriteWarning(String.Format("Unable to connect to chat! Provider {0}, Error: {1}", ChatProvider.GetType().ToString(), res), TGServerService.EventID.ChatConnectFail);
				ChatProviders.Add(ChatProvider);
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
			TGServerService.WriteInfo(String.Format("Chat Command from {0} ({2}): {1}", speaker, String.Join(" ", asList), channel), TGServerService.EventID.ChatCommand);
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

			byte[] plaintext = Encoding.UTF8.GetBytes(rawdata);

			Config.ChatProviderData = Program.EncryptData(plaintext, out string entrp);
			Config.ChatProviderEntropy = entrp;
		}

		public IList<TGChatSetupInfo> ProviderInfos()
		{
			var infosList = new List<TGChatSetupInfo>();
			foreach (var ChatProvider in ChatProviders)
				infosList.Add(ChatProvider.ProviderInfo());
			return infosList;
		}

		//public api
		IList<TGChatSetupInfo> InitProviderInfos()
		{
			lock (ChatLock)
			{
				var Config = Properties.Settings.Default;
				var rawdata = Config.ChatProviderData;
				if (rawdata == "NEEDS INITIALIZING")
					return new List<TGChatSetupInfo>() { new TGIRCSetupInfo(), new TGDiscordSetupInfo() };

				byte[] plaintext;
				try
				{
					plaintext = Program.DecryptData(rawdata, Config.ChatProviderEntropy);
					
					var lists = new JavaScriptSerializer().Deserialize<List<List<string>>>(Encoding.UTF8.GetString(plaintext));
					var output = new List<TGChatSetupInfo>(lists.Count);
                    var foundirc = 0;
                    var founddiscord = 0;
                    foreach (var l in lists)
                    {
                        var info = new TGChatSetupInfo(l);
                        if (info.Provider == TGChatProvider.Discord)
                            ++founddiscord;
                        else if (info.Provider == TGChatProvider.IRC)
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
		public string SetProviderInfo(TGChatSetupInfo info)
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
		public bool Connected(TGChatProvider providerType)
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
			foreach (TGChatProvider I in Enum.GetValues(typeof(TGChatProvider)))
				if(!Connected(I))
					Reconnect(I);
		}

		//public api
		public string Reconnect(TGChatProvider providerType)
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
		public void SendMessage(string msg, ChatMessageType mt)
		{
			lock (ChatLock)
			{
				foreach (var ChatProvider in ChatProviders)
					try
					{
						ChatProvider.SendMessage(msg, mt);
					}catch(Exception e)
					{
						TGServerService.WriteWarning(String.Format("Chat broadcast failed (Provider: {3}) (Flags: {0}) (Message: {1}): {2}", mt, msg, e.ToString(), ChatProvider.ProviderInfo().Provider), TGServerService.EventID.ChatBroadcastFail);
					}
			}
		}
	}
}
