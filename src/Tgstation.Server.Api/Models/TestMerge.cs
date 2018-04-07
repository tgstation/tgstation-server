using System;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models
{
	public class TestMerge : TestMergeParameters
	{
		public long Id { get; set; }

		[Required]
		public string OriginCommit { get; set; }

		public string MergeCommit { get; set; }

		[Required]
		public DateTimeOffset MergedAt { get; set; }

		[Required]
		public DateTimeOffset RemovedAt { get; set; }
		
		public User MergedBy { get; set; }

		[Required]
		public string TitleAtMerge { get; set; }

		[Required]
		public string BodyAtMerge { get; set; }

		[Required]
		public string Author { get; set; }
	}
}