using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Common.Http;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Utils.GitHub;

namespace Tgstation.Server.Host.Security.OAuth
{
	/// <inheritdoc />
	sealed class OAuthProviders : IOAuthProviders
	{
		/// <summary>
		/// The <see cref="IReadOnlyCollection{T}"/> of <see cref="IOAuthValidator"/>s.
		/// </summary>
		readonly IReadOnlyCollection<IOAuthValidator> validators;

		/// <summary>
		/// Initializes a new instance of the <see cref="OAuthProviders"/> class.
		/// </summary>
		/// <param name="gitHubServiceFactory">The <see cref="IGitHubServiceFactory"/> to use.</param>
		/// <param name="httpClientFactory">The <see cref="IAbstractHttpClientFactory"/> to use.</param>
		/// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use.</param>
		/// <param name="securityConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the <see cref="SecurityConfiguration"/> to use.</param>
		public OAuthProviders(
			IGitHubServiceFactory gitHubServiceFactory,
			IAbstractHttpClientFactory httpClientFactory,
			ILoggerFactory loggerFactory,
			IOptions<SecurityConfiguration> securityConfigurationOptions)
		{
			ArgumentNullException.ThrowIfNull(loggerFactory);

			var securityConfiguration = securityConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(securityConfigurationOptions));

			var validatorsBuilder = new List<IOAuthValidator>();
			validators = validatorsBuilder;

			if (securityConfiguration.OAuth == null)
				return;

			if (securityConfiguration.OAuth.TryGetValue(OAuthProvider.GitHub, out var gitHubConfig))
				validatorsBuilder.Add(
					new GitHubOAuthValidator(
						gitHubServiceFactory,
						loggerFactory.CreateLogger<GitHubOAuthValidator>(),
						gitHubConfig));

			if (securityConfiguration.OAuth.TryGetValue(OAuthProvider.Discord, out var discordConfig))
				validatorsBuilder.Add(
					new DiscordOAuthValidator(
						httpClientFactory,
						loggerFactory.CreateLogger<DiscordOAuthValidator>(),
						discordConfig));

			if (securityConfiguration.OAuth.TryGetValue(OAuthProvider.Keycloak, out var keyCloakConfig))
				validatorsBuilder.Add(
					new KeycloakOAuthValidator(
						httpClientFactory,
						loggerFactory.CreateLogger<KeycloakOAuthValidator>(),
						keyCloakConfig));

			if (securityConfiguration.OAuth.TryGetValue(OAuthProvider.InvisionCommunity, out var invisionConfig))
				validatorsBuilder.Add(
					new InvisionCommunityOAuthValidator(
						httpClientFactory,
						loggerFactory.CreateLogger<InvisionCommunityOAuthValidator>(),
						invisionConfig));
		}

		/// <inheritdoc />
		public IOAuthValidator? GetValidator(OAuthProvider oAuthProvider, bool forLogin)
			=> validators.FirstOrDefault(
				x => x.Provider == oAuthProvider
				&& ((forLogin && x.GatewayStatus != OAuthGatewayStatus.Only) || (!forLogin && x.GatewayStatus != OAuthGatewayStatus.Disabled)));

		/// <inheritdoc />
		public Dictionary<OAuthProvider, OAuthProviderInfo> ProviderInfos()
		{
			var providersAndTasks = validators.ToDictionary(
				x => x.Provider,
				x => x.GetProviderInfo());

			return providersAndTasks
				.Where(x => x.Value != null)
				.ToDictionary(
					x => x.Key,
					x => x.Value);
		}
	}
}
