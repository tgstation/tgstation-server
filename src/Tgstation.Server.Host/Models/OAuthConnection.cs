namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc cref="Api.Models.OAuthConnection" />
	public sealed class OAuthConnection : Api.Models.OAuthConnection, ILegacyApiTransformable<Api.Models.OAuthConnection>
	{
		/// <summary>
		/// The row Id.
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The owning <see cref="Models.User"/>.
		/// </summary>
		public User? User { get; set; }

		/// <inheritdoc />
		public Api.Models.OAuthConnection ToApi() => new()
		{
			Provider = Provider,
			ExternalUserId = ExternalUserId,
		};
	}
}
