using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Swarm;
using Tgstation.Server.Host.Utils.GitHub;

namespace Tgstation.Server.Host.Core
{
	/// <inheritdoc cref="IServerUpdater" />
	sealed class ServerUpdater : IServerUpdater, IServerUpdateExecutor
	{
		/// <summary>
		/// The <see cref="IGitHubServiceFactory"/> for the <see cref="ServerUpdater"/>.
		/// </summary>
		readonly IGitHubServiceFactory gitHubServiceFactory;

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="ServerUpdater"/>.
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="IFileDownloader"/> for the <see cref="ServerUpdater"/>.
		/// </summary>
		readonly IFileDownloader fileDownloader;

		/// <summary>
		/// The <see cref="IServerControl"/> for the <see cref="ServerUpdater"/>.
		/// </summary>
		readonly IServerControl serverControl;

		/// <summary>
		/// The <see cref="IOptionsMonitor{TOptions}"/> of <see cref="GeneralConfiguration"/> for the <see cref="ServerUpdater"/>.
		/// </summary>
		readonly IOptionsMonitor<GeneralConfiguration> generalConfigurationOptions;

		/// <summary>
		/// The <see cref="IOptionsMonitor{TOptions}"/> of <see cref="UpdatesConfiguration"/> for the <see cref="ServerUpdater"/>.
		/// </summary>
		readonly IOptionsMonitor<UpdatesConfiguration> updatesConfigurationOptions;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="ServerUpdater"/>.
		/// </summary>
		readonly ILogger<ServerUpdater> logger;

		/// <summary>
		/// Lock <see cref="object"/> used when initiating an update.
		/// </summary>
		readonly object updateInitiationLock;

		/// <summary>
		/// <see cref="ServerUpdateOperation"/> for an in-progress update operation.
		/// </summary>
		ServerUpdateOperation? serverUpdateOperation;

		/// <summary>
		/// Initializes a new instance of the <see cref="ServerUpdater"/> class.
		/// </summary>
		/// <param name="gitHubServiceFactory">The value of <see cref="gitHubServiceFactory"/>.</param>
		/// <param name="ioManager">The value of <see cref="ioManager"/>.</param>
		/// <param name="fileDownloader">The value of <see cref="fileDownloader"/>.</param>
		/// <param name="serverControl">The value of <see cref="serverControl"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="generalConfigurationOptions">The value of <see cref="generalConfigurationOptions"/>.</param>
		/// <param name="updatesConfigurationOptions">The value of <see cref="updatesConfigurationOptions"/>.</param>
		public ServerUpdater(
			IGitHubServiceFactory gitHubServiceFactory,
			IIOManager ioManager,
			IFileDownloader fileDownloader,
			IServerControl serverControl,
			ILogger<ServerUpdater> logger,
			IOptionsMonitor<GeneralConfiguration> generalConfigurationOptions,
			IOptionsMonitor<UpdatesConfiguration> updatesConfigurationOptions)
		{
			this.gitHubServiceFactory = gitHubServiceFactory ?? throw new ArgumentNullException(nameof(gitHubServiceFactory));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.fileDownloader = fileDownloader ?? throw new ArgumentNullException(nameof(fileDownloader));
			this.serverControl = serverControl ?? throw new ArgumentNullException(nameof(serverControl));
			this.generalConfigurationOptions = generalConfigurationOptions ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
			this.updatesConfigurationOptions = updatesConfigurationOptions ?? throw new ArgumentNullException(nameof(updatesConfigurationOptions));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

			updateInitiationLock = new object();
		}

		/// <inheritdoc />
		public async ValueTask<ServerUpdateResult> BeginUpdate(ISwarmService swarmService, IFileStreamProvider? fileStreamProvider, Version version, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(swarmService);

			ArgumentNullException.ThrowIfNull(version);

			if (!swarmService.ExpectedNumberOfNodesConnected)
				return ServerUpdateResult.SwarmIntegrityCheckFailed;

			return await BeginUpdateImpl(swarmService, fileStreamProvider, version, false, cancellationToken);
		}

