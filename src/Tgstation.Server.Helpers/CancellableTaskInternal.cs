using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Helpers
{
	/// <summary>
	/// A <typeparamref name="TTask"/> with an associated <see cref="CancellationTokenSource"/>.
	/// </summary>
	/// <typeparam name="TTask">The <see cref="Type"/> of the result produced by <see cref="Task"/>.</typeparam>
	public abstract class CancellableTaskInternal<TTask> : IAsyncDisposable where TTask : Task
	{
		/// <summary>
		/// The <see cref="CancellationTokenSource"/> for <see cref="Task"/>.
		/// </summary>
		readonly CancellationTokenSource cancellationTokenSource;

		/// <summary>
		/// The <see cref="Task{TResult}"/>.
		/// </summary>
		public TTask Task { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="CancellableTaskInternal{TTask}"/> class.
		/// </summary>
		/// <param name="taskStarter">A <see cref="Func{T, TResult}"/> to launch the <see cref="Task"/> with a <see cref="CancellationToken"/>.</param>
		internal CancellableTaskInternal(Func<CancellationToken, TTask> taskStarter)
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
			lock (cancellationTokenSource)
			{
				if (!Task.IsCompleted)
					Cancel();

				cancellationTokenSource.Dispose();
			}

			await Task.ConfigureAwait(false);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Cancel the <see cref="Task"/>.
		/// </summary>
		public void Cancel() => cancellationTokenSource.Cancel();
	}
}
