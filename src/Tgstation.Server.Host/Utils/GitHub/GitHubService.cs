using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Octokit;

using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Extensions;

namespace Tgstation.Server.Host.Utils.GitHub
{
	/// <summary>
	/// Service for interacting with the GitHub API. Authenticated or otherwise.
	/// </summary>
	sealed class GitHubService : IAuthenticatedGitHubService
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
		public async Task<string> CreateOAuthAccessToken(OAuthConfiguration oAuthConfiguration, string code, CancellationToken cancellationToken)
		{
			if (oAuthConfiguration == null)
				throw new ArgumentNullException(nameof(oAuthConfiguration));

			if (code == null)
				throw new ArgumentNullException(nameof(code));

			logger.LogTrace("CreateOAuthAccessToken");

			var response = await gitHubClient
				.Oauth
				.CreateAccessToken(
					new OauthTokenRequest(
						oAuthConfiguration.ClientId,
						oAuthConfiguration.ClientSecret,
						code)
					{
						RedirectUri = oAuthConfiguration.RedirectUrl,
					})
				.WithToken(cancellationToken);

			var token = response.AccessToken;
			return token;
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
					.Where(release =>
					{
						if (!release.PublishedAt.HasValue)
						{
							logger.LogDebug("Release tag without PublishedAt: {releaseTag}", release.TagName);
							return false;
						}

						if (!release.TagName.StartsWith(updatesConfiguration.GitTagPrefix, StringComparison.InvariantCulture))
							return false;

						return true;
					})
					.GroupBy(release =>
					{
						if (!Version.TryParse(release.TagName.Replace(updatesConfiguration.GitTagPrefix, String.Empty, StringComparison.Ordinal), out var version))
						{
							logger.LogDebug("Unparsable release tag: {releaseTag}", release.TagName);
							return null;
						}

						return version;
					})
					.Where(grouping => grouping.Key != null)

					// GitHub can return the same result twice or some other nonsense
					.Select(grouping => Tuple.Create(grouping.Key, grouping.OrderBy(x => x.PublishedAt.Value).First()))
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

		/// <inheritdoc />
		public async Task<int> GetCurrentUserId(CancellationToken cancellationToken)
		{
			logger.LogTrace("CreateOAuthAccessToken");

			var userDetails = await gitHubClient.User.Current().WithToken(cancellationToken);
			return userDetails.Id;
		}

		/// <inheritdoc />
		public Task CommentOnIssue(string repoOwner, string repoName, string comment, int issueNumber, CancellationToken cancellationToken)
		{
			if (repoOwner == null)
				throw new ArgumentNullException(nameof(repoOwner));

			if (repoName == null)
				throw new ArgumentNullException(nameof(repoName));

			if (comment == null)
				throw new ArgumentNullException(nameof(comment));

			logger.LogTrace("CommentOnIssue");

			return gitHubClient
				.Issue
				.Comment
				.Create(
					repoOwner,
					repoName,
					issueNumber,
					comment)
				.WithToken(cancellationToken);
		}

		/// <inheritdoc />
		public async Task<long> GetRepositoryId(string repoOwner, string repoName, CancellationToken cancellationToken)
		{
			if (repoOwner == null)
				throw new ArgumentNullException(nameof(repoOwner));

			if (repoName == null)
				throw new ArgumentNullException(nameof(repoName));

			logger.LogTrace("GetRepositoryId");

			var repo = await gitHubClient
				.Repository
				.Get(
					repoOwner,
					repoName)
				.WithToken(cancellationToken);

			return repo.Id;
		}

		/// <inheritdoc />
		public async Task<int> CreateDeployment(NewDeployment newDeployment, string repoOwner, string repoName, CancellationToken cancellationToken)
		{
			if (newDeployment == null)
				throw new ArgumentNullException(nameof(newDeployment));

			if (repoOwner == null)
				throw new ArgumentNullException(nameof(repoOwner));

			if (repoName == null)
				throw new ArgumentNullException(nameof(repoName));

			logger.LogTrace("CreateDeployment");

			var deployment = await gitHubClient
				.Repository
				.Deployment
				.Create(
					repoOwner,
					repoName,
					newDeployment)
				.WithToken(cancellationToken);

			return deployment.Id;
		}

		/// <inheritdoc />
		public Task CreateDeploymentStatus(NewDeploymentStatus newDeploymentStatus, string repoOwner, string repoName, int deploymentId, CancellationToken cancellationToken)
		{
			if (newDeploymentStatus == null)
				throw new ArgumentNullException(nameof(newDeploymentStatus));

			if (repoOwner == null)
				throw new ArgumentNullException(nameof(repoOwner));

			if (repoName == null)
				throw new ArgumentNullException(nameof(repoName));

			logger.LogTrace("CreateDeploymentStatus");
			return gitHubClient
				.Repository
				.Deployment
				.Status
				.Create(
					repoOwner,
					repoName,
					deploymentId,
					newDeploymentStatus)
				.WithToken(cancellationToken);
		}

		/// <inheritdoc />
		public Task CreateDeploymentStatus(NewDeploymentStatus newDeploymentStatus, long repoId, int deploymentId, CancellationToken cancellationToken)
		{
			if (newDeploymentStatus == null)
				throw new ArgumentNullException(nameof(newDeploymentStatus));

			logger.LogTrace("CreateDeploymentStatus");
			return gitHubClient
				.Repository
				.Deployment
				.Status
				.Create(
					repoId,
					deploymentId,
					newDeploymentStatus)
				.WithToken(cancellationToken);
		}

		/// <inheritdoc />
		public Task<PullRequest> GetPullRequest(string repoOwner, string repoName, int pullRequestNumber, CancellationToken cancellationToken)
		{
			if (repoOwner == null)
				throw new ArgumentNullException(nameof(repoOwner));

			if (repoName == null)
				throw new ArgumentNullException(nameof(repoName));

			logger.LogTrace("GetPullRequest");
			return gitHubClient
				.Repository
				.PullRequest
				.Get(
					repoOwner,
					repoName,
					pullRequestNumber)
				.WithToken(cancellationToken);
		}
	}
}
