using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models
{
	public class TestMergeParameters
	{
		public int Number { get; set; }

		[Required]
		public string PullRequestRevision { get; set; }
	}
}