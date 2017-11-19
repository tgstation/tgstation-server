using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;
using TGS.Server.ChatCommands;
using TGS.Server.ChatProviders;
using TGS.Interface;

namespace TGS.Server.Components
{
	/// <inheritdoc />
	[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
	sealed class ChatManager : IChatManager, IDisposable
	{
		/// <summary>
		/// Used for indicating unintialized encrypted data
		/// </summary>
		public const string UninitializedString = "NEEDS INITIALIZING";
		
		// Topic command return parameters
		/// <summary>
		/// The help text for a <see cref="ServerChatCommand"/>
		/// </summary>
		const string CCPHelpText = "help_text";
		/// <summary>
		/// Whether or not a <see cref="ServerChatCommand"/> is admin only
		/// </summary>
		const string CCPAdminOnly = "admin_only";
		/// <summary>
		/// The required parameters for a <see cref="ServerChatCommand"/>
		/// </summary>
		const string CCPRequiredParameters = "required_parameters";

		/// <inheritdoc />
		public event EventHandler OnRequireChatCommands;
		/// <inheritdoc />
		public event EventHandler<PopulateCommandInfoEventArgs> OnPopulateCommandInfo;

		/// <summary>
		/// The <see cref="IInstanceLogger"/> for the <see cref="ChatManager"/>
		/// </summary>
		readonly IInstanceLogger Logger;
		/// <summary>
		/// The <see cref="IInstanceConfig"/> for the <see cref="ChatManager"/>
		/// </summary>
		readonly IInstanceConfig Config;

		/// <summary>
		/// List of <see cref="IChatProvider"/>s under the <see cref="ChatManager"/>
		/// </summary>
		IList<IChatProvider> ChatProviders;

		/// <summary>
		/// List of known <see cref="ServerChatCommand"/>s
		/// </summary>
		List<Command> serverChatCommands;

		/// <summary>
		/// Construct a <see cref="ChatManager"/>
		/// </summary>
		/// <param name="logger">The value of <see cref="Logger"/></param>
		/// <param name="config">The value of <see cref="Config"/></param>
		public ChatManager(IInstanceLogger logger, IInstanceConfig config)
		{
			Logger = logger;
			Config = config;

			var infos = InitProviderInfos();
			ChatProviders = new List<IChatProvider>(infos.Count);
			foreach (var info in infos)
			{
				IChatProvider chatProvider;
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
							Logger.WriteError(String.Format("Invalid chat provider: {0}", info.Provider), EventID.InvalidChatProvider);
							continue;
					}
				}
				catch (Exception e)
				{
					Logger.WriteError(String.Format("Failed to start chat provider {0}! Error: {1}", info.Provider, e.ToString()), EventID.ChatProviderStartFail);
					continue;
				}
				chatProvider.OnChatMessage += ChatProvider_OnChatMessage;
				var res = chatProvider.Connect();
				if (res != null)
					Logger.WriteWarning(String.Format("Unable to connect to chat! Provider {0}, Error: {1}", chatProvider.GetType().ToString(), res), EventID.ChatConnectFail);
				ChatProviders.Add(chatProvider);
			}
		}

		/// <summary>
		/// Properly shuts down all <see cref="ChatProviders"/>
		/// </summary>
		public void Dispose()
		{
			if (ChatProviders != null)
			{
				var infosList = new List<IList<string>>();

				foreach (var ChatProvider in ChatProviders)
				{
					infosList.Add(ChatProvider.ProviderInfo().DataFields);
					ChatProvider.Dispose();
				}
				ChatProviders = null;

				var rawdata = JsonConvert.SerializeObject(infosList);

				Config.ChatProviderData = Interface.Helpers.EncryptData(rawdata, out string entrp);
				Config.ChatProviderEntropy = entrp;
			}
		}

		/// <summary>
		/// Implementation of <see cref="OnChatMessage"/> that recieves messages from all channels of all connected <see cref="ChatProviders"/>
		/// </summary>
		/// <param name="ChatProvider">The <see cref="IChatProvider"/> that heard the <paramref name="message"/></param>
		/// <param name="speaker">The user who wrote the <paramref name="message"/></param>
		/// <param name="channel">The channel the <paramref name="message"/> is from</param>
		/// <param name="message">The recieved message</param>
		/// <param name="isAdmin"><see langword="true"/> if <paramref name="speaker"/> is considered a chat admin, <see langword="false"/> otherwise</param>
		/// <param name="isAdminChannel"><see langword="true"/> if <paramref name="channel"/> is an admin channel, <see langword="false"/> otherwise</param>
		void ChatProvider_OnChatMessage(IChatProvider ChatProvider, string speaker, string channel, string message, bool isAdmin, bool isAdminChannel)
		{
			var splits = message.Trim().Split(' ');

			if (splits.Length == 1 && splits[0] == "")
			{
				ChatProvider.SendMessageDirect("Hi!", channel);
				return;
			}

			var asList = new List<string>(splits);

			Logger.WriteInfo(String.Format("Chat command from {0} ({2}): {1}", speaker, String.Join(" ", asList), channel), EventID.ChatCommand);

			Command.OutputProcVar.Value = (m) => ChatProvider.SendMessageDirect(m, channel);

			var CI = new CommandInfo()
			{
				IsAdmin = isAdmin,
				IsAdminChannel = isAdminChannel,
				Speaker = speaker,
				Logger = Logger,
			};

			lock (this)
			{
				OnPopulateCommandInfo(this, new PopulateCommandInfoEventArgs(CI));
				ChatCommand.ThreadCommandInfo.Value = CI;
				if (serverChatCommands == null)
					OnRequireChatCommands(this, new EventArgs());
				new RootChatCommand(serverChatCommands).DoRun(asList);
			}
		}

