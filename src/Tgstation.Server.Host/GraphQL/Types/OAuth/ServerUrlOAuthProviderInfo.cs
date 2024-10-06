using System;

using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.GraphQL.Types.OAuth
{
	/// <summary>
	/// OAuth provider info with a <see cref="ServerUrl"/>.
	/// </summary>
	public sealed class ServerUrlOAuthProviderInfo : BasicOAuthProviderInfo
	{
		/// <summary>
		/// The remote service URL.
		/// </summary>
		public Uri ServerUrl { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ServerUrlOAuthProviderInfo"/> class.
		/// </summary>
		/// <param name="providerInfo">The <see cref="OAuthProviderInfo"/> to build from.</param>
		public ServerUrlOAuthProviderInfo(OAuthProviderInfo providerInfo)
			: base(providerInfo)
		{
			ServerUrl = providerInfo!.ServerUrl ?? throw new InvalidOperationException("ServerUrl not set!");
		}
	}
}
