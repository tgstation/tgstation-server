using Discord;
using System;
using System.Collections.Generic;
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
		TGStationServer Parent;

		IDictionary<ulong, Channel> SeenPrivateChannels = new Dictionary<ulong, Channel>();

		public TGDiscordChatProvider(TGChatSetupInfo info, TGStationServer parent)
		{
			Parent = parent;
			Init(info);
		}

		void Init(TGChatSetupInfo info)
		{
			DiscordConfig = new TGDiscordSetupInfo(info);
			client = new DiscordClient();
			client.MessageReceived += Client_MessageReceived;
		}

		private void Client_MessageReceived(object sender, MessageEventArgs e)
		{
			var isValidChannel = Parent.Config.ChatChannels.Contains("#" + e.Channel.Name) || e.Channel.IsPrivate;
			if (!isValidChannel)
				return;

			var tagged = e.Channel.IsPrivate && e.User.Id != client.CurrentUser.Id;
			if (tagged && !SeenPrivateChannels.ContainsKey(e.Channel.Id))
				SeenPrivateChannels.Add(e.Channel.Id, e.Channel);

			var splits = new List<string>(e.Message.Text.Trim().Split(' '));
			if (splits[0] == "@" + client.CurrentUser.Name)
			{
				splits.RemoveAt(0);
				tagged = true;
			}
			OnChatMessage(e.User.Id.ToString(), e.Channel.Id.ToString(), String.Join(" ", splits), tagged);
		}

		public string Connect()
		{
			try
			{
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

		public string SendMessage(string msg, bool adminOnly = false)
		{
			try
			{
				lock (DiscordLock)
				{
					var tasks = new List<Task>();
					foreach (var I in client.Servers)
						foreach (var J in I.TextChannels)
						{
							var SendToThisChannel = adminOnly ? ("#" + J.Name) == Parent.Config.ChatAdminChannel : Parent.Config.ChatChannels.Contains("#" + J.Name);
							if (SendToThisChannel)
								tasks.Add(J.SendMessage(msg));
						}
					foreach (var I in tasks)
						I.Wait();
					TGServerService.WriteInfo(String.Format("Discord Send{0}: {1}", adminOnly ? " (ADMIN)" : "", msg), adminOnly ? TGServerService.EventID.ChatAdminBroadcast : TGServerService.EventID.ChatBroadcast, Parent);
					return null;
				}
			}
			catch (Exception e)
			{
				return e.ToString();
			}
		}

		public string SendMessageDirect(string message, string channelname)
		{
			try
			{
				lock (DiscordLock)
				{
					var tasks = new List<Task>();
					var channel = Convert.ToUInt64(channelname);
					if (SeenPrivateChannels.ContainsKey(channel))
						SeenPrivateChannels[channel].SendMessage(message).Wait();
					else
						foreach (var I in client.Servers)
							foreach (var J in I.TextChannels)
								if (J.Id == channel)
									J.SendMessage(message).Wait();
					TGServerService.WriteInfo(String.Format("Discord Send ({0}): {1}", channelname, message), TGServerService.EventID.ChatSend, Parent);
					return null;
				}
			}
			catch (Exception e)
			{
				return e.ToString();
			}
		}

		public void SetChannels(string[] channels = null, string adminchannel = null)
		{
			//noop
		}

		void DisconnectAndDispose()
		{
			try
			{
				client.Disconnect().Wait();
			}
			catch (Exception e) {
				TGServerService.WriteError("Discord failed DnD: " + e.ToString(), TGServerService.EventID.ChatDisconnectFail, Parent);
			}
			client.Dispose();
		}

		public string SetProviderInfo(TGChatSetupInfo info)
		{
			try
			{
				lock (DiscordLock)
				{
					DisconnectAndDispose();
					Init(info);
					if (Parent.Config.ChatEnabled)
						Connect();
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
