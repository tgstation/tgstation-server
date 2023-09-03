using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc cref="Api.Models.Internal.RevisionInformation" />
	public sealed class RevisionInformation : Api.Models.Internal.RevisionInformation, IApiTransformable<Api.Models.RevisionInformation>
	{
		/// <summary>
		/// The row Id.
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The instance <see cref="Api.Models.EntityId.Id"/>.
		/// </summary>
		public long InstanceId { get; set; }

		/// <summary>
		/// The <see cref="Models.Instance"/> the <see cref="RevisionInformation"/> belongs to.
		/// </summary>
		[Required]
		public Instance Instance { get; set; }

		/// <summary>
		/// See <see cref="Api.Models.RevisionInformation.PrimaryTestMerge"/>.
		/// </summary>
		public TestMerge PrimaryTestMerge { get; set; }

		/// <summary>
		/// See <see cref="Api.Models.RevisionInformation.ActiveTestMerges"/>.
		/// </summary>
		public ICollection<RevInfoTestMerge> ActiveTestMerges { get; set; }

		/// <summary>
		/// See <see cref="CompileJob"/>s made from this <see cref="RevisionInformation"/>.
		/// </summary>
		public ICollection<CompileJob> CompileJobs { get; set; }

		/// <inheritdoc />
		public Api.Models.RevisionInformation ToApi() => new Api.Models.RevisionInformation
		{
			CommitSha = CommitSha,
			Timestamp = Timestamp,
			OriginCommitSha = OriginCommitSha,
			PrimaryTestMerge = PrimaryTestMerge?.ToApi(),
			ActiveTestMerges = ActiveTestMerges.Select(x => x.TestMerge.ToApi()).ToList(),
			CompileJobs = CompileJobs.Select(x => new Api.Models.EntityId
			{
				Id = x.Id,
			}).ToList(),
		};
	}
}
