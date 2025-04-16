using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Octokit;

using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Properties;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Utils;
using Tgstation.Server.Host.Utils.GitHub;

namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// Handles TGS version reporting, if enabled.
	/// </summary>
	sealed class VersionReportingService : BackgroundService
	{
		/// <summary>
		/// The <see cref="IGitHubClientFactory"/> for the <see cref="VersionReportingService"/>.
		/// </summary>
		readonly IGitHubClientFactory gitHubClientFactory;

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="VersionReportingService"/>.
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="IAsyncDelayer"/> for the <see cref="VersionReportingService"/>.
		/// </summary>
		readonly IAsyncDelayer asyncDelayer;

		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="VersionReportingService"/>.
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="VersionReportingService"/>.
		/// </summary>
		readonly ILogger<VersionReportingService> logger;

		/// <summary>
		/// The <see cref="TelemetryConfiguration"/> for the <see cref="VersionReportingService"/>.
		/// </summary>
		readonly TelemetryConfiguration telemetryConfiguration;

		/// <summary>
		/// The <see cref="CancellationToken"/> passed to <see cref="StopAsync(CancellationToken)"/>.
		/// </summary>
		CancellationToken shutdownCancellationToken;

		/// <summary>
		/// Initializes a new instance of the <see cref="VersionReportingService"/> class.
		/// </summary>
		/// <param name="gitHubClientFactory">The value of <see cref="gitHubClientFactory"/>.</param>
		/// <param name="ioManager">The value of <see cref="ioManager"/>.</param>
		/// <param name="asyncDelayer">The value of <see cref="asyncDelayer"/>.</param>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/>.</param>
		/// <param name="telemetryConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="telemetryConfiguration"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public VersionReportingService(
			IGitHubClientFactory gitHubClientFactory,
			IIOManager ioManager,
			IAsyncDelayer asyncDelayer,
			IAssemblyInformationProvider assemblyInformationProvider,
			IOptions<TelemetryConfiguration> telemetryConfigurationOptions,
			ILogger<VersionReportingService> logger)
		{
			this.gitHubClientFactory = gitHubClientFactory ?? throw new ArgumentNullException(nameof(gitHubClientFactory));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.asyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
			telemetryConfiguration = telemetryConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(telemetryConfigurationOptions));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public override Task StopAsync(CancellationToken cancellationToken)
		{
			shutdownCancellationToken = cancellationToken;
			return base.StopAsync(cancellationToken);
		}

		/// <inheritdoc />
		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			if (telemetryConfiguration.DisableVersionReporting)
			{
				logger.LogDebug("Version telemetry disabled");
				return;
			}

			if (!telemetryConfiguration.VersionReportingRepositoryId.HasValue)
			{
				logger.LogError("Version reporting repository is misconfigured. Telemetry cannot be sent!");
				return;
			}

			var attribute = TelemetryAppSerializedKeyAttribute.Instance;
			if (attribute == null)
			{
				logger.LogDebug("TGS build configuration does not allow for version telemetry");
				return;
			}

			logger.LogDebug("Starting...");

			try
			{
				var telemetryIdDirectory = ioManager.GetPathInLocalDirectory(assemblyInformationProvider);
				var telemetryIdFile = ioManager.ResolvePath(
					ioManager.ConcatPath(
						telemetryIdDirectory,
						"telemetry.id"));

				Guid telemetryId;
				if (!await ioManager.FileExists(telemetryIdFile, stoppingToken))
				{
					telemetryId = Guid.NewGuid();
					await ioManager.CreateDirectory(telemetryIdDirectory, stoppingToken);
					await ioManager.WriteAllBytes(telemetryIdFile, Encoding.UTF8.GetBytes(telemetryId.ToString()), stoppingToken);
					logger.LogInformation("Generated telemetry ID {telemetryId} and wrote to {file}", telemetryId, telemetryIdFile);
				}
				else
				{
					var contents = await ioManager.ReadAllBytes(telemetryIdFile, stoppingToken);

					string guidStr;
					try
					{
						guidStr = Encoding.UTF8.GetString(contents.Span);
					}
					catch (Exception ex)
					{
						logger.LogError(ex, "Cannot decode telemetry ID from installation file ({path}). Telemetry will not be sent!", telemetryIdFile);
						return;
					}

					if (!Guid.TryParse(guidStr, out telemetryId))
					{
						logger.LogError("Cannot parse telemetry ID from installation file ({path}). Telemetry will not be sent!", telemetryIdFile);
						return;
					}
				}

				try
				{
					while (!stoppingToken.IsCancellationRequested)
					{
						var nextDelayHours = await TryReportVersion(
							telemetryId,
							attribute.SerializedKey,
							telemetryConfiguration.VersionReportingRepositoryId.Value,
							false,
							stoppingToken)
							? 24
							: 1;

						logger.LogDebug("Next version report in {hours} hours", nextDelayHours);
						await asyncDelayer.Delay(TimeSpan.FromHours(nextDelayHours), stoppingToken);
					}
				}
				catch (OperationCanceledException ex)
				{
					logger.LogTrace(ex, "Inner cancellation");
				}

				shutdownCancellationToken.ThrowIfCancellationRequested();

				logger.LogDebug("Sending shutdown telemetry");
				await TryReportVersion(
					telemetryId,
					attribute.SerializedKey,
					telemetryConfiguration.VersionReportingRepositoryId.Value,
					true,
					shutdownCancellationToken);
			}
			catch (OperationCanceledException ex)
			{
				logger.LogTrace(ex, "Exiting due to outer cancellation...");
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Crashed!");
			}
		}

		/// <summary>
		/// Make an attempt to report the current <see cref="IAssemblyInformationProvider.Version"/> to the configured GitHub repository.
		/// </summary>
		/// <param name="telemetryId">The telemetry <see cref="Guid"/> for the installation.</param>
		/// <param name="serializedPem">The serialized authentication <see cref="string"/> for the <see cref="gitHubClientFactory"/>.</param>
		/// <param name="repositoryId">The ID of the repository to send telemetry to.</param>
		/// <param name="shutdown">If this is shutdown telemetry.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in <see langword="true"/> if telemetry was reported successfully, <see langword="false"/> otherwise.</returns>
		async ValueTask<bool> TryReportVersion(Guid telemetryId, string serializedPem, long repositoryId, bool shutdown, CancellationToken cancellationToken)
		{
			logger.LogDebug("Sending version telemetry...");

			var serverFriendlyName = telemetryConfiguration.ServerFriendlyName;
			if (String.IsNullOrWhiteSpace(serverFriendlyName))
				serverFriendlyName = null;

			logger.LogTrace(
				 "Repository ID: {repoId}, Server friendly name: {friendlyName}",
				 repositoryId,
				 serverFriendlyName == null
					? "(null)"
					: $"\"{serverFriendlyName}\"");
			try
			{
				var gitHubClient = await gitHubClientFactory.CreateClientForRepository(
					serializedPem,
					new RepositoryIdentifier(repositoryId),
					cancellationToken);

				if (gitHubClient == null)
				{
					logger.LogWarning("Could not create GitHub client to connect to repository ID {repoId}!", repositoryId);
					return false;
				}

				// remove this lookup once https://github.com/octokit/octokit.net/pull/2960 is merged and released
				var repository = await gitHubClient.Repository.Get(repositoryId);

				logger.LogTrace("Repository ID {id} resolved to {owner}/{name}", repositoryId, repository.Owner.Name, repository.Name);

				var inputs = new Dictionary<string, object>
				{
					{ "telemetry_id", telemetryId.ToString() },
					{ "tgs_semver", assemblyInformationProvider.Version.Semver().ToString() },
					{ "shutdown", shutdown ? "true" : "false" },
				};

				if (serverFriendlyName != null)
					inputs.Add("server_friendly_name", serverFriendlyName);

				await gitHubClient.Actions.Workflows.CreateDispatch(
					repository.Owner.Login,
					repository.Name,
					".github/workflows/tgs_deployments_telemetry.yml",
					new CreateWorkflowDispatch("main")
					{
						Inputs = inputs,
					});

				logger.LogTrace("Telemetry sent successfully");

				return true;
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Failed to report version!");
				return false;
			}
		}
	}
}
