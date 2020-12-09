using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class TestMerge : Api.Models.Internal.TestMerge
	{
		/// <summary>
		/// See <see cref="Api.Models.TestMerge.MergedBy"/>
		/// </summary>
		[Required]
		public User MergedBy { get; set; }

		/// <summary>
		/// The initial <see cref="RevisionInformation"/> the <see cref="TestMerge"/> was merged with
		/// </summary>
		[Required]
		public RevisionInformation PrimaryRevisionInformation { get; set; }

		/// <summary>
		/// Foreign key for <see cref="PrimaryRevisionInformation"/>
		/// </summary>
		public long? PrimaryRevisionInformationId { get; set; }

		/// <summary>
		/// All the <see cref="RevInfoTestMerge"/> for the <see cref="TestMerge"/>
		/// </summary>
		public ICollection<RevInfoTestMerge> RevisonInformations { get; set; }

		/// <summary>
		/// Convert the <see cref="TestMerge"/> to it's API form
		/// </summary>
		/// <returns>A new <see cref="Api.Models.TestMerge"/></returns>
		public Api.Models.TestMerge ToApi() => new Api.Models.TestMerge
		{
			Author = Author,
			BodyAtMerge = BodyAtMerge,
			MergedAt = MergedAt,
			TitleAtMerge = TitleAtMerge,
			Comment = Comment,
			Id = Id,
			MergedBy = MergedBy.ToApi(false),
			Number = Number,
			TargetCommitSha = TargetCommitSha,
			Url = Url
		};
	}
}
