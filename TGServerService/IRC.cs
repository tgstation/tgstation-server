﻿using System;
using System.Collections.Generic;
using System.Threading;
using TGServiceInterface;
using Meebey.SmartIrc4net;


namespace TGServerService
{
	class TGIRCChatProvider : ITGChatProvider
	{
		const string PrivateMessageMarker = "---PRIVATE-MSG---";
		IrcFeatures irc;

		object IRCLock = new object();

		TGIRCSetupInfo IRCConfig;
		TGStationServer Parent;

		public event OnChatMessage OnChatMessage;
		
<<<<<<< HEAD
		public TGChatSetupInfo ProviderInfo()
		{
			return IRCConfig;
		}

		public TGIRCChatProvider(TGChatSetupInfo info)
=======
		public TGIRCChatProvider(TGChatSetupInfo info, TGStationServer parent)
>>>>>>> Instances
		{
			Parent = parent;
			IRCConfig = new TGIRCSetupInfo(info);
			irc = new IrcFeatures()
			{
				SupportNonRfc = true,
				CtcpUserInfo = TGServerService.Version,
				AutoRejoin = true,
				AutoRejoinOnKick = true,
				AutoRelogin = true,
				AutoRetry = true,
				AutoRetryLimit = 5,
				AutoRetryDelay = 5,
				ActiveChannelSyncing = true,
			};
			irc.OnChannelMessage += Irc_OnChannelMessage;
			irc.OnQueryMessage += Irc_OnQueryMessage;
		}
		
		//public api
		public string SendMessageDirect(string message, string channel)
		{
			try
			{
				if (!Connected())
					return "Disconnected.";
				lock (IRCLock)
				{
					if (channel.Contains(PrivateMessageMarker))
						channel = channel.Replace(PrivateMessageMarker, "");
					irc.SendMessage(SendType.Message, channel, message);
				}
<<<<<<< HEAD
				TGServerService.WriteInfo(String.Format("IRC Send ({0}): {1}", channel, message), TGServerService.EventID.ChatSend);
=======
				TGServerService.WriteInfo(String.Format("IRC Send ({0}): {1}", channel, message), TGServerService.EventID.ChatSend, Parent);
>>>>>>> Instances
				return null;
			}
			catch (Exception e)
			{
				return e.ToString();
			}
		}

		public string SetProviderInfo(TGChatSetupInfo info)
		{
			var convertedInfo = (TGIRCSetupInfo)info;
			var serverChange = convertedInfo.URL != IRCConfig.URL || convertedInfo.Port != IRCConfig.Port;
			IRCConfig = convertedInfo;
			if (!IRCConfig.Enabled)
			{
				Disconnect();
				return null;
			}
			else if (serverChange || !Connected())
				return Reconnect();

			if (IRCConfig.Nickname != irc.Nickname)
				irc.RfcNick(convertedInfo.Nickname);
			Login();
			JoinChannels();
			return null;
		}

