using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Components.Chat.Providers.Tests
{
	[TestClass]
	public sealed class TestDiscordProvider
	{
		string testToken1;

		[TestInitialize]
		public void Initialize()
		{
			testToken1 = Environment.GetEnvironmentVariable("TGS4_TEST_DISCORD_TOKEN_1");
		}

		[TestMethod]
		public void TestConstructionAndDisposal()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new DiscordProvider(null, null, null, 1));
			var mockAss = new Mock<IAssemblyInformationProvider>();
			Assert.ThrowsException<ArgumentNullException>(() => new DiscordProvider(mockAss.Object, null, null, 1));
			var mockLogger = new Mock<ILogger<DiscordProvider>>();
			Assert.ThrowsException<ArgumentNullException>(() => new DiscordProvider(mockAss.Object, mockLogger.Object, null, 1));
			var mockToken = "asdf";
			Assert.ThrowsException<ArgumentOutOfRangeException>(() => new DiscordProvider(mockAss.Object, mockLogger.Object, mockToken, 0));
			new DiscordProvider(mockAss.Object, mockLogger.Object, mockToken, 1).Dispose();
		}

		[TestMethod]
		public async Task TestConnectWithFakeTokenFails()
		{
			var mockLogger = new Mock<ILogger<DiscordProvider>>();
			using var provider = new DiscordProvider(Mock.Of<IAssemblyInformationProvider>(), mockLogger.Object, "asdf", 1);
			Assert.IsFalse(await provider.Connect(default).ConfigureAwait(false));
			Assert.IsFalse(provider.Connected);
		}

		[Ignore("Broken due to dependency issues after first call to .Connect()")]
		[TestMethod]
		public async Task TestConnectAndDisconnect()
		{
			if (testToken1 == null)
				Assert.Inconclusive("Required environment variable TGS4_TEST_DISCORD_TOKEN_1 isn't set!");


			var mockLogger = new Mock<ILogger<DiscordProvider>>();
			using var provider = new DiscordProvider(Mock.Of<IAssemblyInformationProvider>(), mockLogger.Object, testToken1, 1);
			Assert.IsFalse(provider.Connected);
			await provider.Disconnect(default).ConfigureAwait(false);
			Assert.IsFalse(provider.Connected);
			Assert.IsTrue(await provider.Connect(default).ConfigureAwait(false));
			Assert.IsTrue(provider.Connected);
			Assert.IsTrue(await provider.Connect(default).ConfigureAwait(false));
			Assert.IsTrue(provider.Connected);

			await provider.Disconnect(default).ConfigureAwait(false);
			Assert.IsFalse(provider.Connected);
			await provider.Disconnect(default).ConfigureAwait(false);
			Assert.IsFalse(provider.Connected);

			//now try it with cancellationTokens
			using var cts = new CancellationTokenSource();
			cts.Cancel();
			var cancellationToken = cts.Token;
			await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => provider.Connect(cancellationToken)).ConfigureAwait(false);
			Assert.IsFalse(provider.Connected);
			Assert.IsTrue(await provider.Connect(default).ConfigureAwait(false));
			Assert.IsTrue(provider.Connected);
			await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => provider.Disconnect(cancellationToken)).ConfigureAwait(false);
			Assert.IsTrue(provider.Connected);
			await provider.Disconnect(default).ConfigureAwait(false);
			Assert.IsFalse(provider.Connected);
		}
	}
}
