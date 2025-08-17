using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc cref="Api.Models.OAuthConnection" />
	public sealed class OAuthConnection : Api.Models.OAuthConnection,
		ILegacyApiTransformable<Api.Models.OAuthConnection>
	{
		/// <summary>
		/// The row Id.
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The <see cref="Api.Models.EntityId.Id"/> of <see cref="User"/>.
		/// </summary>
		public long UserId { get; set; }

		/// <summary>
		/// The owning <see cref="Models.User"/>.
		/// </summary>
		[Required]
		public User? User { get; set; }

		/// <inheritdoc />
		public Api.Models.OAuthConnection ToApi() => new()
		{
			Provider = Provider,
			ExternalUserId = ExternalUserId,
		};
	}
}
