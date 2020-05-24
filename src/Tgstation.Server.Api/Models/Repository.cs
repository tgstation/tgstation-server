using System.Collections.Generic;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a git repository
	/// </summary>
	public sealed class Repository : Internal.RepositorySettings
	{
		/// <summary>
		/// The origin URL. If <see langword="null"/>, the <see cref="Repository"/> does not exist
		/// </summary>
		public string? Origin { get; set; }

		/// <summary>
		/// The commit HEAD should point to. Not populated in responses, use <see cref="RevisionInformation"/> instead for retrieval
		/// </summary>
		public string? CheckoutSha { get; set; }

		/// <summary>
		/// The current <see cref="Models.RevisionInformation"/> for the <see cref="Repository"/>
		/// </summary>
		public RevisionInformation? RevisionInformation { get; set; }

		/// <summary>
		/// If the repository was cloned from GitHub.com this will be set with the owner of the repository
		/// </summary>
		public string? GitHubOwner { get; set; }

		/// <summary>
		/// If the repository was cloned from GitHub.com this will be set with the name of the repository
		/// </summary>
		public string? GitHubName { get; set; }

		/// <summary>
		/// The <see cref="Job"/> started by the <see cref="Repository"/> if any
		/// </summary>
		public Job? ActiveJob { get; set; }

		/// <summary>
		/// Do the equivalent of a git pull. Will attempt to merge unless <see cref="Reference"/> is also specified in which case a hard reset will be performed after checking out
		/// </summary>
		public bool? UpdateFromOrigin { get; set; }

		/// <summary>
		/// The branch or tag HEAD points to
		/// </summary>
		public string? Reference { get; set; }

		/// <summary>
		/// <see cref="TestMergeParameters"/> for new <see cref="TestMerge"/>s. Note that merges that conflict will not be performed
		/// </summary>
		public ICollection<TestMergeParameters>? NewTestMerges { get; set; }
	}
}