using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Octokit;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Common.Tests;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Tests;

namespace Tgstation.Server.Host.Utils.GitHub.Tests
{
	[TestClass]
	public sealed class TestGitHubClientFactory
	{
		const string FakePrivateKey = @"-----BEGIN RSA PRIVATE KEY-----
MIIEowIBAAKCAQEAq3oP6NMRwRZY8eMbm4GRLyfJ07LNpHzjRcjTvMf8LGGSVb8v
DBApR/5+TvWB5qnnh5fJBrR40sboJhIXUXkpyebIu/7lDqXhfjroAk8oNJhyLpW7
u+8DpwTuZYQSSUgdMvNqrWBt7SMrFbhTtEVvQLW5LcwXL9E+V83pwQ1q37wCsedq
fsQbZSxXMMu4efaFoGJ1G60WqYiCCBSsJMPZY5St2G5Tn2caUVQX4V/kyU09gGyN
lJWFpZJP37zBdfmX+VkNj+UFusmcUe7GnklzYhyiXX2sVxzCekCAUxZKO3WKDHbg
CA02oZ8mfQiIbxCYG/uqYmAoAMbQJKqc7iu/0wIDAQABAoIBAEa37F/EzImpQb1g
QD59zPZ5nk7katLvfnuFO22bvHBBPSyH0EtVTvEWD9lYft42K/pLquhM/ZdP2OX6
iAtdwNI3j4mYsba8yqZYfN6W7rojRRTZQ7dZ91OmQPs04KXAS+p7YP9nyW4HFvm6
Lyslh6BUUa6FgPqDfQaRMVogwnbKUilob1eNncXRTw1PUQ+YOK28COYwp+XtNdEM
lecUVqZv5HKSMvP643sMKnj93PXWyisaj77pEdw/1CDQkP1cgqTyX4SaEgx2QDhR
ljaSIQH41JqQCDpWqbqrx3XiqpaWrIAnmUHhP0WRUlD8wCaW5PxY/HwkT/McgyGD
QUofyuECgYEA1XH6850JOrqYZagmTwgSzfbuOmQyYWxdvnq/oRjyy2rpcrISRdMh
3NdpwDX41EhWvMEjfA7l1AYccf72AbwGK+kzosqXn53NHcCjsVd1VzNo9Zm7l9Wj
SJE3ZLSrQ/8nlvrwGr0tgybhq3kLJZLZ+blBSvuYx2YHC4PQEuAo3DcCgYEAzaoO
tPCvpo23jHcevYaQQCYh1sAts9m99LUg2GUYHQ/U11VGq+DTUfReTI7LhXZuyvyx
V+bR5e6NPTuL0XBmESjDSUnj+SCVK8x+NMJMQKqS4OcGSZmx0CCtQCAykZvOA9NQ
zoXW8DkTtiwyQ+AMh9msYnWpJJj2y86/6pOqQ0UCgYB/B+Lu8dr4VO02Myj5iDiI
1BlcLx281Z3FK5C48/wsDGj7lfdCDzHsGVgayQRacuMMW3Ye807dLPXo8nC+/4Q8
xgGxNRmgKW5V8rx5Yy+2wiYJZYE8EC2plqN9D/mN8mFBff9AKq7Xi2BriRKVPhz0
fsjZM3vt0E8JD13angYzaQKBgHHkfBJ9u3grwPrbuL1SOK4dr92iPWz85zIN4GuV
yH3Hl6HMCsACWGRpRJN2/IQjawWkXH2GSLThn3vKbwqECTH1dfgvID2FarZ/n2CO
PPYOwBomNhgqMgtFHUyGyBpUwwjhTD2iZr5PjXf0D74A5E+THuDDsfCfeQSysRsx
vTdVAoGBAI/jjUMdjkY43zhe3w2piwT0fhGfqm9ikdAB9IcgcptuS0ML0ZaWV/eO
8a/G4KMV387BgLUPotWL29EHTdD5ir2yVIhF4JtQEpPucktacruu2TbGUm/5gV96
0+rEPfoEdN+gHI+3w7N6owFmir7Wdz0VK9K8OxsjMLshW3sRlphg
-----END RSA PRIVATE KEY-----";

		static ILoggerFactory loggerFactory;

