using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Octokit;

using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Extensions;

namespace Tgstation.Server.Host.Utils
{
	/// <inheritdoc />
	sealed class GitHubService : IGitHubService
	{
		/// <summary>
		/// The <see cref="IGitHubClient"/> for the <see cref="GitHubService"/>.
		/// </summary>
		readonly IGitHubClient gitHubClient;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="GitHubService"/>.
		/// </summary>
		readonly ILogger<GitHubService> logger;

		/// <summary>
		/// The <see cref="UpdatesConfiguration"/> for the <see cref="GitHubService"/>.
		/// </summary>
		readonly UpdatesConfiguration updatesConfiguration;

		/// <summary>
		/// Initializes a new instance of the <see cref="GitHubService"/> class.
		/// </summary>
		/// <param name="gitHubClient">The value of <see cref="gitHubClient"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="updatesConfiguration">The value of <see cref="updatesConfiguration"/>.</param>
		public GitHubService(IGitHubClient gitHubClient, ILogger<GitHubService> logger, UpdatesConfiguration updatesConfiguration)
		{
			this.gitHubClient = gitHubClient ?? throw new ArgumentNullException(nameof(gitHubClient));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.updatesConfiguration = updatesConfiguration ?? throw new ArgumentNullException(nameof(updatesConfiguration));
		}

		/// <inheritdoc />
		public async Task<Dictionary<Version, Release>> GetTgsReleases(CancellationToken cancellationToken)
		{
			logger.LogTrace("GetTgsReleases");
			var allReleases = await gitHubClient
				.Repository
				.Release
					.GetAll(updatesConfiguration.GitHubRepositoryId)
					.WithToken(cancellationToken);

			logger.LogTrace("{totalReleases} total releases", allReleases.Count);
			var releases = allReleases
					.Select(release =>
					{
						if (!release.TagName.StartsWith(updatesConfiguration.GitTagPrefix, StringComparison.InvariantCulture))
							return null;

						if (!Version.TryParse(release.TagName.Replace(updatesConfiguration.GitTagPrefix, String.Empty, StringComparison.Ordinal), out var version))
						{
							logger.LogDebug("Unparsable release tag: {releaseTag}", release.TagName);
							return null;
						}

						return Tuple.Create(version, release);
					})
					.Where(tuple => tuple != null)
					.ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2);

			logger.LogTrace("{parsedReleases} parsed releases", releases.Count);
			return releases;
		}

		/// <inheritdoc />
		public async Task<Uri> GetUpdatesRepositoryUrl(CancellationToken cancellationToken)
		{
			logger.LogTrace("GetUpdatesRepositoryUrl");
			var repository = await gitHubClient
				.Repository
					.Get(updatesConfiguration.GitHubRepositoryId)
					.WithToken(cancellationToken);

			var repoUrl = new Uri(repository.HtmlUrl);
			logger.LogTrace("Maps to {repostioryUrl}", repoUrl);

			return repoUrl;
		}
	}
}
