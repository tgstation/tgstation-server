using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc cref="Api.Models.OidcConnection" />
	public sealed class OidcConnection : Api.Models.OidcConnection,
		ILegacyApiTransformable<Api.Models.OidcConnection>
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
		public User User { get; set; } = null!; // recommended by EF

		/// <inheritdoc />
		public Api.Models.OidcConnection ToApi() => new()
		{
			SchemeKey = SchemeKey,
			ExternalUserId = ExternalUserId,
		};
	}
}
