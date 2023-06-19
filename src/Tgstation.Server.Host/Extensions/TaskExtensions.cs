using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extensions for the <see cref="Task"/> class.
	/// </summary>
	static class TaskExtensions
	{
		/// <summary>
		/// A <see cref="TaskCompletionSource"/> that never completes.
		/// </summary>
		static readonly TaskCompletionSource InfiniteTaskCompletionSource = new ();

		/// <summary>
		/// Gets a <see cref="Task"/> that never completes.
		/// </summary>
		public static Task InfiniteTask => InfiniteTaskCompletionSource.Task;

		/// <summary>
		/// Create a <see cref="Task"/> that can be awaited while respecting a given <paramref name="cancellationToken"/>.
		/// </summary>
		/// <param name="task">The <see cref="Task"/> to add cancel support to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		public static Task WithToken(this Task task, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(task);

			async Task<object> Wrap()
			{
				await task;
				return null;
			}

			return Wrap().WithToken(cancellationToken);
		}

		/// <summary>
		/// Create a <see cref="Task{TResult}"/> that can be awaited while respecting a given <paramref name="cancellationToken"/>.
		/// </summary>
		/// <typeparam name="T">The result <see cref="Type"/> of the <paramref name="task"/>.</typeparam>
		/// <param name="task">The <see cref="Task{TResult}"/> to add cancel support to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the result of <paramref name="task"/>.</returns>
		public static async Task<T> WithToken<T>(this Task<T> task, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(task);

			var cancelTcs = new TaskCompletionSource();
			Task completedTask;
			using (cancellationToken.Register(() => cancelTcs.SetCanceled(cancellationToken)))
				completedTask = await Task.WhenAny(task, cancelTcs.Task);

			if (completedTask != task)
				await cancelTcs.Task;

			return await task;
		}
	}
}
