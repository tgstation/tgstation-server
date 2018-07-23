using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Standard <see cref="IDmbFactory"/>
	/// </summary>
	sealed class DmbFactory : IDmbFactory, ICompileJobConsumer
	{
		/// <inheritdoc />
		public Task OnNewerDmb
		{
			get
			{
				lock (this)
					return newerDmbTcs.Task;
			}
		}

		/// <summary>
		/// The <see cref="IDatabaseContextFactory"/> for the <see cref="DmbFactory"/>
		/// </summary>
		readonly IDatabaseContextFactory databaseContextFactory;
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="DmbFactory"/>
		/// </summary>
		readonly IIOManager ioManager;
		/// <summary>
		/// The <see cref="CancellationTokenSource"/> for <see cref="cleanupTask"/>
		/// </summary>
		readonly CancellationTokenSource cleanupCts;

		/// <summary>
		/// The <see cref="Api.Models.Instance.Id"/> the <see cref="DmbFactory"/> belongs to
		/// </summary>
		readonly long instanceId;
		
		/// <summary>
		/// <see cref="Task"/> representing calls to <see cref="CleanJob(CompileJob)"/>
		/// </summary>
		Task cleanupTask;
		/// <summary>
		/// <see cref="TaskCompletionSource{TResult}"/> resulting in the latest <see cref="DmbProvider"/> yet to exist
		/// </summary>
		TaskCompletionSource<IDmbProvider> newerDmbTcs;
		/// <summary>
		/// The latest <see cref="DmbProvider"/>
		/// </summary>
		IDmbProvider nextDmbProvider;

		Dictionary<long, int> jobLockCounts;

		/// <summary>
		/// Construct a <see cref="DmbFactory"/>
		/// </summary>
		/// <param name="databaseContextFactory">The value of <see cref="databaseContextFactory"/></param>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="instance">The <see cref="Models.Instance"/> used to populate <see cref="instanceId"/></param>
		public DmbFactory(IDatabaseContextFactory databaseContextFactory, IIOManager ioManager, Models.Instance instance)
		{
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			instanceId = instance?.Id ?? throw new ArgumentNullException(nameof(instance));

			cleanupCts = new CancellationTokenSource();
			jobLockCounts = new Dictionary<long, int>();
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
			{
				var currentVal = jobLockCounts[job.Id];
				if (--jobLockCounts[job.Id] == 0)
					cleanupTask = HandleCleanup();
			}
		}

		/// <inheritdoc />
		public Task LoadCompileJob(CompileJob job, CancellationToken cancellationToken) => LoadCompileJob(job, true, cancellationToken);

		async Task LoadCompileJob(CompileJob job, bool setAsStagedInDb, CancellationToken cancellationToken)
		{
			if (job == null)
				throw new ArgumentNullException(nameof(job));
			if (!job.DMApiValidated || job.Job.Cancelled || job.Job.ExceptionDetails != null || job.Job.StoppedAt == null)
				throw new InvalidOperationException("Cannot load incomplete compile job!");
			if (setAsStagedInDb)
				await databaseContextFactory.UseContext(async db =>
				{
					var ddsettings = new DreamDaemonSettings
					{
						InstanceId = instanceId
					};
					db.DreamDaemonSettings.Attach(ddsettings);
					ddsettings.StagedCompileJob = job;
					await db.Save(cancellationToken).ConfigureAwait(false);
				}).ConfigureAwait(false);
			lock (this)
			{
				var oldDmbProvider = nextDmbProvider;
				if (oldDmbProvider != null && oldDmbProvider.CompileJob.Job.StoppedAt < oldDmbProvider.CompileJob.Job.StoppedAt)
					throw new InvalidOperationException("Loaded compile job older than current job!");
				nextDmbProvider = FromCompileJob(job);
				newerDmbTcs.SetResult(nextDmbProvider);
				newerDmbTcs = new TaskCompletionSource<IDmbProvider>();
			}
		}

		/// <inheritdoc />
		public async Task<IDmbProvider> LockNextDmb(CancellationToken cancellationToken)
		{
			Task<IDmbProvider> task;
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
		public Task StartAsync(CancellationToken cancellationToken) => databaseContextFactory.UseContext(async (db) =>
		{
			//where complete clause not necessary, only successful COMPILEjobs get in the db
			var cj = await db.Instances.Where(x => x.Id == instanceId).SelectMany(x => x.CompileJobs).OrderByDescending(x => x.Job.StoppedAt).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
			if (cj == default(CompileJob))
				return;
			var directoriesTask = ioManager.GetDirectories(".", cancellationToken);
			var compileJobTask = LoadCompileJob(cj, false, cancellationToken);
			//delete all other compile jobs
			var directories = await directoriesTask.ConfigureAwait(false);
			await Task.WhenAll(directories.Where(x => x != cj.Job.ToString()).Select(x => ioManager.DeleteDirectory(x, cancellationToken))).ConfigureAwait(false);
			await compileJobTask.ConfigureAwait(false);
		});

		/// <inheritdoc />
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			using (cancellationToken.Register(() => cleanupCts.Cancel()))
				await cleanupTask.ConfigureAwait(false);
		}

		/// <inheritdoc />
		public IDmbProvider FromCompileJob(CompileJob compileJob)
		{
			lock (this)
			{
				if (!jobLockCounts.TryGetValue(compileJob.Id, out int value))
					jobLockCounts.Add(compileJob.Id, 1);
				else
					jobLockCounts[compileJob.Id] = ++value;
				return new DmbProvider(compileJob, ioManager, () => CleanJob(compileJob));
			}
		}
	}
}
