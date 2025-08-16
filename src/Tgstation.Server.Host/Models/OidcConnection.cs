using System.ComponentModel.DataAnnotations;

using Tgstation.Server.Host.GraphQL.Transformers;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc cref="Api.Models.OidcConnection" />
	public sealed class OidcConnection : Api.Models.OidcConnection,
		ILegacyApiTransformable<Api.Models.OidcConnection>,
		IApiTransformable<OidcConnection, GraphQL.Types.OAuth.OidcConnection, OidcConnectionTransformer>
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
		public Api.Models.OidcConnection ToApi() => new()
		{
			SchemeKey = SchemeKey,
			ExternalUserId = ExternalUserId,
		};
	}
}
