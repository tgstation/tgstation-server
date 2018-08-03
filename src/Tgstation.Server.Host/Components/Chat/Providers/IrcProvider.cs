using Meebey.SmartIrc4net;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Components.Chat.Providers
{
	/// <summary>
	/// <see cref="IProvider"/> for internet relay chat
	/// </summary>
	sealed class IrcProvider : Provider
	{
		const int TimeoutSeconds = 5;

		/// <inheritdoc />
		public override bool Connected => client.IsConnected;

		/// <inheritdoc />
		public override string BotMention => client.Nickname;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="IrcProvider"/>
		/// </summary>
		readonly ILogger<IrcProvider> logger;

		/// <summary>
		/// The <see cref="IrcFeatures"/> client
		/// </summary>
		readonly IrcFeatures client;

		/// <summary>
		/// Address of the server to connect to
		/// </summary>
		readonly string address;
		/// <summary>
		/// Port of the server to connect to
		/// </summary>
		readonly ushort port;
		/// <summary>
		/// IRC nickname
		/// </summary>
		readonly string nickname;
		/// <summary>
		/// Password which will used for authentication
		/// </summary>
		readonly string password;
		/// <summary>
		/// The <see cref="IrcPasswordType"/> of <see cref="password"/>
		/// </summary>
		readonly IrcPasswordType? passwordType;

		/// <summary>
		/// Map of <see cref="Channel.RealId"/>s to channel names
		/// </summary>
		readonly Dictionary<ulong, string> channelIdMap;

		/// <summary>
		/// Map of <see cref="Channel.RealId"/>s to query users
		/// </summary>
		readonly Dictionary<ulong, string> queryChannelIdMap;

		/// <summary>
		/// Id counter for <see cref="channelIdMap"/>
		/// </summary>
		ulong channelIdCounter;

		/// <summary>
		/// The <see cref="Task"/> used for <see cref="IrcConnection.Listen(bool)"/>
		/// </summary>
		Task listenTask;

		/// <summary>
		/// If we are disconnecting
		/// </summary>
		bool disconnecting;

		/// <summary>
		/// Construct an <see cref="IrcProvider"/>
		/// </summary>
		/// <param name="logger">The value of logger</param>
		/// <param name="application">The <see cref="IApplication"/> to get the <see cref="IApplication.VersionString"/> from</param>
		/// <param name="address">The value of <see cref="address"/></param>
		/// <param name="port">The value of <see cref="port"/></param>
		/// <param name="nickname">The value of <see cref="nickname"/></param>
		/// <param name="password">The value of <see cref="password"/></param>
		/// <param name="passwordType">The value of <see cref="passwordType"/></param>
		/// <param name="useSsl">If <see cref="IrcConnection.UseSsl"/> should be used</param>
		public IrcProvider(ILogger<IrcProvider> logger, IApplication application, string address, ushort port, string nickname, string password, IrcPasswordType? passwordType, bool useSsl)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			if (application == null)
				throw new ArgumentNullException(nameof(application));

			this.address = address ?? throw new ArgumentNullException(nameof(address));
			this.port = port;
			this.nickname = nickname ?? throw new ArgumentNullException(nameof(nickname));

			if (passwordType.HasValue && password == null)
				throw new ArgumentNullException(nameof(password));

			if(password != null && !passwordType.HasValue)
				throw new ArgumentNullException(nameof(passwordType));

			this.password = password;
			this.passwordType = passwordType;

			client = new IrcFeatures
			{
				SupportNonRfc = true,
				CtcpUserInfo = "You are going to play. And I am going to watch. And everything will be just fine...",
				AutoRejoin = true,
				AutoRejoinOnKick = true,
				AutoRelogin = true,
				AutoRetry = true,
				AutoRetryLimit = TimeoutSeconds,
				AutoRetryDelay = TimeoutSeconds,
				ActiveChannelSyncing = true,
				AutoNickHandling = true,
				CtcpVersion = application.VersionString,
				UseSsl = useSsl
			};
			if (useSsl)
				client.ValidateServerCertificate = true;    //dunno if it defaults to that or what

			client.OnChannelMessage += Client_OnChannelMessage;
			client.OnQueryMessage += Client_OnQueryMessage;

			channelIdMap = new Dictionary<ulong, string>();
			queryChannelIdMap = new Dictionary<ulong, string>();
			channelIdCounter = 1;
			disconnecting = false;
		}

		/// <inheritdoc />
		public override void Dispose()
		{
			if (Connected)
			{
				disconnecting = true;
				client.Disconnect();    //just closes the socket
			}
		}

		/// <summary>
		/// Handle an IRC message
		/// </summary>
		/// <param name="e">The <see cref="IrcEventArgs"/></param>
		/// <param name="isPrivate">If this is a query message</param>
		void HandleMessage(IrcEventArgs e, bool isPrivate)
		{
			if (e.Data.Nick.ToUpperInvariant() == client.Nickname.ToUpperInvariant())
				return;

			var username = e.Data.Nick;
			var channelName = isPrivate ? username : e.Data.Channel;
			ulong channelId = 0;
			lock (this)
			{
				var dicToCheck = isPrivate ? queryChannelIdMap : channelIdMap;
				if (!dicToCheck.Any(x =>
				{
					if (x.Value != channelName)
						return false;
					channelId = x.Key;
					return true;
				}))
				{
					channelId = ++channelIdCounter;
					dicToCheck.Add(channelId, channelName);
					if (isPrivate)
						channelIdMap.Add(channelId, null);
				}
			}

			var message = new Message
			{
				Content = e.Data.Message,
				User = new User
				{
					Channel = new Channel
					{
						IsAdmin = false,
						ConnectionName = address,
						FriendlyName = isPrivate ? String.Format(CultureInfo.InvariantCulture, "PM: {0}", channelName) : channelName,
						RealId = channelId,
						IsPrivate = isPrivate
					},
					FriendlyName = username,
					RealId = channelId,
					Mention = username
				}
			};

			EnqueueMessage(message);
		}

		/// <summary>
		/// When a query message is received in IRC
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The <see cref="IrcEventArgs"/></param>
		void Client_OnQueryMessage(object sender, IrcEventArgs e) => HandleMessage(e, true);

		/// <summary>
		/// When a channel message is received in IRC
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The <see cref="IrcEventArgs"/></param>
		void Client_OnChannelMessage(object sender, IrcEventArgs e) => HandleMessage(e, false);

		/// <inheritdoc />
		public override Task<bool> Connect(CancellationToken cancellationToken) => Task.Factory.StartNew(() =>
		{
			disconnecting = false;
			lock (this)
				try
				{
					client.Connect(address, port);

					cancellationToken.ThrowIfCancellationRequested();

					if (passwordType == IrcPasswordType.Server)
						client.Login(nickname, nickname, 0, nickname, password);
					else
					{
						if (passwordType == IrcPasswordType.Sasl)
						{
							client.WriteLine("CAP REQ :sasl", Priority.Critical);  //needs to be put in the buffer before anything else
							cancellationToken.ThrowIfCancellationRequested();
						}
						client.Login(nickname, nickname, 0, nickname);
					}

					if (passwordType == IrcPasswordType.NickServ)
					{
						cancellationToken.ThrowIfCancellationRequested();
						client.SendMessage(SendType.Message, "NickServ", String.Format(CultureInfo.InvariantCulture, "IDENTIFY {0}", password));
					}
					else if (passwordType == IrcPasswordType.Sasl)
					{
						//wait for the sasl ack or timeout
						var recievedAck = false;
						var recievedPlus = false;
						client.OnReadLine += (sender, e) =>
						{
							if (e.Line.Contains("ACK :sasl"))
								recievedAck = true;
							else if (e.Line.Contains("AUTHENTICATE +"))
								recievedPlus = true;
						};

						var startTime = DateTimeOffset.Now;
						var endTime = DateTimeOffset.Now.AddSeconds(TimeoutSeconds);
						cancellationToken.ThrowIfCancellationRequested();
						for (; !recievedAck && DateTimeOffset.Now <= endTime; Task.Delay(10, cancellationToken).GetAwaiter().GetResult())
							client.Listen(false);

						client.WriteLine("AUTHENTICATE PLAIN", Priority.Critical);
						cancellationToken.ThrowIfCancellationRequested();

						for (; !recievedPlus && DateTimeOffset.Now <= endTime; Task.Delay(10, cancellationToken).GetAwaiter().GetResult())
							client.Listen(false);

						//Stolen! https://github.com/znc/znc/blob/1e697580155d5a38f8b5a377f3b1d94aaa979539/modules/sasl.cpp#L196
						var authString = String.Format(CultureInfo.InvariantCulture, "{0}{1}{0}{1}{2}", nickname, '\0', password);
						var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(authString));
						var authLine = String.Format(CultureInfo.InvariantCulture, "AUTHENTICATE {0}", b64);
						var chars = authLine.ToCharArray();
						client.WriteLine(authLine, Priority.Critical);

						cancellationToken.ThrowIfCancellationRequested();
						client.WriteLine("CAP END", Priority.Critical);
					}

					client.Listen(false);
					if (client.Nickname != nickname)
						//run this now cause it has to go up and down the pipe for us to get a proper check
						client.GetIrcUser(nickname);

					listenTask = Task.Factory.StartNew(() =>
					{
						while (!disconnecting && client.IsConnected)
						{
							client.ListenOnce(true);
							if (disconnecting || !client.IsConnected)
								break;
							client.Listen(false);
							//ensure we have the correct nick
							if (client.Nickname != nickname && client.GetIrcUser(nickname) == null)
								client.RfcNick(nickname);
						}
					}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
				}
				catch (Exception e)
				{
					logger.LogWarning("Unable to connect to IRC: {0}", e);
				}
			return true;
		}, cancellationToken,TaskCreationOptions.LongRunning, TaskScheduler.Current);

		/// <inheritdoc />
		public override async Task Disconnect(CancellationToken cancellationToken)
		{
			if (!Connected)
				return;
			try
			{
				await Task.Factory.StartNew(() =>
				{
					try
					{
						client.RfcQuit("Mr. Stark, I don't feel so good...", Priority.Critical);   //priocritical otherwise it wont go through
					}
					catch (Exception e)
					{
						logger.LogWarning("Error quitting IRC: {0}", e);
					}
				}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);
				Dispose();
				await listenTask.ConfigureAwait(false);
			}
			catch (Exception e)
			{
				logger.LogWarning("Error disconnecting from IRC! Exception: {0}", e);
			}
		}

		/// <inheritdoc />
		public override Task<IReadOnlyList<Channel>> MapChannels(IEnumerable<ChatChannel> channels, CancellationToken cancellationToken) => Task.Factory.StartNew(() =>
		{
			if (channels.Any(x => x.IrcChannel == null))
				throw new InvalidOperationException("ChatChannel missing IrcChannel!");
			lock (this)
			{
				var hs = new HashSet<string>(); //for unique inserts
				foreach (var I in channels)
					hs.Add(I.IrcChannel);
				var toPart = new List<string>();
				foreach (var I in client.JoinedChannels)
					if (!hs.Remove(I))
						toPart.Add(I);

				foreach (var I in toPart)
					client.RfcPart(I);
				foreach (var I in hs)
					client.RfcJoin(I);

				return (IReadOnlyList<Channel>)channels.Select(x => {
					var id = channelIdCounter;
					if (!channelIdMap.Any(y =>
					{
						if (y.Value != x.IrcChannel)
							return false;
						id = y.Key;
						return true;
					}))
					{
						channelIdMap.Add(id, x.IrcChannel);
						++channelIdCounter;
					}
					return new Channel
					{
						RealId = id,
						IsAdmin = x.IsAdminChannel == true,
						ConnectionName = address,
						FriendlyName = channelIdMap[id],
						IsPrivate = false
	 				};
				}).ToList();
			}
		}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
		
		/// <inheritdoc />
		public override Task SendMessage(ulong channelId, string message, CancellationToken cancellationToken) => Task.Factory.StartNew(() =>
		{
			var channelName = channelIdMap[channelId];
			SendType sendType;
			if (channelName == null)
			{
				channelName = queryChannelIdMap[channelId];
				sendType = SendType.Notice;
			}
			else
				sendType = SendType.Message;
			try
			{
				client.SendMessage(sendType, channelName, message);
			}
			catch(Exception e)
			{
				logger.LogWarning("Unable to send to channel: {0}", e);
			}
		}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
	}
}
