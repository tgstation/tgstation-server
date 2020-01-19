using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Jobs
{
	/// <summary>
	/// Class for pairing <see cref="Task"/>s with <see cref="CancellationTokenSource"/>s
	/// </summary>
	sealed class JobHandler : IDisposable
	{
		/// <summary>
		/// The <see cref="CancellationTokenSource"/> for <see cref="task"/>
		/// </summary>
		readonly CancellationTokenSource cancellationTokenSource;

		/// <summary>
		/// The <see cref="Task"/> being run
		/// </summary>
		readonly Task task;

		/// <summary>
		/// Construct a <see cref="JobHandler"/>
		/// </summary>
		/// <param name="job">A <see cref="Func{T, TResult}"/> taking a <see cref="CancellationToken"/> and returning a <see cref="Task"/> that the <see cref="JobHandler"/> will wrap</param>
		public JobHandler(Func<CancellationToken, Task> job)
		{
			if (job == null)
				throw new ArgumentNullException(nameof(job));
			cancellationTokenSource = new CancellationTokenSource();
			task = job(cancellationTokenSource.Token);
		}

		/// <inheritdoc />
		public void Dispose() => cancellationTokenSource.Dispose();

		/// <summary>
		/// The progress of the job
		/// </summary>
		public int? Progress { get; set; }

		/// <summary>
		/// Wait for <see cref="task"/> to complete
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		public async Task Wait(CancellationToken cancellationToken)
		{
			TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
			using (cancellationToken.Register(() => tcs.SetCanceled()))
				await Task.WhenAny(tcs.Task, task).ConfigureAwait(false);
			cancellationToken.ThrowIfCancellationRequested();
		}

		/// <summary>
		/// Cancels <see cref="task"/>
		/// </summary>
		public void Cancel() => cancellationTokenSource.Cancel();
	}
}