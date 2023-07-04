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
	}
}
