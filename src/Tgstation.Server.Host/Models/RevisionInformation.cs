using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using Microsoft.EntityFrameworkCore;

using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class RevisionInformation : Api.Models.Internal.RevisionInformation, IApiTransformable<Api.Models.RevisionInformation>
	{
		/// <summary>
		/// The row Id.
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The instance <see cref="EntityId.Id"/>.
		/// </summary>
		public long InstanceId { get; set; }

		/// <summary>
		/// The <see cref="Models.Instance"/> the <see cref="RevisionInformation"/> belongs to.
		/// </summary>
		[Required]
		[BackingField(nameof(instance))]
		public Instance Instance
		{
			get => instance ?? throw new InvalidOperationException("Instance not set!");
			set => instance = value;
		}

		/// <summary>
		/// <see cref="Api.Models.Internal.RevisionInformation.CommitSha"/>.
		/// </summary>
		[StringLength(Limits.MaximumCommitShaLength)]
		public new string CommitSha
		{
			get => base.CommitSha ?? throw new InvalidOperationException("CommitSha was null!");
			set => base.CommitSha = value;
		}

		/// <summary>
		/// <see cref="Api.Models.Internal.RevisionInformation.OriginCommitSha"/>.
		/// </summary>
		[StringLength(Limits.MaximumCommitShaLength)]
		public new string OriginCommitSha
		{
			get => base.OriginCommitSha ?? throw new InvalidOperationException("OriginCommitSha was null!");
			set => base.OriginCommitSha = value;
		}

		/// <summary>
		/// See <see cref="Api.Models.RevisionInformation.PrimaryTestMerge"/>.
		/// </summary>
		public TestMerge? PrimaryTestMerge { get; set; }

		/// <summary>
		/// See <see cref="Api.Models.RevisionInformation.ActiveTestMerges"/>.
		/// </summary>
		[BackingField(nameof(activeTestMerges))]
		public ICollection<RevInfoTestMerge> ActiveTestMerges
		{
			get => activeTestMerges ?? throw new InvalidOperationException("ActiveTestMerges not set!");
			set => activeTestMerges = value;
		}

		/// <summary>
		/// See <see cref="CompileJob"/>s made from this <see cref="RevisionInformation"/>.
		/// </summary>
		[BackingField(nameof(compileJobs))]
		public ICollection<CompileJob> CompileJobs
		{
			get => compileJobs ?? throw new InvalidOperationException("CompileJobs not set!");
			set => compileJobs = value;
		}

		/// <summary>
		/// Backing field for <see cref="Instance"/>.
		/// </summary>
		Instance? instance;

		/// <summary>
		/// Backing field for <see cref="ActiveTestMerges"/>.
		/// </summary>
		ICollection<RevInfoTestMerge>? activeTestMerges;

		/// <summary>
		/// Backing field for <see cref="CompileJobs"/>.
		/// </summary>
		ICollection<CompileJob>? compileJobs;

		/// <inheritdoc />
		public Api.Models.RevisionInformation ToApi() => new ()
		{
			CommitSha = CommitSha,
			Timestamp = Timestamp,
			OriginCommitSha = OriginCommitSha,
			PrimaryTestMerge = PrimaryTestMerge?.ToApi(),
			ActiveTestMerges = ActiveTestMerges.Select(x => x.TestMerge.ToApi()).ToList(),
			CompileJobs = CompileJobs.Select(x => new EntityId
			{
				Id = x.Id,
			}).ToList(),
		};
	}
}
