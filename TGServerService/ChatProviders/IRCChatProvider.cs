using System;
using System.Collections.Generic;
using System.Threading;
using TGServiceInterface;
using Meebey.SmartIrc4net;

namespace TGServerService.ChatProviders
{
	/// <summary>
	/// <see cref="IChatProvider"/> for internet relay chat
	/// </summary>
	sealed class IRCChatProvider : IChatProvider
	{
		/// <summary>
		/// Header used to mark that a channel is actually a query message
		/// </summary>
		const string PrivateMessageMarker = "---PRIVATE-MSG---";
		/// <summary>
		/// The irc client
		/// </summary>
		IrcFeatures irc;
		/// <summary>
		/// Used for multithreading safety
		/// </summary>
		object IRCLock = new object();

		/// <summary>
		/// The setup info for the provider
		/// </summary>
		IRCSetupInfo IRCConfig;

		/// <inheritdoc />
		public event OnChatMessage OnChatMessage;

		/// <inheritdoc />
		public ChatSetupInfo ProviderInfo()
		{
			return IRCConfig;
		}

		/// <summary>
		/// Construct a <see cref="IRCChatProvider"/>
		/// </summary>
		/// <param name="info">The <see cref="ChatSetupInfo"/></param>
		public IRCChatProvider(ChatSetupInfo info)
		{
			IRCConfig = new IRCSetupInfo(info);
			irc = new IrcFeatures()
			{
				SupportNonRfc = true,
				CtcpUserInfo = Service.Version,
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

		/// <inheritdoc />
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
				Service.WriteInfo(String.Format("IRC Send ({0}): {1}", channel, message), EventID.ChatSend);
				return null;
			}
			catch (Exception e)
			{
				return e.ToString();
			}
		}

		/// <inheritdoc />
		public string SetProviderInfo(ChatSetupInfo info)
		{
			var convertedInfo = (IRCSetupInfo)info;
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

		/// <summary>
		/// Checks if a message is considered sent from a chat admin
		/// </summary>
		/// <param name="e">The <see cref="IrcMessageData"/></param>
		/// <returns><see langword="true"/> if <paramref name="e"/> was sent by a chat admin, <see langword="false"/> otherwise</returns>
		private bool CheckAdmin(IrcMessageData e)
		{
			if (IRCConfig.AdminsAreSpecial)
			{
				if (e.Channel == null)
					return false;
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
			}
			return false;
		}

		/// <summary>
		/// Called when the bot recieves a query message
		/// </summary>
		/// <param name="sender">The sender of the event (usually <see cref="irc"/>)</param>
		/// <param name="e">The <see cref="IrcEventArgs"/></param>
		private void Irc_OnQueryMessage(object sender, IrcEventArgs e)
		{
			OnChatMessage(this, e.Data.Nick, e.Data.Nick + PrivateMessageMarker, e.Data.Message, CheckAdmin(e.Data), true);
		}

		/// <summary>
		/// Called when a channel the bot is in recieves a message
		/// </summary>
		/// <param name="sender">The sender of the event (usually <see cref="irc"/>)</param>
		/// <param name="e">The <see cref="IrcEventArgs"/></param>
		private void Irc_OnChannelMessage(object sender, IrcEventArgs e)
		{
			var formattedMessage = e.Data.Message.Trim();

			var splits = new List<string>(formattedMessage.Split(' '));
			var test = splits[0];
			if (test.Length > 1 && (test[test.Length - 1] == ':' || test[test.Length - 1] == ','))
				test = test.Substring(0, test.Length - 1);
			if (test.ToLower() != irc.Nickname.ToLower())
				return;

			splits.RemoveAt(0);
			formattedMessage = String.Join(" ", splits);

			OnChatMessage(this, e.Data.Nick, e.Data.Channel, formattedMessage, CheckAdmin(e.Data), IRCConfig.AdminChannels.Contains(e.Data.Channel.ToLower()));
		}

		/// <summary>
		/// Joins all channels specified in <see cref="IRCConfig"/>
		/// </summary>
		void JoinChannels()
		{
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
				irc.RfcJoin(I);
		}

		/// <summary>
		/// Sends a login query to <see cref="IRCSetupInfo.AuthTarget"/> with message <see cref="IRCSetupInfo.AuthMessage"/> 
		/// </summary>
		void Login()
		{
			lock (IRCLock)
			{
				if (IRCConfig.AuthTarget != null)
					irc.SendMessage(SendType.Message, IRCConfig.AuthTarget, IRCConfig.AuthMessage);
			}
		}
		/// <inheritdoc />
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

		/// <summary>
		/// Runs the <see cref="irc"/> listener in a safe loop
		/// </summary>
		void IRCListen()
		{
			while (irc != null && Connected())
				try
				{
					irc.Listen();
				}
				catch { }
		}

		/// <inheritdoc />
		public string Reconnect()
		{
			Disconnect();
			return Connect();
		}

		/// <inheritdoc />
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
				Service.WriteError("IRC failed QnD: " + e.ToString(), EventID.ChatDisconnectFail);
			}
		}
		/// <inheritdoc />
		public bool Connected()
		{
			lock (IRCLock)
			{
				return irc != null && irc.IsConnected;
			}
		}
		/// <inheritdoc />
		public void SendMessage(string message, MessageType mt)
		{
			if (!Connected())
				return;
			lock (IRCLock)
			{
				foreach (var cid in irc.JoinedChannels)
				{
					bool SendToThisChannel = (mt.HasFlag(MessageType.AdminInfo) && IRCConfig.AdminChannels.Contains(cid))
						|| (mt.HasFlag(MessageType.DeveloperInfo) && IRCConfig.DevChannels.Contains(cid))
						|| (mt.HasFlag(MessageType.GameInfo) && IRCConfig.GameChannels.Contains(cid))
						|| (mt.HasFlag(MessageType.WatchdogInfo) && IRCConfig.WatchdogChannels.Contains(cid));
					if (SendToThisChannel)
						irc.SendMessage(SendType.Message, cid, message);
				}
			}
		}


		#region IDisposable Support
		/// <summary>
		/// To detect redundant <see cref="Dispose()"/> calls
		/// </summary>
		private bool disposedValue = false;

		/// <summary>
		/// Implements the <see cref="IDisposable"/> pattern. Calls <see cref="Disconnect"/> and sets <see cref="irc"/> to <see langword="null"/>
		/// </summary>
		/// <param name="disposing"><see langword="true"/> if <see cref="Dispose()"/> was called manually, <see langword="false"/> if it was from the finalizer</param>
		void Dispose(bool disposing)
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
		/// <summary>
		/// Implements the <see cref="IDisposable"/> pattern
		/// </summary>
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
