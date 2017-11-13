using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;

namespace TGS.Interface
{
	/// <summary>
	/// For setting up authentication no matter the chat provider
	/// </summary>
	[DataContract]
	[KnownType(typeof(IRCSetupInfo))]
	[KnownType(typeof(DiscordSetupInfo))]
	public class ChatSetupInfo
	{
		const int AdminListIndex = 0;
		const int AdminModeIndex = 1;
		const int AdminChannelIndex = 2;
		const int DevChannelIndex = 3;
		const int WDChannelIndex = 4;
		const int GameChannelIndex = 5;
		const int ProviderIndex = 6;
		const int EnabledIndex = 7;
		/// <summary>
		/// Starting index of <see cref="DataFields"/> which child classes should use to write their custom data to
		/// </summary>
		protected const int BaseIndex = 8;
		/// <summary>
		/// Set to <see langword="true"/> if a child constructor should use the baseInfo parameter of <see cref="ChatSetupInfo.ChatSetupInfo(ChatProvider, ChatSetupInfo, int)"/> to initialize it's property fields, <see langword="false"/> otherwise
		/// </summary>
		protected readonly bool InitializeFields;
		
		/// <summary>
		/// Raw access to the underlying data
		/// </summary>
		[DataMember]
		public IList<string> DataFields { get; protected set; }

		/// <summary>
		/// Constructs a <see cref="ChatSetupInfo"/> from optional <paramref name="baseInfo"/>
		/// </summary>
		/// <param name="provider">The <see cref="ChatProvider"/> that this <see cref="ChatSetupInfo"/> is for</param>
		/// <param name="baseInfo">Optional past data</param>
		/// <param name="numFields">The number of fields in this chat provider</param>
		protected internal ChatSetupInfo(ChatProvider provider, ChatSetupInfo baseInfo, int numFields)
		{
			numFields += BaseIndex;
			InitializeFields = baseInfo == null || baseInfo.DataFields.Count != numFields;

			if (InitializeFields)
			{
				DataFields = new List<string>(numFields);
				for (var I = 0; I < numFields; ++I)
					DataFields.Add(null);

				AdminList = new List<string>();
				AdminChannels = new List<string>();
				DevChannels = new List<string>();
				GameChannels = new List<string>();
				WatchdogChannels = new List<string>();
				AdminsAreSpecial = false;
				Enabled = false;
			}
			else
				DataFields = baseInfo.DataFields;
			Provider = provider;
			Specialize(true);   //to check we have a valid provider
		}

		/// <summary>
		/// Recreates <see langword="this"/> as the correct child <see cref="ChatSetupInfo"/>
		/// </summary>
		/// <param name="checkOnly">If <see langword="true"/>, <see langword="null"/> is returned provided <see cref="Provider"/> is a valid <see cref="ChatProvider"/></param>
		/// <returns>A new <see cref="ChatSetupInfo"/> based on the <see cref="Provider"/> type</returns>
		ChatSetupInfo Specialize(bool checkOnly)
		{
			switch (Provider)
			{
				case ChatProvider.IRC:
					if (!checkOnly)
						return new IRCSetupInfo(this);
					break;
				case ChatProvider.Discord:
					if (!checkOnly)
						return new DiscordSetupInfo(this);
					break;
				default:
					throw new Exception("Invalid provider!");
			}
			return null;
		}

		/// <summary>
		/// Properly formats a <paramref name="channel"/> name for the <see cref="ChatProvider"/>
		/// </summary>
		/// <param name="channel">The <see cref="string"/> to format</param>
		/// <returns>The formatted <see cref="string"/></returns>
		protected virtual string SanitizeChannelName(string channel)
		{
			return Specialize(false).SanitizeChannelName(channel);
		}

