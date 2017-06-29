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
			var command = asList[0].ToLower();
			asList.RemoveAt(0);

			Command.OutputProcVar.Value = (m) => ChatProvider.SendMessageDirect(m, channel);
			ChatCommand.CommandInfo.Value = new CommandInfo()
			{
				IsAdmin = isAdmin,
				IsAdminChannel = isAdminChannel
			};
			TGServerService.WriteInfo(String.Format("Chat Command from {0} ({3}): {1} {2}", speaker, command, String.Join(" ", asList), channel), TGServerService.EventID.ChatCommand);
			new RootChatCommand().DoRun(asList);
		}

		string HasChatAdmin(bool isAdmin, bool isAdminChannel)
		{
			if (!isAdmin)
				return "You are not authorized to use that command!";
			if (!isAdminChannel)
				return "Use this command in an admin channel!";
			return null;
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

		//Do stuff with words that were spoken to us
		string ChatCommand2(string command, string speaker, string channel, IList<string> parameters, bool isAdmin, bool isAdminChannel)
		{
			var adminmessage = HasChatAdmin(isAdmin, isAdminChannel);
			switch (command)
			{
				case "byond":
					if (parameters.Count > 0)
						if (parameters[0].ToLower() == "--staged")
							return GetVersion(TGByondVersion.Staged) ?? "None";
						else if (parameters[0].ToLower() == "--latest")
							return GetVersion(TGByondVersion.Latest) ?? "Unknown";
					return GetVersion(TGByondVersion.Installed) ?? "Uninstalled";
				case "status":
					return adminmessage ?? SendCommand(SCIRCStatus);
				case "adminwho":
					return adminmessage ?? SendCommand(SCAdminWho);
				case "ahelp":
					if (adminmessage != null)
						return adminmessage;
					if (parameters.Count < 2)
						return "Usage: ahelp <ckey> <message>";
					var ckey = parameters[0];
					parameters.RemoveAt(0);
					return SendPM(ckey, speaker, String.Join(" ", parameters));
				case "namecheck":
					if (adminmessage != null)
						return adminmessage;
					if (parameters.Count < 1)
						return "Usage: namecheck <target>";
					return NameCheck(parameters[0], speaker);
				case "prs":
					var PRs = MergedPullRequests(out string res);
					if (PRs == null)
						return res;
					if (PRs.Count == 0)
						return "None!";
					res = "";
					foreach(var I in PRs)
						res += I.Number + " ";
					return res;
				case "version":
					return TGServerService.Version;
				case "kek":
					return "kek";
			}
			return "Unknown command: " + command;
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
					foreach (var l in lists)
						output.Add(new TGChatSetupInfo(l));
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
