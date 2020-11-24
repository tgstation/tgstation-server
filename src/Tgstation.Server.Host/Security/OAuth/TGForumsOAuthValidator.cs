using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.System;

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
		/// Initializes a new instance of the <see cref="TGForumsOAuthValidator"/> <see langword="class"/>.
		/// </summary>
		/// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/> for the <see cref="BaseOAuthValidator"/></param>
		/// <param name="assemblyInformationProvider">The <see cref="IAssemblyInformationProvider"/> for the <see cref="BaseOAuthValidator"/></param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="BaseOAuthValidator"/></param>
		/// <param name="oAuthConfiguration">The <see cref="OAuthConfiguration"/> for the <see cref="BaseOAuthValidator"/>.</param>
		public TGForumsOAuthValidator(
			IHttpClientFactory httpClientFactory,
			IAssemblyInformationProvider assemblyInformationProvider,
			ILogger<TGForumsOAuthValidator> logger,
			OAuthConfiguration oAuthConfiguration)
			: base(
				 httpClientFactory,
				 assemblyInformationProvider,
				 logger,
				 oAuthConfiguration)
		{
			sessions = new List<Tuple<TGCreateSessionResponse, DateTimeOffset>>();
		}

		/// <inheritdoc />
		public override async Task<string> GetClientId(CancellationToken cancellationToken)
		{
			var expiredSessions = sessions.RemoveAll(x => x.Item2.AddMinutes(SessionRetentionMinutes) < DateTimeOffset.Now);
			if (expiredSessions > 0)
				Logger.LogTrace("Expired {0} sessions", expiredSessions);

			Logger.LogTrace("Creating new session...");
			try
			{
				UriBuilder builder = new UriBuilder("https://tgstation13.org/phpBB/oauth_create_session.php")
				{
					Query = $"site_private_token={HttpUtility.UrlEncode(Convert.ToBase64String(Encoding.UTF8.GetBytes(OAuthConfiguration.ClientSecret)))}&return_uri={HttpUtility.UrlEncode(OAuthConfiguration.ClientId)}"
				};

				using var request = new HttpRequestMessage(HttpMethod.Get, builder.Uri);
				using var httpClient = CreateHttpClient();

				using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
				response.EnsureSuccessStatusCode();

				var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
				var newSession = JsonConvert.DeserializeObject<TGCreateSessionResponse>(json, SerializerSettings());

				if (newSession.Status != TGBaseResponse.OkStatus)
				{
					Logger.LogWarning("Invalid status from /tg/ API! Status: {0}, Error: {1}", newSession.Status, newSession.Error);
					return null;
				}

				sessions.Add(Tuple.Create(newSession, DateTimeOffset.Now));
				return newSession.SessionPublicToken;
			}
			catch (Exception ex)
			{
				Logger.LogWarning(ex, "Failed to create TG Forums session!");
				return null;
			}
		}

		/// <inheritdoc />
		public override async Task<string> ValidateResponseCode(string code, CancellationToken cancellationToken)
		{
			try
			{
				var sessionTuple = sessions.FirstOrDefault(x => x.Item1.SessionPublicToken == code);
				if(sessionTuple == null)
				{
					Logger.LogWarning("No known session with this code active!");
					return null;
				}

				Logger.LogTrace("Validating session...");

				UriBuilder builder = new UriBuilder("https://tgstation13.org/phpBB/oauth_get_session_info.php")
				{
					Query = $"site_private_token={HttpUtility.UrlEncode(Convert.ToBase64String(Encoding.UTF8.GetBytes(OAuthConfiguration.ClientSecret)))}&session_private_token={HttpUtility.UrlEncode(sessionTuple.Item1.SessionPrivateToken)}"
				};

				using var request = new HttpRequestMessage(HttpMethod.Get, builder.Uri);
				using var httpClient = CreateHttpClient();

				using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
				response.EnsureSuccessStatusCode();

				var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
				var sessionInfo = JsonConvert.DeserializeObject<TGGetSessionInfoResponse>(json, SerializerSettings());

				if (sessionInfo.Status != TGBaseResponse.OkStatus)
				{
					Logger.LogWarning("Invalid status from /tg/ API! Status: {0}, Error: {1}", sessionInfo.Status, sessionInfo.Error);
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
