using System.Collections.Generic;
using System.ServiceModel;

namespace TGS.Interface.Components
{
	/// <summary>
	/// Interface for managing the code repository
	/// </summary>
	[ServiceContract]
	public interface ITGRepository
	{
		/// <summary>
		/// If the repo is currently undergoing an operation
		/// </summary>
		/// <returns><see langword="true"/> if the repo is busy, <see langword="false"/> otherwise</returns>
		[OperationContract]
		bool OperationInProgress();

		/// <summary>
		/// Gets the progress of repository operations, not all operations are supported
		/// </summary>
		/// <returns>A value between 0 and 100 inclusive representing the progress of the current operation or -1 if the operation cannot be monitored</returns>
		[OperationContract]
		int CheckoutProgress();

		/// <summary>
		/// Check if the repository is valid, if not <see cref="Setup(string, string)"/> must be called
		/// </summary>
		/// <returns><see langword="true"/> if the repository is valid, <see langword="false"/> otherwise</returns>
		[OperationContract]
		bool Exists();

		/// <summary>
		/// Deletes whatever may be left over and clones the repo at <paramref name="remote"/> and checks out <paramref name="branch"/>. Will move config and data dirs to a backup location if they exist. Runs asyncronously
		/// </summary>
		/// <param name="remote">The address of the repo to clone. If ssh protocol is used, private_key.txt and public_key.txt must exist in the server RepoKey directory.</param>
		/// <param name="branch">The branch of the repo to checkout</param>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		[OperationContract]
		string Setup(string remote, string branch = "master");

		/// <summary>
		/// Gets the sha of the current HEAD
		/// </summary>
		/// <param name="useTracked">If set to true and HEAD is currently a branch, will instead return the sha of the tracked remote branch if it exists</param>
		/// <param name="error"><see langword="null"/> on success, error message on failure</param>
		/// <returns>The sha of the current HEAD on success, <see langword="null"/> on failure</returns>
		[OperationContract]
		string GetHead(bool useTracked, out string error);

		/// <summary>
		/// Gets the name of the current branch
		/// </summary>
		/// <param name="error"><see langword="null"/> on success, error message on failure</param>
		/// <returns>The name of the current branch on success, <see langword="null"/> on failure</returns>
		[OperationContract]
		string GetBranch(out string error);

		/// <summary>
		/// Gets the url of the current origin
		/// </summary>
		/// <param name="error"><see langword="null"/> on success, error message on failure</param>
		/// <returns>The url of the current origin on success, <see langword="null"/> on failure</returns>
		[OperationContract]
		string GetRemote(out string error);

		/// <summary>
		/// Hard checks out the passed object name
		/// </summary>
		/// <param name="objectName">The branch, commit, or tag to checkout</param>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		[OperationContract]
		string Checkout(string objectName);

		/// <summary>
		/// Fetches the origin and merges it into the current branch
		/// </summary>
		/// <param name="reset">If <see langword="true"/>, the operation will perform a hard reset instead of a merge</param>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		[OperationContract]
		string Update(bool reset);

		/// <summary>
		/// Runs git reset --hard
		/// </summary>
		/// <param name="tracked">Changes command to git reset --hard origin/branch_name if <see langword="true"/></param>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		[OperationContract]
		string Reset(bool tracked);

		/// <summary>
		/// Merges the target pull request into the current branch if the remote is a github repository
		/// </summary>
		/// <param name="PRnumber">The github pull request number in the remote repository</param>
		/// <param name="atSHA">The SHA of the pull request to merge</param>
		/// <param name="silent">Suppresses chat messages if <see langword="true"/></param>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		[OperationContract]
		string MergePullRequest(int PRnumber, string atSHA = null, bool silent = false);

		/// <summary>
		/// Merges the target pull requests into the current branch if the remote is a github repository
		/// </summary>
		/// <param name="pullRequestInfos"><see cref="IEnumerable{T}"/> of <see cref="PullRequestInfo"/>s containing pull request numbers and shas in the remote repository</param>
		/// <param name="silent">Suppresses chat messages if <see langword="true"/></param>
		/// <returns><see langword="null"/> on success, list of error messages if any fail failure. Those with indexes of those that succeed will have null entries</returns>
		[OperationContract]
		IEnumerable<string> MergePullRequests(IEnumerable<PullRequestInfo> pullRequestInfos, bool silent = false);

