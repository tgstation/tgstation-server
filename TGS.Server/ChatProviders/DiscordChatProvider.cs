using Discord;
using Discord.WebSocket;
using Discord.Net.Providers.WS4Net;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TGS.Interface;

namespace TGS.Server.ChatProviders
{
	/// <summary>
	/// <see cref="IChatProvider"/> for Discord: https://discordapp.com/
	/// </summary>
	sealed class DiscordChatProvider : IChatProvider
	{
		/// <inheritdoc />
		public event OnChatMessage OnChatMessage;

		private const int ConnectionOperationTimeoutSeconds = 10;

		/// <summary>
		/// The Discord API client
		/// </summary>
		DiscordSocketClient client;
		/// <summary>
		/// The setup info for the provider
		/// </summary>
		DiscordSetupInfo DiscordConfig;
		/// <summary>
		/// Used for multithreading safety
		/// </summary>
		object DiscordLock = new object();

		/// <summary>
		/// An <see cref="IDictionary{TKey, TValue}"/> of internal identifers => <see cref="ISocketMessageChannel"/>s we have seen
		/// </summary>
		IDictionary<ulong, ISocketMessageChannel> SeenPrivateChannels = new Dictionary<ulong, ISocketMessageChannel>();

		/// <summary>
		/// Construct a <see cref="DiscordChatProvider"/>
		/// </summary>
		/// <param name="info">The <see cref="ChatSetupInfo"/></param>
		public DiscordChatProvider(ChatSetupInfo info)
		{
			Init(info);
		}
		
		/// <inheritdoc />
		public ChatSetupInfo ProviderInfo()
		{
			return DiscordConfig;
		}

		/// <summary>
		/// Sets up the Discord API <see cref="client"/> and <see cref="DiscordConfig"/>
		/// </summary>
		/// <param name="info">The <see cref="ChatSetupInfo"/> to init <see cref="DiscordConfig"/> with</param>
		void Init(ChatSetupInfo info)
		{
			DiscordConfig = new DiscordSetupInfo(info);
			client = new DiscordSocketClient(new DiscordSocketConfig
			{
				WebSocketProvider = WS4NetProvider.Instance
			});
			client.MessageReceived += Client_MessageReceived;
		}

		/// <summary>
		/// Checks if a <paramref name="user"/> is considered a chat admin
		/// </summary>
		/// <param name="user">The sender of a message</param>
		/// <returns><see langword="true"/> if <paramref name="user"/> is a chat admin, <see langword="false"/> otherwise</returns>
		private bool CheckAdmin(SocketUser user)
		{
			if (!DiscordConfig.AdminsAreSpecial)
				return DiscordConfig.AdminList.Contains(user.Id.ToString());
			if(user is SocketGuildUser sgu)
				foreach (var I in sgu.Roles)
					if (DiscordConfig.AdminList.Contains(I.Id.ToString()))
						return true;
			return false;
		}

		/// <summary>
		/// Called when a channel the bot is in recieves a message or the bot is PM'd directly
		/// </summary>
		/// <param name="e">The event arguments</param>
		/// <returns>The task to run when this occurs</returns>
		private async Task Client_MessageReceived(SocketMessage e)
		{
			await Task.Run(() =>
			{
				if (e.Author.Id == client.CurrentUser.Id)
					return;

				var pm = e.Channel is IPrivateChannel;

				if (pm && !SeenPrivateChannels.ContainsKey(e.Channel.Id))
					SeenPrivateChannels.Add(e.Channel.Id, e.Channel);

				bool found = false;

				var formattedMessage = e.Content;
				foreach (var u in e.MentionedUsers)
					if (u.Id == client.CurrentUser.Id)
					{
						found = true;
						formattedMessage = formattedMessage.Replace(u.Mention, "");
						var nicknameMention = u.Mention.Replace("!", "");
						formattedMessage = formattedMessage.Replace(nicknameMention, "");
						break;
					}

				if (!found)
				{
					var splits = formattedMessage.Split(' ');
					found = splits[0].ToLower() == "!tgs";
					if (found)
					{
						var asList = new List<string>();
						asList.AddRange(splits);
						asList.RemoveAt(0);
						formattedMessage = String.Join(" ", asList);
					}
				}

				if (!found && !pm)
					return;

				formattedMessage = formattedMessage.Trim();

				var cid = e.Channel.Id.ToString();

				OnChatMessage(this, e.Author.Id.ToString(), cid, formattedMessage, CheckAdmin(e.Author), pm || DiscordConfig.AdminChannels.Contains(cid));
			});
		}

