using System;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Utils.GitHub;

namespace Tgstation.Server.Tests.Live
{
	sealed class DummyGitHubServiceFactory : IGitHubServiceFactory
	{
		readonly ICryptographySuite cryptographySuite;
		readonly ILogger<DummyGitHubService> logger;

		public DummyGitHubServiceFactory(ICryptographySuite cryptographySuite, ILogger<DummyGitHubService> logger)
		{
			this.cryptographySuite = cryptographySuite ?? throw new ArgumentNullException(nameof(cryptographySuite));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public IGitHubService CreateService() => CreateDummyService();

		public IAuthenticatedGitHubService CreateService(string accessToken)
		{
			ArgumentNullException.ThrowIfNull(accessToken);

			return CreateDummyService();
		}

		DummyGitHubService CreateDummyService() => new DummyGitHubService(cryptographySuite, logger);
	}
}
