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
		/// The ID of the <see cref="TestMergeApiBase"/>.
		/// </summary>
		/// <example>1</example>
		public long Id { get; set; }

		/// <summary>
		/// When the <see cref="TestMergeApiBase"/> was created.
		/// </summary>
		[Required]
		public DateTimeOffset MergedAt { get; set; }
	}
}
