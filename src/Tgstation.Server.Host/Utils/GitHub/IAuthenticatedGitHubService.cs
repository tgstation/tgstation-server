using System.Threading;
using System.Threading.Tasks;

using Octokit;

namespace Tgstation.Server.Host.Utils.GitHub
{
	/// <summary>
	/// <see cref="IGitHubService"/> that exposes functions that require authentication.
	/// </summary>
	public interface IAuthenticatedGitHubService : IGitHubService
	{
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
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the new deployment's ID.</returns>
		ValueTask<long> CreateDeployment(NewDeployment newDeployment, string repoOwner, string repoName, CancellationToken cancellationToken);

		/// <summary>
		/// Create a <paramref name="newDeploymentStatus"/> on a target deployment.
		/// </summary>
		/// <param name="newDeploymentStatus">The <see cref="NewDeploymentStatus"/>.</param>
		/// <param name="repoOwner">The owner of the target repository.</param>
		/// <param name="repoName">The name of the target repository.</param>
		/// <param name="deploymentId">The ID of the parent deployment.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task CreateDeploymentStatus(NewDeploymentStatus newDeploymentStatus, string repoOwner, string repoName, long deploymentId, CancellationToken cancellationToken);

		/// <summary>
		/// Create a <paramref name="newDeploymentStatus"/> on a target deployment.
		/// </summary>
		/// <param name="newDeploymentStatus">The <see cref="NewDeploymentStatus"/>.</param>
		/// <param name="repoId">The ID of the target repository.</param>
		/// <param name="deploymentId">The ID of the parent deployment.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task CreateDeploymentStatus(NewDeploymentStatus newDeploymentStatus, long repoId, long deploymentId, CancellationToken cancellationToken);
	}
}
