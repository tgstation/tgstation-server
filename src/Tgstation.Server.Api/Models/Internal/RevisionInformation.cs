using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Represents information about a current git revison
	/// </summary>
	public class RevisionInformation
	{
		/// <summary>
		/// The revision sha
		/// </summary>
		[Required]
		[StringLength(Limits.MaximumCommitShaLength)]
		public string? CommitSha { get; set; }

		/// <summary>
		/// The sha of the most recent remote commit
		/// </summary>
		[Required]
		[StringLength(Limits.MaximumCommitShaLength)]
		public string? OriginCommitSha { get; set; }
	}
}
