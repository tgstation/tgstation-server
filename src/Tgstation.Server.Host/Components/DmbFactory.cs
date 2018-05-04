using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Standard <see cref="IDmbFactory"/>
	/// </summary>
	sealed class DmbFactory : IDmbFactory, ICompileJobConsumer
	{
		/// <summary>
		/// The <see cref="IDatabaseContext"/> for the <see cref="DmbFactory"/>
		/// </summary>
		readonly IDatabaseContext databaseContext;
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="DmbFactory"/>
		/// </summary>
		readonly IIOManager ioManager;
		/// <summary>
		/// The <see cref="CancellationTokenSource"/> for <see cref="cleanupTask"/>
		/// </summary>
		readonly CancellationTokenSource cleanupCts;
		
		/// <summary>
		/// <see cref="Task"/> representing calls to <see cref="CleanJob(CompileJob)"/>
		/// </summary>
		Task cleanupTask;
		/// <summary>
		/// <see cref="TaskCompletionSource{TResult}"/> resulting in the latest <see cref="DmbProvider"/> yet to exist
		/// </summary>
		TaskCompletionSource<DmbProvider> newerDmbTcs;
		/// <summary>
		/// The latest <see cref="DmbProvider"/>
		/// </summary>
		DmbProvider nextDmbProvider;

		/// <summary>
		/// Construct a <see cref="DmbFactory"/>
		/// </summary>
		/// <param name="databaseContext">The value of <see cref="databaseContext"/></param>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		public DmbFactory(IDatabaseContext databaseContext, IIOManager ioManager)
		{
			this.databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));

			cleanupCts = new CancellationTokenSource();
		}

		/// <inheritdoc />
		public void Dispose() => cleanupCts.Dispose();

		/// <summary>
		/// Delete the <see cref="Api.Models.Internal.CompileJob.DirectoryName"/> of <paramref name="job"/>
		/// </summary>
		/// <param name="job">The <see cref="CompileJob"/> to clean</param>
		void CleanJob(CompileJob job)
		{
			async Task HandleCleanup()
			{
				var deleteJob = ioManager.DeleteDirectory(job.DirectoryName.ToString(), cleanupCts.Token);
				Task otherTask;
				lock (this)
					otherTask = cleanupTask;
				await Task.WhenAll(otherTask, deleteJob).ConfigureAwait(false);
			}
			lock (this)
				cleanupTask = HandleCleanup();
		}

		/// <inheritdoc />
		public void LoadCompileJob(CompileJob job)
		{
			if (job == null)
				throw new ArgumentNullException(nameof(job));
			if (!job.DMApiValidated || job.Job.Cancelled || job.Job.ExceptionDetails != null || job.Job.StoppedAt == null)
				throw new InvalidOperationException("Cannot load incomplete compile job!");
			lock (this)
			{
				var oldDmbProvider = nextDmbProvider;
				if (oldDmbProvider != null && oldDmbProvider.CompileJob.Job.StoppedAt < oldDmbProvider.CompileJob.Job.StoppedAt)
					throw new InvalidOperationException("Loaded compile job older than current job!");
				nextDmbProvider = new DmbProvider(job, ioManager, () => CleanJob(job));
				newerDmbTcs.SetResult(nextDmbProvider);
				newerDmbTcs = new TaskCompletionSource<DmbProvider>();
			}
		}

		/// <inheritdoc />
		public async Task<IDmbProvider> LockNextDmb(CancellationToken cancellationToken)
		{
			Task<DmbProvider> task;
			lock (this)
				if (nextDmbProvider != null)
					return nextDmbProvider;
				else
					task = newerDmbTcs.Task;

			var result = await task.ConfigureAwait(false);
			//so there's currently a race condition in DreamMakerController where the setting of CompileJob.RevisionInformation and thus IDmbProvider.RevisionInformation can be delayed to after this if someone tries to start the server instantly after compiling
			//This is a terrible terrible hack to get around that
			//I'm sorry future me, I can't think of any other way to fix this other than giving DreamMaker an IDatabaseContext or having the controller load the CompileJob
			await Task.Delay(new TimeSpan(0, 0, 10), cancellationToken).ConfigureAwait(false);
			return result;
		}

		/// <inheritdoc />
		public Task OnNewerDmb()
		{
			lock (this)
				return newerDmbTcs.Task;
		}

		/// <inheritdoc />
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			var cj = await databaseContext.CompileJobs.OrderByDescending(x => x.Job.StoppedAt).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
			if (cj == default(CompileJob))
				return;
			LoadCompileJob(cj);
			//delete all other compile jobs
			var directories = await ioManager.GetDirectories(".", cancellationToken).ConfigureAwait(false);
			await Task.WhenAll(directories.Where(x => x != cj.Job.ToString()).Select(x => ioManager.DeleteDirectory(x, cancellationToken))).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			using (cancellationToken.Register(() => cleanupCts.Cancel()))
				await cleanupTask.ConfigureAwait(false);
		}
	}
}
