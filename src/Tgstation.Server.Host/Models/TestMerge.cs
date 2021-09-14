using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class TestMerge : Api.Models.Internal.TestMergeApiBase, IApiTransformable<Api.Models.TestMerge>
	{
		/// <summary>
		/// The ID of the <see cref="TestMerge"/>.
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// <see cref="Api.Models.TestMergeParameters.TargetCommitSha"/>.
		/// </summary>
		[NotMapped]
		public new string TargetCommitSha
		{
			get => base.TargetCommitSha ?? throw new InvalidOperationException("TargetCommitSha was null!");
			set => base.TargetCommitSha = value;
		}

		/// <summary>
		/// Foreign key for <see cref="PrimaryRevisionInformation"/>.
		/// </summary>
		public long? PrimaryRevisionInformationId { get; set; }

		/// <summary>
		/// All the <see cref="RevInfoTestMerge"/> for the <see cref="TestMerge"/>.
		/// </summary>
		[BackingField(nameof(revisionInformations))]
		public ICollection<RevInfoTestMerge> RevisionInformations
		{
			get => revisionInformations ?? throw new InvalidOperationException("RevisionInformations not set!");
			set => revisionInformations = value;
		}

		/// <summary>
		/// See <see cref="Api.Models.TestMerge.MergedBy"/>.
		/// </summary>
		[Required]
		[BackingField(nameof(mergedBy))]
		public User MergedBy
		{
			get => mergedBy ?? throw new InvalidOperationException("MergedBy not set!");
			set => mergedBy = value;
		}

		/// <summary>
		/// The initial <see cref="RevisionInformation"/> the <see cref="TestMerge"/> was merged with.
		/// </summary>
		[Required]
		[BackingField(nameof(primaryRevisionInformation))]
		public RevisionInformation PrimaryRevisionInformation
		{
			get => primaryRevisionInformation ?? throw new InvalidOperationException("PrimaryRevisionInformation not set!");
			set => primaryRevisionInformation = value;
		}

		/// <summary>
		/// Backing field for <see cref="RevisionInformations"/>.
		/// </summary>
		ICollection<RevInfoTestMerge>? revisionInformations;

		/// <summary>
		/// Backing field for <see cref="PrimaryRevisionInformation"/>.
		/// </summary>
		RevisionInformation? primaryRevisionInformation;

		/// <summary>
		/// Backing field for <see cref="MergedBy"/>.
		/// </summary>
		User? mergedBy;

		/// <inheritdoc />
		public Api.Models.TestMerge ToApi() => new ()
		{
			Author = Author,
			BodyAtMerge = BodyAtMerge,
			MergedAt = MergedAt,
			TitleAtMerge = TitleAtMerge,
			Comment = Comment,
			MergedBy = MergedBy.CreateUserName(),
			Number = Number,
			TargetCommitSha = TargetCommitSha,
			Url = Url,
		};
	}
}
