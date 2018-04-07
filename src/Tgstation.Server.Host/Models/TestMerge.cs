using System;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Represent a merge of a live pull request
	/// </summary>
	sealed class TestMerge : Api.Models.TestMerge
	{
		[Required]
		new User MergedBy { get; set; }
	}
}
