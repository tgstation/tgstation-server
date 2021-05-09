using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Parameters for creating a <see cref="TestMerge"/>.
	/// </summary>
	public class TestMergeParameters
	{
		/// <summary>
		/// The number of the test merge source.
		/// </summary>
		public int Number { get; set; }

		/// <summary>
		/// The sha of the test merge revision to merge. If not specified, the latest commit from the source will be used.
		/// </summary>
		[Required]
		[StringLength(40)]
		public virtual string? TargetCommitSha { get; set; }

		/// <summary>
		/// Optional comment about the test.
		/// </summary>
		[ResponseOptions]
		[StringLength(Limits.MaximumStringLength)]
		public string? Comment { get; set; }
	}
}
