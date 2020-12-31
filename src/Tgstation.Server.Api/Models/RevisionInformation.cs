using System.Collections.Generic;

namespace Tgstation.Server.Api.Models
{
	/// <inheritdoc />
	public sealed class RevisionInformation : Internal.RevisionInformation
	{
		/// <summary>
		/// The <see cref="TestMerge"/> that was created with this <see cref="RevisionInformation"/>
		/// </summary>
		public TestMerge? PrimaryTestMerge { get; set; }

		/// <summary>
		/// The <see cref="TestMerge"/>s active in the <see cref="RevisionInformation"/>
		/// </summary>
		public ICollection<TestMerge>? ActiveTestMerges { get; set; }

		/// <summary>
		/// The <see cref="CompileJob"/>s made from the <see cref="RevisionInformation"/>
		/// </summary>
		public ICollection<EntityId>? CompileJobs { get; set; }
	}
}
