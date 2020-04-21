using System;
using System.Globalization;
using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Host.Components.Interop.Runtime
{
	/// <summary>
	/// This model mirrors /datum/tgs_revision_information/test_merge
	/// </summary>
	sealed class RuntimeTestMerge : TestMergeBase
	{
		/// <summary>
		/// The unix time of when the test merge was applied
		/// </summary>
		public string TimeMerged { get; set; }

		/// <summary>
		/// The <see cref="RevisionInformation"/> of the <see cref="RuntimeTestMerge"/>
		/// </summary>
		public RevisionInformation Revision { get; set; }

		/// <summary>
		/// Construct a <see cref="RuntimeTestMerge"/>
		/// </summary>
		/// <param name="testMerge">The <see cref="Models.TestMerge"/> to build from</param>
		/// <param name="revision">The value of <see cref="Revision"/></param>
		public RuntimeTestMerge(Models.TestMerge testMerge, RevisionInformation revision) : base(testMerge)
		{
			TimeMerged = testMerge.MergedAt.Ticks.ToString(CultureInfo.InvariantCulture);
			Revision = revision ?? throw new ArgumentNullException(nameof(revision));
		}
	}
}
