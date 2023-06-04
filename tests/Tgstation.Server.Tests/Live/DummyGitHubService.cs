using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using Octokit;

using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Utils.GitHub;

namespace Tgstation.Server.Tests.Live
{
	sealed class DummyGitHubService : IAuthenticatedGitHubService
	{
		static Dictionary<Version, Release> releasesDictionary;
		static PullRequest testPr;

		readonly ICryptographySuite cryptographySuite;
		readonly ILogger<DummyGitHubService> logger;

		public static async Task InitializeAndInject(CancellationToken cancellationToken)
		{
			var mockOptions = new Mock<IOptions<GeneralConfiguration>>();
			mockOptions.SetupGet(x => x.Value).Returns(new GeneralConfiguration
			{
				GitHubAccessToken = Environment.GetEnvironmentVariable("TGS_TEST_GITHUB_TOKEN")
			});

			var gitHubClientFactory = new GitHubClientFactory(new AssemblyInformationProvider(), Mock.Of<ILogger<GitHubClientFactory>>(), mockOptions.Object);
			var gitHubClient = gitHubClientFactory.CreateClient();

			Release targetRelease;
			do
			{
				var releases = await gitHubClient
					.Repository
					.Release
					.GetAll("tgstation", "tgstation-server")
					.WithToken(cancellationToken);

				targetRelease = releases.FirstOrDefault(release => release.TagName == $"{new UpdatesConfiguration().GitTagPrefix}{TestLiveServer.TestUpdateVersion}");
			}
			while (targetRelease == null);

			releasesDictionary = new Dictionary<Version, Release>
			{
				{ TestLiveServer.TestUpdateVersion, targetRelease }
			};

			testPr = await gitHubClient
				.PullRequest
				.Get("Cyberboss", "common_core", 2)
				.WithToken(cancellationToken);

			ServiceCollectionExtensions.UseGitHubServiceFactory<DummyGitHubServiceFactory>();
		}

		public DummyGitHubService(ICryptographySuite cryptographySuite, ILogger<DummyGitHubService> logger)
		{
			this.cryptographySuite = cryptographySuite ?? throw new ArgumentNullException(nameof(cryptographySuite));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			logger.LogTrace("Created");
		}

		public Task CommentOnIssue(string repoOwner, string repoName, string comment, int issueNumber, CancellationToken cancellationToken)
		{
			logger.LogTrace("CommentOnIssue");
			return Task.CompletedTask;
		}

		public Task<int> CreateDeployment(NewDeployment newDeployment, string repoOwner, string repoName, CancellationToken cancellationToken)
		{
			logger.LogTrace("CreateDeployment");
			return Task.FromResult(new Random().Next()); ;
		}

		public Task CreateDeploymentStatus(NewDeploymentStatus newDeploymentStatus, string repoOwner, string repoName, int deploymentId, CancellationToken cancellationToken)
		{
			logger.LogTrace("CreateDeploymentStatus");
			return Task.CompletedTask;
		}

		public Task CreateDeploymentStatus(NewDeploymentStatus newDeploymentStatus, long repoId, int deploymentId, CancellationToken cancellationToken)
		{
			logger.LogTrace("CreateDeploymentStatus");
			return Task.CompletedTask;
		}

		public Task<string> CreateOAuthAccessToken(OAuthConfiguration oAuthConfiguration, string code, CancellationToken cancellationToken)
		{
			logger.LogTrace("CreateOAuthAccessToken");
			return Task.FromResult(cryptographySuite.GetSecureString());
		}

		public Task<int> GetCurrentUserId(CancellationToken cancellationToken)
		{
			logger.LogTrace("GetCurrentUserId");
			return Task.FromResult(new Random().Next());
		}

		public Task<long> GetRepositoryId(string repoOwner, string repoName, CancellationToken cancellationToken)
		{
			logger.LogTrace("GetRepositoryId");
			return Task.FromResult(new Random().NextInt64());
		}

		public Task<Uri> GetUpdatesRepositoryUrl(CancellationToken cancellationToken)
		{
			logger.LogTrace("GetUpdatesRepositoryUrl");
			return Task.FromResult(new Uri("https://github.com/tgstation/tgstation-server"));
		}

		public Task<PullRequest> GetPullRequest(string repoOwner, string repoName, int pullRequestNumber, CancellationToken cancellationToken)
		{
			logger.LogTrace("GetPullRequest");
			return Task.FromResult(testPr);
		}

		public Task<Dictionary<Version, Release>> GetTgsReleases(CancellationToken cancellationToken)
		{
			logger.LogTrace("GetTgsReleases");
			return Task.FromResult(releasesDictionary);
		}
	}
}
