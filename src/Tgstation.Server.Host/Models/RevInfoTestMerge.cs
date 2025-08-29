using System;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Many to many relationship for <see cref="Models.RevisionInformation"/> and <see cref="Models.TestMerge"/>.
	/// </summary>
	public sealed class RevInfoTestMerge
	{
		/// <summary>
		/// The row Id.
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The <see cref="Models.TestMerge"/>.
		/// </summary>
		public TestMerge TestMerge { get; set; } = null!; // recommended by EF

		/// <summary>
		/// The <see cref="Models.RevisionInformation"/>.
		/// </summary>
		public RevisionInformation RevisionInformation { get; set; } = null!; // recommended by EF

		/// <summary>
		/// Initializes a new instance of the <see cref="RevInfoTestMerge"/> class.
		/// </summary>
		[Obsolete("For use by EFCore only", true)]
		public RevInfoTestMerge()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RevInfoTestMerge"/> class.
		/// </summary>
		/// <param name="testMerge">The value of <see cref="TestMerge"/>.</param>
		/// <param name="revisionInformation">The value of <see cref="RevisionInformation"/>.</param>
		public RevInfoTestMerge(TestMerge testMerge, RevisionInformation revisionInformation)
		{
			TestMerge = testMerge ?? throw new ArgumentNullException(nameof(testMerge));
			RevisionInformation = revisionInformation ?? throw new ArgumentNullException(nameof(revisionInformation));
		}
	}
}
