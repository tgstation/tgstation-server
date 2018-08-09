using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// This model mirrors /datum/tgs_revision_information/test_merge
	/// </summary>
	sealed class TestMerge : TestMergeBase
	{
		/// <summary>
		/// The unix time of when the test merge was applied
		/// </summary>
		public long TimeMerged { get; set; }

		/// <summary>
		/// The <see cref="RevisionInformation"/> of the <see cref="TestMerge"/>
		/// </summary>
		public RevisionInformation Revision { get; set; }

		/// <summary>
		/// Construct a <see cref="TestMerge"/>
		/// </summary>
		/// <param name="testMerge">The <see cref="Models.TestMerge"/> to build from</param>
		public TestMerge(Models.TestMerge testMerge) : base(testMerge)
		{
			TimeMerged = testMerge.MergedAt.Ticks;
			Revision = testMerge.PrimaryRevisionInformation;
		}
	}
}
