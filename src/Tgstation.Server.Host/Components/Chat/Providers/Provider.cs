using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Components.Chat.Providers
{
	/// <inheritdoc />
	abstract class Provider : IProvider
	{
		/// <summary>
		/// <see cref="Queue{T}"/> of received <see cref="Message"/>s
		/// </summary>
		readonly Queue<Message> messageQueue;

		/// <summary>
		/// <see cref="TaskCompletionSource{TResult}"/> that completes while <see cref="messageQueue"/> isn't empty
		/// </summary>
		TaskCompletionSource<object> nextMessage;

		/// <summary>
		/// Construct a <see cref="Provider"/>
		/// </summary>
		protected Provider()
		{
			messageQueue = new Queue<Message>();
			nextMessage = new TaskCompletionSource<object>();
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
		public abstract void Dispose();

		/// <inheritdoc />
		public abstract Task<bool> Connect(CancellationToken cancellationToken);

		/// <inheritdoc />
		public abstract Task Disconnect(CancellationToken cancellationToken);

		/// <inheritdoc />
		public abstract Task<IReadOnlyList<Channel>> MapChannels(IEnumerable<ChatChannel> channels, CancellationToken cancellationToken);

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

		/// <inheritdoc />
		public abstract Task SendMessage(ulong channelId, string message, CancellationToken cancellationToken);
	}
}
