using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;

using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Utils.GitHub
{
	/// <inheritdoc />
	sealed class GitHubClientFactory : IGitHubClientFactory
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
		readonly Dictionary<string, (GitHubClient, DateTimeOffset)> clientCache;

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
		}

		/// <inheritdoc />
		public IGitHubClient CreateClient() => GetOrCreateClient(generalConfiguration.GitHubAccessToken);

		/// <inheritdoc />
		public IGitHubClient CreateClient(string accessToken)
			=> GetOrCreateClient(
				accessToken ?? throw new ArgumentNullException(nameof(accessToken)));

		/// <summary>
		/// Retrieve a <see cref="GitHubClient"/> from the <see cref="clientCache"/> or add a new one based on a given <paramref name="accessToken"/>.
		/// </summary>
		/// <param name="accessToken">Optional access token to use as credentials.</param>
		/// <returns>The <see cref="GitHubClient"/> for the given <paramref name="accessToken"/>.</returns>
		GitHubClient GetOrCreateClient(string accessToken)
		{
			GitHubClient client;
			bool cacheHit;
			DateTimeOffset? lastUsed;
			lock (clientCache)
			{
				string cacheKey;
				if (String.IsNullOrWhiteSpace(accessToken))
				{
					accessToken = null;
					cacheKey = DefaultCacheKey;
				}
				else
					cacheKey = accessToken;

				cacheHit = clientCache.TryGetValue(cacheKey, out var tuple);

				var now = DateTimeOffset.UtcNow;
				if (!cacheHit)
				{
					client = new GitHubClient(
						new ProductHeaderValue(
							assemblyInformationProvider.ProductInfoHeaderValue.Product.Name,
							assemblyInformationProvider.ProductInfoHeaderValue.Product.Version));

					if (accessToken != null)
						client.Credentials = new Credentials(accessToken);

					clientCache.Add(cacheKey, (client, now));
					lastUsed = null;
				}
				else
				{
					logger.LogTrace("Cache hit for GitHubClient");
					client = tuple.Item1;
					lastUsed = tuple.Item2;
					tuple.Item2 = now;
				}

				// Prune the cache
				var purgeCount = 0U;
				var purgeAfter = now.AddDays(-ClientCacheDays);
				foreach (var key in clientCache.Keys.ToList())
				{
					if (key == cacheKey)
						continue; // save the hash lookup

					tuple = clientCache[key];
					if (tuple.Item2 <= purgeAfter)
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
						"Requested GitHub client has {remainingRequests} requests remaining after the usage {lastUse}. Limit resets at {resetTime}",
						rateLimitInfo.Remaining,
						lastUsed,
						rateLimitInfo.Reset.ToString("o"));

			return client;
		}
	}
}
