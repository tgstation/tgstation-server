using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Helpers
{
	/// <summary>
	/// A <see cref="Task"/> with an associated <see cref="CancellationTokenSource"/>.
	/// </summary>
	public sealed class CancellableTask : CancellableTaskInternal<Task>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CancellableTask"/> class.
		/// </summary>
		/// <param name="taskStarter">A <see cref="Func{T, TResult}"/> to launch the <see cref="Task"/> with a <see cref="CancellationToken"/>.</param>
		public CancellableTask(Func<CancellationToken, Task> taskStarter)
			: base(taskStarter)
		{
		}
	}
}
