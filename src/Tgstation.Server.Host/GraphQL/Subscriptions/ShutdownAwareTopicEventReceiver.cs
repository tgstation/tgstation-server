using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using HotChocolate.Execution;
using HotChocolate.Subscriptions;

using Microsoft.Extensions.Hosting;

namespace Tgstation.Server.Host.GraphQL.Subscriptions
{
	/// <inheritdoc cref="ITopicEventReceiver" />
	sealed class ShutdownAwareTopicEventReceiver : ITopicEventReceiver, IAsyncDisposable
	{
		/// <summary>
		/// The <see cref="IHostApplicationLifetime"/> for the <see cref="ShutdownAwareTopicEventReceiver"/>.
		/// </summary>
		readonly IHostApplicationLifetime hostApplicationLifetime;

		/// <summary>
		/// The wrapped <see cref="HotChocolate.Subscriptions.ITopicEventReceiver"/>.
		/// </summary>
		readonly HotChocolate.Subscriptions.ITopicEventReceiver hotChocolateReceiver;

		/// <summary>
		/// A <see cref="ConcurrentBag{T}"/> of <see cref="CancellationTokenRegistration"/>s that were created for this scope.
		/// </summary>
		readonly ConcurrentBag<CancellationTokenRegistration> registrations;

		/// <summary>
		/// A <see cref="ConcurrentBag{T}"/> of <see cref="ValueTask"/>s returned from initiating <see cref="IAsyncDisposable.DisposeAsync"/> calls on <see cref="ISourceStream"/>s.
		/// </summary>
		readonly ConcurrentBag<Task> disposeTasks;

		/// <summary>
		/// Initializes a new instance of the <see cref="ShutdownAwareTopicEventReceiver"/> class.
		/// </summary>
		/// <param name="hostApplicationLifetime">The value of <see cref="hostApplicationLifetime"/>.</param>
		/// <param name="hotChocolateReceiver">The value of <see cref="hotChocolateReceiver"/>.</param>
		public ShutdownAwareTopicEventReceiver(
			IHostApplicationLifetime hostApplicationLifetime,
			HotChocolate.Subscriptions.ITopicEventReceiver hotChocolateReceiver)
		{
			this.hostApplicationLifetime = hostApplicationLifetime ?? throw new ArgumentNullException(nameof(hostApplicationLifetime));
			this.hotChocolateReceiver = hotChocolateReceiver ?? throw new ArgumentNullException(nameof(hotChocolateReceiver));

			registrations = new ConcurrentBag<CancellationTokenRegistration>();
			disposeTasks = new ConcurrentBag<Task>();
		}

		/// <inheritdoc />
		public async ValueTask DisposeAsync()
		{
			foreach (var registration in registrations)
			{
				registration.Dispose();
			}

			await Task.WhenAll(disposeTasks);
		}

		/// <inheritdoc />
		public ValueTask<ISourceStream<TMessage>> SubscribeAsync<TMessage>(string topicName, CancellationToken cancellationToken)
			=> WrapWithApplicationLifetimeCancellation(
				hotChocolateReceiver.SubscribeAsync<TMessage>(topicName, cancellationToken));

		/// <inheritdoc />
		public ValueTask<ISourceStream<TMessage>> SubscribeAsync<TMessage>(string topicName, int? bufferCapacity, TopicBufferFullMode? bufferFullMode, CancellationToken cancellationToken)
			=> WrapWithApplicationLifetimeCancellation(
				hotChocolateReceiver.SubscribeAsync<TMessage>(topicName, bufferCapacity, bufferFullMode, cancellationToken));

		/// <summary>
		/// Wraps a given <paramref name="sourceStreamTask"/> with <see cref="hostApplicationLifetime"/> cancellation awareness.
		/// </summary>
		/// <typeparam name="TMessage">The <see cref="Type"/> of message.</typeparam>
		/// <param name="sourceStreamTask">The result of a call to the <see cref="hotChocolateReceiver"/>.</param>
		/// <returns>The result of <paramref name="sourceStreamTask"/> with lifetime aware cancellation.</returns>
		async ValueTask<ISourceStream<TMessage>> WrapWithApplicationLifetimeCancellation<TMessage>(ValueTask<ISourceStream<TMessage>> sourceStreamTask)
		{
			var sourceStream = await sourceStreamTask;
			registrations.Add(
				hostApplicationLifetime.ApplicationStopping.Register(
					() => disposeTasks.Add(
						sourceStream.DisposeAsync().AsTask())));
			return sourceStream;
		}
	}
}
