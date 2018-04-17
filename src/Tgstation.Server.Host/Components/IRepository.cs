using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Represents an on-disk git repository
	/// </summary>
	interface IRepository
	{
		/// <summary>
		/// Check if the <see cref="IRepository"/> is in a working state
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="true"/> if the <see cref="IRepository"/> is in a working state, <see langword="false"/> otherwise</returns>
		Task<bool> Exists(CancellationToken cancellationToken);

		/// <summary>
		/// Check if the <see cref="IRepository"/> was cloned from GitHub.com
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="true"/> if the <see cref="IRepository"/> was cloned from GitHub.com, <see langword="false"/> otherwise</returns>
		Task<bool> IsGitHubRepository(CancellationToken cancellationToken);

		/// <summary>
		/// Get the SHA of the <see cref="IRepository"/> HEAD
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the SHA of the <see cref="IRepository"/> HEAD</returns>
		Task<string> GetHead(CancellationToken cancellationToken);

		/// <summary>
		/// Get the current reference the <see cref="IRepository"/> HEAD is using. This can be a branch or tag
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the current reference the <see cref="IRepository"/> HEAD is using. Will be <see langword="null"/> if not on a branch or tag</returns>
		Task<string> GetReference(CancellationToken cancellationToken);

		/// <summary>
		/// Get the current origin remote the <see cref="IRepository"/> is using
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the current origin remote the <see cref="IRepository"/> is using</returns>
		Task<string> GetOrigin(CancellationToken cancellationToken);

		/// <summary>
		/// Deletes the <see cref="IRepository"/> and clones a <paramref name="newOrigin"/> using an <paramref name="accessString"/> if provided
		/// </summary>
		/// <param name="newOrigin">The new remote url</param>
		/// <param name="accessString">The access string to clone the repository</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task SetOrigin(string newOrigin, string accessString, CancellationToken cancellationToken);

		/// <summary>
		/// Checks out a given <paramref name="committish"/>
		/// </summary>
		/// <param name="committish">The sha or reference to checkout</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task CheckoutObject(string committish, CancellationToken cancellationToken);

		/// <summary>
		/// Attempt to merge a GitHub pull request into HEAD
		/// </summary>
		/// <param name="pullRequestNumber">The pull request number on the remote repository</param>
		/// <param name="targetCommit">The commit in the pull request to merge</param>
		/// <param name="committerName">The name of the merge committer</param>
		/// <param name="committerEmail">The e-mail of the merge committer</param>
		/// <param name="commitBody">The body of the commit message</param>
		/// <param name="accessString">The access string to fetch from the origin repository</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the SHA of the new HEAD on success, <see langword="null"/> on merge conflict</returns>
		Task<string> AddTestMerge(int pullRequestNumber, string targetCommit, string committerName, string committerEmail, string commitBody, string accessToken, CancellationToken cancellationToken);

		/// <summary>
		/// Fetch commits from the origin repository
		/// </summary>
		/// <param name="accessString">The access string to fetch from the origin repository</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task FetchOrigin(string accessString, CancellationToken cancellationToken);

		/// <summary>
		/// Requires the current HEAD to be a tracked reference. Hard resets the reference to what it tracks on the origin repository
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the SHA of the new HEAD</returns>
		Task<string> ResetToOrigin(CancellationToken cancellationToken);

		/// <summary>
		/// Push the current repository HEAD to a temporary GitHub branch and then delete it
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task PushHeadToTemporaryBranch(CancellationToken cancellationToken);
	}
}
