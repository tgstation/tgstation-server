using System;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Represents a test merge of a remote "pull request".
	/// </summary>
	public class TestMerge : TestMergeBase
	{
		/// <summary>
		/// The ID of the <see cref="TestMerge"/>
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// When the <see cref="TestMerge"/> was created
		/// </summary>
		[Required]
		public DateTimeOffset MergedAt { get; set; }
	}
}
