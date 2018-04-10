using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Represent a merge of a live pull request
	/// </summary>
	sealed class TestMerge : Api.Models.Internal.TestMerge
	{
		[Required]
		public User MergedBy { get; set; }
	}
}
