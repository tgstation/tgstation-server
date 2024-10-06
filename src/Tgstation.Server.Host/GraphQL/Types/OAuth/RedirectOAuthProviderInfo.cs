using System;

using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.GraphQL.Types.OAuth
{
	/// <summary>
	/// OAuth provider info with a <see cref="RedirectUri"/>.
	/// </summary>
	public class RedirectOAuthProviderInfo : BasicOAuthProviderInfo
	{
		/// <summary>
		/// The authentication server URL.
		/// </summary>
		public Uri RedirectUri { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="RedirectOAuthProviderInfo"/> class.
		/// </summary>
		/// <param name="providerInfo">The <see cref="OAuthProviderInfo"/> to build from.</param>
		public RedirectOAuthProviderInfo(OAuthProviderInfo providerInfo)
			: base(providerInfo)
		{
			RedirectUri = providerInfo!.RedirectUri ?? throw new InvalidOperationException("RedirectUri not set!");
		}
	}
}
