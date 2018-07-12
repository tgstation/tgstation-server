using System;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Represents a merge of a GitHub pull request
	/// </summary>
	public class TestMerge : TestMergeParameters
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

		/// <summary>
		/// The title of the pull request
		/// </summary>
		[Required]
		public string TitleAtMerge { get; set; }

		/// <summary>
		/// The body of the pull request
		/// </summary>
		[Required]
		public string BodyAtMerge { get; set; }

		/// <summary>
		/// The URL of the pull request
		/// </summary>
		[Required]
#pragma warning disable CA1056 // Uri properties should not be strings
		public string Url { get; set; }
#pragma warning restore CA1056 // Uri properties should not be strings

		/// <summary>
		/// The author of the pull request
		/// </summary>
		[Required]
		public string Author { get; set; }
	}
}