		private bool CheckAdmin(IrcMessageData e)
		{
<<<<<<< HEAD
			if (IRCConfig.AdminsAreSpecial)
			{
				var Chan = irc.GetChannel(e.Channel);
				var user = (NonRfcChannelUser)irc.GetChannelUser(e.Channel, e.Nick);
				if (user != null)
					switch (IRCConfig.AuthLevel)
					{
						case IRCMode.Voice:
							if (user.IsVoice)
								return true;
							goto case IRCMode.Halfop;
						case IRCMode.Halfop:
							if (user.IsHalfop)
								return true;
							goto case IRCMode.Op;
						case IRCMode.Op:
							if (user.IsOp)
								return true;
							goto case IRCMode.Owner;
						case IRCMode.Owner:
							if (user.IsOwner)
								return true;
							break;
					}
			}
			else
			{
				var lowerNick = e.Nick.ToLower();
				foreach (var I in IRCConfig.AdminList)
					if (lowerNick == I.ToLower())
						return true;
=======
			lock (IRCLock) {
				var channelsList = new List<string>(channels);
				foreach (var I in irc.JoinedChannels)
					if (!channelsList.Contains(I))
						irc.RfcPart(I);
				foreach (var I in channelsList)
					if (!irc.JoinedChannels.Contains(I))
						irc.RfcJoin(I);
>>>>>>> Instances
			}
			return false;
		}

		//private message
		private void Irc_OnQueryMessage(object sender, IrcEventArgs e)
		{
			OnChatMessage(this, e.Data.Nick, e.Data.Nick + PrivateMessageMarker, e.Data.Message, CheckAdmin(e.Data), true);
		}

		private void Irc_OnChannelMessage(object sender, IrcEventArgs e)
		{
			var formattedMessage = e.Data.Message.Trim();

			var splits = new List<string>(formattedMessage.Split(' '));
			var test = splits[0];
			if (test.Length > 1 && test[test.Length - 1] == ':')
				test = test.Substring(0, test.Length - 1);
			if (test.ToLower() != irc.Nickname.ToLower())
				return;

			splits.RemoveAt(0);
			formattedMessage = String.Join(" ", splits);

			OnChatMessage(this, e.Data.Nick, e.Data.Channel, formattedMessage, CheckAdmin(e.Data), IRCConfig.AdminChannels.Contains(e.Data.Channel.ToLower()));
		}
		//Joins configured channels
		void JoinChannels()
		{
<<<<<<< HEAD
			var hs = new HashSet<string>();	//for unique inserts
			foreach (var I in IRCConfig.AdminChannels)
				hs.Add(I);
			foreach (var I in IRCConfig.DevChannels)
				hs.Add(I);
			foreach (var I in IRCConfig.GameChannels)
				hs.Add(I);
			foreach (var I in IRCConfig.WatchdogChannels)
				hs.Add(I);
			var ToPart = new List<string>();
			foreach (var I in irc.JoinedChannels)
				if (!hs.Remove(I))
					ToPart.Add(I);
			foreach (var I in ToPart)
				irc.RfcPart(I);
			foreach (var I in hs)
=======
			foreach (var I in Parent.Config.ChatChannels)
>>>>>>> Instances
				irc.RfcJoin(I);
		}
		//runs the login command
		void Login()
		{
			lock (IRCLock)
			{
				if (IRCConfig.AuthTarget != null)
					irc.SendMessage(SendType.Message, IRCConfig.AuthTarget, IRCConfig.AuthMessage);
			}
		}
		//public api
		public string Connect()
		{
			if (Connected() || !IRCConfig.Enabled)
				return null;
			lock (IRCLock)
			{
				try
				{
					try
					{
						irc.Connect(IRCConfig.URL, IRCConfig.Port);
					}
					catch (Exception e)
					{
						return "IRC server unreachable: " + e.ToString();
					}

					try
					{
						irc.Login(IRCConfig.Nickname, IRCConfig.Nickname);
					}
					catch (Exception e)
					{
						return "Bot name is already taken: " + e.ToString();
					}
					Login();
					JoinChannels();
					
					new Thread(new ThreadStart(IRCListen)) { IsBackground = true }.Start();
					return null;
				}
				catch (Exception e)
				{
					return e.ToString();
				}
			}
		}

		//This is the thread that listens for irc messages
		void IRCListen()
		{
			while (irc != null && Connected())
				try
				{
					irc.Listen();
				}
				catch { }
		}

		//public api
		public string Reconnect()
		{
			Disconnect();
			return Connect();
		}

		//public api
		public void Disconnect()
		{ 
			try
			{
				lock (IRCLock)
				{
					if (irc.IsConnected)
					{
						irc.RfcQuit("Twas meant to be...");
						irc.Disconnect();
					}
				}
			}
			catch (Exception e)
			{
<<<<<<< HEAD
				TGServerService.WriteError("IRC failed QnD: " + e.ToString(), TGServerService.EventID.ChatDisconnectFail);
=======
				TGServerService.WriteError("IRC failed QnD: " + e.ToString(), TGServerService.EventID.ChatDisconnectFail, Parent);
>>>>>>> Instances
			}
		}
		//public api
		public bool Connected()
		{
			lock (IRCLock)
			{
				return irc != null && irc.IsConnected;
			}
		}
		//public api
		public void SendMessage(string message, ChatMessageType mt)
		{
			if (!Connected())
				return;
			lock (IRCLock)
			{
				foreach (var cid in irc.JoinedChannels)
				{
<<<<<<< HEAD
					bool SendToThisChannel = (mt.HasFlag(ChatMessageType.AdminInfo) && IRCConfig.AdminChannels.Contains(cid))
						|| (mt.HasFlag(ChatMessageType.DeveloperInfo) && IRCConfig.DevChannels.Contains(cid))
						|| (mt.HasFlag(ChatMessageType.GameInfo) && IRCConfig.GameChannels.Contains(cid))
						|| (mt.HasFlag(ChatMessageType.WatchdogInfo) && IRCConfig.WatchdogChannels.Contains(cid));
					if (SendToThisChannel)
						irc.SendMessage(SendType.Message, cid, message);
				}
=======
					if (adminOnly)
						irc.SendMessage(SendType.Message, Parent.Config.ChatAdminChannel, message);
					else
						foreach (var I in Parent.Config.ChatChannels)
							irc.SendMessage(SendType.Message, I, message);
				}
				TGServerService.WriteInfo(String.Format("IRC Send{0}: {1}", adminOnly ? " (ADMIN)" : "", message), adminOnly ? TGServerService.EventID.ChatAdminBroadcast : TGServerService.EventID.ChatBroadcast, Parent);
				return null;
			}
			catch (Exception e)
			{
				return e.ToString();
>>>>>>> Instances
			}
		}


		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects).
					Disconnect();
					irc = null;
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~TGIRCChatProvider() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
	}
}
