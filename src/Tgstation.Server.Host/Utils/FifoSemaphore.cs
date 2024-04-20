using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Utils
{
	/// <summary>
	/// A first-in first-out async semaphore.
	/// </summary>
	/// <remarks>This is contentious and could be re-written using <see cref="TaskCompletionSource"/>s to make it promise based. However, it has a lower memory footprint without them and is fine for our uses.</remarks>
	sealed class FifoSemaphore : IDisposable
	{
		/// <summary>
		/// <see langword="class"/> to represent a ticket in the <see cref="ticketQueue"/> and whether or not it is <see cref="Abandoned"/>.
		/// </summary>
		sealed class FifoSemaphoreTicket
		{
			/// <summary>
			/// Set if the wait operation on a <see cref="FifoSemaphore"/> was cancelled to avoid clogging the queue.
			/// </summary>
			public bool Abandoned { get; set; }
		}

		/// <summary>
		/// The backing <see cref="SemaphoreSlim"/>.
		/// </summary>
		readonly SemaphoreSlim semaphore;

		/// <summary>
		/// The <see cref="Queue{T}"/> of ticket <see cref="FifoSemaphoreTicket"/>s.
		/// </summary>
		readonly Queue<FifoSemaphoreTicket> ticketQueue;

		/// <summary>
		/// Initializes a new instance of the <see cref="FifoSemaphore"/> class.
		/// </summary>
		public FifoSemaphore()
		{
			ticketQueue = new Queue<FifoSemaphoreTicket>();
			semaphore = new SemaphoreSlim(1);
		}

		/// <inheritdoc />
		public void Dispose() => semaphore.Dispose();

		/// <summary>
		/// Locks the <see cref="FifoSemaphore"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the locked <see cref="SemaphoreSlimContext"/>.</returns>
		public async ValueTask<SemaphoreSlimContext> Lock(CancellationToken cancellationToken)
		{
			FifoSemaphoreTicket? ticket = null;
			using (cancellationToken.Register(
				() =>
				{
					if (ticket != null)
						ticket.Abandoned = true;
				}))
				while (true)
				{
					var context = await SemaphoreSlimContext.Lock(semaphore, cancellationToken);
					try
					{
						FifoSemaphoreTicket? peekedTicket = null;
						while (ticketQueue.Count > 0)
						{
							peekedTicket = ticketQueue.Peek();
							if (peekedTicket.Abandoned)
								ticketQueue.Dequeue();
							else
								break;
						}

						cancellationToken.ThrowIfCancellationRequested();

						bool goTime;
						if (ticketQueue.Count == 0)
							goTime = true;
						else if (ticket == null)
						{
							ticket = new FifoSemaphoreTicket();
							cancellationToken.ThrowIfCancellationRequested();
							ticketQueue.Enqueue(ticket);
							goTime = false;
						}
						else
						{
							goTime = peekedTicket == ticket;
							if (goTime)
								ticketQueue.Dequeue();
						}

						if (goTime)
						{
							var localContext = context;
							context = null;
							return localContext;
						}
					}
					finally
					{
						context?.Dispose();
					}

					await Task.Yield();
				}
		}
	}
}
