using Microsoft.Extensions.Logging;
using Octokit;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Core;

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
		public string ClientId => oAuthConfiguration.ClientId;

		/// <summary>
		/// The <see cref="IGitHubClientFactory"/> for the <see cref="GitHubOAuthValidator"/>.
		/// </summary>
		readonly IGitHubClientFactory gitHubClientFactory;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="GitHubOAuthValidator"/>.
		/// </summary>
		readonly ILogger<GitHubOAuthValidator> logger;

		/// <summary>
		/// The <see cref="OAuthConfiguration"/> for the <see cref="GitHubOAuthValidator"/>.
		/// </summary>
		readonly OAuthConfiguration oAuthConfiguration;

		/// <summary>
		/// Initializes a new instance of the <see cref="GitHubOAuthValidator"/> <see langword="class"/>.
		/// </summary>
		/// <param name="gitHubClientFactory">The value of <see cref="gitHubClientFactory"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="oAuthConfiguration">The value of <see cref="oAuthConfiguration"/>.</param>
		public GitHubOAuthValidator(
			IGitHubClientFactory gitHubClientFactory,
			ILogger<GitHubOAuthValidator> logger,
			OAuthConfiguration oAuthConfiguration)
		{
			this.gitHubClientFactory = gitHubClientFactory ?? throw new ArgumentNullException(nameof(gitHubClientFactory));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.oAuthConfiguration = oAuthConfiguration ?? throw new ArgumentNullException(nameof(oAuthConfiguration));
		}

		/// <inheritdoc />
		public async Task<ulong?> ValidateResponseCode(string code, CancellationToken cancellationToken)
		{
			if (code == null)
				throw new ArgumentNullException(nameof(code));

			var client = gitHubClientFactory.CreateClient();
			try
			{
				logger.LogTrace("Validating response code...");
				var response = await client
					.Oauth
					.CreateAccessToken(
						new OauthTokenRequest(
							oAuthConfiguration.ClientId,
							oAuthConfiguration.ClientSecret,
							code))
					.ConfigureAwait(false);

				var token = response.AccessToken;
				if (token == null)
					return null;

				var authenticatedClient = gitHubClientFactory.CreateClient(token);

				logger.LogTrace("Getting user details...");
				var userDetails = await authenticatedClient
					.User
					.Current()
					.ConfigureAwait(false);

				return (ulong)userDetails.Id;
			}
			catch (RateLimitExceededException)
			{
				throw;
			}
			catch(ApiException ex)
			{
				logger.LogWarning(ex, "API error while completing OAuth handshake!");
				return null;
			}
		}
	}
}