		/// <summary>
		/// Returns a list of <see cref="ChatSetupInfo"/>s loaded from the config or the defaults if none are set
		/// </summary>
		/// <returns>A list of <see cref="ChatSetupInfo"/>s loaded from the config or the defaults if none are set</returns>
		IList<ChatSetupInfo> InitProviderInfos()
		{
			lock (this)
			{
				var rawdata = Config.ChatProviderData;
				if (rawdata == UninitializedString)
					return new List<ChatSetupInfo>() { new IRCSetupInfo() { Nickname = Config.Name }, new DiscordSetupInfo() };

				string plaintext;
				try
				{
					plaintext = Interface.Helpers.DecryptData(rawdata, Config.ChatProviderEntropy);

					var lists = JsonConvert.DeserializeObject<List<List<string>>>(plaintext);
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
					Config.ChatProviderData = UninitializedString;
				}
			}
			//if we get here we want to retry
			return InitProviderInfos();
		}

		/// <inheritdoc />
		public IList<ChatSetupInfo> ProviderInfos()
		{
			var infosList = new List<ChatSetupInfo>();
			foreach (var chatProvider in ChatProviders)
				infosList.Add(chatProvider.ProviderInfo());
			return infosList;
		}

		/// <inheritdoc />
		public void LoadServerChatCommands(string json)
		{
			if (String.IsNullOrWhiteSpace(json))
				return;
			var tmp = new List<Command>();
			try
			{
				foreach (var I in JsonConvert.DeserializeObject<IDictionary<string, object>>(json))
				{
					var innerDick = ((JObject)I.Value).ToObject<IDictionary<string, object>>();
					var helpText = (string)innerDick[CCPHelpText];
					var adminOnly = ((long)innerDick[CCPAdminOnly]) == 1;
					var requiredParams = (int)((long)innerDick[CCPRequiredParameters]);
					tmp.Add(new ServerChatCommand(I.Key, helpText, adminOnly, requiredParams));
				}
				serverChatCommands = tmp;
			}
			catch { }
		}

		/// <inheritdoc />
		public string SetProviderInfo(ChatSetupInfo info)
		{
			info.AdminList.RemoveAll(x => String.IsNullOrWhiteSpace(x));
			info.AdminChannels.RemoveAll(x => String.IsNullOrWhiteSpace(x));
			info.GameChannels.RemoveAll(x => String.IsNullOrWhiteSpace(x));
			info.DevChannels.RemoveAll(x => String.IsNullOrWhiteSpace(x));
			info.WatchdogChannels.RemoveAll(x => String.IsNullOrWhiteSpace(x));
			try
			{
				lock (this)
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

		/// <inheritdoc />
		public bool Connected(ChatProvider providerType)
		{
			lock (this)
				foreach (var I in ChatProviders)
					if (I.ProviderInfo().Provider == providerType)
						return I.Connected();
			return false;
		}

		/// <inheritdoc />
		public void CheckConnectivity()
		{
			foreach (ChatProvider I in Enum.GetValues(typeof(ChatProvider)))
				if(!Connected(I))
					Reconnect(I);
		}

		/// <inheritdoc />
		public string Reconnect(ChatProvider providerType)
		{
			foreach (var I in ChatProviders)
				if (I.ProviderInfo().Provider == providerType)
					return I.Reconnect();
			return "Could not find specified provider!";
		}
		
		/// <inheritdoc />
		public Task SendMessage(string msg, MessageType mt)
		{
			return Task.Factory.StartNew(() =>
			{
				lock (this)
				{
					var tasks = new Dictionary<ChatProvider, Task>();
					foreach (var ChatProvider in ChatProviders)
						tasks.Add(ChatProvider.ProviderInfo().Provider, ChatProvider.SendMessage(msg, mt));
					foreach (var T in tasks)
						try
						{
							T.Value.Wait();
						}
						catch (Exception e)
						{
							Logger.WriteWarning(String.Format("Chat broadcast failed (Provider: {3}) (Flags: {0}) (Message: {1}): {2}", mt, msg, e.ToString(), T.Key), EventID.ChatBroadcastFail);
						}
				}
			});
		}

		public void ResetChatCommands()
		{
			lock (this)
				serverChatCommands = null;
		}
	}
}
