using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Components.Byond;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Compiler;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Components.Watchdog;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components
{
	/// <inheritdoc />
	sealed class Instance : IInstance
	{
		/// <inheritdoc />
		public IRepositoryManager RepositoryManager { get; }

		/// <inheritdoc />
		public IByondManager ByondManager { get; }

		/// <inheritdoc />
		public IDreamMaker DreamMaker { get; }

		/// <inheritdoc />
		public IWatchdog Watchdog { get; }

		/// <inheritdoc />
		public IChat Chat { get; }

		/// <inheritdoc />
		public StaticFiles.IConfiguration Configuration { get; }

		/// <inheritdoc />
		public ICompileJobConsumer CompileJobConsumer { get; }

		/// <summary>
		/// The <see cref="IDatabaseContextFactory"/> for the <see cref="Instance"/>
		/// </summary>
		readonly IDatabaseContextFactory databaseContextFactory;

		/// <summary>
		/// The <see cref="IDmbFactory"/> for the <see cref="Instance"/>
		/// </summary>
		readonly IDmbFactory dmbFactory;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="Instance"/>
		/// </summary>
		readonly ILogger<Instance> logger;

		/// <summary>
		/// The <see cref="Api.Models.Instance"/> for the <see cref="Instance"/>
		/// </summary>
		readonly Api.Models.Instance metadata;

		/// <summary>
		/// The auto update <see cref="Task"/>
		/// </summary>
		Task timerTask;
		/// <summary>
		/// <see cref="CancellationTokenSource"/> for <see cref="timerTask"/>
		/// </summary>
		CancellationTokenSource timerCts;

		/// <summary>
		/// Construct an <see cref="Instance"/>
		/// </summary>
		/// <param name="metadata">The value of <see cref="metadata"/></param>
		/// <param name="repositoryManager">The value of <see cref="RepositoryManager"/></param>
		/// <param name="byondManager">The value of <see cref="ByondManager"/></param>
		/// <param name="dreamMaker">The value of <see cref="DreamMaker"/></param>
		/// <param name="watchdog">The value of <see cref="Watchdog"/></param>
		/// <param name="chat">The value of <see cref="Chat"/></param>
		/// <param name="configuration">The value of <see cref="Configuration"/></param>
		/// <param name="compileJobConsumer">The value of <see cref="CompileJobConsumer"/></param>
		/// <param name="databaseContextFactory">The value of <see cref="databaseContextFactory"/></param>
		/// <param name="dmbFactory">The value of <see cref="dmbFactory"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		public Instance(Api.Models.Instance metadata, IRepositoryManager repositoryManager, IByondManager byondManager, IDreamMaker dreamMaker, IWatchdog watchdog, IChat chat, StaticFiles.IConfiguration configuration, ICompileJobConsumer compileJobConsumer, IDatabaseContextFactory databaseContextFactory, IDmbFactory dmbFactory, ILogger<Instance> logger)
		{
			this.metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
			RepositoryManager = repositoryManager ?? throw new ArgumentNullException(nameof(repositoryManager));
			ByondManager = byondManager ?? throw new ArgumentNullException(nameof(byondManager));
			DreamMaker = dreamMaker ?? throw new ArgumentNullException(nameof(dreamMaker));
			Watchdog = watchdog ?? throw new ArgumentNullException(nameof(watchdog));
			Chat = chat ?? throw new ArgumentNullException(nameof(chat));
			Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
			CompileJobConsumer = compileJobConsumer ?? throw new ArgumentNullException(nameof(compileJobConsumer));
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.dmbFactory = dmbFactory ?? throw new ArgumentNullException(nameof(dmbFactory));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public void Dispose()
		{
			timerCts?.Dispose();
			CompileJobConsumer.Dispose();
			Configuration.Dispose();
			Chat.Dispose();
			Watchdog.Dispose();
			RepositoryManager.Dispose();
		}

		/// <summary>
		/// Pull the repository and compile for every set of given <paramref name="minutes"/>
		/// </summary>
		/// <param name="minutes">How many minutes the operation should repeat. Does not include running time</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task TimerLoop(int minutes, CancellationToken cancellationToken)
		{
			while (true)
				try
				{
					await Task.Delay(new TimeSpan(0, minutes, 0), cancellationToken).ConfigureAwait(false);

					RepositorySettings repositorySettings = null;
					string projectName = null;
					DreamDaemonSettings ddSettings = null;
					var dbTask = databaseContextFactory.UseContext(async (db) =>
					{
						var instanceQuery = db.Instances.Where(x => x.Id == metadata.Id);
						var ddSettingsTask = instanceQuery.Select(x => x.DreamDaemonSettings).Select(x => new DreamDaemonSettings
						{
							StartupTimeout = x.StartupTimeout,
							SecurityLevel = x.SecurityLevel
						}).FirstAsync(cancellationToken);
						var projectNameTask = instanceQuery.Select(x => x.DreamMakerSettings.ProjectName).FirstOrDefaultAsync(cancellationToken);
						repositorySettings = await instanceQuery.Select(x => x.RepositorySettings).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
						projectName = await projectNameTask.ConfigureAwait(false);
						ddSettings = await ddSettingsTask.ConfigureAwait(false);
					});
					using (var repo = await RepositoryManager.LoadRepository(cancellationToken).ConfigureAwait(false))
					{
						try
						{
							await dbTask.ConfigureAwait(false);
						}
						catch (OperationCanceledException)
						{
							throw;
						}
						catch (Exception e)
						{
							logger.LogWarning("Database error in auto update loop! Exception: {0}", e);
							continue;
						}
						if (repo == null)
							continue;
						
						await repo.FetchOrigin(repositorySettings.AccessUser, repositorySettings.AccessToken, null, cancellationToken).ConfigureAwait(false);

						var startSha = repo.Head;
						bool shouldSyncTracked;
						if (repositorySettings.AutoUpdatesKeepTestMerges.Value)
						{
							var result = await repo.MergeOrigin(repositorySettings.CommitterName, repositorySettings.CommitterEmail, cancellationToken).ConfigureAwait(false);
							if (!result.HasValue)
								continue;
							shouldSyncTracked = result.Value;
						}
						else
						{
							await repo.ResetToOrigin(cancellationToken).ConfigureAwait(false);
							shouldSyncTracked = true;
						}

						if (repositorySettings.AutoUpdatesSynchronize.Value && startSha != repo.Head)
							await repo.Sychronize(repositorySettings.AccessUser, repositorySettings.AccessToken, shouldSyncTracked, cancellationToken).ConfigureAwait(false);

						var job = await DreamMaker.Compile(projectName, ddSettings.SecurityLevel.Value, ddSettings.StartupTimeout.Value, repo, cancellationToken).ConfigureAwait(false);
					}
				}
				catch (OperationCanceledException) { }
				catch (Exception e)
				{
					logger.LogError("Error in auto update loop! Exception: {0}", e);
				}
		}

		/// <inheritdoc />
		public Api.Models.Instance GetMetadata() => metadata.CloneMetadata();

		/// <inheritdoc />
		public void Rename(string newName)
		{
			if (String.IsNullOrWhiteSpace(newName))
				throw new ArgumentNullException(nameof(newName));
			metadata.Name = newName;
		}

		/// <inheritdoc />
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			await Task.WhenAll(SetAutoUpdateInterval(metadata.AutoUpdateInterval), Configuration.StartAsync(cancellationToken), ByondManager.StartAsync(cancellationToken), Chat.StartAsync(cancellationToken), CompileJobConsumer.StartAsync(cancellationToken)).ConfigureAwait(false);

			//dependent on so many things, its just safer this way
			await Watchdog.StartAsync(cancellationToken).ConfigureAwait(false);

			CompileJob latestCompileJob = null;
			await databaseContextFactory.UseContext(async db =>
			{
				latestCompileJob = await db.CompileJobs.Where(x => x.Job.Instance.Id == metadata.Id && x.Job.ExceptionDetails == null).OrderByDescending(x => x.Job.StoppedAt).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
			}).ConfigureAwait(false);
			await dmbFactory.CleanUnusedCompileJobs(latestCompileJob, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public Task StopAsync(CancellationToken cancellationToken) => Task.WhenAll(SetAutoUpdateInterval(null), Configuration.StopAsync(cancellationToken), ByondManager.StopAsync(cancellationToken), Watchdog.StopAsync(cancellationToken), Chat.StopAsync(cancellationToken), CompileJobConsumer.StopAsync(cancellationToken));

		/// <inheritdoc />
		public async Task SetAutoUpdateInterval(int? newInterval)
		{
			Task toWait;
			lock (this)
			{
				if (timerTask != null)
				{
					timerCts.Cancel();
					toWait = timerTask;
				}
				else
					toWait = Task.CompletedTask;
			}
			await toWait.ConfigureAwait(false);
			if (!newInterval.HasValue)
				return;
			lock (this)
			{
				//race condition, just quit
				if (timerTask != null)
					return;
				timerCts?.Dispose();
				timerCts = new CancellationTokenSource();
				timerTask = TimerLoop(newInterval.Value, timerCts.Token);
			}
		}

		/// <inheritdoc />
		public CompileJob LatestCompileJob()
		{
			if (!dmbFactory.DmbAvailable)
				return null;
			using (var dmb = dmbFactory.LockNextDmb())
				return dmb.CompileJob;
		}
	}
}