		[ClassInitialize]
		public static void Initialize(TestContext _)
		{
			loggerFactory = LoggerFactory.Create(builder =>
			{
				builder.AddConsole();
				builder.SetMinimumLevel(LogLevel.Trace);
			});
		}

		[ClassCleanup]
		public static void Cleanup()
		{
			loggerFactory.Dispose();
		}

		[TestMethod]
		public void TestContructionThrows()
		{
			Assert.ThrowsExactly<ArgumentNullException>(() => new GitHubClientFactory(null, null, null, null));
			Assert.ThrowsExactly<ArgumentNullException>(() => new GitHubClientFactory(Mock.Of<IAssemblyInformationProvider>(), null, null, null));
			Assert.ThrowsExactly<ArgumentNullException>(() => new GitHubClientFactory(Mock.Of<IAssemblyInformationProvider>(), Mock.Of<IHttpMessageHandlerFactory>(), null, null));
			Assert.ThrowsExactly<ArgumentNullException>(() => new GitHubClientFactory(Mock.Of<IAssemblyInformationProvider>(), Mock.Of<IHttpMessageHandlerFactory>(), Mock.Of<IOptionsMonitor<GeneralConfiguration>>(), null));
		}

		[TestMethod]
		public async Task TestCreateBasicClient()
		{
			var mockApp = new Mock<IAssemblyInformationProvider>();
			mockApp.SetupGet(x => x.ProductInfoHeaderValue).Returns(new ProductInfoHeaderValue("TGSTests", "1.2.3")).Verifiable();

			var mockOptions = new Mock<IOptionsMonitor<GeneralConfiguration>>();

			var gc = new GeneralConfiguration();
			Assert.IsNull(gc.GitHubAccessToken);
			mockOptions.SetupGet(x => x.CurrentValue).Returns(gc);
			var factory = new GitHubClientFactory(mockApp.Object, new BasicHttpMessageHandlerFactory(), mockOptions.Object, loggerFactory.CreateLogger<GitHubClientFactory>());

			var client = await factory.CreateClient(CancellationToken.None);
			Assert.IsNotNull(client);
			var credentials = await client.Connection.CredentialStore.GetCredentials();

			Assert.AreEqual(AuthenticationType.Anonymous, credentials.AuthenticationType);

			gc.GitHubAccessToken = "asdfasdfasdfasdfasdfasdf";
			client = await factory.CreateClient(CancellationToken.None);
			Assert.IsNotNull(client);
			credentials = await client.Connection.CredentialStore.GetCredentials();

			Assert.AreEqual(AuthenticationType.Oauth, credentials.AuthenticationType);

			mockApp.VerifyAll();
		}

		[TestMethod]
		public async Task TestCreateTokenClient()
		{
			var mockApp = new Mock<IAssemblyInformationProvider>();
			mockApp.SetupGet(x => x.ProductInfoHeaderValue).Returns(new ProductInfoHeaderValue("TGSTests", "1.2.3")).Verifiable();

			var mockOptions = new Mock<IOptionsMonitor<GeneralConfiguration>>();
			mockOptions.SetupGet(x => x.CurrentValue).Returns(new GeneralConfiguration());
			var factory = new GitHubClientFactory(mockApp.Object, new BasicHttpMessageHandlerFactory(), mockOptions.Object, loggerFactory.CreateLogger<GitHubClientFactory>());

			await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => factory.CreateClient(null, CancellationToken.None).AsTask());

			var client = await factory.CreateClient("asdf", CancellationToken.None);
			Assert.IsNotNull(client);

			var credentials = await client.Connection.CredentialStore.GetCredentials();

			Assert.AreEqual(AuthenticationType.Oauth, credentials.AuthenticationType);

			mockApp.VerifyAll();
		}

