using System.ComponentModel.DataAnnotations;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Represents configurable settings for a <see cref="Repository"/>
	/// </summary>
	public class RepositorySettings
	{
		/// <summary>
		/// The name of the committer
		/// </summary>
		[Required]
		public string CommitterName { get; set; }

		/// <summary>
		/// The e-mail of the committer
		/// </summary>
		[Required]
		public string CommitterEmail { get; set; }

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
		[Required]
		public bool? PushTestMergeCommits { get; set; }

		/// <summary>
		/// If test merge commits are signed with the username of the person who merged it. Note this only affects future commits
		/// </summary>
		[Required]
		public bool? ShowTestMergeCommitters { get; set; }

		/// <summary>
		/// If test merge commits should be kept when auto updating. May cause merge conflicts which will block the update
		/// </summary>
		[Required]
		public bool? AutoUpdatesKeepTestMerges { get; set; }

		/// <summary>
		/// If synchronization should occur when auto updating
		/// </summary>
		[Required]
		public bool? AutoUpdatesSynchronize { get; set; }
	}
}
