using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Utils
{
	/// <summary>
	/// Async lock context helper.
	/// </summary>
	public sealed class SemaphoreSlimContext : IDisposable
	{
		/// <summary>
		/// Asyncronously locks a <paramref name="semaphore"/>.
		/// </summary>
		/// <param name="semaphore">The <see cref="SemaphoreSlim"/> to lock.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="SemaphoreSlimContext"/> for the lock.</returns>
		public static async ValueTask<SemaphoreSlimContext> Lock(SemaphoreSlim semaphore, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(semaphore);
			cancellationToken.ThrowIfCancellationRequested();
			await semaphore.WaitAsync(cancellationToken);
			return new SemaphoreSlimContext(semaphore);
		}

		/// <summary>
		/// The locked <see cref="SemaphoreSlim"/>.
		/// </summary>
		readonly SemaphoreSlim lockedSemaphore;

		/// <summary>
		/// Initializes a new instance of the <see cref="SemaphoreSlimContext"/> class.
		/// </summary>
		/// <param name="lockedSemaphore">The value of <see cref="lockedSemaphore"/>.</param>
		SemaphoreSlimContext(SemaphoreSlim lockedSemaphore)
		{
			this.lockedSemaphore = lockedSemaphore;
		}

		/// <summary>
		/// Release the lock on <see cref="lockedSemaphore"/>.
		/// </summary>
		public void Dispose() => lockedSemaphore.Release();
	}
}
