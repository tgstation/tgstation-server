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
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Components.Chat.Providers
{
	/// <summary>
	/// <see cref="IProvider"/> for internet relay chat
	/// </summary>
	sealed class IrcProvider : Provider
	{
		/// <summary>
		/// Number of seconds used for several IRC related timeouts.
		/// </summary>
		const int TimeoutSeconds = 5;

		/// <inheritdoc />
		public override bool Connected => client.IsConnected;

		/// <inheritdoc />
		public override string BotMention => client.Nickname;

		/// <summary>
		/// The <see cref="IAsyncDelayer"/> for the <see cref="IrcProvider"/>
		/// </summary>
		readonly IAsyncDelayer asyncDelayer;

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
		/// Map of <see cref="ChannelRepresentation.RealId"/>s to channel names
		/// </summary>
		readonly Dictionary<ulong, string> channelIdMap;

		/// <summary>
		/// Map of <see cref="ChannelRepresentation.RealId"/>s to query users
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
		/// <param name="jobManager">The <see cref="IJobManager"/> for the provider.</param>
		/// <param name="assemblyInformationProvider">The <see cref="IAssemblyInformationProvider"/> to get the <see cref="IAssemblyInformationProvider.VersionString"/> from</param>
		/// <param name="asyncDelayer">The value of <see cref="asyncDelayer"/></param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="Provider"/>.</param>
		/// <param name="chatBot">The <see cref="Models.ChatBot"/> for the <see cref="Provider"/>.</param>
		public IrcProvider(
			IJobManager jobManager,
			IAssemblyInformationProvider assemblyInformationProvider,
			IAsyncDelayer asyncDelayer,
			ILogger<IrcProvider> logger,
			Models.ChatBot chatBot)
			: base(jobManager, logger, chatBot)
		{
			if (assemblyInformationProvider == null)
				throw new ArgumentNullException(nameof(assemblyInformationProvider));
			this.asyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));

			var builder = chatBot.CreateConnectionStringBuilder();
			if (builder == null || !builder.Valid || !(builder is IrcConnectionStringBuilder ircBuilder))
				throw new InvalidOperationException("Invalid ChatConnectionStringBuilder!");

			address = ircBuilder.Address;
			port = ircBuilder.Port.Value;
			nickname = ircBuilder.Nickname;

			password = ircBuilder.Password;
			passwordType = ircBuilder.PasswordType.Value;

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
				CtcpVersion = assemblyInformationProvider.VersionString,
				UseSsl = ircBuilder.UseSsl.Value
			};
			if (ircBuilder.UseSsl.Value)
				client.ValidateServerCertificate = true; // dunno if it defaults to that or what

			client.OnChannelMessage += Client_OnChannelMessage;
			client.OnQueryMessage += Client_OnQueryMessage;

			channelIdMap = new Dictionary<ulong, string>();
			queryChannelIdMap = new Dictionary<ulong, string>();
			channelIdCounter = 1;
		}

		/// <inheritdoc />
		public override async ValueTask DisposeAsync()
		{
			await base.DisposeAsync().ConfigureAwait(false);

			// DCT: None available
			await HardDisconnect(default).ConfigureAwait(false);
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

			ulong MapAndGetChannelId(Dictionary<ulong, string> dicToCheck)
			{
				ulong? resultId = null;
				if (!dicToCheck.Any(x =>
				{
					if (x.Value != channelName)
						return false;
					resultId = x.Key;
					return true;
				}))
				{
					resultId = channelIdCounter++;
					dicToCheck.Add(resultId.Value, channelName);
					if (dicToCheck == queryChannelIdMap)
						channelIdMap.Add(resultId.Value, null);
				}

				return resultId.Value;
			}

			ulong userId, channelId;
			lock (client)
			{
				userId = MapAndGetChannelId(queryChannelIdMap);
				channelId = isPrivate ? userId : MapAndGetChannelId(channelIdMap);
			}

			var message = new Message
			{
				Content = e.Data.Message,
				User = new ChatUser
				{
					Channel = new ChannelRepresentation
					{
						ConnectionName = address,
						FriendlyName = isPrivate ? String.Format(CultureInfo.InvariantCulture, "PM: {0}", channelName) : channelName,
						RealId = channelId,
						IsPrivateChannel = isPrivate

						// isAdmin and Tag populated by manager
					},
					FriendlyName = username,
					RealId = userId,
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
		protected override async Task Connect(CancellationToken cancellationToken)
		{
			disconnecting = false;
			cancellationToken.ThrowIfCancellationRequested();
			try
			{
				client.Connect(address, port);

				cancellationToken.ThrowIfCancellationRequested();

				Logger.LogTrace("Authenticating ({0})...", passwordType);
				switch (passwordType)
				{
					case IrcPasswordType.Server:
						client.Login(nickname, nickname, 0, nickname, password);
						break;
					case IrcPasswordType.NickServ:
						client.Login(nickname, nickname, 0, nickname);
						cancellationToken.ThrowIfCancellationRequested();
						client.SendMessage(SendType.Message, "NickServ", String.Format(CultureInfo.InvariantCulture, "IDENTIFY {0}", password));
						break;
					case IrcPasswordType.Sasl:
						await SaslAuthenticate(cancellationToken).ConfigureAwait(false);
						break;
					case null:
						break;
					default:
						throw new InvalidOperationException($"Invalid IrcPasswordType: {passwordType.Value}");
				}

				cancellationToken.ThrowIfCancellationRequested();
				Logger.LogTrace("Processing initial messages...");
				await NonBlockingListen(cancellationToken).ConfigureAwait(false);

				var nickCheckCompleteTcs = new TaskCompletionSource<object>();
				using (cancellationToken.Register(() => nickCheckCompleteTcs.TrySetCanceled()))
				{
					listenTask = Task.Factory.StartNew(
					async () =>
					{
						Logger.LogTrace("Entering nick check loop");
						while (!disconnecting && client.IsConnected && client.Nickname != nickname)
						{
							client.ListenOnce(true);
							if (disconnecting || !client.IsConnected)
								break;
							await NonBlockingListen(cancellationToken).ConfigureAwait(false);

							// ensure we have the correct nick
							if (client.GetIrcUser(nickname) == null)
								client.RfcNick(nickname);
						}

						nickCheckCompleteTcs.TrySetResult(null);

						Logger.LogTrace("Starting blocking listen...");
						try
						{
							client.Listen();
						}
						catch (Exception ex)
						{
							Logger.LogWarning(ex, "IRC Main Listen Exception!");
						}

						Logger.LogTrace("Exiting listening task...");
					},
					cancellationToken,
					DefaultIOManager.BlockingTaskCreationOptions,
					TaskScheduler.Current);

					await nickCheckCompleteTcs.Task.ConfigureAwait(false);
				}

				Logger.LogTrace("Connection established!");
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception e)
			{
				throw new JobException(ErrorCode.ChatCannotConnectProvider, e);
			}
		}

		/// <summary>
		/// Perform a non-blocking <see cref="IrcConnection.Listen(bool)"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task NonBlockingListen(CancellationToken cancellationToken) => Task.Factory.StartNew(
			() =>
			{
				try
				{
					client.Listen(false);
				}
				catch (Exception ex)
				{
					Logger.LogWarning(ex, "IRC Non-Blocking Listen Exception!");
				}
			},
			cancellationToken,
			TaskCreationOptions.None,
			TaskScheduler.Current)
			.WithToken(cancellationToken);

		/// <summary>
		/// Run SASL authentication on <see cref="client"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task SaslAuthenticate(CancellationToken cancellationToken)
		{
			client.WriteLine("CAP REQ :sasl", Priority.Critical); // needs to be put in the buffer before anything else
			cancellationToken.ThrowIfCancellationRequested();

			Logger.LogTrace("Logging in...");
			client.Login(nickname, nickname, 0, nickname);
			cancellationToken.ThrowIfCancellationRequested();

			// wait for the sasl ack or timeout
			var recievedAck = false;
			var recievedPlus = false;

			void AuthenticationDelegate(object sender, ReadLineEventArgs e)
			{
				if (e.Line.Contains("ACK :sasl", StringComparison.Ordinal))
					recievedAck = true;
				else if (e.Line.Contains("AUTHENTICATE +", StringComparison.Ordinal))
					recievedPlus = true;
			}

			Logger.LogTrace("Performing handshake...");
			client.OnReadLine += AuthenticationDelegate;
			try
			{
				using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
				timeoutCts.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));
				var timeoutToken = timeoutCts.Token;

				var listenTimeSpan = TimeSpan.FromMilliseconds(10);
				for (; !recievedAck;
					await asyncDelayer.Delay(listenTimeSpan, timeoutToken).ConfigureAwait(false))
					await NonBlockingListen(cancellationToken).ConfigureAwait(false);

				client.WriteLine("AUTHENTICATE PLAIN", Priority.Critical);
				timeoutToken.ThrowIfCancellationRequested();

				for (; !recievedPlus;
					await asyncDelayer.Delay(listenTimeSpan, timeoutToken).ConfigureAwait(false))
					await NonBlockingListen(cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				client.OnReadLine -= AuthenticationDelegate;
			}

			cancellationToken.ThrowIfCancellationRequested();

			// Stolen! https://github.com/znc/znc/blob/1e697580155d5a38f8b5a377f3b1d94aaa979539/modules/sasl.cpp#L196
			Logger.LogTrace("Sending credentials...");
			var authString = String.Format(
				CultureInfo.InvariantCulture,
				"{0}{1}{0}{1}{2}",
				nickname,
				'\0',
				password);
			var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(authString));
			var authLine = $"AUTHENTICATE {b64}";
			client.WriteLine(authLine, Priority.Critical);
			cancellationToken.ThrowIfCancellationRequested();

			Logger.LogTrace("Finishing authentication...");
			client.WriteLine("CAP END", Priority.Critical);
		}

		/// <inheritdoc />
		protected override async Task DisconnectImpl(CancellationToken cancellationToken)
		{
			try
			{
				await Task.Factory.StartNew(
					() =>
					{
						try
						{
							client.RfcQuit("Mr. Stark, I don't feel so good...", Priority.Critical); // priocritical otherwise it wont go through
						}
						catch (Exception e)
						{
							Logger.LogWarning(e, "Error quitting IRC!");
						}
					},
					cancellationToken,
					DefaultIOManager.BlockingTaskCreationOptions,
					TaskScheduler.Current)
					.ConfigureAwait(false);
				await HardDisconnect(cancellationToken).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception e)
			{
				Logger.LogWarning(e, "Error disconnecting from IRC!");
			}
		}

		async Task HardDisconnect(CancellationToken cancellationToken)
		{
			if (!Connected)
			{
				Logger.LogTrace("Not hard disconnecting, already offline");
				return;
			}

			Logger.LogTrace("Hard disconnect");

			disconnecting = true;

			// This call blocks permanently randomly sometimes
			// Frankly I don't give a shit
			var disconnectTask = Task.Factory.StartNew(
				() =>
				{
					try
					{
						client.Disconnect();
					}
					catch (Exception e)
					{
						Logger.LogWarning(e, "Error disconnecting IRC!");
					}
				},
				cancellationToken,
				DefaultIOManager.BlockingTaskCreationOptions,
				TaskScheduler.Current);

			await Task.WhenAny(
				Task.WhenAll(
					disconnectTask,
					listenTask ?? Task.CompletedTask),
				asyncDelayer.Delay(TimeSpan.FromSeconds(TimeoutSeconds), cancellationToken))
				.ConfigureAwait(false);
		}

		/// <inheritdoc />
		public override Task<IReadOnlyCollection<ChannelRepresentation>> MapChannels(
			IEnumerable<ChatChannel> channels,
			CancellationToken cancellationToken)
			=> Task.Factory.StartNew(() =>
			{
				if (channels.Any(x => x.IrcChannel == null))
					throw new InvalidOperationException("ChatChannel missing IrcChannel!");
				lock (client)
				{
					var channelsWithKeys = new Dictionary<string, string>();
					var hs = new HashSet<string>(); // for unique inserts
					foreach (var channel in channels)
					{
						var name = channel.GetIrcChannelName();
						var key = channel.GetIrcChannelKey();
						if (hs.Add(name) && key != null)
							channelsWithKeys.Add(name, key);
					}

					var toPart = new List<string>();
					foreach (var activeChannel in client.JoinedChannels)
						if (!hs.Remove(activeChannel))
							toPart.Add(activeChannel);

					foreach (var channelToLeave in toPart)
						client.RfcPart(channelToLeave, "Pretty nice abscond!");
					foreach (var channelToJoin in hs)
						if (channelsWithKeys.TryGetValue(channelToJoin, out var key))
							client.RfcJoin(channelToJoin, key);
						else
							client.RfcJoin(channelToJoin);

					return (IReadOnlyCollection<ChannelRepresentation>)channels
						.Select(x =>
						{
							var channelName = x.GetIrcChannelName();
							ulong? id = null;
							if (!channelIdMap.Any(y =>
							{
								if (y.Value != channelName)
									return false;
								id = y.Key;
								return true;
							}))
							{
								id = channelIdCounter++;
								channelIdMap.Add(id.Value, channelName);
							}

							return new ChannelRepresentation
							{
								RealId = id.Value,
								IsAdminChannel = x.IsAdminChannel == true,
								ConnectionName = address,
								FriendlyName = channelIdMap[id.Value],
								IsPrivateChannel = false,
								Tag = x.Tag
							};
						})
						.ToList();
				}
			}, cancellationToken, DefaultIOManager.BlockingTaskCreationOptions, TaskScheduler.Current);

		/// <inheritdoc />
		public override Task SendMessage(ulong channelId, string message, CancellationToken cancellationToken) => Task.Factory.StartNew(() =>
		{
			// IRC doesn't allow newlines
			message = String.Concat(
				message
					.Where(x => x != '\r')
					.Select(x => x == '\n' ? '|' : x));

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
			catch (Exception e)
			{
				Logger.LogWarning(e, "Unable to send to channel {0}!", channelName);
			}
		}, cancellationToken, DefaultIOManager.BlockingTaskCreationOptions, TaskScheduler.Current);

		/// <inheritdoc />
		public override async Task<Func<string, string, Task>> SendUpdateMessage(
			Models.RevisionInformation revisionInformation,
			Version byondVersion,
			DateTimeOffset? estimatedCompletionTime,
			string gitHubOwner,
			string gitHubRepo,
			ulong channelId,
			bool localCommitPushed,
			CancellationToken cancellationToken)
		{
			var commitInsert = revisionInformation.CommitSha.Substring(0, 7);
			string remoteCommitInsert;
			if (revisionInformation.CommitSha == revisionInformation.OriginCommitSha)
			{
				commitInsert = String.Format(CultureInfo.InvariantCulture, localCommitPushed ? "^{0}" : "{0}", commitInsert);
				remoteCommitInsert = String.Empty;
			}
			else
				remoteCommitInsert = String.Format(CultureInfo.InvariantCulture, ". Remote commit: ^{0}", revisionInformation.OriginCommitSha.Substring(0, 7));

			var testmergeInsert = (revisionInformation.ActiveTestMerges?.Count ?? 0) == 0 ? String.Empty : String.Format(CultureInfo.InvariantCulture, " (Test Merges: {0})",
				String.Join(", ", revisionInformation.ActiveTestMerges.Select(x => x.TestMerge).Select(x =>
				{
					var result = String.Format(CultureInfo.InvariantCulture, "#{0} at {1}", x.Number, x.TargetCommitSha.Substring(0, 7));
					if (x.Comment != null)
						result += String.Format(CultureInfo.InvariantCulture, " ({0})", x.Comment);
					return result;
				})));

			await SendMessage(
				channelId,
				String.Format(
					CultureInfo.InvariantCulture,
					"DM: Deploying revision: {0}{1}{2} BYOND Version: {3}{4}",
					commitInsert,
					testmergeInsert,
					remoteCommitInsert,
					byondVersion.Build > 0
						? byondVersion.ToString()
						: $"{byondVersion.Major}.{byondVersion.Minor}",
					estimatedCompletionTime.HasValue
						? $" ETA: {estimatedCompletionTime - DateTimeOffset.Now}"
						: String.Empty),
				cancellationToken).ConfigureAwait(false);

			return (errorMessage, dreamMakerOutput) => SendMessage(
				channelId,
				$"DM: Deployment {(errorMessage == null ? "complete" : "failed")}!",
				cancellationToken);
		}
	}
}
