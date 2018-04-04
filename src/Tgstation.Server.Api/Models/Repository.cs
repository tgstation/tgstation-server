using System.Collections.Generic;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a git repository
	/// </summary>
	[Model(typeof(RepositoryRights), ReadRight = RepositoryRights.Read)]
	public sealed class Repository
	{
		/// <summary>
		/// The origin URL. If <see langword="null"/>, the <see cref="Repository"/> does not exist
		/// </summary>
		[Permissions(ComplexWrite = true)]
		public string Origin { get; set; }

		/// <summary>
		/// The commit HEAD points to
		/// </summary>
		[Permissions(WriteRight = RepositoryRights.SetSha)]
		public string Sha { get; set; }

		/// <summary>
		/// The branch or tag HEAD points to
		/// </summary>
		[Permissions(ComplexWrite = true)]
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
		/// Associated list of GitHub pull request number -> sha for merged pull requests. Adding a <see langword="null"/> value to this list will merge the latest commit of the pull request numbered by the key
		/// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
		[Permissions(ComplexWrite = true)]
		public Dictionary<int, string> PullRequests { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
		
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