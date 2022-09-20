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
		/// Create a <see cref="Task"/> that can be awaited while respecting a given <paramref name="cancellationToken"/>.
		/// </summary>
		/// <param name="task">The <see cref="Task"/> to add cancel support to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		public static Task WithToken(this Task task, CancellationToken cancellationToken)
		{
			if (task == null)
				throw new ArgumentNullException(nameof(task));

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
			if (task == null)
				throw new ArgumentNullException(nameof(task));

			var cancelTcs = new TaskCompletionSource<object>();
			using (cancellationToken.Register(() => cancelTcs.SetCanceled()))
				await Task.WhenAny(task, cancelTcs.Task);
			cancellationToken.ThrowIfCancellationRequested();

			return await task;
		}

		/// <summary>
		/// Creates a <see cref="Task"/> that never completes.
		/// </summary>
		/// <returns>A never ending <see cref="Task"/>.</returns>
		public static Task InfiniteTask() => new TaskCompletionSource<object>().Task;
	}
}
