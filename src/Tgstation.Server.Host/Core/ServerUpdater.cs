using System;
using System.IO;
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
	/// <inheritdoc />
	sealed class ServerUpdater : IServerUpdater, IServerUpdateExecutor
	{
		/// <summary>
		/// The <see cref="IGitHubService"/> for the <see cref="ServerUpdater"/>.
		/// </summary>
		readonly IGitHubService gitHubService;

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
		/// The <see cref="ILogger"/> for the <see cref="ServerUpdater"/>.
		/// </summary>
		readonly ILogger<ServerUpdater> logger;

		/// <summary>
		/// The <see cref="GeneralConfiguration"/> for the <see cref="ServerUpdater"/>.
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;

		/// <summary>
		/// The <see cref="UpdatesConfiguration"/> for the <see cref="ServerUpdater"/>.
		/// </summary>
		readonly UpdatesConfiguration updatesConfiguration;

		/// <summary>
		/// <see cref="ServerUpdateOperation"/> for an in-progress update operation.
		/// </summary>
		ServerUpdateOperation serverUpdateOperation;

		/// <summary>
		/// Initializes a new instance of the <see cref="ServerUpdater"/> class.
		/// </summary>
		/// <param name="gitHubService">The value of <see cref="gitHubService"/>.</param>
		/// <param name="ioManager">The value of <see cref="ioManager"/>.</param>
		/// <param name="fileDownloader">The value of <see cref="fileDownloader"/>.</param>
		/// <param name="serverControl">The value of <see cref="serverControl"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="generalConfiguration"/>.</param>
		/// <param name="updatesConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="updatesConfiguration"/>.</param>
		public ServerUpdater(
			IGitHubService gitHubService,
			IIOManager ioManager,
			IFileDownloader fileDownloader,
			IServerControl serverControl,
			ILogger<ServerUpdater> logger,
			IOptions<GeneralConfiguration> generalConfigurationOptions,
			IOptions<UpdatesConfiguration> updatesConfigurationOptions)
		{
			this.gitHubService = gitHubService ?? throw new ArgumentNullException(nameof(gitHubService));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.fileDownloader = fileDownloader ?? throw new ArgumentNullException(nameof(fileDownloader));
			this.serverControl = serverControl ?? throw new ArgumentNullException(nameof(serverControl));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
			updatesConfiguration = updatesConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(updatesConfigurationOptions));
		}

		/// <inheritdoc />
		public async Task<ServerUpdateResult> BeginUpdate(ISwarmService swarmService, Version version, CancellationToken cancellationToken)
		{
			if (swarmService == null)
				throw new ArgumentNullException(nameof(swarmService));

			if (version == null)
				throw new ArgumentNullException(nameof(version));

			if (!swarmService.ExpectedNumberOfNodesConnected)
				return ServerUpdateResult.SwarmIntegrityCheckFailed;

			return await BeginUpdateImpl(swarmService, version, false, cancellationToken);
		}

		/// <inheritdoc />
		public async Task<bool> ExecuteUpdate(string updatePath, CancellationToken cancellationToken, CancellationToken criticalCancellationToken)
		{
			if (updatePath == null)
				throw new ArgumentNullException(nameof(updatePath));

			var serverUpdateOperation = this.serverUpdateOperation;
			if (serverUpdateOperation == null)
				throw new InvalidOperationException($"{nameof(serverUpdateOperation)} was null!");

			logger.LogInformation(
				"Updating server to version {version} ({zipUrl})...",
				serverUpdateOperation.TargetVersion,
				serverUpdateOperation.UpdateZipUrl);

			var inMustCommitUpdate = false;
			try
			{
				var updatePrepareResult = await serverUpdateOperation.SwarmService.PrepareUpdate(serverUpdateOperation.TargetVersion, cancellationToken);
				if (!updatePrepareResult)
					return false;

				async Task TryAbort(Exception ex)
				{
					try
					{
						await serverUpdateOperation.SwarmService.AbortUpdate(cancellationToken);
					}
					catch (Exception e2)
					{
						throw new AggregateException(ex, e2);
					}
				}

				var stagingDirectory = $"{updatePath}-stage";
				var updateZipData = new MemoryStream();
				try
				{
					logger.LogTrace("Downloading zip package...");
					var bearerToken = generalConfiguration.GitHubAccessToken;
					if (String.IsNullOrWhiteSpace(bearerToken))
						bearerToken = null;

					await using var download = await fileDownloader.DownloadFile(serverUpdateOperation.UpdateZipUrl, bearerToken, cancellationToken);
					await download.CopyToAsync(updateZipData, cancellationToken);
				}
				catch (Exception ex)
				{
					await TryAbort(ex);
					await updateZipData.DisposeAsync();
					throw;
				}

				try
				{
					try
					{
						await using (updateZipData)
						{
							logger.LogTrace("Extracting zip package to {stagingDirectory}...", stagingDirectory);
							await ioManager.DeleteDirectory(stagingDirectory, cancellationToken);
							await ioManager.ZipToDirectory(stagingDirectory, updateZipData, cancellationToken);
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
					logger.LogTrace("Moving {stagingDirectory} to {updateDirectory}", stagingDirectory, updatePath);
					await ioManager.MoveDirectory(stagingDirectory, updatePath, criticalCancellationToken);
				}
				catch (Exception e)
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
		/// Start the process of downloading and applying an update to a new server version. Doesn't perform argument checking.
		/// </summary>
		/// <param name="swarmService">The <see cref="ISwarmService"/> to use to coordinate the update.</param>
		/// <param name="newVersion">The TGS <see cref="Version"/> to update to.</param>
		/// <param name="recursed">If this is a recursive call.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="ServerUpdateResult"/>.</returns>
		async Task<ServerUpdateResult> BeginUpdateImpl(ISwarmService swarmService, Version newVersion, bool recursed, CancellationToken cancellationToken)
		{
			logger.LogDebug("Looking for GitHub releases version {version}...", newVersion);

			var releases = await gitHubService.GetTgsReleases(cancellationToken);
			foreach (var kvp in releases)
			{
				var version = kvp.Key;
				var release = kvp.Value;
				if (version == newVersion)
				{
					var asset = release.Assets.Where(x => x.Name.Equals(updatesConfiguration.UpdatePackageAssetName, StringComparison.Ordinal)).FirstOrDefault();
					if (asset == default)
						continue;

					serverUpdateOperation = new ServerUpdateOperation
					{
						TargetVersion = version,
						UpdateZipUrl = new Uri(asset.BrowserDownloadUrl),
						SwarmService = swarmService,
					};

					try
					{
						if (!serverControl.TryStartUpdate(this, version))
							return ServerUpdateResult.UpdateInProgress;
					}
					finally
					{
						serverUpdateOperation = null;
					}

					return ServerUpdateResult.Started;
				}
			}

			if (!recursed)
			{
				logger.LogWarning("We didn't find the requested release, but GitHub has been known to just not give full results when querying all releases. We'll try one more time.");
				return await BeginUpdateImpl(swarmService, newVersion, true, cancellationToken);
			}

			return ServerUpdateResult.ReleaseMissing;
		}
	}
}
