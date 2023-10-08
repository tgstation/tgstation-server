using System.Threading.Tasks;

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extensions for the <see cref="Task"/> and <see cref="Task{TResult}"/> <see langword="class"/>es.
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
	}
}
