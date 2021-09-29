using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Security.OAuth
{
	/// <summary>
	/// Base <see langword="class"/> for <see cref="IOAuthValidator"/>s.
	/// </summary>
	abstract class BaseOAuthValidator : IOAuthValidator
	{
		/// <inheritdoc />
		public abstract OAuthProvider Provider { get; }

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="BaseOAuthValidator"/>.
		/// </summary>
		protected ILogger<BaseOAuthValidator> Logger { get; }

		/// <summary>
		/// The <see cref="OAuthConfiguration"/> for the <see cref="BaseOAuthValidator"/>.
		/// </summary>
		protected OAuthConfiguration OAuthConfiguration { get; }

		/// <summary>
		/// The <see cref="IHttpClientFactory"/> for the <see cref="BaseOAuthValidator"/>.
		/// </summary>
		readonly IHttpClientFactory httpClientFactory;

		/// <summary>
		/// Gets <see cref="JsonSerializerSettings"/> that should be used.
		/// </summary>
		/// <returns>A new <see cref="JsonSerializerSettings"/> <see cref="object"/>.</returns>
		protected static JsonSerializerSettings SerializerSettings() => new ()
		{
			ContractResolver = new DefaultContractResolver
			{
				NamingStrategy = new SnakeCaseNamingStrategy(),
			},
		};

		/// <summary>
		/// Initializes a new instance of the <see cref="BaseOAuthValidator"/> class.
		/// </summary>
		/// <param name="httpClientFactory">The value of <see cref="httpClientFactory"/>.</param>
		/// <param name="logger">The value of <see cref="Logger"/>.</param>
		/// <param name="oAuthConfiguration">The value of <see cref="OAuthConfiguration"/>.</param>
		public BaseOAuthValidator(
			IHttpClientFactory httpClientFactory,
			ILogger<BaseOAuthValidator> logger,
			OAuthConfiguration oAuthConfiguration)
		{
			this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
			OAuthConfiguration = oAuthConfiguration ?? throw new ArgumentNullException(nameof(oAuthConfiguration));
		}

		/// <inheritdoc />
		public abstract Task<OAuthProviderInfo?> GetProviderInfo(CancellationToken cancellationToken);

		/// <inheritdoc />
		public abstract Task<string?> ValidateResponseCode(string code, CancellationToken cancellationToken);

		/// <summary>
		/// Create a new configured <see cref="HttpClient"/>.
		/// </summary>
		/// <returns>A new configured <see cref="HttpClient"/>.</returns>
		protected HttpClient CreateHttpClient()
		{
			var httpClient = httpClientFactory.CreateClient();
			try
			{
				httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
				return httpClient;
			}
			catch
			{
				httpClient.Dispose();
				throw;
			}
		}
	}
}
