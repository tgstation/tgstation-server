using System;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Security.OAuth;

namespace Tgstation.Server.Host.GraphQL.Types.OAuth
{
	/// <summary>
	/// Description of configured OAuth services.
	/// </summary>
	public sealed class OAuthProviderInfos
	{
		/// <summary>
		/// https://discord.com.
		/// </summary>
		public BasicOAuthProviderInfo? Discord { get; }

		/// <summary>
		/// https://github.com.
		/// </summary>
		public RedirectOAuthProviderInfo? GitHub { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="OAuthProviderInfos"/> class.
		/// </summary>
		/// <param name="oAuthProviders">The <see cref="IOAuthProviders"/> to get data from.</param>
		public OAuthProviderInfos(IOAuthProviders oAuthProviders)
		{
			ArgumentNullException.ThrowIfNull(oAuthProviders);

			var dic = oAuthProviders.ProviderInfos();

			TProviderInfo? TryBuild<TProviderInfo>(OAuthProvider oAuthProvider, Func<OAuthProviderInfo, TProviderInfo> contructor)
				where TProviderInfo : BasicOAuthProviderInfo
			{
				if (dic.TryGetValue(oAuthProvider, out var providerInfo))
				{
					return contructor(providerInfo);
				}

				return null;
			}

			Discord = TryBuild(OAuthProvider.Discord, info => new BasicOAuthProviderInfo(info));
			GitHub = TryBuild(OAuthProvider.GitHub, info => new RedirectOAuthProviderInfo(info));
		}
	}
}
