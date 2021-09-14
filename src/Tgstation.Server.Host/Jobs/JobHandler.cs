using System;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.Extensions;

namespace Tgstation.Server.Host.Jobs
{
	/// <summary>
	/// Class for pairing <see cref="Task"/>s with <see cref="CancellationTokenSource"/>s.
	/// </summary>
	sealed class JobHandler : IDisposable
	{
		/// <summary>
		/// The <see cref="CancellationTokenSource"/> for <see cref="task"/>.
		/// </summary>
		readonly CancellationTokenSource cancellationTokenSource;

		/// <summary>
		/// A <see cref="Func{T, TResult}"/> taking a <see cref="CancellationToken"/> and returning a <see cref="Task"/> that the <see cref="JobHandler"/> will wrap.
		/// </summary>
		readonly Func<CancellationToken, Task> jobActivator;

		/// <summary>
		/// The <see cref="Task"/> being run.
		/// </summary>
		Task? task;

		/// <summary>
		/// Initializes a new instance of the <see cref="JobHandler"/> class.
		/// </summary>
		/// <param name="jobActivator">The value of <see cref="jobActivator"/>.</param>
		public JobHandler(Func<CancellationToken, Task> jobActivator)
		{
			this.jobActivator = jobActivator ?? throw new ArgumentNullException(nameof(jobActivator));
			cancellationTokenSource = new CancellationTokenSource();
		}

		/// <inheritdoc />
		public void Dispose() => cancellationTokenSource.Dispose();

		/// <summary>
		/// The progress of the job.
		/// </summary>
		public int? Progress { get; set; }

		/// <summary>
		/// Wait for <see cref="task"/> to complete.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		public Task Wait(CancellationToken cancellationToken)
		{
			if (task == null)
				throw new InvalidOperationException("Job not started!");

			return task.WithToken(cancellationToken);
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
