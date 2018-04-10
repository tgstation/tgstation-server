using System;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models.Internal
{
	public class TestMerge
	{
		public long Id { get; set; }

		[Required]
		public DateTimeOffset MergedAt { get; set; }

		[Required]
		public string TitleAtMerge { get; set; }

		[Required]
		public string BodyAtMerge { get; set; }

		[Required]
		public string Author { get; set; }
	}
}
