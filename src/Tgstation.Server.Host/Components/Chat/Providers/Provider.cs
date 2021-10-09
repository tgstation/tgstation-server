using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components.Chat.Providers
{
	/// <inheritdoc />
	abstract class Provider : IProvider
	{
		/// <inheritdoc />
		public Task InitialConnectionJob => initialConnectionTcs.Task;

		/// <summary>
		/// The <see cref="ChatBot"/> the <see cref="Provider"/> is for.
		/// </summary>
		protected ChatBot ChatBot { get; }

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="Provider"/>.
		/// </summary>
		protected ILogger<Provider> Logger { get; }

		/// <summary>
		/// The <see cref="IJobManager"/> for the <see cref="Provider"/>.
		/// </summary>
		readonly IJobManager jobManager;

		/// <summary>
		/// <see cref="Queue{T}"/> of received <see cref="Message"/>s.
		/// </summary>
		readonly Queue<Message> messageQueue;

		/// <summary>
		/// The backing <see cref="TaskCompletionSource{TResult}"/> for <see cref="InitialConnectionJob"/>.
		/// </summary>
		readonly TaskCompletionSource<object> initialConnectionTcs;

		/// <summary>
		/// Used for synchronizing access to <see cref="reconnectCts"/> and <see cref="reconnectTask"/>.
		/// </summary>
		readonly object reconnectTaskLock;

		/// <summary>
		/// <see cref="TaskCompletionSource{TResult}"/> that completes while <see cref="messageQueue"/> isn't empty.
		/// </summary>
		TaskCompletionSource<object> nextMessage;

		/// <summary>
		/// The auto reconnect <see cref="Task"/>.
		/// </summary>
		Task reconnectTask;

		/// <summary>
		/// <see cref="CancellationTokenSource"/> for <see cref="reconnectTask"/>.
		/// </summary>
		CancellationTokenSource reconnectCts;

		/// <summary>
		/// Initializes a new instance of the <see cref="Provider"/> class.
		/// </summary>
		/// <param name="jobManager">The value of <see cref="jobManager"/>.</param>
		/// <param name="logger">The value of <see cref="Logger"/>.</param>
		/// <param name="chatBot">The value of <paramref name="chatBot"/>.</param>
		protected Provider(IJobManager jobManager, ILogger<Provider> logger, ChatBot chatBot)
		{
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
			ChatBot = chatBot ?? throw new ArgumentNullException(nameof(chatBot));

			messageQueue = new Queue<Message>();
			nextMessage = new TaskCompletionSource<object>();
			initialConnectionTcs = new TaskCompletionSource<object>();
			reconnectTaskLock = new object();

			logger.LogTrace("Created.");
		}

		/// <inheritdoc />
		public abstract bool Connected { get; }

		/// <inheritdoc />
		public abstract string BotMention { get; }

		/// <inheritdoc />
		public bool Disposed { get; private set; }

		/// <inheritdoc />
		public virtual async ValueTask DisposeAsync()
		{
			Disposed = true;
			await StopReconnectionTimer().ConfigureAwait(false);
			Logger.LogTrace("Disposed");
		}

		/// <inheritdoc />
		public async Task Disconnect(CancellationToken cancellationToken)
		{
			await StopReconnectionTimer().ConfigureAwait(false);

			if (Connected)
			{
				await DisconnectImpl(cancellationToken).ConfigureAwait(false);
				Logger.LogTrace("Disconnected");
			}
		}

		/// <inheritdoc />
		public abstract Task<IReadOnlyCollection<ChannelRepresentation>> MapChannels(IEnumerable<Api.Models.ChatChannel> channels, CancellationToken cancellationToken);

		/// <inheritdoc />
		public async Task<Message> NextMessage(CancellationToken cancellationToken)
		{
			while (true)
			{
				await nextMessage.Task.WithToken(cancellationToken).ConfigureAwait(false);
				lock (messageQueue)
					if (messageQueue.Count > 0)
					{
						var result = messageQueue.Dequeue();
						if (messageQueue.Count == 0)
							nextMessage = new TaskCompletionSource<object>();
						return result;
					}
			}
		}

		/// <inheritdoc />
		public Task SetReconnectInterval(uint reconnectInterval, bool connectNow)
		{
			if (reconnectInterval == 0)
				throw new ArgumentOutOfRangeException(nameof(reconnectInterval), reconnectInterval, "Reconnect interval cannot be zero!");

			Task stopOldTimerTask;
			lock (reconnectTaskLock)
			{
				stopOldTimerTask = StopReconnectionTimer();
				reconnectCts = new CancellationTokenSource();
				reconnectTask = ReconnectionLoop(reconnectInterval, connectNow, reconnectCts.Token);
			}

			return stopOldTimerTask;
		}

		/// <inheritdoc />
		public abstract Task SendMessage(ulong channelId, string message, CancellationToken cancellationToken);

		/// <inheritdoc />
		public abstract Task<Func<string, string, Task>> SendUpdateMessage(
			RevisionInformation revisionInformation,
			Version byondVersion,
			DateTimeOffset? estimatedCompletionTime,
			string gitHubOwner,
			string gitHubRepo,
			ulong channelId,
			bool localCommitPushed,
			CancellationToken cancellationToken);

		/// <summary>
		/// Attempt to connect the <see cref="Provider"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		protected abstract Task Connect(CancellationToken cancellationToken);

		/// <summary>
		/// Gracefully disconnects the provider.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		protected abstract Task DisconnectImpl(CancellationToken cancellationToken);

		/// <summary>
		/// Queues a <paramref name="message"/> for <see cref="NextMessage(CancellationToken)"/>.
		/// </summary>
		/// <param name="message">The <see cref="Message"/> to queue.</param>
		protected void EnqueueMessage(Message message)
		{
			if (message == null)
				Logger.LogTrace("Requesting channel remap...");

			lock (messageQueue)
			{
				messageQueue.Enqueue(message);
				nextMessage.TrySetResult(null);
			}
		}

		/// <summary>
		/// Stops and awaits the <see cref="reconnectTask"/>.
		/// </summary>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task StopReconnectionTimer()
		{
			lock (reconnectTaskLock)
				if (reconnectCts != null)
				{
					reconnectCts.Cancel();
					reconnectCts.Dispose();
					reconnectCts = null;
					Task reconnectTask = this.reconnectTask;
					this.reconnectTask = null;
					return reconnectTask;
				}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Creates a <see cref="Task"/> that will attempt to reconnect the <see cref="Provider"/> every <paramref name="reconnectInterval"/> minutes.
		/// </summary>
		/// <param name="reconnectInterval">The amount of minutes to wait between reconnection attempts.</param>
		/// <param name="connectNow">If a connection attempt should be immediately made.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task ReconnectionLoop(uint reconnectInterval, bool connectNow, CancellationToken cancellationToken)
		{
			do
			{
				try
				{
					if (!connectNow)
						await Task.Delay(TimeSpan.FromMinutes(reconnectInterval), cancellationToken).ConfigureAwait(false);
					else
						connectNow = false;
					if (!Connected)
					{
						var job = new Job
						{
							Description = $"Reconnect chat bot: {ChatBot.Name}",
							CancelRight = (ulong)ChatBotRights.WriteEnabled,
							CancelRightsType = RightsType.ChatBots,
							Instance = ChatBot.Instance,
						};

						await jobManager.RegisterOperation(
							job,
							async (core, databaseContextFactory, paramJob, progressReporter, jobCancellationToken) =>
							{
								if (Connected)
								{
									Logger.LogTrace("Disconnecting...");
									await DisconnectImpl(jobCancellationToken).ConfigureAwait(false);
								}
								else
									Logger.LogTrace("Already disconnected not doing disconnection attempt!");

								Logger.LogTrace("Connecting...");
								await Connect(jobCancellationToken).ConfigureAwait(false);
								Logger.LogTrace("Connected successfully");
								EnqueueMessage(null);
							},
							cancellationToken)
							.ConfigureAwait(false);

						// DCT: Always wait for the job to complete here
						await jobManager.WaitForJobCompletion(job, null, cancellationToken, default).ConfigureAwait(false);
					}
				}
				catch (OperationCanceledException)
				{
					break;
				}
				catch (Exception e)
				{
					Logger.LogError(e, "Error reconnecting!");
				}
				finally
				{
					initialConnectionTcs.TrySetResult(null);
				}
			}
			while (true);
		}
	}
}
