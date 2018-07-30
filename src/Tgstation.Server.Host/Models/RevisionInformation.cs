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
		/// The <see cref="Models.Instance"/> the <see cref="RevisionInformation"/> belongs to
		/// </summary>
		public Instance Instance { get; set; }

		/// <summary>
		/// See <see cref="Api.Models.RevisionInformation.TestMerges"/>
		/// </summary>
		public List<TestMerge> TestMerges { get; set; }

		/// <summary>
		/// See <see cref="CompileJob"/>s made from this <see cref="RevisionInformation"/>
		/// </summary>
		public List<CompileJob> CompileJobs { get; set; }

		/// <inheritdoc />
		public Api.Models.RevisionInformation ToApi() => new Api.Models.RevisionInformation
		{
			CommitSha = CommitSha,
			OriginCommitSha = OriginCommitSha,
			TestMerges = TestMerges.Select(x => x.ToApi()).ToList(),
			CompileJobs = CompileJobs.Select(x => x.ToApi()).ToList()
		};
	}
}
