using System;
using System.Globalization;
using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Host.Components.Interop
{
	/// <summary>
	/// This model mirrors /datum/tgs_revision_information/test_merge
	/// </summary>
	sealed class TestMerge : TestMergeBase
	{
		/// <summary>
		/// The unix time of when the test merge was applied
		/// </summary>
		public string TimeMerged { get; set; }

		/// <summary>
		/// The <see cref="RevisionInformation"/> of the <see cref="TestMerge"/>
		/// </summary>
		public RevisionInformation Revision { get; set; }

		/// <summary>
		/// Construct a <see cref="TestMerge"/>
		/// </summary>
		/// <param name="testMerge">The <see cref="Models.TestMerge"/> to build from</param>
		/// <param name="revision">The value of <see cref="Revision"/></param>
		public TestMerge(Models.TestMerge testMerge, RevisionInformation revision) : base(testMerge)
		{
			TimeMerged = testMerge.MergedAt.Ticks.ToString(CultureInfo.InvariantCulture);
			Revision = revision ?? throw new ArgumentNullException(nameof(revision));
		}
	}
}