		/// <inheritdoc />
		public async ValueTask<bool> ExecuteUpdate(string updatePath, CancellationToken cancellationToken, CancellationToken criticalCancellationToken)
		{
			ArgumentNullException.ThrowIfNull(updatePath);

			var serverUpdateOperation = this.serverUpdateOperation;
			if (serverUpdateOperation == null)
				throw new InvalidOperationException($"{nameof(serverUpdateOperation)} was null!");

			var inMustCommitUpdate = false;
			try
			{
				var stagingDirectory = $"{updatePath}-stage";
				var tuple = await PrepareUpdateClearStagingAndBufferStream(stagingDirectory, cancellationToken);
				if (tuple == null)
					return false;

				await using var bufferedStream = tuple.Item1;
				var needStreamUntilCommit = tuple.Item2;
				var createdStagingDirectory = false;
				try
				{
					try
					{
						logger.LogTrace("Extracting zip package to {stagingDirectory}...", stagingDirectory);
						var updateZipData = await bufferedStream.GetResult(cancellationToken);

						createdStagingDirectory = true;
						await ioManager.ZipToDirectory(stagingDirectory, updateZipData, cancellationToken);

						if (!needStreamUntilCommit)
						{
							logger.LogTrace("Early disposing update stream provider...");
							await bufferedStream.DisposeAsync(); // don't leave this in memory
						}
					}
					catch (Exception ex)
					{
						await TryAbort(ex);
						throw;
					}

					var updateCommitResult = await serverUpdateOperation.SwarmService.CommitUpdate(criticalCancellationToken);
					if (updateCommitResult == SwarmCommitResult.AbortUpdate)
					{
						logger.LogError("Swarm distributed commit failed, not applying update!");
						return false;
					}

					inMustCommitUpdate = updateCommitResult == SwarmCommitResult.MustCommitUpdate;
					logger.LogTrace("Moving {stagingDirectory} to {updateDirectory}...", stagingDirectory, updatePath);
					await ioManager.MoveDirectory(stagingDirectory, updatePath, criticalCancellationToken);
				}
				catch (Exception e) when (createdStagingDirectory)
				{
					try
					{
						// important to not leave this directory around if possible
						await ioManager.DeleteDirectory(stagingDirectory, default);
					}
					catch (Exception e2)
					{
						throw new AggregateException(e, e2);
					}

					throw;
				}

				return true;
			}
			catch (OperationCanceledException) when (!inMustCommitUpdate)
			{
				logger.LogInformation("Server update cancelled!");
			}
			catch (Exception ex) when (!inMustCommitUpdate)
			{
				logger.LogError(ex, "Error updating server!");
			}
			catch (Exception ex) when (inMustCommitUpdate)
			{
				logger.LogCritical(ex, "Could not complete committed swarm update!");
			}

			return false;
		}

		/// <summary>
		/// Attempt to abort a prepared swarm update.
		/// </summary>
		/// <param name="exception">The <see cref="Exception"/> being thrown.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		/// <exception cref="AggregateException">A new <see cref="AggregateException"/> containing <paramref name="exception"/> and the swarm abort <see cref="Exception"/> if thrown.</exception>
		/// <remarks>Requires <see cref="serverUpdateOperation"/> to be populated.</remarks>
		async ValueTask TryAbort(Exception exception)
		{
			try
			{
				await serverUpdateOperation!.SwarmService.AbortUpdate();
			}
			catch (Exception e2)
			{
				throw new AggregateException(exception, e2);
			}
		}

		/// <summary>
		/// Prepares the swarm update, deletes the <paramref name="stagingDirectory"/>, and buffers the <see cref="ServerUpdateOperation.FileStreamProvider"/>.
		/// </summary>
		/// <param name="stagingDirectory">The directory the server update is initially extracted to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in <see cref="Tuple{T1, T2}"/> containing a new <see cref="BufferedFileStreamProvider"/> based on the <see cref="ServerUpdateOperation.FileStreamProvider"/> of <see cref="serverUpdateOperation"/> and <see langword="true"/> if it needs to be kept active until the swarm commit. If <see langword="null"/>, the update failed to prepare.</returns>
		/// <remarks>Requires <see cref="serverUpdateOperation"/> to be populated.</remarks>
		async ValueTask<Tuple<BufferedFileStreamProvider, bool>?> PrepareUpdateClearStagingAndBufferStream(string stagingDirectory, CancellationToken cancellationToken)
		{
			await using var fileStreamProvider = serverUpdateOperation!.FileStreamProvider;

			var bufferedStream = new BufferedFileStreamProvider(
				await fileStreamProvider.GetResult(cancellationToken));
			try
			{
				var updatePrepareResult = await serverUpdateOperation.SwarmService.PrepareUpdate(
					bufferedStream,
					serverUpdateOperation.TargetVersion,
					cancellationToken);
				if (updatePrepareResult == SwarmPrepareResult.Failure)
				{
					await bufferedStream.DisposeAsync();
					return null;
				}

				try
				{
					// simply buffer the result at this point
					var bufferingTask = bufferedStream.EnsureBuffered(cancellationToken);

					// clear out the staging directory first
					await ioManager.DeleteDirectory(stagingDirectory, cancellationToken);

					// Dispose warning avoidance
					var result = Tuple.Create(
						bufferedStream,
						updatePrepareResult == SwarmPrepareResult.SuccessHoldProviderUntilCommit);

					await bufferingTask;
					bufferedStream = null;

					return result;
				}
				catch (Exception ex)
				{
					await TryAbort(ex);
					throw;
				}
			}
			finally
			{
				if (bufferedStream != null)
					await bufferedStream.DisposeAsync();
			}
		}

