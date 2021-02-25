using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Represents configurable settings for a git repository.
	/// </summary>
	public class RepositorySettings
	{
		/// <summary>
		/// The name of the committer
		/// </summary>
		[Required]
		[StringLength(Limits.MaximumStringLength)]
		public string? CommitterName { get; set; }

		/// <summary>
		/// The e-mail of the committer
		/// </summary>
		[Required]
		[StringLength(Limits.MaximumStringLength)]
		[EmailAddress]
		public string? CommitterEmail { get; set; }

		/// <summary>
		/// The username to access the git repository with
		/// </summary>
		[StringLength(Limits.MaximumStringLength)]
		[ResponseOptions]
		public string? AccessUser { get; set; }

		/// <summary>
		/// The token/password to access the git repository with
		/// </summary>
		[StringLength(Limits.MaximumStringLength)]
		[ResponseOptions(Presence = FieldPresence.Ignored)]
		public string? AccessToken { get; set; }

		/// <summary>
		/// If commits created from testmerges are pushed to the remote. Requires <see cref="AccessUser"/> and <see cref="AccessToken"/> to be set to function.
		/// </summary>
		[Required]
		public bool? PushTestMergeCommits { get; set; }

		/// <summary>
		/// If GitHub deployments should be created. Requires <see cref="AccessUser"/>, <see cref="AccessToken"/>, and <see cref="PushTestMergeCommits"/> to be set to function.
		/// </summary>
		[Required]
		public bool? CreateGitHubDeployments { get; set; }

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
		/// If synchronization should occur when auto updating. Requries <see cref="AccessUser"/> and <see cref="AccessToken"/> to be set to function.
		/// </summary>
		[Required]
		public bool? AutoUpdatesSynchronize { get; set; }

		/// <summary>
		/// If test merging should create a comment. Requires <see cref="AccessToken"/> to be set to function.
		/// </summary>
		[Required]
		public bool? PostTestMergeComment { get; set; }
	}
}
