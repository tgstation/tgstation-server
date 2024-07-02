using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Octokit;

using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Utils.GitHub
{
	/// <summary>
	/// Service for interacting with the GitHub API.
	/// </summary>
	public interface IGitHubService
	{
		/// <summary>
		/// Gets the <see cref="Uri"/> of the repository designated as the updates repository.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="Uri"/> of the designated updates repository.</returns>
		ValueTask<Uri> GetUpdatesRepositoryUrl(CancellationToken cancellationToken);

		/// <summary>
		/// Get all valid TGS <see cref="Release"/>s from the configured update source.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="Dictionary{TKey, TValue}"/> of TGS <see cref="Release"/>s keyed by their <see cref="Version"/>.</returns>
		/// <remarks>GitHub has been known to return incomplete results from the API with this call.</remarks>
		ValueTask<Dictionary<Version, Release>> GetTgsReleases(CancellationToken cancellationToken);

		/// <summary>
		/// Attempt to get an OAuth token from a given <paramref name="code"/>.
		/// </summary>
		/// <param name="oAuthConfiguration">The <see cref="OAuthConfiguration"/>. Must have <see cref="OAuthConfiguration.RedirectUrl"/>, <see cref="OAuthConfigurationBase.ClientId"/> and <see cref="OAuthConfigurationBase.ClientSecret"/> set.</param>
		/// <param name="code">The OAuth response code.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="string"/> representing the returned OAuth code from GitHub on success, <see langword="null"/> otherwise.</returns>
		ValueTask<string> CreateOAuthAccessToken(OAuthConfiguration oAuthConfiguration, string code, CancellationToken cancellationToken);

		/// <summary>
		/// Get a target repostiory's ID.
		/// </summary>
		/// <param name="repoOwner">The owner of the target repository.</param>
		/// <param name="repoName">The name of the target repository.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the target repository's ID.</returns>
		ValueTask<long> GetRepositoryId(string repoOwner, string repoName, CancellationToken cancellationToken);

		/// <summary>
		/// Get the current user's ID.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the current user's ID.</returns>
		ValueTask<long> GetCurrentUserId(CancellationToken cancellationToken);

		/// <summary>
		/// Get a given <paramref name="pullRequestNumber"/>.
		/// </summary>
		/// <param name="repoOwner">The owner of the target repository.</param>
		/// <param name="repoName">The name of the target repository.</param>
		/// <param name="pullRequestNumber">The target <see cref="PullRequest.Number"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the target <see cref="PullRequest"/>.</returns>
		Task<PullRequest> GetPullRequest(string repoOwner, string repoName, int pullRequestNumber, CancellationToken cancellationToken);

		/// <summary>
		/// Get a given <paramref name="committish"/>.
		/// </summary>
		/// <param name="repoOwner">The owner of the target repository.</param>
		/// <param name="repoName">The name of the target repository.</param>
		/// <param name="committish">The target SHA or ref to get the commit for.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the target <see cref="GitHubCommit"/>.</returns>
		Task<GitHubCommit> GetCommit(string repoOwner, string repoName, string committish, CancellationToken cancellationToken);
	}
}
