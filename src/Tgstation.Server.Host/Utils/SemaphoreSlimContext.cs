using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Utils
{
	/// <summary>
	/// Async lock context helper.
	/// </summary>
	sealed class SemaphoreSlimContext : IDisposable
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
			await semaphore.WaitAsync(cancellationToken);
			return new SemaphoreSlimContext(semaphore);
		}

		/// <summary>
		/// Asyncronously attempts to lock a <paramref name="semaphore"/>.
		/// </summary>
		/// <param name="semaphore">The <see cref="SemaphoreSlim"/> to lock.</param>
		/// <param name="locked">The <see cref="bool"/> result of the lock attempt.</param>
		/// <returns>A <see cref="SemaphoreSlimContext"/> for the lock on success, or <see langword="null"/> if it was not acquired.</returns>
		public static SemaphoreSlimContext TryLock(SemaphoreSlim semaphore, out bool locked)
		{
			ArgumentNullException.ThrowIfNull(semaphore);
			locked = semaphore.Wait(TimeSpan.Zero);
			return locked
				? new SemaphoreSlimContext(semaphore)
				: null;
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
