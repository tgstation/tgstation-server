using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Octokit;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Utils.GitHub
{
	/// <inheritdoc />
	sealed class GitHubClientFactory : IGitHubClientFactory, IDisposable
	{
		/// <summary>
		/// Limit to the amount of days a <see cref="GitHubClient"/> can live in the <see cref="clientCache"/>.
		/// </summary>
		/// <remarks>God forbid someone leak server memory by constantly changing an instance's GitHub token.</remarks>
		const uint ClientCacheDays = 7;

		/// <summary>
		/// The <see cref="clientCache"/> <see cref="KeyValuePair{TKey, TValue}.Key"/> used in place of <see langword="null"/> when accessing a configuration-based client with no token set in <see cref="GeneralConfiguration.GitHubAccessToken"/>.
		/// </summary>
		const string DefaultCacheKey = "~!@TGS_DEFAULT_GITHUB_CLIENT_CACHE_KEY@!~";

		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="GitHubClientFactory"/>.
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="GitHubClientFactory"/>.
		/// </summary>
		readonly ILogger<GitHubClientFactory> logger;

		/// <summary>
		/// The <see cref="GeneralConfiguration"/> for the <see cref="GitHubClientFactory"/>.
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;

		/// <summary>
		/// Cache of created <see cref="GitHubClient"/>s and last used times, keyed by access token.
		/// </summary>
		readonly Dictionary<string, (GitHubClient Client, DateTimeOffset LastUsed)> clientCache;

		/// <summary>
		/// The <see cref="SemaphoreSlim"/> used to guard access to <see cref="clientCache"/>.
		/// </summary>
		readonly SemaphoreSlim clientCacheSemaphore;

		/// <summary>
		/// Initializes a new instance of the <see cref="GitHubClientFactory"/> class.
		/// </summary>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="generalConfiguration"/>.</param>
		public GitHubClientFactory(
			IAssemblyInformationProvider assemblyInformationProvider,
			ILogger<GitHubClientFactory> logger,
			IOptions<GeneralConfiguration> generalConfigurationOptions)
		{
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));

			clientCache = new Dictionary<string, (GitHubClient, DateTimeOffset)>();
			clientCacheSemaphore = new SemaphoreSlim(1, 1);
		}

		/// <inheritdoc />
		public void Dispose() => clientCacheSemaphore.Dispose();

		/// <inheritdoc />
		public async ValueTask<IGitHubClient> CreateClient(CancellationToken cancellationToken)
			=> (await GetOrCreateClient(
				generalConfiguration.GitHubAccessToken,
				null,
				cancellationToken))!;

		/// <inheritdoc />
		public async ValueTask<IGitHubClient> CreateClient(string accessToken, CancellationToken cancellationToken)
			=> (await GetOrCreateClient(
				accessToken ?? throw new ArgumentNullException(nameof(accessToken)),
				null,
				cancellationToken))!;

		/// <inheritdoc />
		public ValueTask<IGitHubClient?> CreateClientForRepository(string accessString, RepositoryIdentifier repositoryIdentifier, CancellationToken cancellationToken)
			=> GetOrCreateClient(accessString, repositoryIdentifier, cancellationToken);

		/// <inheritdoc />
		public IGitHubClient? CreateAppClient(string tgsEncodedAppPrivateKey)
			=> CreateAppClientInternal(tgsEncodedAppPrivateKey ?? throw new ArgumentNullException(nameof(tgsEncodedAppPrivateKey)));

		/// <summary>
		/// Retrieve a <see cref="GitHubClient"/> from the <see cref="clientCache"/> or add a new one based on a given <paramref name="accessString"/>.
		/// </summary>
		/// <param name="accessString">Optional access token to use as credentials or GitHub App private key. If using a TGS encoded app private key, <paramref name="repositoryIdentifier"/> must be set.</param>
		/// <param name="repositoryIdentifier">The optional <see cref="RepositoryIdentifier"/> for the GitHub ID that the client will be used to connect to. Must be set if <paramref name="accessString"/> is a TGS encoded app private key.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="GitHubClient"/> for the given <paramref name="accessString"/> or <see langword="null"/> if authentication failed.</returns>
#pragma warning disable CA1506 // TODO: Decomplexify
		async ValueTask<IGitHubClient?> GetOrCreateClient(string? accessString, RepositoryIdentifier? repositoryIdentifier, CancellationToken cancellationToken)
