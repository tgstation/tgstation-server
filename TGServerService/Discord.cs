using Discord;
using Discord.WebSocket;
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
		DiscordSocketClient client;
		TGDiscordSetupInfo DiscordConfig;
		object DiscordLock = new object();

		IDictionary<ulong, ISocketMessageChannel> SeenPrivateChannels = new Dictionary<ulong, ISocketMessageChannel>();

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
			client = new DiscordSocketClient();
			client.MessageReceived += Client_MessageReceived;
		}

		private bool CheckAdmin(SocketUser u)
		{
			if (!DiscordConfig.AdminsAreSpecial)
				return DiscordConfig.AdminList.Contains(u.Id.ToString());
			/*
			foreach (var I in u.Roles)
				if (DiscordConfig.AdminList.Contains(I.Id.ToString()))
					return true;
			*/
			return false;
		}

		private async Task Client_MessageReceived(SocketMessage e)
		{
			await Task.Run(() =>
			{
				if (e.Author.Id == client.CurrentUser.Id)
					return;

				var pm = false; //TODO

				if (pm && !SeenPrivateChannels.ContainsKey(e.Channel.Id))
					SeenPrivateChannels.Add(e.Channel.Id, e.Channel);

				bool found = false;

				var formattedMessage = e.Content;
				foreach (var u in e.MentionedUsers)
					if (u.Id == client.CurrentUser.Id)
					{
						found = true;
						formattedMessage = formattedMessage.Replace(u.Mention, "");
						break;
					}

				if (!found && !pm)
					return;

				formattedMessage = formattedMessage.Trim();

				var cid = e.Channel.Id.ToString();

				OnChatMessage(this, e.Author.Id.ToString(), cid, formattedMessage, CheckAdmin(e.Author), pm || DiscordConfig.AdminChannels.Contains(cid));
			});
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
					client.LoginAsync(TokenType.Bot, DiscordConfig.BotToken).Wait();
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
				return client.ConnectionState == ConnectionState.Connected;
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
					client.LogoutAsync().Wait();
				}
			}
			catch { }
		}

		public string Reconnect()
		{
			TGDiscordSetupInfo tmp;
			lock (DiscordLock)
			{
				tmp = new TGDiscordSetupInfo(DiscordConfig);
				DiscordConfig.BotToken = "THIS_IS_NOT_A_BOT_TOKEN";
			}
			return SetProviderInfo(tmp);
		}

		public void SendMessage(string msg, ChatMessageType mt)
		{
			if (!Connected())
				return;
			lock (DiscordLock)
			{
				var tasks = new List<Task>();
				var Config = Properties.Settings.Default;
				foreach (var I in client.Guilds)
					foreach (var J in I.TextChannels)
					{
						var cid = J.Id.ToString();
						var wdc = DiscordConfig.WatchdogChannels;
						bool SendToThisChannel = (mt.HasFlag(ChatMessageType.AdminInfo) && DiscordConfig.AdminChannels.Contains(cid))
							|| (mt.HasFlag(ChatMessageType.DeveloperInfo) && DiscordConfig.DevChannels.Contains(cid))
							|| (mt.HasFlag(ChatMessageType.GameInfo) && DiscordConfig.GameChannels.Contains(cid))
							|| (mt.HasFlag(ChatMessageType.WatchdogInfo) && DiscordConfig.WatchdogChannels.Contains(cid));

						if (SendToThisChannel)
							tasks.Add(J.SendMessageAsync(msg));
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
						SeenPrivateChannels[channel].SendMessageAsync(message).Wait();
					else
						foreach (var I in client.Guilds)
							foreach (var J in I.TextChannels)
								if (J.Id == channel)
									J.SendMessageAsync(message).Wait();
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
				client.LogoutAsync().Wait();
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
