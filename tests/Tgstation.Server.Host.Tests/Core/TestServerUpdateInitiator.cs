using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Swarm;

namespace Tgstation.Server.Host.Core.Tests
{
	[TestClass]
	public sealed class TestServerUpdateInitiator
	{
		[TestMethod]
		public void TestConstructor()
		{
			Assert.ThrowsExactly<ArgumentNullException>(() => new ServerUpdateInitiator(null, null));
			Assert.ThrowsExactly<ArgumentNullException>(() => new ServerUpdateInitiator(Mock.Of<ISwarmService>(), null));
			_ = new ServerUpdateInitiator(Mock.Of<ISwarmService>(), Mock.Of<IServerUpdater>());
		}

		[TestMethod]
		public void TestInitiateUpdate()
		{
			var mockSwarmService = Mock.Of<ISwarmService>();
			var mockServerUpdater = new Mock<IServerUpdater>();
			var mockFileProvider = Mock.Of<IFileStreamProvider>();

			var testVersion = new Version(Random.Shared.Next(), Random.Shared.Next(), Random.Shared.Next());

			mockServerUpdater.Setup(x => x.BeginUpdate(mockSwarmService, It.IsAny<IFileStreamProvider>(), testVersion, It.IsAny<CancellationToken>())).Returns(ValueTask.FromResult(ServerUpdateResult.Started)).Verifiable();
			var updateInitiator = new ServerUpdateInitiator(mockSwarmService, mockServerUpdater.Object);

			Assert.IsTrue(updateInitiator.InitiateUpdate(mockFileProvider, testVersion, default).IsCompleted);
			Assert.IsTrue(updateInitiator.InitiateUpdate(null, testVersion, default).IsCompleted);
			mockServerUpdater.VerifyAll();
			Assert.AreEqual(2, mockServerUpdater.Invocations.Count);
		}
	}
}
