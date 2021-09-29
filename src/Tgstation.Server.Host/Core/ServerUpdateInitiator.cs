using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;

using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Core
{
	/// <inheritdoc />
	sealed class ServerUpdateInitiator : IServerUpdateInitiator
	{
		/// <summary>
		/// The <see cref="IGitHubClientFactory"/> for the <see cref="ServerUpdateInitiator"/>.
		/// </summary>
		readonly IGitHubClientFactory gitHubClientFactory;

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="ServerUpdateInitiator"/>.
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="IFileDownloader"/> for the <see cref="ServerUpdateInitiator"/>.
		/// </summary>
		readonly IFileDownloader fileDownloader;

		/// <summary>
		/// The <see cref="IServerControl"/> for the <see cref="ServerUpdateInitiator"/>.
		/// </summary>
		readonly IServerControl serverControl;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="ServerUpdateInitiator"/>.
		/// </summary>
		readonly ILogger<ServerUpdateInitiator> logger;

		/// <summary>
		/// The <see cref="UpdatesConfiguration"/> for the <see cref="ServerUpdateInitiator"/>.
		/// </summary>
		readonly UpdatesConfiguration updatesConfiguration;

		/// <summary>
		/// Initializes a new instance of the <see cref="ServerUpdateInitiator"/> class.
		/// </summary>
		/// <param name="gitHubClientFactory">The value of <see cref="gitHubClientFactory"/>.</param>
		/// <param name="ioManager">The value of <see cref="ioManager"/>.</param>
		/// <param name="fileDownloader">The value of <see cref="fileDownloader"/>.</param>
		/// <param name="serverControl">The value of <see cref="serverControl"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="updatesConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="updatesConfiguration"/>.</param>
		public ServerUpdateInitiator(
			IGitHubClientFactory gitHubClientFactory,
			IIOManager ioManager,
			IFileDownloader fileDownloader,
			IServerControl serverControl,
			ILogger<ServerUpdateInitiator> logger,
			IOptions<UpdatesConfiguration> updatesConfigurationOptions)
		{
			this.gitHubClientFactory = gitHubClientFactory ?? throw new ArgumentNullException(nameof(gitHubClientFactory));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.fileDownloader = fileDownloader ?? throw new ArgumentNullException(nameof(fileDownloader));
			this.serverControl = serverControl ?? throw new ArgumentNullException(nameof(serverControl));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			updatesConfiguration = updatesConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(updatesConfigurationOptions));
		}

		/// <inheritdoc />
		public async Task<ServerUpdateResult> BeginUpdate(Version newVersion, CancellationToken cancellationToken)
		{
			logger.LogDebug("Looking for GitHub releases version {0}...", newVersion);
			IEnumerable<Release> releases;
			var gitHubClient = gitHubClientFactory.CreateClient();
			releases = await gitHubClient
				.Repository
				.Release
				.GetAll(updatesConfiguration.GitHubRepositoryId)
				.WithToken(cancellationToken)
				.ConfigureAwait(false);

			releases = releases.Where(x => x.TagName.StartsWith(updatesConfiguration.GitTagPrefix, StringComparison.InvariantCulture));

			logger.LogTrace("Release query complete!");

			foreach (var release in releases)
				if (Version.TryParse(
					release.TagName.Replace(
						updatesConfiguration.GitTagPrefix, String.Empty, StringComparison.Ordinal),
					out var version)
					&& version == newVersion)
				{
					var asset = release.Assets.Where(x => x.Name.Equals(updatesConfiguration.UpdatePackageAssetName, StringComparison.Ordinal)).FirstOrDefault();
					if (asset == default)
						continue;

					if (!serverControl.ApplyUpdate(version, new Uri(asset.BrowserDownloadUrl), ioManager, fileDownloader))
						return ServerUpdateResult.UpdateInProgress;
					return ServerUpdateResult.Started;
				}

			return ServerUpdateResult.ReleaseMissing;
		}
	}
}
