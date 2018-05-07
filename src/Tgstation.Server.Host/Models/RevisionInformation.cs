using System.Collections.Generic;
using System.Linq;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class RevisionInformation : Api.Models.Internal.RevisionInformation, IApiConvertable<Api.Models.RevisionInformation>
	{
		/// <summary>
		/// The row Id
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// See <see cref="Api.Models.RevisionInformation.TestMerges"/>
		/// </summary>
		public List<TestMerge> TestMerges { get; set; }

		/// <inheritdoc />
		public Api.Models.RevisionInformation ToApi() => new Api.Models.RevisionInformation
		{
			Commit = Commit,
			OriginRevision = OriginRevision,
			TestMerges = TestMerges.Select(x => x.ToApi()).ToList()
		};
	}
}