		/// <inheritdoc />
		public string Connect()
		{
			try
			{
				if (Connected() || !DiscordConfig.Enabled)
					return null;
				if (!Disconnected()) //not connected OR disconnected. Pending operation, so lets just reinit the connection client
				{
                    lock (DiscordLock)
                    {
                        DisconnectAndDispose();
                        Init(DiscordConfig);
                    }
				}
				if (ConnectionOperationPending())
					return "Can not connect. A connection or disconnection operation is pending and attempting to delete the socket client failed to stop the pending operation";
				lock (DiscordLock)
				{
					SeenPrivateChannels.Clear();
					client.LoginAsync(TokenType.Bot, DiscordConfig.BotToken).Wait();
					client.StartAsync().Wait();
					for (var i = 0; i >= ConnectionOperationTimeoutSeconds; i++)
					{
						if (client.ConnectionState != ConnectionState.Connecting)
							break;
						Thread.Sleep(1000);
					}
				}
                if (!Connected())
                {
                    lock (DiscordLock)
                        if (client.ConnectionState != ConnectionState.Connecting)
                        {
                            DisconnectAndDispose();
                            Init(DiscordConfig);
                        }
                    return "Connection failed!";
                }
                return null;
			}
			catch (Exception e)
			{
				return e.ToString();
			}
		}

		/// <inheritdoc />
		public bool Connected()
		{
			lock (DiscordLock)
				return client.ConnectionState == ConnectionState.Connected;
		}
		public bool Disconnected()
		{
			lock (DiscordLock)
				return client.ConnectionState == ConnectionState.Disconnected;
		}
		public bool ConnectionOperationPending()
		{
			lock (DiscordLock)
				return (client.ConnectionState == ConnectionState.Connecting || client.ConnectionState == ConnectionState.Disconnecting);
		}

		/// <inheritdoc />
		public void Disconnect()
		{
			try
			{

				if (Disconnected())
					return;
				lock (DiscordLock)
				{
					SeenPrivateChannels.Clear();
					client.StopAsync().Wait();
					for (var i = 0; i >= ConnectionOperationTimeoutSeconds; i++)
					{
						if (client.ConnectionState != ConnectionState.Disconnecting)
							break;
						Thread.Sleep(1000);
					}
					client.LogoutAsync().Wait();
				}

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
		public void SendMessage(string msg, MessageType mt)
		{
			if (!Connected())
				return;
			lock (DiscordLock)
			{
				var tasks = new List<Task>();
				foreach (var I in client.Guilds)
					foreach (var J in I.TextChannels)
					{
						var cid = J.Id.ToString();
						var wdc = DiscordConfig.WatchdogChannels;
						bool SendToThisChannel = (mt.HasFlag(MessageType.AdminInfo) && DiscordConfig.AdminChannels.Contains(cid))
							|| (mt.HasFlag(MessageType.DeveloperInfo) && DiscordConfig.DevChannels.Contains(cid))
							|| (mt.HasFlag(MessageType.GameInfo) && DiscordConfig.GameChannels.Contains(cid))
							|| (mt.HasFlag(MessageType.WatchdogInfo) && DiscordConfig.WatchdogChannels.Contains(cid));

						if (SendToThisChannel)
							tasks.Add(J.SendMessageAsync(msg));
					}
				foreach (var I in tasks)
					I.Wait();
			}
		}

		/// <inheritdoc />
		public string SendMessageDirect(string message, string channelname)
		{
			if (!Connected())
				return "Disconnected.";
			try
			{
				lock (DiscordLock)
				{
					var tasks = new List<Task>();
					var channel = Convert.ToUInt64(channelname);
					if (SeenPrivateChannels.ContainsKey(channel))
						SeenPrivateChannels[channel].SendMessageAsync(message).Wait();
					else
						foreach (var I in client.Guilds)
							foreach (var J in I.TextChannels)
								if (J.Id == channel)
									J.SendMessageAsync(message).Wait();
					return null;
				}
			}
			catch (Exception e)
			{
				return e.ToString();
			}
		}

		/// <summary>
		/// Shutsdown and disposes <see cref="client"/>
		/// </summary>
		void DisconnectAndDispose()
		{
			try
			{
				client.StopAsync().Wait();
                for (var i = 0; i >= ConnectionOperationTimeoutSeconds; i++)
                {
                    if (client.ConnectionState != ConnectionState.Disconnecting)
                        break;
                    Thread.Sleep(1000);
                }
                client.LogoutAsync().Wait();
			}
			catch { }
			client.Dispose();
		}

		/// <inheritdoc />
		public string SetProviderInfo(ChatSetupInfo info)
		{
			try
			{
				lock (DiscordLock)
				{
					var odc = DiscordConfig;
					DiscordConfig = new DiscordSetupInfo(info);
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
		/// <summary>
		/// To detect redundant <see cref="Dispose()"/> calls
		/// </summary>
		private bool disposedValue = false;

		/// <summary>
		/// Implements the <see cref="IDisposable"/> pattern. Calls <see cref="DisconnectAndDispose"/>
		/// </summary>
		/// <param name="disposing"><see langword="true"/> if <see cref="Dispose()"/> was called manually, <see langword="false"/> if it was from the finalizer</param>
		void Dispose(bool disposing)
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
