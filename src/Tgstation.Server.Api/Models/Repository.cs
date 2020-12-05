using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a git repository
	/// </summary>
	public sealed class Repository : RepositorySettings, IGitRemoteInformation
	{
		/// <summary>
		/// The origin URL. If <see langword="null"/>, the <see cref="Repository"/> does not exist
		/// </summary>
		public string? Origin { get; set; }

		/// <summary>
		/// If submodules should be recursively cloned.
		/// </summary>
		public bool? RecurseSubmodules { get; set; }

		/// <summary>
		/// The commit HEAD should point to. Not populated in responses, use <see cref="RevisionInformation"/> instead for retrieval
		/// </summary>
		[StringLength(Limits.MaximumCommitShaLength)]
		public string? CheckoutSha { get; set; }

		/// <summary>
		/// The current <see cref="Models.RevisionInformation"/> for the <see cref="Repository"/>
		/// </summary>
		public RevisionInformation? RevisionInformation { get; set; }

		/// <inheritdoc />
		public RemoteGitProvider? RemoteGitProvider { get; set; }

		/// <inheritdoc />
		public string? RemoteRepositoryOwner { get; set; }

		/// <inheritdoc />
		public string? RemoteRepositoryName { get; set; }

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
		[StringLength(Limits.MaximumStringLength)]
		public string? Reference { get; set; }

		/// <summary>
		/// <see cref="TestMergeParameters"/> for new <see cref="TestMerge"/>s. Note that merges that conflict will not be performed
		/// </summary>
		public ICollection<TestMergeParameters>? NewTestMerges { get; set; }
	}
}
