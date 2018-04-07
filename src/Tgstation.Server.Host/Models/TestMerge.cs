using System;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Represent a merge of a live pull request
	/// </summary>
	sealed class TestMerge
	{
		long Id { get; set; }

		int Number { get; set; }

		[Required]
		string OriginCommit { get; set; }

		[Required]
		string PullRequestCommit { get; set; }
		
		string MergeCommit { get; set; }

		[Required]
		DateTimeOffset MergedAt { get; set; }

		[Required]
		DateTimeOffset RemovedAt { get; set; }

		[Required]
		User MergedBy { get; set; }

		[Required]
		string TitleAtMerge { get; set; }

		[Required]
		string BodyAtMerge { get; set; }

		[Required]
		string Author { get; set; }
	}
}
