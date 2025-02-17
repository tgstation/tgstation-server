using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Newtonsoft.Json;

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
		readonly ConcurrentDictionary<ulong, ChannelRepresentation> knownChannels;

		bool connectedOnce;
		bool connected;

		ulong channelIdAllocator;

		public static Task MessageGuard = Task.CompletedTask;

		static IAsyncDelayer CreateMockDelayer()
		{
			// at time of writing, this is used exclusively for the reconnection interval which works in minutes
			// shorten it to 3s
			var mock = new Mock<IAsyncDelayer>();
			mock.Setup(x => x.Delay(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns<TimeSpan, CancellationToken>(async (delay, cancellationToken) => await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken));
			return mock.Object;
		}

		public static void RandomDisconnections(bool enabled)
		{
			// we just don't do random disconnections when live testing these days, too many potential issue vectors like thread exhaustion on actions runners
			enableRandomDisconnections = enabled ? 1 : 0;
		}

		public DummyChatProvider(
			IJobManager jobManager,
			ILogger logger,
			ChatBot chatBot,
			ICryptographySuite cryptographySuite,
			IReadOnlyCollection<ICommand> commands,
			Random random)
			: base(jobManager, CreateMockDelayer(), new Logger<DummyChatProvider>(TestingUtils.CreateLoggerFactoryForLogger(logger, out var mockLoggerFactory)), chatBot)
		{
			mockLoggerFactory.VerifyAll();
			this.cryptographySuite = cryptographySuite ?? throw new ArgumentNullException(nameof(cryptographySuite));
			this.commands = commands ?? throw new ArgumentNullException(nameof(commands));
			this.random = random ?? throw new ArgumentNullException(nameof(random));

			knownChannels = new ();
			randomMessageCts = new CancellationTokenSource();
			randomMessageTask = RandomMessageLoop(this.randomMessageCts.Token);
		}

		public override async ValueTask DisposeAsync()
		{
			this.randomMessageCts.Cancel();
			this.randomMessageCts.Dispose();
			await this.randomMessageTask;
			await base.DisposeAsync();
		}

		public override ValueTask SendMessage(Message replyTo, MessageContent message, ulong channelId, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(message);

			Assert.IsTrue(knownChannels.ContainsKey(channelId));

			cancellationToken.ThrowIfCancellationRequested();

			/* SendMessage is no-throw
			if (random.Next(0, 100) > 70)
				throw new Exception("Random SendMessage failure!"); */

			return ValueTask.CompletedTask;
		}

		public override ValueTask<Func<string, string, ValueTask<Func<bool, ValueTask>>>> SendUpdateMessage(
			RevisionInformation revisionInformation,
			RevisionInformation previousRevisionInformation,
			Api.Models.EngineVersion engineVersion,
			DateTimeOffset? estimatedCompletionTime,
			string gitHubOwner,
			string gitHubRepo,
			ulong channelId,
			bool localCommitPushed,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(revisionInformation);
			ArgumentNullException.ThrowIfNull(engineVersion);
			ArgumentNullException.ThrowIfNull(gitHubOwner);
			ArgumentNullException.ThrowIfNull(gitHubRepo);

			Assert.IsTrue(knownChannels.ContainsKey(channelId));

			cancellationToken.ThrowIfCancellationRequested();

			/* SendUpdateMessage is no-throw
			if (random.Next(0, 100) > 70)
				throw new Exception("Random SendUpdateMessage failure!"); */

			return ValueTask.FromResult<Func<string, string, ValueTask<Func<bool, ValueTask>>>>((_, _) =>
			{
				cancellationToken.ThrowIfCancellationRequested();

				/* SendUpdateMessage callbacks are no-throw
				if (random.Next(0, 100) > 70)
					throw new Exception("Random SendUpdateMessage failure!"); */

				return ValueTask.FromResult<Func<bool, ValueTask>>(_ => ValueTask.CompletedTask);
			});
		}

		protected override ValueTask Connect(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			// 30% chance to fail AFTER initial connection
			if (connectedOnce && enableRandomDisconnections != 0 && random.Next(0, 100) > 70)
				throw new Exception("Random connection failure!");

			connected = true;
			connectedOnce = true;
			return ValueTask.CompletedTask;
		}

		protected override ValueTask DisconnectImpl(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			connected = false;

			if (random.Next(0, 100) > 70)
				throw new Exception("Random disconnection failure!");
			return ValueTask.CompletedTask;
		}

		protected override ValueTask<Dictionary<ChatChannel, IEnumerable<ChannelRepresentation>>> MapChannelsImpl(IEnumerable<ChatChannel> channels, CancellationToken cancellationToken)
		{
			channels = channels.ToList();

			cancellationToken.ThrowIfCancellationRequested();

			/* MapChannelsImpl is no-throw
			if (random.Next(0, 100) > 70)
				throw new Exception("Random MapChannelsImpl failure!"); */

			return ValueTask.FromResult(
				new Dictionary<ChatChannel, IEnumerable<ChannelRepresentation>>(
					channels.Select(
						channel => new KeyValuePair<ChatChannel, IEnumerable<ChannelRepresentation>>(
							channel,
							new List<ChannelRepresentation>
							{
								CreateChannel(channel)
							}))));
		}

		private ChannelRepresentation CreateChannel(ChatChannel channel)
		{
			ulong channelId;
			if (channel.DiscordChannelId.HasValue)
				channelId = channel.DiscordChannelId.Value;
			else
				channelId = (ulong)channel.IrcChannel.GetHashCode();

			var entry = new ChannelRepresentation(
				$"Connection_{channelId}",
				$"(Friendly) Channel_ID_{channelId}",
				channelId)
			{
				IsAdminChannel = channel.IsAdminChannel.Value,
				EmbedsSupported = ChatBot.Provider.Value != Api.Models.ChatProvider.Irc,
				IsPrivateChannel = false,
				Tag = channel.Tag,
			};

			knownChannels[channelId] = entry;

			return CloneChannel(entry);
		}

		static ChannelRepresentation CloneChannel(ChannelRepresentation channel)
			=> JsonConvert.DeserializeObject<ChannelRepresentation>(
				JsonConvert.SerializeObject(
					channel));

		async Task RandomMessageLoop(CancellationToken cancellationToken)
		{
			try
			{
				for (var i = 0UL; !cancellationToken.IsCancellationRequested; ++i)
				{
					// random intervals under 10s
					var delay = random.Next(0, 10000);
					await Task.Delay(delay, cancellationToken);

					await MessageGuard;

					// %5 chance to disconnect randomly
					if (enableRandomDisconnections != 0 && random.Next(0, 100) > 95)
						connected = false;

					if (!connected)
						continue;

					var isPm = channelIdAllocator == 0 || random.Next(0, 100) > 80;

					ChannelRepresentation channel;
					var username = $"RandomUser{i}";
					if (isPm)
						// 50% chance to be a new user
						if (random.Next(0, 100) > 50)
						{
							var enumerator = knownChannels
								.Where(x => x.Value.IsPrivateChannel)
								.ToList();
							if (enumerator.Count == 0)
								continue;

							var index = random.Next(0, enumerator.Count);
							channel = enumerator[index].Value;
							username = channel.ConnectionName[..^11];
						}
						else
						{
							ulong channelId;
							do
							{
								channelId = ++channelIdAllocator;
							}
							while (knownChannels.ContainsKey(channelId));

							channel = new ChannelRepresentation($"{username}_Connection", $"{username}_Channel", channelId)
							{
								IsPrivateChannel = true,
								EmbedsSupported = ChatBot.Provider.Value != Api.Models.ChatProvider.Irc,

								// isAdmin and Tag populated by manager
							};

							knownChannels[channelId] = channel;
						}
					else
					{
						var enumerator = knownChannels
							.Where(x => !x.Value.IsPrivateChannel)
							.ToList();
						if (enumerator.Count == 0)
							continue;

						var index = random.Next(0, enumerator.Count);
						channel = enumerator[index].Value;
					}

					var sender = new ChatUser(
						CloneChannel(channel),
						username,
						$"@{username}",
						i + 50000);

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
							content = $"{content} embeds_test"; // NEVER send the response_overload_test, it causes so much havoc in CI and we test it manually

					EnqueueMessage(new Message(sender, content));
				}
			}
			catch (OperationCanceledException)
			{
			}
		}
	}
}
