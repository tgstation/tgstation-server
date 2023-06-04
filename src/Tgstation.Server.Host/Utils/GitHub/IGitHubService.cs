using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Octokit;

using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Utils.GitHub
{
	/// <summary>
	/// Service for interacting with GitHub.
	/// </summary>
	public interface IGitHubService
	{
		/// <summary>
		/// Gets the <see cref="Uri"/> of the repository designated as the updates repository.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="Uri"/> of the designated updates repository.</returns>
		Task<Uri> GetUpdatesRepositoryUrl(CancellationToken cancellationToken);

		/// <summary>
		/// Get all valid TGS <see cref="Release"/>s from the configured update source.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="Dictionary{TKey, TValue}"/> of TGS <see cref="Release"/>s keyed by their <see cref="Version"/>.</returns>
		/// <remarks>GitHub has been known to return incomplete results from the API with this call.</remarks>
		Task<Dictionary<Version, Release>> GetTgsReleases(CancellationToken cancellationToken);

		/// <summary>
		/// Attempt to get an OAuth token from a given <paramref name="code"/>.
		/// </summary>
		/// <param name="oAuthConfiguration">The <see cref="OAuthConfiguration"/>. Must have <see cref="OAuthConfiguration.RedirectUrl"/>, <see cref="OAuthConfigurationBase.ClientId"/> and <see cref="OAuthConfigurationBase.ClientSecret"/> set.</param>
		/// <param name="code">The OAuth response code.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="string"/> representing the returned OAuth code from GitHub on success, <see langword="null"/> otherwise.</returns>
		Task<string> CreateOAuthAccessToken(OAuthConfiguration oAuthConfiguration, string code, CancellationToken cancellationToken);

		/// <summary>
		/// Get the current user's ID.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the current user's ID.</returns>
		Task<int> GetCurrentUserId(CancellationToken cancellationToken);

		/// <summary>
		/// Get a target repostiory's ID.
		/// </summary>
		/// <param name="repoOwner">The owner of the target repository.</param>
		/// <param name="repoName">The name of the target repository.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the target repository's ID.</returns>
		Task<long> GetRepositoryId(string repoOwner, string repoName, CancellationToken cancellationToken);

		/// <summary>
		/// Create a comment on a given <paramref name="issueNumber"/>.
		/// </summary>
		/// <param name="repoOwner">The owner of the target repository.</param>
		/// <param name="repoName">The name of the target repository.</param>
		/// <param name="comment">The text of the comment.</param>
		/// <param name="issueNumber">The number of the issue to comment on.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task CommentOnIssue(string repoOwner, string repoName, string comment, int issueNumber, CancellationToken cancellationToken);

		/// <summary>
		/// Create a <paramref name="newDeployment"/> on a target repostiory.
		/// </summary>
		/// <param name="newDeployment">The <see cref="NewDeployment"/>.</param>
		/// <param name="repoOwner">The owner of the target repository.</param>
		/// <param name="repoName">The name of the target repository.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the new deployment's ID.</returns>
		Task<int> CreateDeployment(NewDeployment newDeployment, string repoOwner, string repoName, CancellationToken cancellationToken);

		/// <summary>
		/// Create a <paramref name="newDeploymentStatus"/> on a target deployment.
		/// </summary>
		/// <param name="newDeploymentStatus">The <see cref="NewDeploymentStatus"/>.</param>
		/// <param name="repoOwner">The owner of the target repository.</param>
		/// <param name="repoName">The name of the target repository.</param>
		/// <param name="deploymentId">The ID of the parent deployment.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task CreateDeploymentStatus(NewDeploymentStatus newDeploymentStatus, string repoOwner, string repoName, int deploymentId, CancellationToken cancellationToken);

		/// <summary>
		/// Create a <paramref name="newDeploymentStatus"/> on a target deployment.
		/// </summary>
		/// <param name="newDeploymentStatus">The <see cref="NewDeploymentStatus"/>.</param>
		/// <param name="repoId">The ID of the target repository.</param>
		/// <param name="deploymentId">The ID of the parent deployment.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task CreateDeploymentStatus(NewDeploymentStatus newDeploymentStatus, long repoId, int deploymentId, CancellationToken cancellationToken);

		/// <summary>
		/// Get a given <paramref name="pullRequestNumber"/>.
		/// </summary>
		/// <param name="repoOwner">The owner of the target repository.</param>
		/// <param name="repoName">The name of the target repository.</param>
		/// <param name="pullRequestNumber">The target <see cref="PullRequest.Number"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the target <see cref="PullRequest"/>.</returns>
		Task<PullRequest> GetPullRequest(string repoOwner, string repoName, int pullRequestNumber, CancellationToken cancellationToken);
	}
}
