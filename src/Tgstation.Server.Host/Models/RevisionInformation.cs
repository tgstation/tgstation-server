using System.Collections.Generic;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	sealed class RevisionInformation : Api.Models.Internal.RevisionInformation
	{
		/// <summary>
		/// The row Id
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// See <see cref="Api.Models.RevisionInformation.TestMerges"/>
		/// </summary>
		public List<TestMerge> TestMerges { get; set; }
	}
}
