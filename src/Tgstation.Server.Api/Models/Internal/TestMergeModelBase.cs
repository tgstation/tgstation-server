using System;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Layer of test merge data required internally.
	/// </summary>
	public abstract class TestMergeModelBase : TestMergeParameters
	{
		/// <summary>
		/// The title of the test merge source.
		/// </summary>
		/// <example>Fixes and Breaks everything</example>
		[Required]
		public string? TitleAtMerge { get; set; }

		/// <summary>
		/// The body of the test merge source.
		/// </summary>
		/// <example># GitHub markdown\n\rI assume?</example>
		[Required]
		public string? BodyAtMerge { get; set; }

		/// <summary>
		/// The URL of the test merge source.
		/// </summary>
		/// <example>https://github.com/tgstation/tgstation/pull/31026</example>
		[Required]
#pragma warning disable CA1056 // Uri properties should not be strings
		public string? Url { get; set; }
#pragma warning restore CA1056 // Uri properties should not be strings

		/// <summary>
		/// The author of the test merge source.
		/// </summary>
		/// <example>MrStonedOne</example>
		[Required]
		public string? Author { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TestMergeModelBase"/> class.
		/// </summary>
		protected TestMergeModelBase()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TestMergeModelBase"/> class.
		/// </summary>
		/// <param name="copy">The <see cref="TestMergeModelBase"/> to copy data from.</param>
		protected TestMergeModelBase(TestMergeModelBase copy)
		{
			if (copy == null)
				throw new ArgumentNullException(nameof(copy));
			Author = copy.Author;
			BodyAtMerge = copy.BodyAtMerge;
			Comment = copy.Comment;
			Number = copy.Number;
			SourceRepository = copy.SourceRepository;
			TargetCommitSha = copy.TargetCommitSha;
			TitleAtMerge = copy.TitleAtMerge;
			Url = copy.Url;
		}
	}
}
