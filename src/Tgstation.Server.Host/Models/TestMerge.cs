using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
		/// See <see cref="Api.Models.TestMerge.MergedBy"/>.
		/// </summary>
		[Required]
		public User MergedBy { get; set; }

		/// <summary>
		/// The initial <see cref="RevisionInformation"/> the <see cref="TestMerge"/> was merged with.
		/// </summary>
		[Required]
		public RevisionInformation PrimaryRevisionInformation { get; set; }

		/// <summary>
		/// Foreign key for <see cref="PrimaryRevisionInformation"/>.
		/// </summary>
		public long? PrimaryRevisionInformationId { get; set; }

		/// <summary>
		/// All the <see cref="RevInfoTestMerge"/> for the <see cref="TestMerge"/>.
		/// </summary>
		public ICollection<RevInfoTestMerge> RevisonInformations { get; set; }

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
