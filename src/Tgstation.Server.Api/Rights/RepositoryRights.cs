using System;

namespace Tgstation.Server.Api.Rights
{
	/// <summary>
	/// Rights for the git repository.
	/// </summary>
	[Flags]
	public enum RepositoryRights : ulong
	{
		/// <summary>
		/// User has no rights.
		/// </summary>
		None = 0,

		/// <summary>
		/// User may cancel repository jobs excluding clone operations.
		/// </summary>
		CancelPendingChanges = 1 << 0,

		/// <summary>
		/// User may clone the repository if it does not exist. This also allows setting <see cref="Models.RepositorySettings.UpdateSubmodules"/>, <see cref="Models.RepositorySettings.AccessUser"/>, and <see cref="Models.RepositorySettings.AccessToken"/> at clone time.
		/// </summary>
		SetOrigin = 1 << 1,

		/// <summary>
		/// User may directly checkout a git SHA that the repository's HEAD will point to.
		/// </summary>
		SetSha = 1 << 2,

		/// <summary>
		/// User may create <see cref="Models.TestMerge"/>s.
		/// </summary>
		MergePullRequest = 1 << 3,

		/// <summary>
		/// User may fetch and hard reset to the origin version of the current branch.
		/// </summary>
		UpdateBranch = 1 << 4,

		/// <summary>
		/// User may change <see cref="Models.RepositorySettings.CommitterName"/> and <see cref="Models.RepositorySettings.CommitterEmail"/>.
		/// </summary>
		ChangeCommitter = 1 << 5,

		/// <summary>
		/// User may change <see cref="Models.RepositorySettings.PushTestMergeCommits"/>, <see cref="Models.RepositorySettings.PostTestMergeComment"/>, and <see cref="Models.RepositorySettings.CreateGitHubDeployments"/>.
		/// </summary>
		ChangeTestMergeCommits = 1 << 6,

		/// <summary>
		/// User may read and change <see cref="Models.RepositorySettings.AccessUser"/> and <see cref="Models.RepositorySettings.AccessToken"/>.
		/// </summary>
		ChangeCredentials = 1 << 7,

		/// <summary>
		/// User may set <see cref="Models.Internal.RepositoryApiBase.Reference"/> to another git branch or tag (not a SHA).
		/// </summary>
		SetReference = 1 << 8,

		/// <summary>
		/// User may read repository information.
		/// </summary>
		Read = 1 << 9,

		/// <summary>
		/// User may change <see cref="Models.RepositorySettings.AutoUpdatesKeepTestMerges"/> and <see cref="Models.RepositorySettings.AutoUpdatesSynchronize"/>.
		/// </summary>
		ChangeAutoUpdateSettings = 1 << 10,

		/// <summary>
		/// User may delete the repository and allow it to be cloned again.
		/// </summary>
		Delete = 1 << 11,

		/// <summary>
		/// User may cancel clone jobs.
		/// </summary>
		CancelClone = 1 << 12,

		/// <summary>
		/// User may change submodule update settings.
		/// </summary>
		ChangeSubmoduleUpdate = 1 << 13,

		/// <summary>
		/// User may trigger repository recloning.
		/// </summary>
		Reclone = 1 << 14,
	}
}
