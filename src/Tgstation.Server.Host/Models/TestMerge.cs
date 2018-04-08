using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Represent a merge of a live pull request
	/// </summary>
	sealed class TestMerge : Api.Models.TestMerge
	{
		[Required]
		new public DbUser MergedBy { get; set; }
	}
}
