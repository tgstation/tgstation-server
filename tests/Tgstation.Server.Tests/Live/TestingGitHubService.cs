using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
using Tgstation.Server.Host.Tests;
using Tgstation.Server.Host.Utils.GitHub;

namespace Tgstation.Server.Tests.Live
{
	sealed class TestingGitHubService : IAuthenticatedGitHubService
	{
		static Dictionary<Version, Release> releasesDictionary;
		static PullRequest testPr;
		static GitHubCommit testCommit;

		readonly ICryptographySuite cryptographySuite;
		readonly ILogger<TestingGitHubService> logger;

		public static readonly IGitHubClient RealClient;

		static TestingGitHubService()
		{
			var mockOptions = new Mock<IOptionsMonitor<GeneralConfiguration>>();
			mockOptions.SetupGet(x => x.CurrentValue).Returns(new GeneralConfiguration
			{
				GitHubAccessToken = Environment.GetEnvironmentVariable("TGS_TEST_GITHUB_TOKEN")
			});

			var gitHubClientFactory = new GitHubClientFactory(new AssemblyInformationProvider(), new BasicHttpMessageHandlerFactory(), mockOptions.Object, Mock.Of<ILogger<GitHubClientFactory>>());
			RealClient = gitHubClientFactory.CreateClient(CancellationToken.None).GetAwaiter().GetResult();
		}

		public static async Task InitializeAndInject(CancellationToken cancellationToken)
		{
			Release targetRelease;
			do
			{
				var releases = await RealClient
					.Repository
					.Release
					.GetAll("tgstation", "tgstation-server")
					.WaitAsync(cancellationToken);

				targetRelease = releases.FirstOrDefault(release => release.TagName == $"{new UpdatesConfiguration().GitTagPrefix}{TestLiveServer.TestUpdateVersion}");
			}
			while (targetRelease == null);

			releasesDictionary = new Dictionary<Version, Release>
			{
				{ TestLiveServer.TestUpdateVersion, targetRelease }
			};

			var testCommitTask = RealClient
				.Repository
				.Commit
				.Get("Cyberboss", "common_core", "4b4926dfaf6295f19f8ae7abf03cb357dbb05b29")
				.WaitAsync(cancellationToken);
			testPr = await RealClient
				.PullRequest
				.Get("Cyberboss", "common_core", 2)
				.WaitAsync(cancellationToken);
			testCommit = await testCommitTask;

			ServiceCollectionExtensions.UseGitHubServiceFactory<DummyGitHubServiceFactory>();
		}

		public TestingGitHubService(ICryptographySuite cryptographySuite, ILogger<TestingGitHubService> logger)
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

		public Task AppendCommentOnIssue(string repoOwner, string repoName, string comment, IssueComment issueComment, CancellationToken cancellationToken)
		{
			logger.LogTrace("AppendCommentOnIssue");
			return Task.CompletedTask;
		}

		public ValueTask<IssueComment> GetExistingCommentOnIssue(string repoOwner, string repoName, string header, int issueNumber, CancellationToken cancellationToken)
		{
			logger.LogTrace("GetExistingCommentOnIssue");
			return ValueTask.FromResult<IssueComment>(null);
		}

		public ValueTask<long> CreateDeployment(NewDeployment newDeployment, string repoOwner, string repoName, CancellationToken cancellationToken)
		{
			logger.LogTrace("CreateDeployment");
			return ValueTask.FromResult<long>(new Random().Next()); ;
		}

		public Task CreateDeploymentStatus(NewDeploymentStatus newDeploymentStatus, string repoOwner, string repoName, long deploymentId, CancellationToken cancellationToken)
		{
			logger.LogTrace("CreateDeploymentStatus");
			return Task.CompletedTask;
		}

		public Task CreateDeploymentStatus(NewDeploymentStatus newDeploymentStatus, long repoId, long deploymentId, CancellationToken cancellationToken)
		{
			logger.LogTrace("CreateDeploymentStatus");
			return Task.CompletedTask;
		}

		public ValueTask<string> CreateOAuthAccessToken(OAuthConfiguration oAuthConfiguration, string code, CancellationToken cancellationToken)
		{
			logger.LogTrace("CreateOAuthAccessToken");
			return ValueTask.FromResult(cryptographySuite.GetSecureString());
		}

		public ValueTask<long> GetCurrentUserId(CancellationToken cancellationToken)
		{
			logger.LogTrace("GetCurrentUserId");
			return ValueTask.FromResult<long>(new Random().Next());
		}

		public ValueTask<long> GetRepositoryId(string repoOwner, string repoName, CancellationToken cancellationToken)
		{
			logger.LogTrace("GetRepositoryId");
			return ValueTask.FromResult(new Random().NextInt64());
		}

		public ValueTask<Uri> GetUpdatesRepositoryUrl(CancellationToken cancellationToken)
		{
			logger.LogTrace("GetUpdatesRepositoryUrl");
			return ValueTask.FromResult(new Uri("https://github.com/tgstation/tgstation-server"));
		}

		public Task<PullRequest> GetPullRequest(string repoOwner, string repoName, int pullRequestNumber, CancellationToken cancellationToken)
		{
			logger.LogTrace("GetPullRequest");
			return Task.FromResult(testPr);
		}

		public Task<GitHubCommit> GetCommit(string repoOwner, string repoName, string committish, CancellationToken cancellationToken)
		{
			logger.LogTrace("GetPullRequest");
			return Task.FromResult(testCommit);
		}

		public ValueTask<Dictionary<Version, Release>> GetTgsReleases(CancellationToken cancellationToken)
		{
			logger.LogTrace("GetTgsReleases");
			return ValueTask.FromResult(releasesDictionary);
		}
	}
}
