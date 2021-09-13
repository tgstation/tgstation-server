using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Helpers
{
	/// <summary>
	/// A <see cref="Task{TResult}"/> with an associated <see cref="CancellationTokenSource"/>.
	/// </summary>
	/// <typeparam name="TResult">The <see cref="Type"/> of the result produced by <see cref="Task"/>.</typeparam>
	public sealed class CancellableTask<TResult> : CancellableTaskInternal<Task<TResult>>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CancellableTask{TResult}"/> class.
		/// </summary>
		/// <param name="taskStarter">A <see cref="Func{T, TResult}"/> to launch the <see cref="CancellableTaskInternal{TTask}.Task"/> with a <see cref="CancellationToken"/>.</param>
		public CancellableTask(Func<CancellationToken, Task<TResult>> taskStarter)
			: base(taskStarter)
		{
		}
	}
}
