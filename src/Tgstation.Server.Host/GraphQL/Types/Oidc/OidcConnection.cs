namespace Tgstation.Server.Host.GraphQL.Types.OAuth
{
	/// <summary>
	/// Represents a valid OAuth connection.
	/// </summary>
	public sealed class OidcConnection
	{
		/// <summary>
		/// The scheme key of the <see cref="OidcConnection"/>.
		/// </summary>
		public required string SchemeKey { get; init; }

		/// <summary>
		/// The ID of the user in the OIDC provider ("sub" claim).
		/// </summary>
		public required string ExternalUserId { get; init; }
	}
}
