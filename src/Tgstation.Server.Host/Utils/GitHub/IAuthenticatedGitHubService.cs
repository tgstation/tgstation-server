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
		/// Append a comment on an existing <paramref name="issueComment"/>.
		/// </summary>
		/// <param name="repoOwner">The owner of the target repository.</param>
		/// <param name="repoName">The name of the target repository.</param>
		/// <param name="comment">The text of the comment.</param>
		/// <param name="issueComment">The <see cref="IssueComment"/> to amend.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task AppendCommentOnIssue(string repoOwner, string repoName, string comment, IssueComment issueComment, CancellationToken cancellationToken);

		/// <summary>
		/// Gets an <see cref="IssueComment"/> for a particular <paramref name="issueNumber"/> with the provided <paramref name="header"/> if it exists and is not too large.
		/// </summary>
		/// <param name="repoOwner">The owner of the target repository.</param>
		/// <param name="repoName">The name of the target repository.</param>
		/// <param name="header">The starting text of the comment to search for.</param>
		/// <param name="issueNumber">The number of the issue to comment on.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> of an existing comment if its currently under 250k characters, otherwise the <see cref="IssueComment"/> will be null.</returns>
		ValueTask<IssueComment?> GetExistingCommentOnIssue(string repoOwner, string repoName, string header, int issueNumber, CancellationToken cancellationToken);

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
