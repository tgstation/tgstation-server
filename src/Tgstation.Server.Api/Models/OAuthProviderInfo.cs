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
		public Uri? ServerUrl { get; set; }
	}
}
