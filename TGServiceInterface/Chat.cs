using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Web.Script.Serialization;

namespace TGServiceInterface
{
	/// <summary>
	/// The type of chat provider
	/// </summary>
	public enum TGChatProvider : int
	{
		/// <summary>
		/// IRC chat provider
		/// </summary>
		IRC = 0,
		/// <summary>
		/// Discord chat provider
		/// </summary>
		Discord = 1,
	}

	/// <summary>
	/// Supported irc permission modes
	/// </summary>
	public enum IRCMode : int
	{
		/// <summary>
		/// +
		/// </summary>
		Voice,
		/// <summary>
		/// %
		/// </summary>
		Halfop,
		/// <summary>
		/// @
		/// </summary>
		Op,
		/// <summary>
		/// ~
		/// </summary>
		Owner,
	}

	/// <summary>
	/// For setting up authentication no matter the chat provider
	/// </summary>
	[DataContract]
	[KnownType(typeof(TGIRCSetupInfo))]
	[KnownType(typeof(TGDiscordSetupInfo))]
	public class TGChatSetupInfo
	{
		const int AdminListIndex = 0;
		const int AdminModeIndex = 1;
		const int AdminChannelIndex = 2;
		const int DevChannelIndex = 3;
		const int WDChannelIndex = 4;
		const int GameChannelIndex = 5;
		const int ProviderIndex = 6;
		const int EnabledIndex = 7;
		protected const int BaseIndex = 8;
		protected readonly bool InitializeFields;
		/// <summary>
		/// Constructs a TGChatSetupInfo from optional past data
		/// </summary>
		/// <param name="baseInfo">Optional past data</param>
		/// <param name="numFields">The number of fields in this chat provider</param>
		protected TGChatSetupInfo(TGChatSetupInfo baseInfo, int numFields)
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
		}

		//trims and adds the leading #
		static string SanitizeChannelName(string working)
		{
			if (String.IsNullOrWhiteSpace(working))
				return null;
			working = working.Trim();
			if (working[0] != '#')
				return "#" + working;
			return working;
		}
		static void SanitizeChannelNames(IList<string> working)
		{
			for (var I = 0; I < working.Count; ++I)
				working[I] = SanitizeChannelName(working[I]);
		}

		/// <summary>
		/// Constructs a TGChatSetupInfo from a data list
		/// </summary>
		/// <param name="DeserializedData">The data</param>
		/// <param name="provider">The chat provider</param>
		public TGChatSetupInfo(IList<string> DeserializedData)
		{
			DataFields = DeserializedData;
		}
		/// <summary>
		/// The list of admin entries
		/// </summary>
		public IList<string> AdminList
		{
			get { return new JavaScriptSerializer().Deserialize<IList<string>>(DataFields[AdminListIndex]); }
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
		public IList<string> AdminChannels
		{
			get { return new JavaScriptSerializer().Deserialize<IList<string>>(DataFields[AdminChannelIndex]); }
			set
			{
				SanitizeChannelNames(value);
				DataFields[AdminChannelIndex] = new JavaScriptSerializer().Serialize(value);
			}
		}
		/// <summary>
		/// The channels to which repo and compile messages are sent
		/// </summary>
		public IList<string> DevChannels
		{
			get { return new JavaScriptSerializer().Deserialize<IList<string>>(DataFields[DevChannelIndex]); }
			set
			{
				SanitizeChannelNames(value);
				DataFields[DevChannelIndex] = new JavaScriptSerializer().Serialize(value);
			}
		}
		/// <summary>
		/// The channels to which watchdog messages are sent
		/// </summary>
		public IList<string> WatchdogChannels
		{
			get { return new JavaScriptSerializer().Deserialize<IList<string>>(DataFields[WDChannelIndex]); }
			set
			{
				SanitizeChannelNames(value);
				DataFields[WDChannelIndex] = new JavaScriptSerializer().Serialize(value);
			}
		}
		/// <summary>
		/// The channels to which game messages are sent
		/// </summary>
		public IList<string> GameChannels
		{
			get { return new JavaScriptSerializer().Deserialize<IList<string>>(DataFields[GameChannelIndex]); }
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
		public TGChatProvider Provider
		{
			get { return (TGChatProvider)Convert.ToInt32(DataFields[ProviderIndex]); }
			set { DataFields[ProviderIndex] = Convert.ToString((int)value); }
		}

		/// <summary>
		/// Raw access to the underlying data
		/// </summary>
		[DataMember]
		public IList<string> DataFields { get; protected set; }
	}

	/// <summary>
	/// Chat provider for IRC
	/// Admin entries should be user nicknames in normal mode or required flags in special mode
	/// </summary>
	[DataContract]
	public class TGIRCSetupInfo : TGChatSetupInfo
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
		public TGIRCSetupInfo(TGChatSetupInfo baseInfo = null) : base(baseInfo, FieldsLen)
		{
			Provider = TGChatProvider.IRC;
			if (InitializeFields)
			{
				Nickname = "TGS3";
				URL = "irc.rizon.net";
				Port = 6667;
				AuthTarget = "";
				AuthMessage = "";
				AdminsAreSpecial = true;
				AuthLevel = IRCMode.Op;
			}
		}
		/// <summary>
		/// The port of the IRC server
		/// </summary>
		public ushort Port {
			get { return Convert.ToUInt16(DataFields[BaseIndex + PortIndex]); }
			set { DataFields[BaseIndex + PortIndex] = value.ToString();  }
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
	/// Chat provider for Discord
	/// Admin entires should be user ids in normal mode or group ids in special mode
	/// </summary>
	[DataContract]
	public class TGDiscordSetupInfo : TGChatSetupInfo
	{
		const int BotTokenIndex = 0;
		const int FieldsLen = 1;
		/// <summary>
		/// Construct Discord setup info from optional generic info. Default is not a valid discord bot tokent
		/// </summary>
		/// <param name="baseInfo">Optional generic info</param>
		public TGDiscordSetupInfo(TGChatSetupInfo baseInfo = null) : base(baseInfo, FieldsLen)
		{
			Provider = TGChatProvider.Discord;
			if (InitializeFields)
				BotToken = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";	//needless to say, this is fake
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

	/// <summary>
	/// Interface for handling chat bot
	/// </summary>
	[ServiceContract]
	public interface ITGChat
	{
		/// <summary>
		/// Set the chat provider info
		/// </summary>
		/// <param name="info">The info to set</param>
		[OperationContract]
		string SetProviderInfo(TGChatSetupInfo info);
		/// <summary>
		/// Returns the chat provider info
		/// </summary>
		/// <param name="providerType">The type of provider to get info for</param>
		/// <returns>The chat provider info for the selected provider</returns>
		[OperationContract]
		IList<TGChatSetupInfo> ProviderInfos();

		/// <summary>
		/// Checks connection status
		/// </summary>
		/// <param name="providerType">The type of provider to check if connected</param>
		/// <returns>true if connected, false otherwise</returns>
		[OperationContract]
		bool Connected(TGChatProvider providerType);

		/// <summary>
		/// Reconnect to the chat service
		/// </summary>
		/// <param name="providerType">The type of provider to reconnect</param>
		/// <returns>null on success, error message on failure</returns>
		[OperationContract]
		string Reconnect(TGChatProvider providerType);
	}
}
