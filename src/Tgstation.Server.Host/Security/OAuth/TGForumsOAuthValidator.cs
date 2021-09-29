using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Security.OAuth
{
	/// <summary>
	/// <see cref="IOAuthValidator"/> for /tg/ forums.
	/// </summary>
	sealed class TGForumsOAuthValidator : BaseOAuthValidator
	{
		/// <summary>
		/// Amount of minutes until unused sessions that were created are forgotten.
		/// </summary>
		const uint SessionRetentionMinutes = 10;

		/// <inheritdoc />
		public override OAuthProvider Provider => OAuthProvider.TGForums;

		/// <summary>
		/// The active session.
		/// </summary>
		readonly List<Tuple<TGCreateSessionResponse, DateTimeOffset>> sessions;

		/// <summary>
		/// Initializes a new instance of the <see cref="TGForumsOAuthValidator"/> class.
		/// </summary>
		/// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/> for the <see cref="BaseOAuthValidator"/>.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="BaseOAuthValidator"/>.</param>
		/// <param name="oAuthConfiguration">The <see cref="OAuthConfiguration"/> for the <see cref="BaseOAuthValidator"/>.</param>
		public TGForumsOAuthValidator(
			IHttpClientFactory httpClientFactory,
			ILogger<TGForumsOAuthValidator> logger,
			OAuthConfiguration oAuthConfiguration)
			: base(
				 httpClientFactory,
				 logger,
				 oAuthConfiguration)
		{
			sessions = new List<Tuple<TGCreateSessionResponse, DateTimeOffset>>();
		}

		/// <inheritdoc />
		public override async Task<OAuthProviderInfo?> GetProviderInfo(CancellationToken cancellationToken)
		{
			var expiredSessions = sessions.RemoveAll(x => x.Item2.AddMinutes(SessionRetentionMinutes) < DateTimeOffset.UtcNow);
			if (expiredSessions > 0)
				Logger.LogTrace("Expired {sessionsExpiredCount} sessions", expiredSessions);

			var clientSecret = OAuthConfiguration.ClientSecret;
			if (clientSecret == null)
			{
				Logger.LogError("TGForums OAuth misconfigured, missing {nameofClientSecret}!", nameof(OAuthConfiguration.ClientSecret));
				return null;
			}

			Logger.LogTrace("Creating new session...");
			try
			{
				var privateTokenQueryString = HttpUtility.UrlEncode(
					Convert.ToBase64String(
						Encoding.UTF8.GetBytes(
							clientSecret)));

				var returnUrl = OAuthConfiguration
					.RedirectUrl
					?.ToString();
				if (returnUrl == null)
				{
					Logger.LogError("TGForums OAuth misconfigured, missing {nameofRedirectUrl}!", nameof(OAuthConfiguration.RedirectUrl));
					return null;
				}

				var returnUrlQueryString = HttpUtility.UrlEncode(returnUrl);
				var builder = new UriBuilder("https://tgstation13.org/phpBB/oauth_create_session.php")
				{
					Query = $"site_private_token={privateTokenQueryString}&return_uri={returnUrlQueryString}",
				};

				using var request = new HttpRequestMessage(HttpMethod.Get, builder.Uri);
				using var httpClient = CreateHttpClient();

				using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
				response.EnsureSuccessStatusCode();

				var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
				var newSession = JsonConvert.DeserializeObject<TGCreateSessionResponse>(json, SerializerSettings());
				if (newSession == null)
				{
					Logger.LogWarning("Unable to deserialize new session JSON: {json}", json);
					return null;
				}

				if (newSession.Status != TGBaseResponse.OkStatus)
				{
					Logger.LogWarning("Invalid status from /tg/ API! Status: {status}, Error: {error}", newSession.Status, newSession.Error);
					return null;
				}

				sessions.Add(
					Tuple.Create(
						newSession,
						DateTimeOffset.UtcNow));
				return new OAuthProviderInfo
				{
					ClientId = newSession.SessionPublicToken,
					RedirectUri = OAuthConfiguration.RedirectUrl,
				};
			}
			catch (Exception ex)
			{
				Logger.LogWarning(ex, "Failed to create TG Forums session!");
				return null;
			}
		}

		/// <inheritdoc />
		public override async Task<string?> ValidateResponseCode(string code, CancellationToken cancellationToken)
		{
			try
			{
				var sessionTuple = sessions.FirstOrDefault(x => x.Item1.SessionPublicToken == code);
				if (sessionTuple == null)
				{
					Logger.LogWarning("No known session with this code active!");
					return null;
				}

				var clientSecret = OAuthConfiguration.ClientSecret;
				if (clientSecret == null)
				{
					Logger.LogError("TGForums OAuth misconfigured, missing {nameofClientSecret}!", nameof(OAuthConfiguration.ClientSecret));
					return null;
				}

				Logger.LogTrace("Validating session...");

				var builder = new UriBuilder("https://tgstation13.org/phpBB/oauth_get_session_info.php")
				{
					Query = $"site_private_token={HttpUtility.UrlEncode(Convert.ToBase64String(Encoding.UTF8.GetBytes(clientSecret)))}&session_private_token={HttpUtility.UrlEncode(sessionTuple.Item1.SessionPrivateToken)}",
				};

				using var request = new HttpRequestMessage(HttpMethod.Get, builder.Uri);
				using var httpClient = CreateHttpClient();

				using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
				response.EnsureSuccessStatusCode();

				var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
				var sessionInfo = JsonConvert.DeserializeObject<TGGetSessionInfoResponse>(json, SerializerSettings());
				if (sessionInfo == null)
				{
					Logger.LogWarning("Unable to deserialize session info JSON: {json}", json);
					return null;
				}

				if (sessionInfo.Status != TGBaseResponse.OkStatus)
				{
					Logger.LogWarning("Invalid status from /tg/ API! Status: {status}, Error: {error}", sessionInfo.Status, sessionInfo.Error);
					return null;
				}

				sessions.Remove(sessionTuple);
				return sessionInfo.PhpbbUsername;
			}
			catch (Exception ex)
			{
				Logger.LogWarning(ex, "Failed to create TG Forums session!");
				return null;
			}
		}
	}
}
