using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Parameters for creating a <see cref="TestMerge"/>
	/// </summary>
	public class TestMergeParameters
	{
		/// <summary>
		/// The number of the pull request
		/// </summary>
		public int Number { get; set; }

		/// <summary>
		/// The sha of the pull request revision to merge
		/// </summary>
		[Required]
		public string PullRequestRevision { get; set; }

		/// <summary>
		/// Optional comment about the test
		/// </summary>
		public string Comment { get; set; }
	}
}