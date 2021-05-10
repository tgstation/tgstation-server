using System;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Represents information about a current git revison.
	/// </summary>
	public class RevisionInformation
	{
		/// <summary>
		/// The revision SHA.
		/// </summary>
		[Required]
		[StringLength(Limits.MaximumCommitShaLength)]
		public string? CommitSha { get; set; }

		/// <summary>
		/// The timestamp of the revision.
		/// </summary>
		public DateTimeOffset Timestamp { get; set; }

		/// <summary>
		/// The SHA of the most recent remote commit.
		/// </summary>
		[Required]
		[StringLength(Limits.MaximumCommitShaLength)]
		public string? OriginCommitSha { get; set; }
	}
}
