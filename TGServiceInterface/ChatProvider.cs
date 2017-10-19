﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
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
		/// <summary>
		/// Starting index of <see cref="DataFields"/> which child classes should use to write their custom data to
		/// </summary>
		protected const int BaseIndex = 8;
		/// <summary>
		/// Set to <see langword="true"/> if a child constructor should use the baseInfo parameter of <see cref="TGChatSetupInfo.TGChatSetupInfo(TGChatSetupInfo, int)"/> to initialize it's property fields, <see langword="false"/> otherwise
		/// </summary>
		protected readonly bool InitializeFields;
		
		/// <summary>
		/// Raw access to the underlying data
		/// </summary>
		[DataMember]
		public IList<string> DataFields { get; protected set; }

		/// <summary>
		/// Constructs a <see cref="TGChatSetupInfo"/> from optional <paramref name="baseInfo"/>
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

		/// <summary>
		/// Recreates <see langword="this"/> as the correct child <see cref="TGChatSetupInfo"/>
		/// </summary>
		/// <returns>A new <see cref="TGChatSetupInfo"/> based on the <see cref="Provider"/> type</returns>
		TGChatSetupInfo Specialize()
		{
			switch (Provider)
			{
				case TGChatProvider.IRC:
					return new TGIRCSetupInfo(this);
				case TGChatProvider.Discord:
					return new TGDiscordSetupInfo(this);
				default:
					throw new Exception("Invalid provider!");
			}
		}

		/// <summary>
		/// Properly formats a <paramref name="channel"/> name for the <see cref="TGChatProvider"/>
		/// </summary>
		/// <param name="channel">The <see cref="string"/> to format</param>
		/// <returns>The formatted <see cref="string"/></returns>
		protected virtual string SanitizeChannelName(string channel)
		{
			return Specialize().SanitizeChannelName(channel);
		}

		/// <summary>
		/// Sanitizes a list of <paramref name="channelnames"/>
		/// </summary>
		/// <param name="channelnames">An <see cref="IList{T}"/> of strings</param>
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
		/// Constructs a <see cref="TGChatSetupInfo"/> from a data list
		/// </summary>
		/// <param name="DeserializedData">The data</param>
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
	}

	/// <summary>
	/// Chat provider for IRC. Admin entries should be user nicknames in normal mode or required channel flags in special mode
	/// </summary>
	[DataContract]
	public sealed class TGIRCSetupInfo : TGChatSetupInfo
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
	public sealed class TGDiscordSetupInfo : TGChatSetupInfo
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
