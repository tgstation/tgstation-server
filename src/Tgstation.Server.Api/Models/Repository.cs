using System.Collections.Generic;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a git repository
	/// </summary>
	[Model(RightsType.Repository, ReadRight = RepositoryRights.Read, RequiresInstance = true)]
	public sealed class Repository
	{
		/// <summary>
		/// The origin URL. If <see langword="null"/>, the <see cref="Repository"/> does not exist
		/// </summary>
		[Permissions(WriteRight = RepositoryRights.SetOrigin)]
		public string Origin { get; set; }

		/// <summary>
		/// The commit HEAD points to
		/// </summary>
		[Permissions(WriteRight = RepositoryRights.SetSha)]
		public string Sha { get; set; }

		/// <summary>
		/// The branch or tag HEAD points to
		/// </summary>
		[Permissions(WriteRight = RepositoryRights.SetReference)]
		public string Reference { get; set; }

		/// <summary>
		/// The name of the committer
		/// </summary>
		[Permissions(WriteRight = RepositoryRights.ChangeCommitter)]
		public string CommitterName { get; set; }

		/// <summary>
		/// The e-mail of the committer
		/// </summary>
		[Permissions(WriteRight = RepositoryRights.ChangeCommitter)]
		public string CommitterEmail { get; set; }

		/// <summary>
		/// Associated list of tag name -> sha for repository compiles. Not modifiable
		/// </summary>
		[Permissions(DenyWrite = true)]
		public IReadOnlyDictionary<string, string> Backups { get; set; }

		/// <summary>
		/// List of <see cref="TestMerge"/>s in the repository
		/// </summary>
		[Permissions(DenyWrite = true)]
		public List<TestMerge> TestMerges { get; set; }
		
		[Permissions(WriteRight = RepositoryRights.MergePullRequest)]
		public List<TestMergeParameters> NewTestMerges { get; set; }

		/// <summary>
		/// The username to access the git repository with
		/// </summary>
		[Permissions(ReadRight = RepositoryRights.ChangeCredentials, WriteRight = RepositoryRights.ChangeCredentials)]
		public string AccessUser { get; set; }

		/// <summary>
		/// The token/password to access the git repository with
		/// </summary>
		[Permissions(ReadRight = RepositoryRights.ChangeCredentials, WriteRight = RepositoryRights.ChangeCredentials)]
		public string AccessToken { get; set; }

		/// <summary>
		/// If commits created from testmerges are pushed to the remote
		/// </summary>
		[Permissions(WriteRight = RepositoryRights.ChangeTestMergeCommits)]
		public bool PushTestMergeCommits { get; set; }

		/// <summary>
		/// How often the <see cref="Repository"/> automatically updates in minutes
		/// </summary>
		[Permissions(WriteRight = RepositoryRights.ChangeAutoUpdate)]
		public int? AutoUpdateInterval { get; set; }
	}
}