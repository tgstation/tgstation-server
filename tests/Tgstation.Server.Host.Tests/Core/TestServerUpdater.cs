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
	public sealed class TestServerUpdater
	{
		[TestMethod]
		public void TestConstructor()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new ServerUpdateInitiator(null, null));
			Assert.ThrowsException<ArgumentNullException>(() => new ServerUpdateInitiator(Mock.Of<ISwarmService>(), null));
			_ = new ServerUpdateInitiator(Mock.Of<ISwarmService>(), Mock.Of<IServerUpdater>());
		}

		[TestMethod]
		public void TestInitiateUpdate()
		{
			var mockSwarmService = Mock.Of<ISwarmService>();
			var mockServerUpdater = new Mock<IServerUpdater>();

			var testVersion = new Version(Random.Shared.Next(), Random.Shared.Next(), Random.Shared.Next());

			mockServerUpdater.Setup(x => x.BeginUpdate(mockSwarmService, It.IsAny<IFileStreamProvider>(), testVersion, It.IsAny<CancellationToken>())).Returns(Task.FromResult(ServerUpdateResult.Started)).Verifiable();
			var updateInitiator = new ServerUpdateInitiator(mockSwarmService, mockServerUpdater.Object);

			Assert.IsTrue(updateInitiator.InitiateUpdate(testVersion, default).IsCompleted);
			mockServerUpdater.VerifyAll();
		}
	}
}
