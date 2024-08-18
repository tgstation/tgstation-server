using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Utils.GitHub;

namespace Tgstation.Server.Tests.Live
{
	sealed class DummyGitHubServiceFactory : IGitHubServiceFactory
	{
		readonly ICryptographySuite cryptographySuite;
		readonly ILogger<TestingGitHubService> logger;

		public DummyGitHubServiceFactory(ICryptographySuite cryptographySuite, ILogger<TestingGitHubService> logger)
		{
			this.cryptographySuite = cryptographySuite ?? throw new ArgumentNullException(nameof(cryptographySuite));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public ValueTask<IGitHubService> CreateService(CancellationToken cancellationToken) => ValueTask.FromResult<IGitHubService>(CreateDummyService());

		public ValueTask<IAuthenticatedGitHubService> CreateService(string accessToken, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(accessToken);

			return ValueTask.FromResult<IAuthenticatedGitHubService>(CreateDummyService());
		}

		TestingGitHubService CreateDummyService() => new TestingGitHubService(cryptographySuite, logger);
	}
}