		/// <summary>
		/// Start the process of downloading and applying an update to a new server version. Doesn't perform argument checking.
		/// </summary>
		/// <param name="swarmService">The <see cref="ISwarmService"/> to use to coordinate the update.</param>
		/// <param name="fileStreamProvider">The optional <see cref="IFileStreamProvider"/> used to retrieve the target server version. If not provided, GitHub will be used.</param>
		/// <param name="newVersion">The TGS <see cref="Version"/> to update to.</param>
		/// <param name="recursed">If this is a recursive call.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="ServerUpdateResult"/>.</returns>
		async ValueTask<ServerUpdateResult> BeginUpdateImpl(
			ISwarmService swarmService,
			IFileStreamProvider? fileStreamProvider,
			Version newVersion,
			bool recursed,
			CancellationToken cancellationToken)
		{
			ServerUpdateOperation? ourUpdateOperation = null;
			try
			{
				if (fileStreamProvider == null)
				{
					logger.LogDebug("Looking for GitHub releases version {version}...", newVersion);

					var gitHubService = await gitHubServiceFactory.CreateService(cancellationToken);
					var releases = await gitHubService.GetTgsReleases(cancellationToken);

					var updatesConfiguration = updatesConfigurationOptions.CurrentValue;
					var generalConfiguration = generalConfigurationOptions.CurrentValue;
					foreach (var kvp in releases)
					{
						var version = kvp.Key;
						var release = kvp.Value;
						if (version == newVersion)
						{
							var asset = release.Assets.Where(x => x.Name.Equals(updatesConfiguration.UpdatePackageAssetName, StringComparison.Ordinal)).FirstOrDefault();
							if (asset == default)
								continue;

							logger.LogTrace("Creating download provider for {assetName}...", updatesConfiguration.UpdatePackageAssetName);
							var bearerToken = generalConfiguration.GitHubAccessToken;
							if (String.IsNullOrWhiteSpace(bearerToken))
								bearerToken = null;

							fileStreamProvider = fileDownloader.DownloadFile(new Uri(asset.BrowserDownloadUrl), bearerToken);
							break;
						}
					}

					if (fileStreamProvider == null)
					{
						if (!recursed)
						{
							logger.LogWarning("We didn't find the requested release, but GitHub has been known to just not give full results when querying all releases. We'll try one more time.");
							return await BeginUpdateImpl(swarmService, null, newVersion, true, cancellationToken);
						}

						return ServerUpdateResult.ReleaseMissing;
					}
				}

				lock (updateInitiationLock)
				{
					if (serverUpdateOperation == null)
					{
						ourUpdateOperation = new ServerUpdateOperation(
							fileStreamProvider,
							swarmService,
							newVersion);

						serverUpdateOperation = ourUpdateOperation;

						bool updateStarted = serverControl.TryStartUpdate(this, newVersion);
						if (updateStarted)
						{
							fileStreamProvider = null; // belongs to serverUpdateOperation now
							return ServerUpdateResult.Started;
						}
					}

					return ServerUpdateResult.UpdateInProgress;
				}
			}
			catch
			{
				lock (updateInitiationLock)
					if (serverUpdateOperation == ourUpdateOperation)
						serverUpdateOperation = null;

				throw;
			}
			finally
			{
				if (fileStreamProvider != null)
					await fileStreamProvider.DisposeAsync();
			}
		}
	}
}