		/// <summary>
		/// Get the currently merged pull requests. Note that switching branches will delete this list and switching back won't restore it
		/// </summary>
		/// <param name="error"><see langword="null"/> on success, error message on failure</param>
		/// <returns>A <see cref="IList{T}"/> of <see cref="PullRequestInfo"/></returns>
		[OperationContract]
		List<PullRequestInfo> MergedPullRequests(out string error);

		/// <summary>
		/// Gets the name of the configured git committer
		/// </summary>
		/// <returns>The name of the configured git committer</returns>
		[OperationContract]
		string GetCommitterName();

		/// <summary>
		/// Sets the name of the configured git committer
		/// </summary>
		/// <param name="newName">The name to set</param>
		[OperationContract]
		void SetCommitterName(string newName);

		/// <summary>
		/// Gets the email of the configured git committer
		/// </summary>
		/// <returns>The email of the configured git committer</returns>
		[OperationContract]
		string GetCommitterEmail();

		/// <summary>
		/// Sets the email of the configured git committer
		/// </summary>
		/// <param name="newEmail">The email to set</param>
		[OperationContract]
		void SetCommitterEmail(string newEmail);

		/// <summary>
		/// Updates the html changelog
		/// </summary>
		/// <param name="error"><see langword="null"/> on success, error on failure</param>
		/// <returns>The output of the python script</returns>
		[OperationContract]
		string GenerateChangelog(out string error);

		/// <summary>
		/// Pushes the paths listed in TGS3.json to the currentl git remote. No other commit differences may exist for this function to succeed
		/// </summary>
		/// <returns><see langword="null"/> on success, error on failure</returns>
		[OperationContract]
        string SynchronizePush();

		/// <summary>
		/// List the tagged commits of the repo at which compiles took place
		/// </summary>
		/// <param name="error"><see langword="null"/> on success, error message on failure</param>
		/// <returns>A <see cref="IDictionary{TKey, TValue}"/> of tag name -> commit on success, <see langword="null"/> on failure</returns>
		[OperationContract]
		IDictionary<string, string> ListBackups(out string error);

		/// <summary>
		/// Updates the cached TGS3.json to the repo's version. Compiles will not succeed if these two to not match
		/// </summary>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		[OperationContract]
		string UpdateTGS3Json();

		/// <summary>
		/// (De)Activate and set the interval for the automatic server updater
		/// </summary>
		/// <param name="newInterval">Interval to check for updates in minutes, disables if 0</param>
		[OperationContract]
		void SetAutoUpdateInterval(ulong newInterval);

		/// <summary>
		/// Get the current autoupdate interval
		/// </summary>
		/// <returns>The current auto update interval or 0 if it's disabled</returns>
		[OperationContract]
		ulong AutoUpdateInterval();

		/// <summary>
		/// Check if we push a temporary branch to the remote when we make testmerge commits
		/// </summary>
		/// <returns><see langword="true"/> if we publish testmerge commits to the remote, <see langword="false"/> otherwise</returns>
		[OperationContract]
		bool PushTestmergeCommits();

		/// <summary>
		/// Set if we push a temporary branch to the remote when we make testmerge commits
		/// </summary>
		/// <param name="newValue"><see langword="true"/> if we testmerge commits should be published to the remote, <see langword="false"/> otherwise</param>
		[OperationContract]
		void SetPushTestmergeCommits(bool newValue);

		/// <summary>
		/// Check and set if auto updates preserve test merges
		/// </summary>
		/// <param name="newValue"><see langword="true"/> if auto updates should preserve test merges, <see langword="false"/> otherwise</param>
		/// <returns>The value of the config before the call</returns>
		[OperationContract]
		bool AutoUpdatePreserve(bool? newValue);
	}
}
