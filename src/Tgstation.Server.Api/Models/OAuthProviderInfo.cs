using System;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Public information about a given <see cref="OAuthProvider"/>.
	/// </summary>
	public sealed class OAuthProviderInfo
	{
		/// <summary>
		/// The client ID.
		/// </summary>
		public string? ClientId { get; set; }

		/// <summary>
		/// The redirect URL.
		/// </summary>
		public Uri? RedirectUri { get; set; }

		/// <summary>
		/// The server URL.
		/// </summary>
		[ResponseOptions]
		public Uri? ServerUrl { get; set; }

		/// <summary>
		/// If <see langword="true"/> the OAuth provider may only be used for gateway authentication. If <see langword="false"/> the OAuth provider may be used for server logins or gateway authentication. If <see langword="null"/> the OAuth provider may only be used for server logins.
		/// </summary>
		public bool? GatewayOnly { get; set; }
	}
}
