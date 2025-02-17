using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Utils;

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
		/// The <see cref="IAsyncDelayer"/> for the <see cref="Provider"/>.
		/// </summary>
		protected IAsyncDelayer AsyncDelayer { get; }

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
		readonly Queue<Message?> messageQueue;

		/// <summary>
		/// The backing <see cref="TaskCompletionSource"/> for <see cref="InitialConnectionJob"/>.
		/// </summary>
		readonly TaskCompletionSource initialConnectionTcs;

		/// <summary>
		/// Used for synchronizing access to <see cref="reconnectCts"/> and <see cref="reconnectTask"/>.
		/// </summary>
		readonly object reconnectTaskLock;

		/// <summary>
		/// <see cref="TaskCompletionSource"/> that completes while <see cref="messageQueue"/> isn't empty.
		/// </summary>
		TaskCompletionSource nextMessage;

		/// <summary>
		/// The auto reconnect <see cref="Task"/>.
		/// </summary>
		Task? reconnectTask;

		/// <summary>
		/// <see cref="CancellationTokenSource"/> for <see cref="reconnectTask"/>.
		/// </summary>
		CancellationTokenSource? reconnectCts;

		/// <summary>
		/// Get the prefix for messages about deployments.
		/// </summary>
		/// <param name="engineType">The <see cref="Api.Models.EngineType"/> of the deployment.</param>
		/// <returns>The <see cref="string"/> prefix.</returns>
		protected static string GetEngineCompilerPrefix(Api.Models.EngineType engineType)
			=> engineType switch
			{
				Api.Models.EngineType.Byond => "DM",
				Api.Models.EngineType.OpenDream => "OD",
				_ => throw new InvalidOperationException($"Unsupported engine type: {engineType}"),
			};

		/// <summary>
		/// Initializes a new instance of the <see cref="Provider"/> class.
		/// </summary>
		/// <param name="jobManager">The value of <see cref="jobManager"/>.</param>
		/// <param name="asyncDelayer">The value of <see cref="AsyncDelayer"/>.</param>
		/// <param name="logger">The value of <see cref="Logger"/>.</param>
		/// <param name="chatBot">The value of <paramref name="chatBot"/>.</param>
		protected Provider(IJobManager jobManager, IAsyncDelayer asyncDelayer, ILogger<Provider> logger, ChatBot chatBot)
		{
			this.jobManager = jobManager ?? throw new ArgumentNullException(nameof(jobManager));
			AsyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
			ChatBot = chatBot ?? throw new ArgumentNullException(nameof(chatBot));

			if (chatBot.Instance == null)
				throw new ArgumentException("chatBot must have Instance!", nameof(chatBot));

			messageQueue = new Queue<Message?>();
			nextMessage = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			initialConnectionTcs = new TaskCompletionSource();
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
			await StopReconnectionTimer();

			// queue a final message to shutdown the NextMessage Task
			EnqueueMessage(null);
			Logger.LogTrace("Disposed");
		}

		/// <inheritdoc />
		public async ValueTask Disconnect(CancellationToken cancellationToken)
		{
			await StopReconnectionTimer();

			if (Connected)
			{
				Logger.LogTrace("Disconnecting...");
				await DisconnectImpl(cancellationToken);
				Logger.LogTrace("Disconnected");
			}
		}

		/// <inheritdoc />
		public void InitialMappingComplete() => initialConnectionTcs.TrySetResult();

		/// <inheritdoc />
		public async ValueTask<Dictionary<ChatChannel, IEnumerable<ChannelRepresentation>>> MapChannels(IEnumerable<ChatChannel> channels, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(channels);

			try
			{
				return await MapChannelsImpl(channels, cancellationToken);
			}
			catch
			{
				initialConnectionTcs.TrySetResult();
				throw;
			}
		}

		/// <inheritdoc />
		public async Task<Message?> NextMessage(CancellationToken cancellationToken)
		{
			while (true)
			{
				await nextMessage.Task.WaitAsync(cancellationToken);
				lock (messageQueue)
					if (messageQueue.Count > 0)
					{
						var result = messageQueue.Dequeue();
						if (messageQueue.Count == 0)
							nextMessage = new TaskCompletionSource();
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
		public abstract ValueTask SendMessage(Message? replyTo, MessageContent message, ulong channelId, CancellationToken cancellationToken);

		/// <inheritdoc />
		public abstract ValueTask<Func<string?, string, ValueTask<Func<bool, ValueTask>>>> SendUpdateMessage(
			RevisionInformation revisionInformation,
			RevisionInformation? previousRevisionInformation,
			Api.Models.EngineVersion engineVersion,
			DateTimeOffset? estimatedCompletionTime,
			string? gitHubOwner,
			string? gitHubRepo,
			ulong channelId,
			bool localCommitPushed,
			CancellationToken cancellationToken);

		/// <summary>
		/// Attempt to connect the <see cref="Provider"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		protected abstract ValueTask Connect(CancellationToken cancellationToken);

		/// <summary>
		/// Gracefully disconnects the provider.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		protected abstract ValueTask DisconnectImpl(CancellationToken cancellationToken);

		/// <summary>
		/// Implementation of <see cref="MapChannels(IEnumerable{ChatChannel}, CancellationToken)"/>.
		/// </summary>
		/// <param name="channels">The <see cref="ChatChannel"/>s to map.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="Dictionary{TKey, TValue}"/> of the <see cref="ChatChannel"/>'s <see cref="ChannelRepresentation"/>s representing <paramref name="channels"/>.</returns>
		protected abstract ValueTask<Dictionary<ChatChannel, IEnumerable<ChannelRepresentation>>> MapChannelsImpl(
			IEnumerable<ChatChannel> channels,
			CancellationToken cancellationToken);

		/// <summary>
		/// Queues a <paramref name="message"/> for <see cref="NextMessage(CancellationToken)"/>.
		/// </summary>
		/// <param name="message">The <see cref="Message"/> to queue. A value of <see langword="null"/> indicates the channel mappings are out of date.</param>
		protected void EnqueueMessage(Message? message)
		{
			if (message == null)
				Logger.LogTrace("Requesting channel remap...");

			lock (messageQueue)
			{
				messageQueue.Enqueue(message);
				nextMessage.TrySetResult();
			}
		}

		/// <summary>
		/// Stops and awaits the <see cref="reconnectTask"/>.
		/// </summary>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task StopReconnectionTimer()
		{
			Logger.LogTrace("StopReconnectionTimer");
			lock (reconnectTaskLock)
				if (reconnectCts != null)
				{
					reconnectCts.Cancel();
					reconnectCts.Dispose();
					reconnectCts = null;
					var reconnectTask = this.reconnectTask!;
					this.reconnectTask = null;
					return reconnectTask;
				}
				else
					Logger.LogTrace("Timer wasn't running");

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
						await AsyncDelayer.Delay(TimeSpan.FromMinutes(reconnectInterval), cancellationToken);
					else
						connectNow = false;
					if (!Connected)
					{
						var job = Job.Create(Api.Models.JobCode.ReconnectChatBot, null, ChatBot.Instance!, ChatBotRights.WriteEnabled);
						job.Description += $": {ChatBot.Name}";

						await jobManager.RegisterOperation(
							job,
							async (core, databaseContextFactory, paramJob, progressReporter, jobCancellationToken) =>
							{
								try
								{
									if (Connected)
									{
										Logger.LogTrace("Disconnecting...");
										await DisconnectImpl(jobCancellationToken);
									}
									else
										Logger.LogTrace("Already disconnected not doing disconnection attempt!");

									Logger.LogTrace("Connecting...");
									await Connect(jobCancellationToken);
									Logger.LogTrace("Connected successfully");
									EnqueueMessage(null);
								}
								catch
								{
									// we set this here because otherwise there could be stuff waiting on to connect us forever
									initialConnectionTcs.TrySetResult();
									throw;
								}
							},
							cancellationToken);

						await jobManager.WaitForJobCompletion(job, null, cancellationToken, cancellationToken);
					}
				}
				catch (OperationCanceledException e)
				{
					Logger.LogTrace(e, "ReconnectionLoop cancelled");
				}
				catch (Exception e)
				{
					Logger.LogError(e, "Error reconnecting!");
				}
			}
			while (!cancellationToken.IsCancellationRequested);

			Logger.LogTrace("ReconnectionLoop exiting...");
		}
	}
}
