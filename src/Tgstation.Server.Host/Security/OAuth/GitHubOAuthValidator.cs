using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Octokit;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.Utils.GitHub;

namespace Tgstation.Server.Host.Security.OAuth
{
	/// <summary>
	/// <see cref="IOAuthValidator"/> for GitHub.
	/// </summary>
	sealed class GitHubOAuthValidator : IOAuthValidator
	{
		/// <inheritdoc />
		public OAuthProvider Provider => OAuthProvider.GitHub;

		/// <inheritdoc />
		public OAuthGatewayStatus GatewayStatus => oAuthConfiguration.Gateway!.Value;

		/// <summary>
		/// The <see cref="IGitHubServiceFactory"/> for the <see cref="GitHubOAuthValidator"/>.
		/// </summary>
		readonly IGitHubServiceFactory gitHubServiceFactory;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="GitHubOAuthValidator"/>.
		/// </summary>
		readonly ILogger<GitHubOAuthValidator> logger;

		/// <summary>
		/// The <see cref="OAuthConfiguration"/> for the <see cref="GitHubOAuthValidator"/>.
		/// </summary>
		readonly OAuthConfiguration oAuthConfiguration;

		/// <summary>
		/// Initializes a new instance of the <see cref="GitHubOAuthValidator"/> class.
		/// </summary>
		/// <param name="gitHubServiceFactory">The value of <see cref="gitHubServiceFactory"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="oAuthConfiguration">The value of <see cref="oAuthConfiguration"/>.</param>
		public GitHubOAuthValidator(
			IGitHubServiceFactory gitHubServiceFactory,
			ILogger<GitHubOAuthValidator> logger,
			OAuthConfiguration oAuthConfiguration)
		{
			this.gitHubServiceFactory = gitHubServiceFactory ?? throw new ArgumentNullException(nameof(gitHubServiceFactory));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.oAuthConfiguration = oAuthConfiguration ?? throw new ArgumentNullException(nameof(oAuthConfiguration));
		}

		/// <inheritdoc />
		public async ValueTask<(string? UserID, string AccessCode)?> ValidateResponseCode(string code, bool requireUserID, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(code);

			try
			{
				logger.LogTrace("Validating response code...");

				var gitHubService = await gitHubServiceFactory.CreateService(cancellationToken);
				var token = await gitHubService.CreateOAuthAccessToken(oAuthConfiguration, code, cancellationToken);
				if (token == null)
					return null;

				if (!requireUserID)
					return (null, AccessCode: token);

				var authenticatedClient = await gitHubServiceFactory.CreateService(token, cancellationToken);

				logger.LogTrace("Getting user details...");
				var userId = await authenticatedClient.GetCurrentUserId(cancellationToken);

				return (userId.ToString(CultureInfo.InvariantCulture), AccessCode: token);
			}
			catch (RateLimitExceededException)
			{
				throw;
			}
			catch (ApiException ex)
			{
				logger.LogWarning(ex, "API error while completing OAuth handshake!");
				return null;
			}
		}

		/// <inheritdoc />
		public OAuthProviderInfo GetProviderInfo()
			=> new()
			{
				ClientId = oAuthConfiguration.ClientId,
				RedirectUri = oAuthConfiguration.RedirectUrl,
				GatewayOnly = GatewayStatus.ToBoolean(),
			};
	}
}
