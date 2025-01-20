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
		/// <example>31026</example>
		public int Number { get; set; }

		/// <summary>
		/// The sha of the test merge revision to merge. If not specified, the latest commit from the source will be used.
		/// </summary>
		/// <example>caa1e1f400c8b6a535e03cff28cf57f919e9378c</example>
		[Required]
		[StringLength(Limits.MaximumCommitShaLength, MinimumLength = Limits.MaximumCommitShaLength)]
		public virtual string? TargetCommitSha { get; set; }

		/// <summary>
		/// Optional comment about the test.
		/// </summary>
		/// <example>this will fix everything -Admin</example>
		[ResponseOptions]
		[StringLength(Limits.MaximumStringLength)]
		public string? Comment { get; set; }
	}
}
