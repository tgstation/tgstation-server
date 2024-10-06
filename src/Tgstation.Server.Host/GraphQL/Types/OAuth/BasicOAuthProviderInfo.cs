using System;

using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.GraphQL.Types.OAuth
{
	/// <summary>
	/// Basic OAuth provider info.
	/// </summary>
	public class BasicOAuthProviderInfo
	{
		/// <summary>
		/// The client ID.
		/// </summary>
		public string ClientID { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="BasicOAuthProviderInfo"/> class.
		/// </summary>
		/// <param name="providerInfo">The <see cref="OAuthProviderInfo"/> to build from.</param>
		public BasicOAuthProviderInfo(OAuthProviderInfo providerInfo)
		{
			ArgumentNullException.ThrowIfNull(providerInfo);

			ClientID = providerInfo.ClientId ?? throw new InvalidOperationException("ClientID not set!");
		}
	}
}
