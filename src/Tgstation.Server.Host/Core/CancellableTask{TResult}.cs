using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// A <see cref="Task{TResult}"/> with an associated <see cref="CancellationTokenSource"/>.
	/// </summary>
	/// <typeparam name="TResult">The <see cref="Type"/> of the result produced by <see cref="Task"/>.</typeparam>
	class CancellableTask<TResult> : IAsyncDisposable
	{
		/// <summary>
		/// The <see cref="CancellationTokenSource"/> for <see cref="Task"/>.
		/// </summary>
		readonly CancellationTokenSource cancellationTokenSource;

		/// <summary>
		/// The <see cref="Task{TResult}"/>.
		/// </summary>
		public Task<TResult> Task { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="CancellableTask{TResult}"/> class.
		/// </summary>
		/// <param name="taskStarter">A <see cref="Func{T, TResult}"/> to launch the <see cref="Task"/> with a <see cref="CancellationToken"/>.</param>
		public CancellableTask(Func<CancellationToken, Task<TResult>> taskStarter)
		{
			if (taskStarter == null)
				throw new ArgumentNullException(nameof(taskStarter));

			cancellationTokenSource = new CancellationTokenSource();
			try
			{
				Task = taskStarter(cancellationTokenSource.Token);
			}
			catch
			{
				cancellationTokenSource.Dispose();
				throw;
			}
		}

		/// <inheritdoc />
		public async ValueTask DisposeAsync()
		{
			Cancel();
			cancellationTokenSource.Dispose();

			await Task.ConfigureAwait(false);
		}

		/// <summary>
		/// Cancel the <see cref="Task"/>.
		/// </summary>
		public void Cancel() => cancellationTokenSource.Cancel();
	}
}
