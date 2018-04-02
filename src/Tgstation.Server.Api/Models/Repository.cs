using System.Collections.Generic;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a git repository
	/// </summary>
	public sealed class Repository
	{
		/// <summary>
		/// The origin URL. If <see langword="null"/>, the <see cref="Repository"/> does not exist
		/// </summary>
		public string Origin { get; set; }

		/// <summary>
		/// The commit HEAD points to
		/// </summary>
		public string Sha { get; set; }

		/// <summary>
		/// The branch HEAD points to
		/// </summary>
		public string Branch { get; set; }

		/// <summary>
		/// The name of the committer
		/// </summary>
		public string CommitterName { get; set; }

		/// <summary>
		/// The e-mail of the committer
		/// </summary>
		public string CommitterEmail { get; set; }

		/// <summary>
		/// Associated list of tag name -> sha for repository compiles. Not modifiable
		/// </summary>
		public IReadOnlyDictionary<string, string> Backups { get; set; }

		/// <summary>
		/// Associated list of GitHub pull request number -> sha for merged pull requests. Adding a <see langword="null"/> value to this list will merge the latest commit of the pull request numbered by the key
		/// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
		public Dictionary<int, string> PullRequests { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
		
		/// <summary>
		/// The username to access the git repository with
		/// </summary>
		public string AccessUser { get; set; }

		/// <summary>
		/// The token/password to access the git repository with
		/// </summary>
		public string AccessToken { get; set; }

		/// <summary>
		/// If commits created from testmerges are pushed to the remote
		/// </summary>
		public bool PushTestMergeCommits { get; set; }

		/// <summary>
		/// How often the <see cref="Repository"/> automatically updates in minutes
		/// </summary>
		public int? AutoUpdateInterval { get; set; }
	}
}