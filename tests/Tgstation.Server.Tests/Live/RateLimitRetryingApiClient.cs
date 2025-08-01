using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Tgstation.Server.Api;
using Tgstation.Server.Client;
using Tgstation.Server.Common.Http;

namespace Tgstation.Server.Tests.Live
{
	sealed class RateLimitRetryingApiClient : ApiClient
	{
		public RateLimitRetryingApiClient(
			HttpClient httpClient,
			Uri url,
			ApiHeaders apiHeaders,
			ApiHeaders tokenRefreshHeaders,
			bool authless)
			: base(
				  httpClient,
				  url,
				  apiHeaders,
				  tokenRefreshHeaders,
				  authless)
		{
		}

		protected override async ValueTask<TResult> RunRequest<TResult>(string route, HttpContent content, HttpMethod method, long? instanceId, bool tokenRefresh, CancellationToken cancellationToken)
		{
			var hasGitHubToken = !String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TGS_TEST_GITHUB_TOKEN"));
			while (true)
				try
				{
					return await base.RunRequest<TResult>(route, content, method, instanceId, tokenRefresh, cancellationToken);
				}
				catch (RateLimitException ex) when (hasGitHubToken && ex.RetryAfter.HasValue)
				{
					var now = DateTimeOffset.UtcNow;

					Console.WriteLine($"TEST ERROR RATE LIMITED: {ex}");
					if (!TestingUtils.RunningInGitHubActions)
						Assert.Inconclusive("Rate limited by GitHub!");

					var sleepTime = ex.RetryAfter.Value - now;
					Console.WriteLine($"Sleeping for {sleepTime.TotalMinutes} minutes and retrying...");
					await Task.Delay(sleepTime, cancellationToken);
				}
		}
	}
}
