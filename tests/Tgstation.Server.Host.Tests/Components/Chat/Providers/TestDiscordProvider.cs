using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Components.Chat.Providers.Tests
{
	[TestClass]
	public sealed class TestDiscordProvider
	{
		ChatBot testToken1;
		IJobManager mockJobManager;

		[TestInitialize]
		public void Initialize()
		{
			var actualToken = Environment.GetEnvironmentVariable("TGS_TEST_DISCORD_TOKEN");
			if (!String.IsNullOrWhiteSpace(actualToken))
				testToken1 = new ChatBot
				{
					ConnectionString = actualToken,
					ReconnectionInterval = 1
				};

			var mockSetup = new Mock<IJobManager>();
			mockSetup
				.Setup(x => x.RegisterOperation(It.IsNotNull<Job>(), It.IsNotNull<JobEntrypoint>(), It.IsAny<CancellationToken>()))
				.Callback<Job, JobEntrypoint, CancellationToken>((job, entrypoint, cancellationToken) => job.StartedBy ??= new User { })
				.Returns(Task.CompletedTask);
			mockSetup
				.Setup(x => x.WaitForJobCompletion(It.IsNotNull<Job>(), It.IsAny<User>(), It.IsAny<CancellationToken>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);
			mockJobManager = mockSetup.Object;
		}

		[TestMethod]
		public async Task TestConstructionAndDisposal()
		{
			if (testToken1 == null)
				Assert.Inconclusive("Required environment variable TGS_TEST_DISCORD_TOKEN isn't set!");

			Assert.ThrowsException<ArgumentNullException>(() => new DiscordProvider(null, null, null, null));
			Assert.ThrowsException<ArgumentNullException>(() => new DiscordProvider(mockJobManager, null, null, null));
			var mockAss = new Mock<IAssemblyInformationProvider>();
			Assert.ThrowsException<ArgumentNullException>(() => new DiscordProvider(mockJobManager, mockAss.Object, null, null));
			var mockLogger = new Mock<ILogger<DiscordProvider>>();
			Assert.ThrowsException<ArgumentNullException>(() => new DiscordProvider(mockJobManager, null, mockLogger.Object, null));
			await new DiscordProvider(mockJobManager, mockAss.Object, mockLogger.Object, testToken1).DisposeAsync();
		}

		static Task InvokeConnect(IProvider provider, CancellationToken cancellationToken = default) => (Task)provider.GetType().GetMethod("Connect", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(provider, new object[] { cancellationToken });

		[TestMethod]
		public async Task TestConnectWithFakeTokenFails()
		{
			var mockLogger = new Mock<ILogger<DiscordProvider>>();
			await using var provider = new DiscordProvider(mockJobManager, Mock.Of<IAssemblyInformationProvider>(), mockLogger.Object, new ChatBot
			{
				ReconnectionInterval = 1,
				ConnectionString = "asdf"
			});
			await Assert.ThrowsExceptionAsync<JobException>(() => InvokeConnect(provider));
			Assert.IsFalse(provider.Connected);
		}

		[TestMethod]
		public async Task TestConnectAndDisconnect()
		{
			if (testToken1 == null)
				Assert.Inconclusive("Required environment variable TGS_TEST_DISCORD_TOKEN isn't set!");

			var mockLogger = new Mock<ILogger<DiscordProvider>>();
			await using var provider = new DiscordProvider(mockJobManager, Mock.Of<IAssemblyInformationProvider>(), mockLogger.Object, testToken1);
			Assert.IsFalse(provider.Connected);
			await InvokeConnect(provider);
			Assert.IsTrue(provider.Connected);

			await provider.Disconnect(default);
			Assert.IsFalse(provider.Connected);
		}
	}
}
