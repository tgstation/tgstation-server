using System;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Represents a test merge of a remote commit source.
	/// </summary>
	public class TestMergeApiBase : TestMergeModelBase
	{
		/// <summary>
		/// When the <see cref="TestMergeApiBase"/> was created.
		/// </summary>
		[Required]
		public DateTimeOffset MergedAt { get; set; }
	}
}
