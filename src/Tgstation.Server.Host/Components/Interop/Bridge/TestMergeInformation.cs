using System;

using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Host.Components.Interop.Bridge
{
	/// <summary>
	/// This model mirrors /datum/tgs_revision_information/test_merge.
	/// </summary>
	public sealed class TestMergeInformation : TestMergeModelBase
	{
		/// <summary>
		/// The unix time of when the test merge was applied.
		/// </summary>
		public string TimeMerged { get; set; }

		/// <summary>
		/// Backing field for <see cref="TargetCommitSha"/> needed to continue to support DMAPI 5.
		/// </summary>
		public string? PullRequestRevision { get; set; }

		/// <inheritdoc />
		public override string? TargetCommitSha
		{
			get => PullRequestRevision;
			set => PullRequestRevision = value;
		}

		/// <summary>
		/// The <see cref="RevisionInformation"/> of the <see cref="TestMergeInformation"/>.
		/// </summary>
		public RevisionInformation Revision { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TestMergeInformation"/> class.
		/// </summary>
		/// <param name="testMerge">The <see cref="Models.TestMerge"/> to build from.</param>
		/// <param name="revision">The value of <see cref="Revision"/>.</param>
		public TestMergeInformation(Models.TestMerge testMerge, RevisionInformation revision)
			: base(testMerge)
		{
			TimeMerged = testMerge?.MergedAt.ToString("O") ?? throw new ArgumentNullException(nameof(testMerge));
			Revision = revision ?? throw new ArgumentNullException(nameof(revision));
		}
	}
}