#pragma warning restore CA1506
		{
			GitHubClient? client;
			bool cacheHit;
			DateTimeOffset? lastUsed;
			using (await SemaphoreSlimContext.Lock(clientCacheSemaphore, cancellationToken))
			{
				string cacheKey;
				if (String.IsNullOrWhiteSpace(accessString))
				{
					accessString = null;
					cacheKey = DefaultCacheKey;
				}
				else
					cacheKey = accessString;

				cacheHit = clientCache.TryGetValue(cacheKey, out var tuple);

				var now = DateTimeOffset.UtcNow;
				if (!cacheHit)
				{
					logger.LogTrace("Creating new GitHubClient...");

					if (accessString != null)
					{
						if (accessString.StartsWith(RepositorySettings.TgsAppPrivateKeyPrefix))
						{
							if (repositoryIdentifier == null)
								throw new InvalidOperationException("Cannot create app installation key without target repositoryIdentifier!");

							logger.LogTrace("Performing GitHub App authentication for installation on repository {installationRepositoryId}", repositoryIdentifier);

							client = CreateAppClientInternal(accessString);
							if (client == null)
								return null;

							Installation installation;
							try
							{
								var installationTask = repositoryIdentifier.IsSlug
									? client.GitHubApps.GetRepositoryInstallationForCurrent(repositoryIdentifier.Owner, repositoryIdentifier.Name)
									: client.GitHubApps.GetRepositoryInstallationForCurrent(repositoryIdentifier.RepositoryId.Value);
								installation = await installationTask;
							}
							catch (Exception ex)
							{
								logger.LogError(ex, "Failed to perform app authentication!");
								return null;
							}

							cancellationToken.ThrowIfCancellationRequested();
							try
							{
								var installToken = await client.GitHubApps.CreateInstallationToken(installation.Id);

								client.Credentials = new Credentials(installToken.Token);
							}
							catch (Exception ex)
							{
								logger.LogError(ex, "Failed to perform installation authentication!");
								return null;
							}
						}
						else
						{
							client = CreateUnauthenticatedClient();
							client.Credentials = new Credentials(accessString);
						}
					}
					else
						client = CreateUnauthenticatedClient();

					clientCache.Add(cacheKey, (Client: client, LastUsed: now));
					lastUsed = null;
				}
				else
				{
					logger.LogTrace("Cache hit for GitHubClient");
					client = tuple.Client;
					lastUsed = tuple.LastUsed;
					tuple.LastUsed = now;
				}

				// Prune the cache
				var purgeCount = 0U;
				var purgeAfter = now.AddDays(-ClientCacheDays);
				foreach (var key in clientCache.Keys.ToList())
				{
					if (key == cacheKey)
						continue; // save the hash lookup

					tuple = clientCache[key];
					if (tuple.LastUsed <= purgeAfter)
					{
						clientCache.Remove(key);
						++purgeCount;
					}
				}

				if (purgeCount > 0)
					logger.LogDebug(
						"Pruned {count} expired GitHub client(s) from cache that haven't been used in {purgeAfterHours} days.",
						purgeCount,
						ClientCacheDays);
			}

			var rateLimitInfo = client.GetLastApiInfo()?.RateLimit;
			if (rateLimitInfo != null)
				if (rateLimitInfo.Remaining == 0)
					logger.LogWarning(
						"Requested GitHub client has no requests remaining! Limit resets at {resetTime}",
						rateLimitInfo.Reset.ToString("o"));
				else if (rateLimitInfo.Remaining < 25) // good luck hitting these lines on codecov
					logger.LogWarning(
						"Requested GitHub client has only {remainingRequests} requests remaining after the usage at {lastUse}! Limit resets at {resetTime}",
						rateLimitInfo.Remaining,
						lastUsed,
						rateLimitInfo.Reset.ToString("o"));
				else
					logger.LogDebug(
						"Requested GitHub client has {remainingRequests} requests remaining after the usage at {lastUse}. Limit resets at {resetTime}",
						rateLimitInfo.Remaining,
						lastUsed,
						rateLimitInfo.Reset.ToString("o"));

			return client;
		}

		/// <summary>
		/// Create an App (not installation) authenticated <see cref="GitHubClient"/>.
		/// </summary>
		/// <param name="tgsEncodedAppPrivateKey">The TGS encoded app private key string.</param>
		/// <returns>A new app auth <see cref="GitHubClient"/> for the given <paramref name="tgsEncodedAppPrivateKey"/> on success <see langword="null"/> on failure.</returns>
		GitHubClient? CreateAppClientInternal(string tgsEncodedAppPrivateKey)
		{
			var splits = tgsEncodedAppPrivateKey.Split(':');
			if (splits.Length != 2)
			{
				logger.LogError("Failed to parse serialized Client ID & PEM! Expected 2 chunks, got {chunkCount}", splits.Length);
				return null;
			}

			byte[] pemBytes;
			try
			{
				pemBytes = Convert.FromBase64String(splits[1]);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to parse supposed base64 PEM!");
				return null;
			}

			var pem = Encoding.UTF8.GetString(pemBytes);

			using var rsa = RSA.Create();

			try
			{
				rsa.ImportFromPem(pem);
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Failed to parse PEM!");
				return null;
			}

			var signingCredentials = new SigningCredentials(
				 new RsaSecurityKey(rsa),
				 SecurityAlgorithms.RsaSha256)
			{
				// https://stackoverflow.com/questions/62307933/rsa-disposed-object-error-every-other-test
				CryptoProviderFactory = new CryptoProviderFactory
				{
					CacheSignatureProviders = false,
				},
			};
			var jwtSecurityTokenHandler = new JwtSecurityTokenHandler { SetDefaultTimesOnTokenCreation = false };

			var nowDateTime = DateTime.UtcNow;

			var appOrClientId = splits[0][RepositorySettings.TgsAppPrivateKeyPrefix.Length..];

			var jwt = jwtSecurityTokenHandler.CreateToken(new SecurityTokenDescriptor
			{
				Issuer = appOrClientId,
				Expires = nowDateTime.AddMinutes(10),
				IssuedAt = nowDateTime,
				SigningCredentials = signingCredentials,
			});

			var jwtStr = jwtSecurityTokenHandler.WriteToken(jwt);
			var client = CreateUnauthenticatedClient();
			client.Credentials = new Credentials(jwtStr, AuthenticationType.Bearer);
			return client;
		}

		/// <summary>
		/// Creates an unauthenticated <see cref="GitHubClient"/>.
		/// </summary>
		/// <returns>A new <see cref="GitHubClient"/>.</returns>
		GitHubClient CreateUnauthenticatedClient()
		{
			var product = assemblyInformationProvider.ProductInfoHeaderValue.Product!;
			return new GitHubClient(
				new ProductHeaderValue(
					product.Name,
					product.Version));
		}
	}
}
