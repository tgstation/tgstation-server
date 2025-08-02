using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Components.Chat.Providers.Tests
{
	[TestClass]
	public sealed class TestDiscordProvider
	{
		static ChatBot testToken1;
		static IJobManager mockJobManager;

		[ClassInitialize]
		public static void Initialize(TestContext _)
		{
			var actualToken = Environment.GetEnvironmentVariable("TGS_TEST_DISCORD_TOKEN");
			if (!String.IsNullOrWhiteSpace(actualToken))
				testToken1 = new ChatBot
				{
					ConnectionString = actualToken,
					ReconnectionInterval = 1,
					Instance = new Models.Instance()
				};

			var mockSetup = new Mock<IJobManager>();
			mockSetup
				.Setup(x => x.RegisterOperation(It.IsNotNull<Job>(), It.IsNotNull<JobEntrypoint>(), It.IsAny<CancellationToken>()))
				.Callback<Job, JobEntrypoint, CancellationToken>((job, entrypoint, cancellationToken) => job.StartedBy ??= new User { })
				.Returns(ValueTask.CompletedTask);
			mockSetup
				.Setup(x => x.WaitForJobCompletion(It.IsNotNull<Job>(), It.IsAny<User>(), It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>()))
				.Returns(ValueTask.FromResult<bool?>(true));
			mockJobManager = mockSetup.Object;
		}

		[TestMethod]
		public async Task TestConstructionAndDisposal()
		{
			var bot = new ChatBot
			{
				ConnectionString = "fake_token",
				ReconnectionInterval = 1,
				Instance = new Models.Instance(),
			};

			Assert.ThrowsExactly<ArgumentNullException>(() => new DiscordProvider(null, null, null, null, null, null));
			Assert.ThrowsExactly<ArgumentNullException>(() => new DiscordProvider(mockJobManager, null, null, null, null, null));
			var mockDel = Mock.Of<IAsyncDelayer>();
			Assert.ThrowsExactly<ArgumentNullException>(() => new DiscordProvider(mockJobManager, mockDel, null, null, null, null));
			var mockLogger = Mock.Of<ILogger<DiscordProvider>>();
			Assert.ThrowsExactly<ArgumentNullException>(() => new DiscordProvider(mockJobManager, mockDel, mockLogger, null, null, null));
			var mockAss = Mock.Of<IAssemblyInformationProvider>();
			Assert.ThrowsExactly<ArgumentNullException>(() => new DiscordProvider(mockJobManager, mockDel, mockLogger, mockAss, null, null));
			Assert.ThrowsExactly<ArgumentNullException>(() => new DiscordProvider(mockJobManager, mockDel, mockLogger, mockAss, bot, null));
			var mockGen = new GeneralConfiguration();
			await new DiscordProvider(mockJobManager, mockDel, mockLogger, mockAss, bot, mockGen).DisposeAsync();
		}

		static ValueTask InvokeConnect(IProvider provider, CancellationToken cancellationToken = default) => (ValueTask)provider.GetType().GetMethod("Connect", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(provider, new object[] { cancellationToken });

		[TestMethod]
		public async Task TestConnectWithFakeTokenFails()
		{
			var mockLogger = new Mock<ILogger<DiscordProvider>>();
			await using var provider = new DiscordProvider(mockJobManager, Mock.Of<IAsyncDelayer>(), mockLogger.Object, Mock.Of<IAssemblyInformationProvider>(), new ChatBot
			{
				ReconnectionInterval = 1,
				ConnectionString = "asdf",
				Instance = new Models.Instance(),
			}, new GeneralConfiguration());
			await Assert.ThrowsExactlyAsync<JobException>(async () => await InvokeConnect(provider));
			Assert.IsFalse(provider.Connected);
		}

		[TestMethod]
		public async Task TestConnectAndDisconnect()
		{
			if (testToken1 == null)
				Assert.Inconclusive("Required environment variable TGS_TEST_DISCORD_TOKEN isn't set!");

			if (!new DiscordConnectionStringBuilder(testToken1.ConnectionString).Valid)
				Assert.Fail("TGS_TEST_DISCORD_TOKEN is not a valid Discord connection string!");

			var mockLogger = new Mock<ILogger<DiscordProvider>>();
			await using var provider = new DiscordProvider(mockJobManager, Mock.Of<IAsyncDelayer>(), mockLogger.Object, Mock.Of<IAssemblyInformationProvider>(), testToken1, new GeneralConfiguration());
			Assert.IsFalse(provider.Connected);
			await InvokeConnect(provider);
			Assert.IsTrue(provider.Connected);

			await provider.Disconnect(default);
			Assert.IsFalse(provider.Connected);
		}
	}
}