		/// <summary>
		/// Sanitizes a list of <paramref name="channelnames"/>
		/// </summary>
		/// <param name="channelnames">A <see cref="List{T}"/> of strings</param>
		void SanitizeChannelNames(IList<string> channelnames)
		{
			for (var I = 0; I < channelnames.Count; ++I)

				if (String.IsNullOrWhiteSpace(channelnames[I]))
				{
					channelnames.RemoveAt(I);
					--I;
				}
				else
					channelnames[I] = SanitizeChannelName(channelnames[I].Trim());
		}

		/// <summary>
		/// Constructs a <see cref="ChatSetupInfo"/> from a data list
		/// </summary>
		/// <param name="DeserializedData">The data</param>
		public ChatSetupInfo(IList<string> DeserializedData)
		{
			DataFields = DeserializedData;
			Specialize(false);	//ensure provider type is valid
		}
		/// <summary>
		/// The list of admin entries
		/// </summary>
		public List<string> AdminList
		{
			get { return new JavaScriptSerializer().Deserialize<List<string>>(DataFields[AdminListIndex]); }
			set { DataFields[AdminListIndex] = new JavaScriptSerializer().Serialize(value); }
		}
		/// <summary>
		/// If AdminList corresponds to a Provider specific recognization method
		/// </summary>
		public bool AdminsAreSpecial
		{
			get { return Convert.ToBoolean(DataFields[AdminModeIndex]); }
			set { DataFields[AdminModeIndex] = Convert.ToString(value); }
		}
		/// <summary>
		/// The channels from which admin commands/messages can be sent/received
		/// </summary>
		public List<string> AdminChannels
		{
			get { return new JavaScriptSerializer().Deserialize<List<string>>(DataFields[AdminChannelIndex]); }
			set
			{
				SanitizeChannelNames(value);
				DataFields[AdminChannelIndex] = new JavaScriptSerializer().Serialize(value);
			}
		}
		/// <summary>
		/// The channels to which repo and compile messages are sent
		/// </summary>
		public List<string> DevChannels
		{
			get { return new JavaScriptSerializer().Deserialize<List<string>>(DataFields[DevChannelIndex]); }
			set
			{
				SanitizeChannelNames(value);
				DataFields[DevChannelIndex] = new JavaScriptSerializer().Serialize(value);
			}
		}
		/// <summary>
		/// The channels to which watchdog messages are sent
		/// </summary>
		public List<string> WatchdogChannels
		{
			get { return new JavaScriptSerializer().Deserialize<List<string>>(DataFields[WDChannelIndex]); }
			set
			{
				SanitizeChannelNames(value);
				DataFields[WDChannelIndex] = new JavaScriptSerializer().Serialize(value);
			}
		}
		/// <summary>
		/// The channels to which game messages are sent
		/// </summary>
		public List<string> GameChannels
		{
			get { return new JavaScriptSerializer().Deserialize<List<string>>(DataFields[GameChannelIndex]); }
			set
			{
				SanitizeChannelNames(value);
				DataFields[GameChannelIndex] = new JavaScriptSerializer().Serialize(value);
			}
		}
		/// <summary>
		/// If this chat provider is enabled
		/// </summary>
		public bool Enabled
		{
			get { return Convert.ToBoolean(DataFields[EnabledIndex]); }
			set { DataFields[EnabledIndex] = Convert.ToString(value); }
		}

		/// <summary>
		/// The type of provider
		/// </summary>
		public ChatProvider Provider
		{
			get { return (ChatProvider)Convert.ToInt32(DataFields[ProviderIndex]); }
			set { DataFields[ProviderIndex] = Convert.ToString((int)value); }
		}
	}

	/// <summary>
	/// Chat provider for IRC. Admin entries should be user nicknames in normal mode or required channel flags in special mode
	/// </summary>
	[DataContract]
	public sealed class IRCSetupInfo : ChatSetupInfo
	{
		const int URLIndex = 0;
		const int PortIndex = 1;
		const int NickIndex = 2;
		const int AuthTargetIndex = 3;
		const int AuthMessageIndex = 4;
		const int AuthLevelIndex = 5;
		const int FieldsLen = 6;

