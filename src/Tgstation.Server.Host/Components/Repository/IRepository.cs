using System;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Jobs;

namespace Tgstation.Server.Host.Components.Repository
{
	/// <summary>
	/// Represents an on-disk git repository.
	/// </summary>
	public interface IRepository : IGitRemoteAdditionalInformation, IDisposable
	{
		/// <summary>
		/// If <see cref="Reference"/> tracks an upstream branch.
		/// </summary>
		bool Tracking { get; }

		/// <summary>
		/// The SHA of the <see cref="IRepository"/> HEAD.
		/// </summary>
		string Head { get; }

		/// <summary>
		/// The current reference the <see cref="IRepository"/> HEAD is using. This can be a branch or tag.
		/// </summary>
		string Reference { get; }

		/// <summary>
		/// The current origin remote the <see cref="IRepository"/> is using.
		/// </summary>
		Uri Origin { get; }

		/// <summary>
		/// Checks if a given <paramref name="committish"/> is a sha.
		/// </summary>
		/// <param name="committish">The git object to check.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="true"/> if <paramref name="committish"/> is a sha, <see langword="false"/> otherwise.</returns>
		Task<bool> IsSha(string committish, CancellationToken cancellationToken);

		/// <summary>
		/// Checks out a given <paramref name="committish"/>.
		/// </summary>
		/// <param name="committish">The sha or reference to checkout.</param>
		/// <param name="username">The optional username used for fetching from submodule repositories.</param>
		/// <param name="password">The optional password used for fetching from submodule repositories.</param>
		/// <param name="updateSubmodules">If a submodule update should be attempted after the merge.</param>
		/// <param name="moveCurrentReference">If a hard reset to the target committish should be performed instead of a checkout.</param>
		/// <param name="progressReporter">The optional <see cref="JobProgressReporter"/> to report progress of the operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask CheckoutObject(
			string committish,
			string? username,
			string? password,
			bool updateSubmodules,
			bool moveCurrentReference,
			JobProgressReporter? progressReporter,
			CancellationToken cancellationToken);

		/// <summary>
		/// Attempt to merge the revision specified by a given set of <paramref name="testMergeParameters"/> into HEAD.
		/// </summary>
		/// <param name="testMergeParameters">The <see cref="TestMergeParameters"/> of the pull request.</param>
		/// <param name="committerName">The name of the merge committer.</param>
		/// <param name="committerEmail">The e-mail of the merge committer.</param>
		/// <param name="username">The optional username used to fetch from the origin and submodule repositories.</param>
		/// <param name="password">The optional password used to fetch from the origin and submodule repositories.</param>
		/// <param name="updateSubmodules">If a submodule update should be attempted after the merge.</param>
		/// <param name="progressReporter">The <see cref="JobProgressReporter"/> to report progress of the operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="TestMergeResult"/>.</returns>
		ValueTask<TestMergeResult> AddTestMerge(
			TestMergeParameters testMergeParameters,
			string committerName,
			string committerEmail,
			string? username,
			string? password,
			bool updateSubmodules,
			JobProgressReporter progressReporter,
			CancellationToken cancellationToken);

