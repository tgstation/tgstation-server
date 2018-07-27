using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class TestMerge : Api.Models.Internal.TestMerge, IApiConvertable<Api.Models.TestMerge>
	{
		/// <summary>
		/// See <see cref="Api.Models.TestMerge.MergedBy"/>
		/// </summary>
		[Required]
		public User MergedBy { get; set; }
		
		/// <summary>
		/// The <see cref="Models.RevisionInformation"/> for the <see cref="TestMerge"/>
		/// </summary>
		[Required]
		public RevisionInformation RevisionInformation { get; set; }

		/// <inheritdoc />
		public Api.Models.TestMerge ToApi() => new Api.Models.TestMerge
		{
			Author = Author,
			BodyAtMerge = BodyAtMerge,
			MergedAt = MergedAt,
			TitleAtMerge = TitleAtMerge,
			Comment = Comment,
			Id = Id,
			MergedBy = MergedBy.ToApi(),
			Number =Number,
			PullRequestRevision = PullRequestRevision,
			Url = Url
		};
	}
}
