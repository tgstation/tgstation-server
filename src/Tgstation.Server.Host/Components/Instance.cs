using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Components.Watchdog;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Components
{
	/// <inheritdoc />
	sealed class Instance : IInstance, IDisposable
	{
		/// <inheritdoc />
		public IRepositoryManager RepositoryManager { get; }

		/// <inheritdoc />
		public IByond Byond { get; }

		/// <inheritdoc />
		public IDreamMaker DreamMaker { get; }

		/// <inheritdoc />
		public IWatchdog Watchdog { get; }

		/// <inheritdoc />
		public IChat Chat { get; }

		/// <inheritdoc />
		public IConfiguration Configuration { get; }
		
		/// <summary>
		/// The <see cref="ICompileJobConsumer"/> for the <see cref="Instance"/>
		/// </summary>
		readonly ICompileJobConsumer compileJobConsumer;

		/// <summary>
		/// The <see cref="IDatabaseContextFactory"/> for the <see cref="Instance"/>
		/// </summary>
		readonly IDatabaseContextFactory databaseContextFactory;
		
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

		public Instance(Api.Models.Instance metadata, IRepositoryManager repositoryManager, IByond byond, IDreamMaker dreamMaker, IWatchdog watchdog, IChat chat, IConfiguration configuration, ICompileJobConsumer compileJobConsumer, IDatabaseContextFactory databaseContextFactory)
		{
			this.metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
			RepositoryManager = repositoryManager ?? throw new ArgumentNullException(nameof(repositoryManager));
			Byond = byond ?? throw new ArgumentNullException(nameof(byond));
			DreamMaker = dreamMaker ?? throw new ArgumentNullException(nameof(dreamMaker));
			watchdog = watchdog ?? throw new ArgumentNullException(nameof(watchdog));
			Chat = chat ?? throw new ArgumentNullException(nameof(chat));
			Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
			this.compileJobConsumer = compileJobConsumer ?? throw new ArgumentNullException(nameof(compileJobConsumer));
			this.databaseContextFactory = databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
		}

		/// <inheritdoc />
		public void Dispose() => timerCts?.Dispose();

		/// <summary>
		/// Pull the repository and compile for every set of given <paramref name="minutes"/>
		/// </summary>
		/// <param name="minutes">How many minutes the operation should repeat. Does not include running time</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task TimerLoop(int minutes, CancellationToken cancellationToken)
		{
			try
			{
				while (true)
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
						await dbTask.ConfigureAwait(false);
						await repo.FetchOrigin(accessToken, cancellationToken).ConfigureAwait(false);
						await repo.ResetToOrigin(cancellationToken).ConfigureAwait(false);
						await DreamMaker.Compile(projectName, timeout, repo, cancellationToken).ConfigureAwait(false);
					}
				}
			}
			catch (OperationCanceledException) { }
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
		public Task StartAsync(CancellationToken cancellationToken) => Task.WhenAll(SetAutoUpdateInterval(metadata.AutoUpdateInterval), Watchdog.StartAsync(cancellationToken), Chat.StartAsync(cancellationToken), compileJobConsumer.StartAsync(cancellationToken));

		/// <inheritdoc />
		public Task StopAsync(CancellationToken cancellationToken) => Task.WhenAll(SetAutoUpdateInterval(null), Watchdog.StopAsync(cancellationToken), Chat.StopAsync(cancellationToken), compileJobConsumer.StopAsync(cancellationToken));

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
