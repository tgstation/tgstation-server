using System;

namespace Tgstation.Server.Host.GraphQL.Types.OAuth
{
	/// <summary>
	/// Represents a valid OAuth connection.
	/// </summary>
	public sealed class OidcConnection
	{
		/// <summary>
		/// The scheme key of the <see cref="OidcConnection"/>.
		/// </summary>]
		public string SchemeKey { get; }

		/// <summary>
		/// The ID of the user in the OIDC provider ("sub" claim).
		/// </summary>
		public string ExternalUserId { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="OidcConnection"/> class.
		/// </summary>
		/// <param name="externalUserId">The value of <see cref="ExternalUserId"/>.</param>
		/// <param name="schemeKey">The value of <see cref="SchemeKey"/>.</param>
		public OidcConnection(string externalUserId, string schemeKey)
		{
			ExternalUserId = externalUserId ?? throw new ArgumentNullException(nameof(externalUserId));
			SchemeKey = schemeKey ?? throw new ArgumentNullException(nameof(schemeKey));
		}
	}
}
