using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components.Chat.Providers
{
	/// <inheritdoc />
	abstract class Provider : IProvider
	{
		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="Provider"/>.
		/// </summary>
		protected ILogger Logger { get; }

		/// <summary>
		/// <see cref="Queue{T}"/> of received <see cref="Message"/>s
		/// </summary>
		readonly Queue<Message> messageQueue;

		/// <summary>
		/// Used for synchronizing access to <see cref="reconnectCts"/> and <see cref="reconnectTask"/>.
		/// </summary>
		readonly object reconnectTaskLock;

		/// <summary>
		/// <see cref="TaskCompletionSource{TResult}"/> that completes while <see cref="messageQueue"/> isn't empty
		/// </summary>
		TaskCompletionSource<object> nextMessage;

		/// <summary>
		/// The auto reconnect <see cref="Task"/>
		/// </summary>
		Task reconnectTask;

		/// <summary>
		/// <see cref="CancellationTokenSource"/> for <see cref="reconnectTask"/>
		/// </summary>
		CancellationTokenSource reconnectCts;

		/// <summary>
		/// Construct a <see cref="Provider"/>
		/// </summary>
		/// <param name="logger">The value of <see cref="Logger"/>.</param>
		/// <param name="reconnectInterval">The initial reconnection interval.</param>
		protected Provider(ILogger logger, uint reconnectInterval)
		{
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));

			messageQueue = new Queue<Message>();
			nextMessage = new TaskCompletionSource<object>();

			reconnectTaskLock = new object();

			SetReconnectInterval(reconnectInterval).GetAwaiter().GetResult();
			logger.LogTrace("Created.");
		}

		/// <inheritdoc />
		public abstract bool Connected { get; }

		/// <inheritdoc />
		public abstract string BotMention { get; }

		/// <summary>
		/// Queues a <paramref name="message"/> for <see cref="NextMessage(CancellationToken)"/>
		/// </summary>
		/// <param name="message">The <see cref="Message"/> to queue</param>
		protected void EnqueueMessage(Message message)
		{
			lock (messageQueue)
			{
				messageQueue.Enqueue(message);
				nextMessage.TrySetResult(null);
			}
		}

		/// <inheritdoc />
		public virtual void Dispose()
		{
			StopReconnectionTimer().GetAwaiter().GetResult();
			Logger.LogTrace("Disposed");
		}

		/// <inheritdoc />
		public abstract Task<bool> Connect(CancellationToken cancellationToken);

		/// <inheritdoc />
		public abstract Task Disconnect(CancellationToken cancellationToken);

		/// <inheritdoc />
		public abstract Task<IReadOnlyCollection<ChannelRepresentation>> MapChannels(IEnumerable<Api.Models.ChatChannel> channels, CancellationToken cancellationToken);

		/// <inheritdoc />
		public async Task<Message> NextMessage(CancellationToken cancellationToken)
		{
			var cancelTcs = new TaskCompletionSource<object>();
			using (cancellationToken.Register(() => cancelTcs.SetCanceled()))
				await Task.WhenAny(nextMessage.Task, cancelTcs.Task).ConfigureAwait(false);
			cancellationToken.ThrowIfCancellationRequested();
			lock (messageQueue)
			{
				var result = messageQueue.Dequeue();
				if (messageQueue.Count == 0)
					nextMessage = new TaskCompletionSource<object>();
				return result;
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

		/// <inheritdoc />
		public Task SetReconnectInterval(uint reconnectInterval)
		{
			if (reconnectInterval == 0)
				throw new ArgumentOutOfRangeException(nameof(reconnectInterval), reconnectInterval, "Reconnect interval cannot be zero!");

			Task stopOldTimerTask;
			lock (reconnectTaskLock)
			{
				stopOldTimerTask = StopReconnectionTimer();
				reconnectCts = new CancellationTokenSource();
				reconnectTask = ReconnectionLoop(reconnectInterval, reconnectCts.Token);
			}

			return stopOldTimerTask;
		}

		/// <summary>
		/// Creates a <see cref="Task"/> that will attempt to reconnect the <see cref="Provider"/> every <paramref name="reconnectInterval"/> minutes.
		/// </summary>
		/// <param name="reconnectInterval">The amount of minutes to wait between reconnection attempts.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task ReconnectionLoop(uint reconnectInterval, CancellationToken cancellationToken)
		{
			do
			{
				try
				{
					await Task.Delay(TimeSpan.FromMinutes(reconnectInterval), cancellationToken).ConfigureAwait(false);
					if (!Connected)
					{
						Logger.LogInformation("Attempting to reconnect provider...");
						await Disconnect(cancellationToken).ConfigureAwait(false);
						if (await Connect(cancellationToken).ConfigureAwait(false))
							EnqueueMessage(null);
					}
				}
				catch (OperationCanceledException)
				{
					break;
				}
				catch(Exception e)
				{
					Logger.LogError(e, "Error reconnecting!");
				}
			}
			while (true);
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
	}
}
