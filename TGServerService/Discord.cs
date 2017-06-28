using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TGServiceInterface;

namespace TGServerService
{
	class TGDiscordChatProvider : ITGChatProvider
	{
		public event OnChatMessage OnChatMessage;
		DiscordClient client;
		TGDiscordSetupInfo DiscordConfig;
		object DiscordLock = new object();

		IDictionary<ulong, Channel> SeenPrivateChannels = new Dictionary<ulong, Channel>();

		public TGDiscordChatProvider(TGChatSetupInfo info)
		{
			Init(info);
		}

		public TGChatSetupInfo ProviderInfo()
		{
			return DiscordConfig;
		}

		void Init(TGChatSetupInfo info)
		{
			DiscordConfig = new TGDiscordSetupInfo(info);
			client = new DiscordClient();
			client.MessageReceived += Client_MessageReceived;
		}

		private bool CheckAdmin(User u)
		{
			if (!DiscordConfig.AdminsAreSpecial)
				return DiscordConfig.AdminList.Contains(u.Id.ToString());
			foreach (var I in u.Roles)
				if (DiscordConfig.AdminList.Contains(I.Id.ToString()))
					return true;
			return false;
		}

		private void Client_MessageReceived(object sender, MessageEventArgs e)
		{
			var pm = e.Channel.IsPrivate && e.User.Id != client.CurrentUser.Id;
	
			if (pm && !SeenPrivateChannels.ContainsKey(e.Channel.Id))
				SeenPrivateChannels.Add(e.Channel.Id, e.Channel);

			bool found = false;

			var formattedMessage = e.Message.RawText;
			foreach (var u in e.Message.MentionedUsers)
				if(u.Id == client.CurrentUser.Id)
				{
					found = true;
					formattedMessage = formattedMessage.Replace(u.Mention, "");
					break;
				}

			if (!found && !pm)
				return;

			formattedMessage = formattedMessage.Trim();
			
			var cid = e.Channel.Id.ToString();

			OnChatMessage(this, e.User.Id.ToString(), cid, formattedMessage, CheckAdmin(e.User), pm || DiscordConfig.AdminChannels.Contains(cid));
		}

		public string Connect()
		{
			try
			{
				if (Connected() || !DiscordConfig.Enabled)
					return null;
				lock (DiscordLock)
				{
					SeenPrivateChannels.Clear();
					client.Connect(DiscordConfig.BotToken, TokenType.Bot).Wait();
				}
				return !Connected() ? "Connection failed!" : null;
			}
			catch (Exception e)
			{
				return e.ToString();
			}
		}

		public bool Connected()
		{
			lock (DiscordLock)
			{
				return client.State == ConnectionState.Connected;
			}
		}

		public void Disconnect()
		{
			try
			{
				if (!Connected())
					return;
				lock (DiscordLock)
				{
					SeenPrivateChannels.Clear();
					client.Disconnect().Wait();
				}
			}
			catch { }
		}

		public string Reconnect()
		{
			try
			{
				lock (DiscordLock)
				{
					SeenPrivateChannels.Clear();
					if (client.State == ConnectionState.Connected)
						client.Disconnect().Wait();
					client.Connect(DiscordConfig.BotToken, TokenType.Bot).Wait();
				}
				return !Connected() ? "Connection failed!" : null;
			}
			catch (Exception e)
			{
				return e.ToString();
			}
		}

		public void SendMessage(string msg, ChatMessageType mt)
		{
			if (!Connected())
				return;
			lock (DiscordLock)
			{
				var tasks = new List<Task>();
				var Config = Properties.Settings.Default;
				foreach (var I in client.Servers)
					foreach (var J in I.TextChannels)
					{
						var cid = J.Id.ToString();
						var wdc = DiscordConfig.WatchdogChannels;
						bool SendToThisChannel = (mt.HasFlag(ChatMessageType.AdminInfo) && DiscordConfig.AdminChannels.Contains(cid))
							|| (mt.HasFlag(ChatMessageType.DeveloperInfo) && DiscordConfig.DevChannels.Contains(cid))
							|| (mt.HasFlag(ChatMessageType.GameInfo) && DiscordConfig.GameChannels.Contains(cid))
							|| (mt.HasFlag(ChatMessageType.WatchdogInfo) && DiscordConfig.WatchdogChannels.Contains(cid));

						if (SendToThisChannel)
							tasks.Add(J.SendMessage(msg));
					}
				foreach (var I in tasks)
					I.Wait();
			}
		}

		public string SendMessageDirect(string message, string channelname)
		{
			if (!Connected())
				return "Disconnected.";
			try
			{
				lock (DiscordLock)
				{
					var tasks = new List<Task>();
					var Config = Properties.Settings.Default;
					var channel = Convert.ToUInt64(channelname);
					if (SeenPrivateChannels.ContainsKey(channel))
						SeenPrivateChannels[channel].SendMessage(message).Wait();
					else
						foreach (var I in client.Servers)
							foreach (var J in I.TextChannels)
								if (J.Id == channel)
									J.SendMessage(message).Wait();
					TGServerService.WriteInfo(String.Format("Discord Send ({0}): {1}", channelname, message), TGServerService.EventID.ChatSend);
					return null;
				}
			}
			catch (Exception e)
			{
				return e.ToString();
			}
		}
		void DisconnectAndDispose()
		{
			try
			{
				client.Disconnect().Wait();
			}
			catch (Exception e) {
				TGServerService.WriteError("Discord failed DnD: " + e.ToString(), TGServerService.EventID.ChatDisconnectFail);
			}
			client.Dispose();
		}

		public string SetProviderInfo(TGChatSetupInfo info)
		{
			try
			{
				lock (DiscordLock)
				{
					var odc = DiscordConfig;
					DiscordConfig = new TGDiscordSetupInfo(info);
					if (DiscordConfig.BotToken != odc.BotToken)
					{
						DisconnectAndDispose();
						Init(info);
					}
					if (DiscordConfig.Enabled)
					{
						if (!Connected())
							return Reconnect();
					}
					else
						Disconnect();
				}
				return null;
			}
			catch (Exception e)
			{
				return e.ToString();
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
					DisconnectAndDispose();
					// TODO: dispose managed state (managed objects).
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~TGDiscordChatProvider() {
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
