using System;
using System.Collections.Generic;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc cref="Api.Models.Internal.TestMergeApiBase" />
	public sealed class TestMerge : Api.Models.Internal.TestMergeApiBase, ILegacyApiTransformable<Api.Models.TestMerge>
	{
		/// <summary>
		/// See <see cref="Api.Models.TestMerge.MergedBy"/>.
		/// </summary>
		public User MergedBy { get; set; } = null!; // recommended by EF

		/// <summary>
		/// The initial <see cref="RevisionInformation"/> the <see cref="TestMerge"/> was merged with.
		/// </summary>
		public RevisionInformation PrimaryRevisionInformation { get; set; } = null!; // recommended by EF

		/// <summary>
		/// Foreign key for <see cref="PrimaryRevisionInformation"/>.
		/// </summary>
		public long PrimaryRevisionInformationId { get; set; }

		/// <summary>
		/// All the <see cref="RevInfoTestMerge"/> for the <see cref="TestMerge"/>.
		/// </summary>
		public ICollection<RevInfoTestMerge> RevisonInformations { get; set; } = null!; // recommended by EF

		/// <inheritdoc />
		public Api.Models.TestMerge ToApi() => new()
		{
			Author = Author,
			BodyAtMerge = BodyAtMerge,
			MergedAt = MergedAt,
			TitleAtMerge = TitleAtMerge,
			Comment = Comment,
			Id = Id,
			MergedBy = (MergedBy ?? throw new InvalidOperationException("MergedBy must be set!")).CreateUserName(),
			Number = Number,
			TargetCommitSha = TargetCommitSha,
			Url = Url,
		};
	}
}
