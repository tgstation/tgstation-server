using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

		/// <summary>
		/// The <see cref="ICompileJobConsumer"/> for the <see cref="Instance"/>
		/// </summary>
		readonly ICompileJobConsumer compileJobConsumer;

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
		/// <param name="compileJobConsumer">The value of <see cref="compileJobConsumer"/></param>
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
			this.compileJobConsumer = compileJobConsumer ?? throw new ArgumentNullException(nameof(compileJobConsumer));
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			this.dmbFactory = dmbFactory ?? throw new ArgumentNullException(nameof(dmbFactory));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public void Dispose()
		{
			timerCts?.Dispose();
			compileJobConsumer.Dispose();
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

					string accessToken = null, projectName = null;
					int timeout = 0;
					var dbTask = databaseContextFactory.UseContext(async (db) =>
					{
						var instanceQuery = db.Instances.Where(x => x.Id == metadata.Id);
						var timeoutTask = instanceQuery.Select(x => x.DreamDaemonSettings.StartupTimeout).FirstAsync(cancellationToken);
						var projectNameTask = instanceQuery.Select(x => x.DreamMakerSettings.ProjectName).FirstOrDefaultAsync(cancellationToken);
						accessToken = await instanceQuery.Select(x => x.RepositorySettings.AccessToken).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
						projectName = await projectNameTask.ConfigureAwait(false);
						timeout = (await timeoutTask.ConfigureAwait(false)).Value;
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
						await repo.FetchOrigin(accessToken, null, cancellationToken).ConfigureAwait(false);
						await repo.ResetToOrigin(cancellationToken).ConfigureAwait(false);
						var job = await DreamMaker.Compile(projectName, timeout, repo, cancellationToken).ConfigureAwait(false);
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
		public Task StartAsync(CancellationToken cancellationToken) => Task.WhenAll(SetAutoUpdateInterval(metadata.AutoUpdateInterval), Configuration.StartAsync(cancellationToken), ByondManager.StartAsync(cancellationToken), Watchdog.StartAsync(cancellationToken), Chat.StartAsync(cancellationToken), compileJobConsumer.StartAsync(cancellationToken));

		/// <inheritdoc />
		public Task StopAsync(CancellationToken cancellationToken) => Task.WhenAll(SetAutoUpdateInterval(null), Configuration.StopAsync(cancellationToken), ByondManager.StopAsync(cancellationToken), Watchdog.StopAsync(cancellationToken), Chat.StopAsync(cancellationToken), compileJobConsumer.StopAsync(cancellationToken));

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
	}
}
