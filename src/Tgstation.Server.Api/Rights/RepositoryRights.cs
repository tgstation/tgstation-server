using System;

namespace Tgstation.Server.Api.Rights
{
	/// <summary>
	/// Rights for a <see cref="Models.Repository"/>
	/// </summary>
	[Flags]
	public enum RepositoryRights : ulong
	{
		/// <summary>
		/// User has no rights
		/// </summary>
		None = 0,

		/// <summary>
		/// User may cancel update operations.
		/// </summary>
		CancelPendingChanges = 1,

		/// <summary>
		/// User may create the <see cref="Models.Repository"/> if it does not exist
		/// </summary>
		SetOrigin = 2,

		/// <summary>
		/// User may directly set the sha the <see cref="Models.Repository"/>'s HEAD points to
		/// </summary>
		SetSha = 4,

		/// <summary>
		/// User may fetch and merge GitHub pull requests
		/// </summary>
		MergePullRequest = 8,

		/// <summary>
		/// User may use the update feature
		/// </summary>
		UpdateBranch = 16,

		/// <summary>
		/// User may change <see cref="Models.Internal.RepositorySettings.CommitterName"/> and <see cref="Models.Internal.RepositorySettings.CommitterEmail"/>
		/// </summary>
		ChangeCommitter = 32,

		/// <summary>
		/// User may change <see cref="Models.Internal.RepositorySettings.PushTestMergeCommits"/>, <see cref="Models.Internal.RepositorySettings.PostTestMergeComment"/>, and <see cref="Models.Internal.RepositorySettings.CreateGitHubDeployments"/>.
		/// </summary>
		ChangeTestMergeCommits = 64,

		/// <summary>
		/// User may read and change <see cref="Models.Internal.RepositorySettings.AccessUser"/> and <see cref="Models.Internal.RepositorySettings.AccessToken"/>
		/// </summary>
		ChangeCredentials = 128,

		/// <summary>
		/// User may set <see cref="Models.Repository.Reference"/> to another git reference (not a SHA)
		/// </summary>
		SetReference = 256,

		/// <summary>
		/// User may read all fields in the <see cref="Models.Repository"/> with the exception of <see cref="Models.Internal.RepositorySettings.AccessToken"/>
		/// </summary>
		Read = 512,

		/// <summary>
		/// User may change <see cref="Models.Internal.RepositorySettings.AutoUpdatesKeepTestMerges"/> and  <see cref="Models.Internal.RepositorySettings.AutoUpdatesSynchronize"/>
		/// </summary>
		ChangeAutoUpdateSettings = 1024,

		/// <summary>
		/// User may delete the <see cref="Models.Repository"/>
		/// </summary>
		Delete = 2048,

		/// <summary>
		/// User may cancel clone operations
		/// </summary>
		CancelClone = 4096
	}
}
