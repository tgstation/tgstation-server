namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class OAuthConnection : Api.Models.OAuthConnection, IApiTransformable<Api.Models.OAuthConnection>
	{
		/// <summary>
		/// The row Id.
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The owning <see cref="Models.User"/>.
		/// </summary>
		public User User { get; set; }

		/// <inheritdoc />
		public Api.Models.OAuthConnection ToApi() => new Api.Models.OAuthConnection
		{
			Provider = Provider,
			ExternalUserId = ExternalUserId,
		};
	}
}
