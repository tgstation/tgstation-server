using System;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// A securely generated token.
	/// </summary>
	public record struct GeneratedToken
	{
		/// <summary>
		/// The token string.
		/// </summary>
		public required string Token { get; init; }

		/// <summary>
		/// When the token expires.
		/// </summary>
		public required DateTimeOffset Expiry { get; init; }
	}
}
