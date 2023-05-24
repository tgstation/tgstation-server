using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Chat.Commands;
using Tgstation.Server.Host.Components.Chat.Providers;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Tests.Live
{
	sealed class DummyChatProvider : Provider
	{
		public override bool Connected => connected;

		public override string BotMention => $"Dummy{ChatBot.Provider}-I-{ChatBot.InstanceId}-N-{ChatBot.Name}";

		static int enableRandomDisconnections = 1;

		readonly Random random; // this RNG isn't perfect as calls into this class can theoretically happen in a random order due to async

		readonly IReadOnlyCollection<ICommand> commands;
		readonly ICryptographySuite cryptographySuite;
		readonly CancellationTokenSource randomMessageCts;
		readonly Task randomMessageTask;

		bool connectedOnce;
		bool connected;

		ulong channelIdAllocator;

		static ILoggerFactory CreateLoggerFactoryForLogger(ILogger logger, out Mock<ILoggerFactory> mockLoggerFactory)
		{
			mockLoggerFactory = new Mock<ILoggerFactory>();
			mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() =>
			{
				var temp = logger;
				logger = null;

				Assert.IsNotNull(temp);
				return temp;
			})
			.Verifiable();
			return mockLoggerFactory.Object;
		}

		static IAsyncDelayer CreateMockDelayer()
		{
			// at time of writing, this is used exclusively for the reconnection interval which works in minutes
			// shorten it to 3s
			var mock = new Mock<IAsyncDelayer>();
			mock.Setup(x => x.Delay(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns<TimeSpan, CancellationToken>((delay, cancellationToken) => Task.Delay(TimeSpan.FromSeconds(3), cancellationToken));
			return mock.Object;
		}
		public static async Task RandomDisconnections(bool enabled, CancellationToken cancellationToken)
		{
			if (Interlocked.Exchange(ref enableRandomDisconnections, enabled ? 1 : 0) != 0 && !enabled)
				await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
		}

		public DummyChatProvider(
			IJobManager jobManager,
			ILogger logger,
			ChatBot chatBot,
			ICryptographySuite cryptographySuite,
			IReadOnlyCollection<ICommand> commands,
			Random random)
			: base(jobManager, CreateMockDelayer(), new Logger<DummyChatProvider>(CreateLoggerFactoryForLogger(logger, out var mockLoggerFactory)), chatBot)
		{
			mockLoggerFactory.VerifyAll();
			this.cryptographySuite = cryptographySuite ?? throw new ArgumentNullException(nameof(cryptographySuite));
			this.commands = commands ?? throw new ArgumentNullException(nameof(commands));
			this.random = random ?? throw new ArgumentNullException(nameof(random));

			// this could be random but there's no point
			channelIdAllocator = 100000;
			logger.LogTrace("Base channel ID {baseChannelId}", channelIdAllocator);

			this.randomMessageCts = new CancellationTokenSource();
			this.randomMessageTask = RandomMessageLoop(this.randomMessageCts.Token);
		}

		public override async ValueTask DisposeAsync()
		{
			Logger.LogTrace("DisposeAsync Child");
			this.randomMessageCts.Cancel();
			this.randomMessageCts.Dispose();
			await this.randomMessageTask;
			await base.DisposeAsync();
		}

		public override Task SendMessage(Message replyTo, MessageContent message, ulong channelId, CancellationToken cancellationToken)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			Logger.LogTrace("SendMessage");

			Assert.AreNotEqual(0UL, channelId);
			Assert.IsTrue(channelId <= channelIdAllocator || channelId > (Int32.MaxValue / 2));

			cancellationToken.ThrowIfCancellationRequested();

			/* SendMessage is no-throw
			if (random.Next(0, 100) > 70)
				throw new Exception("Random SendMessage failure!"); */

			return Task.CompletedTask;
		}

		public override Task<Func<string, string, Task>> SendUpdateMessage(RevisionInformation revisionInformation, Version byondVersion, DateTimeOffset? estimatedCompletionTime, string gitHubOwner, string gitHubRepo, ulong channelId, bool localCommitPushed, CancellationToken cancellationToken)
		{
			if (revisionInformation == null)
				throw new ArgumentNullException(nameof(revisionInformation));
			if (byondVersion == null)
				throw new ArgumentNullException(nameof(byondVersion));
			if (gitHubOwner == null)
				throw new ArgumentNullException(nameof(gitHubOwner));
			if (gitHubRepo == null)
				throw new ArgumentNullException(nameof(gitHubRepo));

			Logger.LogTrace("SendUpdateMessage");

			Assert.AreNotEqual(0UL, channelId);
			Assert.IsTrue(channelId <= channelIdAllocator);

			cancellationToken.ThrowIfCancellationRequested();

			/* SendUpdateMessage is no-throw
			if (random.Next(0, 100) > 70)
				throw new Exception("Random SendUpdateMessage failure!"); */

			return Task.FromResult<Func<string, string, Task>>((_, _) =>
			{
				cancellationToken.ThrowIfCancellationRequested();

				/* SendUpdateMessage callbacks are no-throw
				if (random.Next(0, 100) > 70)
					throw new Exception("Random SendUpdateMessage failure!"); */

				return Task.CompletedTask;
			});
		}

		protected override Task Connect(CancellationToken cancellationToken)
		{
			Logger.LogTrace("Connect");
			cancellationToken.ThrowIfCancellationRequested();

			// 30% chance to fail AFTER initial connection
			if (connectedOnce && enableRandomDisconnections != 0 && random.Next(0, 100) > 70)
				throw new Exception("Random connection failure!");

			connected = true;
			connectedOnce = true;
			return Task.CompletedTask;
		}

		protected override Task DisconnectImpl(CancellationToken cancellationToken)
		{
			Logger.LogTrace("DisconnectImpl");
			cancellationToken.ThrowIfCancellationRequested();
			connected = false;

			if (random.Next(0, 100) > 70)
				throw new Exception("Random disconnection failure!");
			return Task.CompletedTask;
		}

		protected override Task<Dictionary<ChatChannel, IEnumerable<ChannelRepresentation>>> MapChannelsImpl(IEnumerable<ChatChannel> channels, CancellationToken cancellationToken)
		{
			channels = channels.ToList();
			Logger.LogTrace("MapChannels: [{channels}]", String.Join(", ", channels.Select(channel => channel.IrcChannel ?? channel.DiscordChannelId?.ToString() ?? throw new InvalidOperationException("BAD CHANNEL"))));

			cancellationToken.ThrowIfCancellationRequested();

			/* MapChannelsImpl is no-throw
			if (random.Next(0, 100) > 70)
				throw new Exception("Random MapChannelsImpl failure!"); */

			return Task.FromResult(
				new Dictionary<ChatChannel, IEnumerable<ChannelRepresentation>>(
					channels.Select(
						channel => new KeyValuePair<ChatChannel, IEnumerable<ChannelRepresentation>>(
							channel,
							new List<ChannelRepresentation>
							{
								new ChannelRepresentation
								{
									IsAdminChannel = channel.IsAdminChannel.Value,
									ConnectionName = $"Connection_{channelIdAllocator + 1}",
									EmbedsSupported = ChatBot.Provider.Value != Api.Models.ChatProvider.Irc,
									FriendlyName = $"(Friendly) Channel_ID_{channelIdAllocator + 1}",
									IsPrivateChannel = false,
									RealId = ++channelIdAllocator,
									Tag = channel.Tag,
								}
							}))));
		}

		async Task RandomMessageLoop(CancellationToken cancellationToken)
		{
			Logger.LogTrace("RandomMessageLoop");
			try
			{
				for (var i = 0UL; !cancellationToken.IsCancellationRequested; ++i)
				{
					// random intervals under 10s
					var delay = random.Next(0, 10000);
					await Task.Delay(delay, cancellationToken);

					if (!connected)
						continue;

					// %5 chance to disconnect randomly
					if (enableRandomDisconnections != 0 && random.Next(0, 100) > 95)
						connected = false;

					if (channelIdAllocator >= Int32.MaxValue / 2)
						Assert.Fail("Too many channels have been allocated!");

					var isPm = channelIdAllocator == 0 || random.Next(0, 100) > 20;
					var realId = (ulong)random.Next(1, (int)channelIdAllocator);

					if (isPm)
						realId += Int32.MaxValue / 2;

					var username = $"RandomUser{i}";
					var sender = new ChatUser
					{
						Channel = new ChannelRepresentation
						{
							RealId = realId,
							IsPrivateChannel = isPm,
							ConnectionName = isPm ? $"{username}_Connection" : $"Connection_{realId}",
							FriendlyName = isPm ? $"{username}_Channel" : $"(Friendly) Channel_ID_{realId}",
							EmbedsSupported = ChatBot.Provider.Value != Api.Models.ChatProvider.Irc,

							// isAdmin and Tag populated by manager
						},
						FriendlyName = username,
						RealId = i + 50000,
						Mention = $"@{username}",
					};

					var dice = random.Next(0, 100);
					string content;
					// 70% chance to be random chat
					if (dice < 70)
						content = cryptographySuite.GetSecureString();
					// 15% chance to be a !tgs
					else if (dice < 85)
						content = "!tgs";
					// 15% chance to be a strict mention
					else
						content = BotMention;

					// 30% chance to request help
					if (random.Next(0, 100) > 70)
						content = $"{content} help";

					dice = random.Next(0, 100);

					// 20% chance to whiff
					if (dice > 20)
						// 40% chance to attempt a built-in TGS command
						if (dice < 68)
							// equal chance for each
							content = $"{content} {commands.ElementAt(random.Next(0, commands.Count)).Name}";
						// 40% chance to attempt a custom chat command in long_running_test
						else
							// equal chance for each
							if (random.Next(0, 100) > 50)
								content = $"{content} embeds_test";
							else
								content = $"{content} response_overload_test";

					EnqueueMessage(new Message
					{
						Content = content,
						User = sender,
					});
				}

			}
			catch (OperationCanceledException)
			{
				Logger.LogTrace("RandomMessageLoop cancelled");
			}
		}
	}
}
