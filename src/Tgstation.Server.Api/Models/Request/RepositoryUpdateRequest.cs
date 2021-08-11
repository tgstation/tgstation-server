using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Api.Models.Request
{
	/// <summary>
	/// Represents a request to change the repository.
	/// </summary>
	public sealed class RepositoryUpdateRequest : RepositoryApiBase
	{
		/// <summary>
		/// The commit HEAD should point to.
		/// </summary>
		[StringLength(Limits.MaximumCommitShaLength)]
		public string? CheckoutSha { get; set; }

		/// <summary>
		/// Do the equivalent of a `git pull`. Will attempt to merge unless <see cref="RepositoryApiBase.Reference"/> is also specified in which case a hard reset will be performed after checking out.
		/// </summary>
		public bool? UpdateFromOrigin { get; set; }

		/// <summary>
		/// Do the equivalent of a `git submodule update --init --recursive` alongside any resets to origin, checkouts, or test merge additions.
		/// </summary>
		public bool? UpdateSubmodules { get; set; }

		/// <summary>
		/// <see cref="TestMergeParameters"/> for new <see cref="TestMerge"/>s. Note that merges that conflict will not be performed.
		/// </summary>
		public ICollection<TestMergeParameters>? NewTestMerges { get; set; }
	}
}
