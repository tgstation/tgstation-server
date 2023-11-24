using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Jobs
{
	/// <summary>
	/// Class for pairing <see cref="Task"/>s with <see cref="CancellationTokenSource"/>s.
	/// </summary>
	sealed class JobHandler : IDisposable
	{
		/// <summary>
		/// If the job has started.
		/// </summary>
		public bool Started => task != null;

		/// <summary>
		/// The progress of the job.
		/// </summary>
		public int? Progress { get; set; }

		/// <summary>
		/// The stage of the job.
		/// </summary>
		public string? Stage { get; set; }

		/// <summary>
		/// The <see cref="CancellationTokenSource"/> for <see cref="task"/>.
		/// </summary>
		readonly CancellationTokenSource cancellationTokenSource;

		/// <summary>
		/// A <see cref="Func{T, TResult}"/> taking a <see cref="CancellationToken"/> and returning a <see cref="Task{TResult}"/> that the <see cref="JobHandler"/> will wrap.
		/// </summary>
		readonly Func<CancellationToken, Task<bool>> jobActivator;

		/// <summary>
		/// The <see cref="Task"/> being run.
		/// </summary>
		Task<bool>? task;

		/// <summary>
		/// Initializes a new instance of the <see cref="JobHandler"/> class.
		/// </summary>
		/// <param name="jobActivator">The value of <see cref="jobActivator"/>.</param>
		public JobHandler(Func<CancellationToken, Task<bool>> jobActivator)
		{
			this.jobActivator = jobActivator ?? throw new ArgumentNullException(nameof(jobActivator));
			cancellationTokenSource = new CancellationTokenSource();
		}

		/// <inheritdoc />
		public void Dispose() => cancellationTokenSource.Dispose();

		/// <summary>
		/// Wait for <see cref="task"/> to complete.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> representing the job. Results in <see langword="true"/> if the job completed without errors, <see langword="false"/> otherwise.</returns>
		public Task<bool> Wait(CancellationToken cancellationToken)
		{
			if (task == null)
				throw new InvalidOperationException("Job not started!");

			return task.WaitAsync(cancellationToken);
		}

		/// <summary>
		/// Cancels <see cref="task"/>.
		/// </summary>
		public void Cancel() => cancellationTokenSource.Cancel();

		/// <summary>
		/// Starts the job.
		/// </summary>
		public void Start()
		{
			lock (cancellationTokenSource)
			{
				if (task != null)
					throw new InvalidOperationException("Job already started");
				task = jobActivator(cancellationTokenSource.Token);
			}
		}
	}
}
