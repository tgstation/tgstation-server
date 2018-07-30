using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components.Repository
{
	/// <summary>
	/// Represents an on-disk git repository
	/// </summary>
	public interface IRepository : IDisposable
	{
		/// <summary>
		/// If the <see cref="IRepository"/> was cloned from GitHub.com
		/// </summary>
		bool IsGitHubRepository { get; }

		/// <summary>
		/// The <see cref="Octokit.Repository.Owner"/> if this <see cref="IsGitHubRepository"/>
		/// </summary>
		string GitHubOwner { get; }

		/// <summary>
		/// The <see cref="Octokit.Repository.Name"/> if this <see cref="IsGitHubRepository"/>
		/// </summary>
		string GitHubRepoName { get; }

		/// <summary>
		/// If <see cref="Reference"/> tracks an upstream branch
		/// </summary>
		bool Tracking { get; }

		/// <summary>
		/// The SHA of the <see cref="IRepository"/> HEAD
		/// </summary>
		string Head { get; }

		/// <summary>
		/// The current reference the <see cref="IRepository"/> HEAD is using. This can be a branch or tag
		/// </summary>
		string Reference { get; }

		/// <summary>
		/// The current origin remote the <see cref="IRepository"/> is using
		/// </summary>
		string Origin { get; }

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
		/// <param name="accessString">The access string to fetch from the origin repository</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the SHA of the new HEAD on success, <see langword="null"/> on merge conflict</returns>
		Task<string> AddTestMerge(int pullRequestNumber, string targetCommit, string committerName, string committerEmail, string accessString, CancellationToken cancellationToken);

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
		/// Requires the current HEAD to be a tracked reference. Merges the reference to what it tracks on the origin repository
		/// </summary>
		/// <param name="committerName">The name of the merge committer</param>
		/// <param name="committerEmail">The e-mail of the merge committer</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the SHA of the new HEAD. <see langword="null"/> if the merge resulted in conflict</returns>
		Task<string> MergeOrigin(string committerName, string committerEmail, CancellationToken cancellationToken);

		/// <summary>
		/// Force push the current repository HEAD to <see cref="Repository.RemoteTemporaryBranchName"/>;
		/// </summary>
		/// <param name="accessString">The access string to fetch from the origin repository</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task PushHeadToTemporaryBranch(string accessString, CancellationToken cancellationToken);

		/// <summary>
		/// Runs the synchronize event script and attempts to push any changes made to the <see cref="IRepository"/> if on a tracked branch
		/// </summary>
		/// <param name="accessString">The access string to push to the origin repository</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Sychronize(string accessString, CancellationToken cancellationToken);

		/// <summary>
		/// Copies the current working directory to a given <paramref name="path"/>
		/// </summary>
		/// <param name="path">The path to copy repository contents to</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task CopyTo(string path, CancellationToken cancellationToken);
	}
}
