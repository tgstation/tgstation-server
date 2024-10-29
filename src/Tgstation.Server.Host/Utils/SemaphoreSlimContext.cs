using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

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
		/// <param name="logger">An optional <see cref="ILogger"/> to write to.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="SemaphoreSlimContext"/> for the lock.</returns>
		public static async ValueTask<SemaphoreSlimContext> Lock(SemaphoreSlim semaphore, CancellationToken cancellationToken, ILogger? logger = null)
		{
			ArgumentNullException.ThrowIfNull(semaphore);
			logger?.LogTrace("Acquiring semaphore...");
			await semaphore.WaitAsync(cancellationToken);
			return new SemaphoreSlimContext(semaphore, logger);
		}

		/// <summary>
		/// Asyncronously attempts to lock a <paramref name="semaphore"/>.
		/// </summary>
		/// <param name="semaphore">The <see cref="SemaphoreSlim"/> to lock.</param>
		/// <param name="logger">An optional <see cref="ILogger"/> to write to.</param>
		/// <param name="locked">The <see cref="bool"/> result of the lock attempt.</param>
		/// <returns>A <see cref="SemaphoreSlimContext"/> for the lock on success, or <see langword="null"/> if it was not acquired.</returns>
		public static SemaphoreSlimContext? TryLock(SemaphoreSlim semaphore, ILogger? logger, out bool locked)
		{
			ArgumentNullException.ThrowIfNull(semaphore);
			logger?.LogTrace("Trying to acquire semaphore...");
			locked = semaphore.Wait(TimeSpan.Zero);
			logger?.LogTrace("Acquired semaphore {un}successfully", locked ? String.Empty : "un");
			return locked
				? new SemaphoreSlimContext(semaphore, logger)
				: null;
		}

		/// <summary>
		/// An optional <see cref="ILogger"/> to write to.
		/// </summary>
		readonly ILogger? logger;

		/// <summary>
		/// The locked <see cref="SemaphoreSlim"/>.
		/// </summary>
		readonly SemaphoreSlim lockedSemaphore;

		/// <summary>
		/// Initializes a new instance of the <see cref="SemaphoreSlimContext"/> class.
		/// </summary>
		/// <param name="lockedSemaphore">The value of <see cref="lockedSemaphore"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		SemaphoreSlimContext(SemaphoreSlim lockedSemaphore, ILogger? logger)
		{
			this.lockedSemaphore = lockedSemaphore;
			this.logger = logger;
		}

		/// <summary>
		/// Release the lock on <see cref="lockedSemaphore"/>.
		/// </summary>
		public void Dispose()
		{
			logger?.LogTrace("Releasing semaphore...");
			lockedSemaphore.Release();
		}
	}
}
