using System;

namespace Tgstation.Server.Api.Rights
{
	/// <summary>
	/// Rights for a <see cref="Models.Repository"/>
	/// </summary>
	[Flags]
	public enum RepositoryRights
	{
		/// <summary>
		/// User has no rights
		/// </summary>
		None = 0,
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
		/// User may change <see cref="Models.Repository.CommitterName"/> and <see cref="Models.Repository.CommitterEmail"/>
		/// </summary>
		ChangeCommitter = 32,
		/// <summary>
		/// User may change <see cref="Models.Repository.PushTestMergeCommits"/>
		/// </summary>
		ChangeTestMergeCommits = 64,
		/// <summary>
		/// User may change <see cref="Models.Repository.AutoUpdateInterval"/>
		/// </summary>
		ChangeAutoUpdate = 128,
		/// <summary>
		/// User may read and change <see cref="Models.Repository.AccessUser"/> and <see cref="Models.Repository.AccessToken"/>
		/// </summary>
		ChangeCredentials = 256,
		/// <summary>
		/// User may set <see cref="Models.Repository.Reference"/> to another git reference (not a SHA)
		/// </summary>
		SetReference = 512,
		/// <summary>
		/// User may read all fields in the <see cref="Models.Repository"/> with the exception of <see cref="Models.Repository.AccessToken"/>
		/// </summary>
		Read = 1024,
	}
}