		/// <summary>
		/// Fetch commits from the origin repository.
		/// </summary>
		/// <param name="progressReporter">The optional <see cref="JobProgressReporter"/> to report progress of the operation.</param>
		/// <param name="username">The optional username to fetch from the origin repository.</param>
		/// <param name="password">The optional password to fetch from the origin repository.</param>
		/// <param name="deploymentPipeline">If any events created should be marked as part of the deployment pipeline.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask FetchOrigin(
			JobProgressReporter? progressReporter,
			string? username,
			string? password,
			bool deploymentPipeline,
			CancellationToken cancellationToken);

		/// <summary>
		/// Requires the current HEAD to be a tracked reference. Hard resets the reference to what it tracks on the origin repository.
		/// </summary>
		/// <param name="progressReporter">The <see cref="JobProgressReporter"/> to report progress of the operation.</param>
		/// <param name="username">The optional username used for fetching from submodule repositories.</param>
		/// <param name="password">The optional password used for fetching from submodule repositories.</param>
		/// <param name="updateSubmodules">If a submodule update should be attempted after the merge.</param>
		/// <param name="deploymentPipeline">If any events created should be marked as part of the deployment pipeline.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the SHA of the new HEAD.</returns>
		ValueTask ResetToOrigin(
			JobProgressReporter progressReporter,
			string? username,
			string? password,
			bool updateSubmodules,
			bool deploymentPipeline,
			CancellationToken cancellationToken);

		/// <summary>
		/// Requires the current HEAD to be a reference. Hard resets the reference to the given sha.
		/// </summary>
		/// <param name="sha">The sha hash to reset to.</param>
		/// <param name="progressReporter">The <see cref="JobProgressReporter"/> to report progress of the operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the SHA of the new HEAD.</returns>
		Task ResetToSha(string sha, JobProgressReporter progressReporter, CancellationToken cancellationToken);

		/// <summary>
		/// Requires the current HEAD to be a tracked reference. Merges the reference to what it tracks on the origin repository.
		/// </summary>
		/// <param name="progressReporter">The <see cref="JobProgressReporter"/> to report progress of the operation.</param>
		/// <param name="committerName">The name of the merge committer.</param>
		/// <param name="committerEmail">The e-mail of the merge committer.</param>
		/// <param name="deploymentPipeline">If any events created should be marked as part of the deployment pipeline.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a <see cref="Nullable{T}"/> <see cref="bool"/> representing the merge result that is <see langword="true"/> after a fast forward, <see langword="false"/> on a merge or up to date, <see langword="null"/> on a conflict.</returns>
		ValueTask<bool?> MergeOrigin(
			JobProgressReporter progressReporter,
			string committerName,
			string committerEmail,
			bool deploymentPipeline,
			CancellationToken cancellationToken);

		/// <summary>
		/// Runs the synchronize event script and attempts to push any changes made to the <see cref="IRepository"/> if on a tracked branch.
		/// </summary>
		/// <param name="progressReporter">The <see cref="JobProgressReporter"/> to report progress of the operation.</param>
		/// <param name="username">The username to fetch from the origin repository.</param>
		/// <param name="password">The password to fetch from the origin repository.</param>
		/// <param name="committerName">The name of the potential committer.</param>
		/// <param name="committerEmail">The e-mail of the potential committer.</param>
		/// <param name="synchronizeTrackedBranch">If the synchronizations should be made to the tracked reference as opposed to a temporary branch.</param>
		/// <param name="deploymentPipeline">If any events created should be marked as part of the deployment pipeline.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in <see langword="true"/> if commits were pushed to the tracked origin reference, <see langword="false"/> otherwise.</returns>
		ValueTask<bool> Synchronize(
			JobProgressReporter progressReporter,
			string? username,
			string? password,
			string committerName,
			string committerEmail,
			bool synchronizeTrackedBranch,
			bool deploymentPipeline,
			CancellationToken cancellationToken);

		/// <summary>
		/// Copies the current working directory to a given <paramref name="path"/>.
		/// </summary>
		/// <param name="path">The path to copy repository contents to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask CopyTo(string path, CancellationToken cancellationToken);

		/// <summary>
		/// Check if a given <paramref name="committish"/> is a parent of the current <see cref="Head"/>.
		/// </summary>
		/// <param name="committish">The committish of the SHA to check.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="true"/> if <paramref name="committish"/> is a parent of <see cref="Head"/>, <see langword="false"/> otherwise.</returns>
		/// <remarks>This function is NOT reentrant.</remarks>
		Task<bool> CommittishIsParent(string committish, CancellationToken cancellationToken);

		/// <summary>
		/// Get the tracked reference's current SHA.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the tracked origin reference's SHA.</returns>
		Task<string> GetOriginSha(CancellationToken cancellationToken);

		/// <summary>
		/// Gets the <see cref="DateTimeOffset"/> a given <paramref name="sha"/> was created on.
		/// </summary>
		/// <param name="sha">The SHA to timestamp.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="DateTimeOffset"/> that <paramref name="sha"/> was created on.</returns>
		Task<DateTimeOffset> TimestampCommit(string sha, CancellationToken cancellationToken);
	}
}