		[TestMethod]
		public async Task TestCreateEncodedClientRealKey()
		{
			var mockApp = new Mock<IAssemblyInformationProvider>();
			mockApp.SetupGet(x => x.ProductInfoHeaderValue).Returns(new ProductInfoHeaderValue("TGSTests", "1.2.3")).Verifiable();

			var mockOptions = new Mock<IOptionsMonitor<GeneralConfiguration>>();
			mockOptions.SetupGet(x => x.CurrentValue).Returns(new GeneralConfiguration());
			var factory = new GitHubClientFactory(mockApp.Object, new BasicHttpMessageHandlerFactory(), mockOptions.Object, loggerFactory.CreateLogger<GitHubClientFactory>());

			var appID = Environment.GetEnvironmentVariable("TGS_TEST_APP_ID");
			var privateKey = Environment.GetEnvironmentVariable("TGS_TEST_APP_PRIVATE_KEY");
			var repoSlug = Environment.GetEnvironmentVariable("TGS_TEST_REPO_SLUG");

			if (String.IsNullOrWhiteSpace(appID))
				Assert.Inconclusive("Missing App ID");

			if (String.IsNullOrWhiteSpace(privateKey))
				Assert.Inconclusive("Missing App Private Key");

			if (String.IsNullOrWhiteSpace(repoSlug))
				Assert.Inconclusive("Missing repo slug");

			var fakeAccessString = $"{RepositorySettings.TgsAppPrivateKeyPrefix}{appID}:{Convert.ToBase64String(Encoding.UTF8.GetBytes(privateKey))}";
			var slugSplits = repoSlug.Split('/');

			Assert.AreEqual(2, slugSplits.Length);

			var client = await factory.CreateClientForRepository(fakeAccessString, new RepositoryIdentifier(slugSplits[0], slugSplits[1]), CancellationToken.None);
			Assert.IsNotNull(client);

			var credentials = await client.Connection.CredentialStore.GetCredentials();

			Assert.AreEqual(AuthenticationType.Oauth, credentials.AuthenticationType);

			mockApp.VerifyAll();
		}

		[TestMethod]
		public async Task TestCreateEncodedClientFakeKey()
		{
			var mockApp = new Mock<IAssemblyInformationProvider>();
			mockApp.SetupGet(x => x.ProductInfoHeaderValue).Returns(new ProductInfoHeaderValue("TGSTests", "1.2.3")).Verifiable();

			var mockOptions = new Mock<IOptionsMonitor<GeneralConfiguration>>();
			mockOptions.SetupGet(x => x.CurrentValue).Returns(new GeneralConfiguration());
			var factory = new GitHubClientFactory(mockApp.Object, new BasicHttpMessageHandlerFactory(), mockOptions.Object, loggerFactory.CreateLogger<GitHubClientFactory>());

			await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => factory.CreateClient(null, CancellationToken.None).AsTask());

			var fakeAccessString = $"{RepositorySettings.TgsAppPrivateKeyPrefix}123:{Convert.ToBase64String(Encoding.UTF8.GetBytes(FakePrivateKey))}";

			var client = await factory.CreateClientForRepository(fakeAccessString, new RepositoryIdentifier("fake_owner", "fake_name"), CancellationToken.None);
			Assert.IsNull(client);

