using System.ComponentModel.DataAnnotations;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models.Internal
{
	public class RepositorySettings
	{
		/// <summary>
		/// The origin URL. If <see langword="null"/>, the <see cref="Repository"/> does not exist
		/// </summary>
		[Permissions(WriteRight = RepositoryRights.SetOrigin)]
		public string Origin { get; set; }

		/// <summary>
		/// The name of the committer
		/// </summary>
		[Permissions(WriteRight = RepositoryRights.ChangeCommitter)]
		[Required]
		public string CommitterName { get; set; }

		/// <summary>
		/// The e-mail of the committer
		/// </summary>
		[Permissions(WriteRight = RepositoryRights.ChangeCommitter)]
		[Required]
		public string CommitterEmail { get; set; }

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