		/// <summary>
		/// Construct IRC setup info from optional generic info. Defaults to TGS3 on rizons IRC server
		/// </summary>
		/// <param name="baseInfo">Optional generic info</param>
		public IRCSetupInfo(ChatSetupInfo baseInfo = null) : base(ChatProvider.IRC, baseInfo, FieldsLen)
		{
			if (!InitializeFields)
				return;

			Nickname = "TGS3";
			URL = "irc.rizon.net";
			Port = 6667;
			AuthTarget = "";
			AuthMessage = "";
			AdminsAreSpecial = true;
			AuthLevel = IRCMode.Op;
		}

		/// <inheritdoc />
		protected override string SanitizeChannelName(string working)
		{
			if (working[0] != '#')
				return "#" + working;
			return working;
		}

		/// <summary>
		/// The port of the IRC server
		/// </summary>
		public ushort Port
		{
			get { return Convert.ToUInt16(DataFields[BaseIndex + PortIndex]); }
			set { DataFields[BaseIndex + PortIndex] = value.ToString(); }
		}
		/// <summary>
		/// The URL of the IRC server
		/// </summary>
		public string URL
		{
			get { return DataFields[BaseIndex + URLIndex]; }
			set { DataFields[BaseIndex + URLIndex] = value; }
		}
		/// <summary>
		/// The nickname of the IRC bot
		/// </summary>
		public string Nickname
		{
			get { return DataFields[BaseIndex + NickIndex]; }
			set { DataFields[BaseIndex + NickIndex] = value; }
		}
		/// <summary>
		/// The target for sending authentication messages
		/// </summary>
		public string AuthTarget
		{
			get { return DataFields[BaseIndex + AuthTargetIndex]; }
			set { DataFields[BaseIndex + AuthTargetIndex] = value; }
		}
		/// <summary>
		/// The authentication message
		/// </summary>
		public string AuthMessage
		{
			get { return DataFields[BaseIndex + AuthMessageIndex]; }
			set { DataFields[BaseIndex + AuthMessageIndex] = value; }
		}
		/// <summary>
		/// The minimum mode required to use admin bot commands when in special auth mode
		/// </summary>
		public IRCMode AuthLevel
		{
			get { return (IRCMode)Convert.ToInt32(DataFields[BaseIndex + AuthLevelIndex]); }
			set { DataFields[BaseIndex + AuthLevelIndex] = Convert.ToString((int)value); }
		}
	}

	/// <summary>
	/// Chat provider for Discord. Admin entires should be user ids in normal mode or group ids in special mode
	/// </summary>
	[DataContract]
	public sealed class DiscordSetupInfo : ChatSetupInfo
	{
		const int BotTokenIndex = 0;
		const int FieldsLen = 1;
		/// <summary>
		/// Construct Discord setup info from optional generic info. Default is not a valid discord bot tokent
		/// </summary>
		/// <param name="baseInfo">Optional generic info</param>
		public DiscordSetupInfo(ChatSetupInfo baseInfo = null) : base(ChatProvider.Discord, baseInfo, FieldsLen)
		{
			if (!InitializeFields)
				return;
			BotToken = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"; //needless to say, this is fake
		}
		/// <inheritdoc />
		protected override string SanitizeChannelName(string working)
		{
			working = working.Replace("<", "").Replace(">", "").Replace("&", "");   //filter out some stuff that can come in the copypasta
			try
			{
				Convert.ToUInt64(working);
			}
			catch
			{
				throw new Exception("Invalid Discord channel ID!");
			}
			return working;
		}

		/// <summary>
		/// The Discord bot token to use. See https://discordapp.com/developers/applications/me for registering bot accounts
		/// </summary>
		public string BotToken
		{
			get { return DataFields[BaseIndex + BotTokenIndex]; }
			set { DataFields[BaseIndex + BotTokenIndex] = value; }
		}
	}
}