			mockApp.VerifyAll();
		}

		[TestMethod]
		public async Task TestBasicClientCaching()
		{
			var mockApp = new Mock<IAssemblyInformationProvider>();
			mockApp.SetupGet(x => x.ProductInfoHeaderValue).Returns(new ProductInfoHeaderValue("TGSTests", "1.2.3")).Verifiable();

			var mockOptions = new Mock<IOptionsMonitor<GeneralConfiguration>>();
			mockOptions.SetupGet(x => x.CurrentValue).Returns(new GeneralConfiguration());
			var factory = new GitHubClientFactory(mockApp.Object, new BasicHttpMessageHandlerFactory(), mockOptions.Object, loggerFactory.CreateLogger<GitHubClientFactory>());

			var client1 = await factory.CreateClient(CancellationToken.None);
			var client2 = await factory.CreateClient("asdf", CancellationToken.None);
			var client3 = await factory.CreateClient(CancellationToken.None);
			var client4 = await factory.CreateClient("asdf", CancellationToken.None);
			Assert.AreSame(client1, client3);
			Assert.AreSame(client2, client4);
		}

		[TestMethod]
		public async Task TestAppClientCaching()
		{
			var mockApp = new Mock<IAssemblyInformationProvider>();
			mockApp.SetupGet(x => x.ProductInfoHeaderValue).Returns(new ProductInfoHeaderValue("TGSTests", "1.2.3")).Verifiable();

			const int MockAppId = 123;
			const int MockRepoId = 42;
			const int MockInstallationId = 542;
			const string MockAccessToken = "asdf_2134_im_the_installation_access_token";

			var mockExpiry = DateTime.UtcNow.AddMinutes(15).AddSeconds(2);

			var mockMessageHandler = new MockHttpMessageHandler(
				(request, cancellationToken) =>
				{
					string json;
					string path = request.RequestUri.AbsolutePath;
					if (path == $"/repositories/{MockRepoId}/installation")
						json = @"{
  ""id"": " + MockInstallationId + @",
  ""account"": {
    ""login"": ""octocat"",
    ""id"": 1,
    ""node_id"": ""MDQ6VXNlcjE="",
    ""avatar_url"": ""https://github.com/images/error/octocat_happy.gif"",
    ""gravatar_id"": """",
    ""url"": ""https://api.github.com/users/octocat"",
    ""html_url"": ""https://github.com/octocat"",
    ""followers_url"": ""https://api.github.com/users/octocat/followers"",
    ""following_url"": ""https://api.github.com/users/octocat/following{/other_user}"",
    ""gists_url"": ""https://api.github.com/users/octocat/gists{/gist_id}"",
    ""starred_url"": ""https://api.github.com/users/octocat/starred{/owner}{/repo}"",
    ""subscriptions_url"": ""https://api.github.com/users/octocat/subscriptions"",
    ""organizations_url"": ""https://api.github.com/users/octocat/orgs"",
    ""repos_url"": ""https://api.github.com/users/octocat/repos"",
    ""events_url"": ""https://api.github.com/users/octocat/events{/privacy}"",
    ""received_events_url"": ""https://api.github.com/users/octocat/received_events"",
    ""type"": ""User"",
    ""site_admin"": false
  },
  ""access_tokens_url"": ""https://api.github.com/app/installations/1/access_tokens"",
  ""repositories_url"": ""https://api.github.com/installation/repositories"",
  ""html_url"": ""https://github.com/organizations/github/settings/installations/1"",
  ""app_id"": 1,
  ""target_id"": 1,
  ""target_type"": ""Organization"",
  ""permissions"": {
    ""checks"": ""write"",
    ""metadata"": ""read"",
    ""contents"": ""read""
  },
  ""events"": [
    ""push"",
    ""pull_request""
  ],
  ""single_file_name"": ""config.yaml"",
  ""has_multiple_single_files"": true,
  ""single_file_paths"": [
    ""config.yml"",
    "".github/issue_TEMPLATE.md""
  ],
  ""repository_selection"": ""selected"",
  ""created_at"": ""2017-07-08T16:18:44-04:00"",
  ""updated_at"": ""2017-07-08T16:18:44-04:00"",
  ""app_slug"": ""github-actions"",
  ""suspended_at"": null,
  ""suspended_by"": null
}";
					else if (path == $"/app/installations/{MockInstallationId}/access_tokens")
						json = @"{
  ""token"": """ + MockAccessToken + @""",
  ""expires_at"": """ + mockExpiry.ToString("O") + @""",
  ""permissions"": {
    ""issues"": ""write"",
    ""contents"": ""read""
  },
  ""repository_selection"": ""selected"",
  ""repositories"": [
    {
      ""id"": 1296269,
      ""node_id"": ""MDEwOlJlcG9zaXRvcnkxMjk2MjY5"",
      ""name"": ""Hello-World"",
      ""full_name"": ""octocat/Hello-World"",
      ""owner"": {
        ""login"": ""octocat"",
        ""id"": 1,
        ""node_id"": ""MDQ6VXNlcjE="",
        ""avatar_url"": ""https://github.com/images/error/octocat_happy.gif"",
        ""gravatar_id"": """",
        ""url"": ""https://api.github.com/users/octocat"",
        ""html_url"": ""https://github.com/octocat"",
        ""followers_url"": ""https://api.github.com/users/octocat/followers"",
        ""following_url"": ""https://api.github.com/users/octocat/following{/other_user}"",
        ""gists_url"": ""https://api.github.com/users/octocat/gists{/gist_id}"",
        ""starred_url"": ""https://api.github.com/users/octocat/starred{/owner}{/repo}"",
        ""subscriptions_url"": ""https://api.github.com/users/octocat/subscriptions"",
        ""organizations_url"": ""https://api.github.com/users/octocat/orgs"",
        ""repos_url"": ""https://api.github.com/users/octocat/repos"",
        ""events_url"": ""https://api.github.com/users/octocat/events{/privacy}"",
        ""received_events_url"": ""https://api.github.com/users/octocat/received_events"",
        ""type"": ""User"",
        ""site_admin"": false
      },
      ""private"": false,
      ""html_url"": ""https://github.com/octocat/Hello-World"",
      ""description"": ""This your first repo!"",
      ""fork"": false,
      ""url"": ""https://api.github.com/repos/octocat/Hello-World"",
      ""archive_url"": ""https://api.github.com/repos/octocat/Hello-World/{archive_format}{/ref}"",
      ""assignees_url"": ""https://api.github.com/repos/octocat/Hello-World/assignees{/user}"",
      ""blobs_url"": ""https://api.github.com/repos/octocat/Hello-World/git/blobs{/sha}"",
      ""branches_url"": ""https://api.github.com/repos/octocat/Hello-World/branches{/branch}"",
      ""collaborators_url"": ""https://api.github.com/repos/octocat/Hello-World/collaborators{/collaborator}"",
      ""comments_url"": ""https://api.github.com/repos/octocat/Hello-World/comments{/number}"",
      ""commits_url"": ""https://api.github.com/repos/octocat/Hello-World/commits{/sha}"",
      ""compare_url"": ""https://api.github.com/repos/octocat/Hello-World/compare/{base}...{head}"",
      ""contents_url"": ""https://api.github.com/repos/octocat/Hello-World/contents/{+path}"",
      ""contributors_url"": ""https://api.github.com/repos/octocat/Hello-World/contributors"",
      ""deployments_url"": ""https://api.github.com/repos/octocat/Hello-World/deployments"",
      ""downloads_url"": ""https://api.github.com/repos/octocat/Hello-World/downloads"",
      ""events_url"": ""https://api.github.com/repos/octocat/Hello-World/events"",
      ""forks_url"": ""https://api.github.com/repos/octocat/Hello-World/forks"",
      ""git_commits_url"": ""https://api.github.com/repos/octocat/Hello-World/git/commits{/sha}"",
      ""git_refs_url"": ""https://api.github.com/repos/octocat/Hello-World/git/refs{/sha}"",
      ""git_tags_url"": ""https://api.github.com/repos/octocat/Hello-World/git/tags{/sha}"",
      ""git_url"": ""git:github.com/octocat/Hello-World.git"",
      ""issue_comment_url"": ""https://api.github.com/repos/octocat/Hello-World/issues/comments{/number}"",
      ""issue_events_url"": ""https://api.github.com/repos/octocat/Hello-World/issues/events{/number}"",
      ""issues_url"": ""https://api.github.com/repos/octocat/Hello-World/issues{/number}"",
      ""keys_url"": ""https://api.github.com/repos/octocat/Hello-World/keys{/key_id}"",
      ""labels_url"": ""https://api.github.com/repos/octocat/Hello-World/labels{/name}"",
      ""languages_url"": ""https://api.github.com/repos/octocat/Hello-World/languages"",
      ""merges_url"": ""https://api.github.com/repos/octocat/Hello-World/merges"",
      ""milestones_url"": ""https://api.github.com/repos/octocat/Hello-World/milestones{/number}"",
      ""notifications_url"": ""https://api.github.com/repos/octocat/Hello-World/notifications{?since,all,participating}"",
      ""pulls_url"": ""https://api.github.com/repos/octocat/Hello-World/pulls{/number}"",
      ""releases_url"": ""https://api.github.com/repos/octocat/Hello-World/releases{/id}"",
      ""ssh_url"": ""git@github.com:octocat/Hello-World.git"",
      ""stargazers_url"": ""https://api.github.com/repos/octocat/Hello-World/stargazers"",
      ""statuses_url"": ""https://api.github.com/repos/octocat/Hello-World/statuses/{sha}"",
      ""subscribers_url"": ""https://api.github.com/repos/octocat/Hello-World/subscribers"",
      ""subscription_url"": ""https://api.github.com/repos/octocat/Hello-World/subscription"",
      ""tags_url"": ""https://api.github.com/repos/octocat/Hello-World/tags"",
      ""teams_url"": ""https://api.github.com/repos/octocat/Hello-World/teams"",
      ""trees_url"": ""https://api.github.com/repos/octocat/Hello-World/git/trees{/sha}"",
      ""clone_url"": ""https://github.com/octocat/Hello-World.git"",
      ""mirror_url"": ""git:git.example.com/octocat/Hello-World"",
      ""hooks_url"": ""https://api.github.com/repos/octocat/Hello-World/hooks"",
      ""svn_url"": ""https://svn.github.com/octocat/Hello-World"",
      ""homepage"": ""https://github.com"",
      ""language"": null,
      ""forks_count"": 9,
      ""stargazers_count"": 80,
      ""watchers_count"": 80,
      ""size"": 108,
      ""default_branch"": ""master"",
      ""open_issues_count"": 0,
      ""is_template"": true,
      ""topics"": [
        ""octocat"",
        ""atom"",
        ""electron"",
        ""api""
      ],
      ""has_issues"": true,
      ""has_projects"": true,
      ""has_wiki"": true,
      ""has_pages"": false,
      ""has_downloads"": true,
      ""archived"": false,
      ""disabled"": false,
      ""visibility"": ""public"",
      ""pushed_at"": ""2011-01-26T19:06:43Z"",
      ""created_at"": ""2011-01-26T19:01:12Z"",
      ""updated_at"": ""2011-01-26T19:14:43Z"",
      ""permissions"": {
        ""admin"": false,
        ""push"": false,
        ""pull"": true
      },
      ""allow_rebase_merge"": true,
      ""template_repository"": null,
      ""temp_clone_token"": ""ABTLWHOULUVAXGTRYU7OC2876QJ2O"",
      ""allow_squash_merge"": true,
      ""allow_auto_merge"": false,
      ""delete_branch_on_merge"": true,
      ""allow_merge_commit"": true,
      ""subscribers_count"": 42,
      ""network_count"": 0,
      ""license"": {
        ""key"": ""mit"",
        ""name"": ""MIT License"",
        ""url"": ""https://api.github.com/licenses/mit"",
        ""spdx_id"": ""MIT"",
        ""node_id"": ""MDc6TGljZW5zZW1pdA=="",
        ""html_url"": ""https://github.com/licenses/mit""
      },
      ""forks"": 1,
      ""open_issues"": 1,
      ""watchers"": 1
    }
  ]
}";
					else
					{
						Assert.Fail($"Unrecognized request path: {request.RequestUri}");
						return Task.FromResult(new HttpResponseMessage());
					}

					var result = new HttpResponseMessage();
					result.StatusCode = HttpStatusCode.OK;
					result.Content = new StringContent(json);
					result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

					return Task.FromResult(result);
				});

			var mockMessageHandlerFactory = new Mock<IHttpMessageHandlerFactory>();
			mockMessageHandlerFactory.Setup(x => x.CreateHandler(It.IsAny<string>())).Returns(mockMessageHandler);

			var mockOptions = new Mock<IOptionsMonitor<GeneralConfiguration>>();
			mockOptions.SetupGet(x => x.CurrentValue).Returns(new GeneralConfiguration());
			var factory = new GitHubClientFactory(mockApp.Object, mockMessageHandlerFactory.Object, mockOptions.Object, loggerFactory.CreateLogger<GitHubClientFactory>());

			var fakeAccessString = $"{RepositorySettings.TgsAppPrivateKeyPrefix}{MockAppId}:{Convert.ToBase64String(Encoding.UTF8.GetBytes(FakePrivateKey))}";
			var repoIdentifier = new RepositoryIdentifier(MockRepoId);

			var client1 = await factory.CreateClientForRepository(fakeAccessString, repoIdentifier, CancellationToken.None);
			Assert.IsNotNull(client1);

			var client2 = await factory.CreateClientForRepository(fakeAccessString, repoIdentifier, CancellationToken.None);
			Assert.AreSame(client1, client2);

			await Task.Delay(TimeSpan.FromSeconds(3));

			var client3 = await factory.CreateClientForRepository(fakeAccessString, repoIdentifier, CancellationToken.None);
			Assert.AreNotSame(client2, client3);
			Assert.IsNotNull(client3);
		}
	}
}
