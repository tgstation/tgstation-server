using System;

using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.GraphQL.Types.OAuth
{
	/// <summary>
	/// OAuth provider info with a <see cref="RedirectOAuthProviderInfo.RedirectUri"/> and <see cref="ServerUrl"/>.
	/// </summary>
	public sealed class FullOAuthProviderInfo : RedirectOAuthProviderInfo
	{
		/// <summary>
		/// The remote service URL.
		/// </summary>
		public Uri ServerUrl { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="FullOAuthProviderInfo"/> class.
		/// </summary>
		/// <param name="providerInfo">The <see cref="OAuthProviderInfo"/> to build from.</param>
		public FullOAuthProviderInfo(OAuthProviderInfo providerInfo)
			: base(providerInfo)
		{
			ArgumentNullException.ThrowIfNull(providerInfo);

			ServerUrl = providerInfo.ServerUrl ?? throw new InvalidOperationException("Missing OAuthProviderInfo ServerUrl!");
		}
	}
}
