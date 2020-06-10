using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class RevisionInformation : Api.Models.Internal.RevisionInformation
	{
		/// <summary>
		/// The row Id
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The instance <see cref="Api.Models.EntityId.Id"/>
		/// </summary>
		public long InstanceId { get; set; }

		/// <summary>
		/// The <see cref="Models.Instance"/> the <see cref="RevisionInformation"/> belongs to
		/// </summary>
		[Required]
		public Instance Instance { get; set; }

		/// <summary>
		/// See <see cref="Api.Models.RevisionInformation.PrimaryTestMerge"/>
		/// </summary>
		public TestMerge PrimaryTestMerge { get; set; }

		/// <summary>
		/// See <see cref="Api.Models.RevisionInformation.ActiveTestMerges"/>
		/// </summary>
		public List<RevInfoTestMerge> ActiveTestMerges { get; set; }

		/// <summary>
		/// See <see cref="CompileJob"/>s made from this <see cref="RevisionInformation"/>
		/// </summary>
		public List<CompileJob> CompileJobs { get; set; }

		/// <summary>
		/// Convert the <see cref="RevisionInformation"/> to it's API form
		/// </summary>
		/// <returns>A new <see cref="Api.Models.RevisionInformation"/></returns>
		public Api.Models.RevisionInformation ToApi() => new Api.Models.RevisionInformation
		{
			CommitSha = CommitSha,
			OriginCommitSha = OriginCommitSha,
			PrimaryTestMerge = PrimaryTestMerge?.ToApi(),
			ActiveTestMerges = ActiveTestMerges.Select(x => x.TestMerge.ToApi()).ToList(),
			CompileJobs = CompileJobs.Select(x => new Api.Models.EntityId
			{
				Id = x.Id
			}).ToList()
		};
	}
}
