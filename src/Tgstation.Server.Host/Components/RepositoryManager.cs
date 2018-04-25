using LibGit2Sharp;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Host.Components
{
	/// <inheritdoc />
	sealed class RepositoryManager : IRepositoryManager, IDisposable
	{
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="RepositoryManager"/>
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="RepositorySettings"/> for the <see cref="RepositoryManager"/>
		/// </summary>
		readonly RepositorySettings repositorySettings;

		/// <summary>
		/// Used for controlling single access to the <see cref="IRepository"/>
		/// </summary>
		readonly SemaphoreSlim semaphore;

		/// <summary>
		/// <see cref="CancellationTokenSource"/> for <see cref="currentTimerTask"/>
		/// </summary>
		CancellationTokenSource timerCancellationTokenSource;

		/// <summary>
		/// Represents the running update timer if any
		/// </summary>
		Task currentTimerTask;

		/// <summary>
		/// Construct a <see cref="RepositoryManager"/>
		/// </summary>
		/// <param name="repositorySettings">The value of <see cref="repositorySettings"/></param>
		/// <param name="ioManager">The value of <see cref="ioManager"/></param>
		public RepositoryManager(RepositorySettings repositorySettings, IIOManager ioManager)
		{
			this.repositorySettings = repositorySettings ?? throw new ArgumentNullException(nameof(repositorySettings));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			semaphore = new SemaphoreSlim(1);
		}

		/// <inheritdoc />
		public void Dispose()
		{
			timerCancellationTokenSource?.Dispose();
			semaphore.Dispose();
		}

		/// <summary>
		/// Stops <see cref="currentTimerTask"/> and joins it
		/// </summary>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task StopTimer()
		{
			if (currentTimerTask == null)
				return;
			timerCancellationTokenSource.Cancel();
			await currentTimerTask.ConfigureAwait(false);
			currentTimerTask = null;
		}
		
		/// <summary>
		/// Asyncronously fetch and reset the current branch for each given amount of <paramref name="minutes"/>
		/// </summary>
		/// <param name="minutes">The delay of the timer</param>
		/// <param name="accessString">The accessString to use for fetch operations</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task TimerLoop(int minutes, string accessString, CancellationToken cancellationToken)
		{
			try
			{
				while (true)
				{
					await Task.Delay(TimeSpan.FromMinutes(minutes), cancellationToken).ConfigureAwait(false);
					using (var repo = await LoadRepository(cancellationToken).ConfigureAwait(false))
					{
						//TODO: Find the unauthorized exception, catch it, and log it
						await repo.FetchOrigin(accessString, cancellationToken).ConfigureAwait(false);
						await repo.ResetToOrigin(cancellationToken).ConfigureAwait(false);
					}
				}
			}
			catch (OperationCanceledException) { }
		}

		/// <inheritdoc />
		public async Task<IRepository> CloneRepository(string url, string accessString, CancellationToken cancellationToken)
		{
			await ioManager.DeleteDirectory(".", cancellationToken).ConfigureAwait(false);

			await Task.Factory.StartNew(() =>
			{
				string path = null;
				try
				{
					path = LibGit2Sharp.Repository.Clone(Repository.GenerateAuthUrl(url, accessString), ioManager.ResolvePath("."), new CloneOptions
					{
						OnProgress = (a) => !cancellationToken.IsCancellationRequested,
						OnTransferProgress = (a) => !cancellationToken.IsCancellationRequested,
						RecurseSubmodules = true,
						OnUpdateTips = (a, b, c) => !cancellationToken.IsCancellationRequested,
						RepositoryOperationStarting = (a) => !cancellationToken.IsCancellationRequested
					});
				}
				catch (UserCancelledException) { }
				cancellationToken.ThrowIfCancellationRequested();
			}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);

			return await LoadRepository(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<IRepository> LoadRepository(CancellationToken cancellationToken)
		{
			await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
			LibGit2Sharp.Repository repo = null;
			await Task.Factory.StartNew(() =>
			{
				repo = new LibGit2Sharp.Repository(ioManager.ResolvePath("."));
			}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);
			return new Repository(repo, ioManager, () => semaphore.Release());
		}

		/// <inheritdoc />
		public async Task SetAutoUpdateInterval(int? newInterval)
		{
			await StopTimer().ConfigureAwait(false);
			if (!newInterval.HasValue)
				return;
			
			string accessString = null;

			if (timerCancellationTokenSource != null)
				timerCancellationTokenSource.Dispose();
			timerCancellationTokenSource = new CancellationTokenSource();

			currentTimerTask = TimerLoop(repositorySettings.AutoUpdateInterval.Value, accessString, timerCancellationTokenSource.Token);
		}

		/// <inheritdoc />
		public Task StartAsync(CancellationToken cancellationToken) => SetAutoUpdateInterval(repositorySettings.AutoUpdateInterval);

		/// <inheritdoc />
		public async Task StopAsync(CancellationToken cancellationToken)
		{
			var timerStopTask = StopTimer();
			var tcs = new TaskCompletionSource<object>();
			using (cancellationToken.Register(() => tcs.SetCanceled()))
				await Task.WhenAny(timerStopTask, tcs.Task).ConfigureAwait(false);
		}
	}
}
