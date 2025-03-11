using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Octokit;

using Tgstation.Server.Host.Configuration;

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
		public async ValueTask<string> CreateOAuthAccessToken(OAuthConfiguration oAuthConfiguration, string code, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(oAuthConfiguration);

			ArgumentNullException.ThrowIfNull(code);

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
				.WaitAsync(cancellationToken);

			var token = response.AccessToken;
			return token;
		}

		/// <inheritdoc />
		public async ValueTask<Dictionary<Version, Release>> GetTgsReleases(CancellationToken cancellationToken)
		{
			logger.LogTrace("GetTgsReleases");
			var allReleases = await gitHubClient
				.Repository
				.Release
					.GetAll(updatesConfiguration.GitHubRepositoryId)
					.WaitAsync(cancellationToken);

			var gitPrefix = updatesConfiguration.GitTagPrefix ?? String.Empty;

			logger.LogTrace("{totalReleases} total releases", allReleases.Count);
			var releases = allReleases!
				.Where(release =>
				{
					if (!release.PublishedAt.HasValue)
					{
						logger.LogDebug("Release tag without PublishedAt: {releaseTag}", release.TagName);
						return false;
					}

					if (!release.TagName.StartsWith(gitPrefix, StringComparison.InvariantCulture))
						return false;

					return true;
				})
				.GroupBy(release =>
				{
					if (!Version.TryParse(release.TagName.Replace(gitPrefix, String.Empty, StringComparison.Ordinal), out var version))
					{
						logger.LogDebug("Unparsable release tag: {releaseTag}", release.TagName);
						return null;
					}

					return version;
				})
				.Where(grouping => grouping.Key != null)

				// GitHub can return the same result twice or some other nonsense
				.Select(grouping => Tuple.Create(grouping.Key!, grouping.OrderBy(x => x.PublishedAt ?? DateTimeOffset.MinValue).First()))
				.ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2);

			logger.LogTrace("{parsedReleases} parsed releases", releases.Count);
			return releases;
		}

		/// <inheritdoc />
		public async ValueTask<Uri> GetUpdatesRepositoryUrl(CancellationToken cancellationToken)
		{
			logger.LogTrace("GetUpdatesRepositoryUrl");
			var repository = await gitHubClient
				.Repository
					.Get(updatesConfiguration.GitHubRepositoryId)
					.WaitAsync(cancellationToken);

			var repoUrl = new Uri(repository.HtmlUrl);
			logger.LogTrace("Maps to {repostioryUrl}", repoUrl);

			return repoUrl;
		}

		/// <inheritdoc />
		public async ValueTask<long> GetCurrentUserId(CancellationToken cancellationToken)
		{
			logger.LogTrace("CreateOAuthAccessToken");

			var userDetails = await gitHubClient.User.Current().WaitAsync(cancellationToken);
			return userDetails.Id;
		}

		/// <inheritdoc />
		public Task CommentOnIssue(string repoOwner, string repoName, string comment, int issueNumber, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(repoOwner);

			ArgumentNullException.ThrowIfNull(repoName);

			ArgumentNullException.ThrowIfNull(comment);

			logger.LogTrace("CommentOnIssue");

			return gitHubClient
				.Issue
				.Comment
				.Create(
					repoOwner,
					repoName,
					issueNumber,
					comment)
				.WaitAsync(cancellationToken);
		}

		/// <inheritdoc />
		public Task AppendCommentOnIssue(string repoOwner, string repoName, string comment, IssueComment issueComment, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(repoOwner);

			ArgumentNullException.ThrowIfNull(repoName);

			ArgumentNullException.ThrowIfNull(comment);

			ArgumentNullException.ThrowIfNull(issueComment);

			logger.LogTrace("AppendCommentOnIssue");

			return gitHubClient
				.Issue
				.Comment
				.Update(
					repoOwner,
					repoName,
					issueComment.Id,
					issueComment.Body + comment)
				.WaitAsync(cancellationToken);
		}

		/// <inheritdoc />
		public async ValueTask<IssueComment?> GetExistingCommentOnIssue(string repoOwner, string repoName, string header, int issueNumber, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(repoOwner);

			ArgumentNullException.ThrowIfNull(repoName);

			ArgumentNullException.ThrowIfNull(header);

			logger.LogTrace("GetExistingCommentOnIssue");

			var comments = await gitHubClient
				.Issue
				.Comment
				.GetAllForIssue(
					repoOwner,
					repoName,
					issueNumber)
				.WaitAsync(cancellationToken);
			if (comments == null)
			{
				return null;
			}

			long userId = await GetCurrentUserId(cancellationToken);

			for (int i = comments.Count - 1; i > -1; i--)
			{
				var currentComment = comments[i];
				if (currentComment.User?.Id == userId && (currentComment.Body?.StartsWith(header) ?? false))
				{
					if (currentComment.Body.Length > 250000)
					{ // Limit should be 262,143 so we'll leave a 12,143 buffer
						return null;
					}

					return currentComment;
				}
			}

			return null;
		}

		/// <inheritdoc />
		public async ValueTask<long> GetRepositoryId(string repoOwner, string repoName, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(repoOwner);

			ArgumentNullException.ThrowIfNull(repoName);

			logger.LogTrace("GetRepositoryId");

			var repo = await gitHubClient
				.Repository
				.Get(
					repoOwner,
					repoName)
				.WaitAsync(cancellationToken);

			return repo.Id;
		}

		/// <inheritdoc />
		public async ValueTask<long> CreateDeployment(NewDeployment newDeployment, string repoOwner, string repoName, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(newDeployment);

			ArgumentNullException.ThrowIfNull(repoOwner);

			ArgumentNullException.ThrowIfNull(repoName);

			logger.LogTrace("CreateDeployment");

			var deployment = await gitHubClient
				.Repository
				.Deployment
				.Create(
					repoOwner,
					repoName,
					newDeployment)
				.WaitAsync(cancellationToken);

			return deployment.Id;
		}

		/// <inheritdoc />
		public Task CreateDeploymentStatus(NewDeploymentStatus newDeploymentStatus, string repoOwner, string repoName, long deploymentId, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(newDeploymentStatus);

			ArgumentNullException.ThrowIfNull(repoOwner);

			ArgumentNullException.ThrowIfNull(repoName);

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
				.WaitAsync(cancellationToken);
		}

		/// <inheritdoc />
		public Task CreateDeploymentStatus(NewDeploymentStatus newDeploymentStatus, long repoId, long deploymentId, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(newDeploymentStatus);

			logger.LogTrace("CreateDeploymentStatus");
			return gitHubClient
				.Repository
				.Deployment
				.Status
				.Create(
					repoId,
					deploymentId,
					newDeploymentStatus)
				.WaitAsync(cancellationToken);
		}

		/// <inheritdoc />
		public Task<PullRequest> GetPullRequest(string repoOwner, string repoName, int pullRequestNumber, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(repoOwner);

			ArgumentNullException.ThrowIfNull(repoName);

			logger.LogTrace("GetPullRequest");
			return gitHubClient
				.Repository
				.PullRequest
				.Get(
					repoOwner,
					repoName,
					pullRequestNumber)
				.WaitAsync(cancellationToken);
		}

		/// <inheritdoc />
		public Task<GitHubCommit> GetCommit(string repoOwner, string repoName, string committish, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(repoOwner);

			ArgumentNullException.ThrowIfNull(repoName);

			logger.LogTrace("GetPulGetCommitlRequest");
			return gitHubClient
				.Repository
				.Commit
				.Get(
					repoOwner,
					repoName,
					committish)
				.WaitAsync(cancellationToken);
		}
	}
}
