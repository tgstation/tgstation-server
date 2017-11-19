using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading;

namespace TGS.Server.Components.Tests
{
	/// <summary>
	/// Unit tests for <see cref="Instance"/>
	/// </summary>
	[TestClass]
	public class TestInstance
	{
		[TestMethod]
		public void TestConstructionAndDisposal()
		{
			var mockConfig = new Mock<IInstanceConfig>();
			var mockLogger = new Mock<ILogger>();
			var mockLoggingIDProvider = new Mock<ILoggingIDProvider>();
			var mockServerConfig = new Mock<IServerConfig>();
			var mockContainer = new Mock<IDependencyInjector>();
			new Instance(mockConfig.Object, mockLogger.Object, mockLoggingIDProvider.Object, mockServerConfig.Object, mockContainer.Object).Dispose();
		}
		
		[TestMethod]
		public void TestBadDependencyGraph()
		{
			var mockConfig = new Mock<IInstanceConfig>();
			var mockLogger = new Mock<ILogger>();
			var mockLoggingIDProvider = new Mock<ILoggingIDProvider>();
			var mockServerConfig = new Mock<IServerConfig>();
			Assert.ThrowsException<NullReferenceException>(() => new Instance(mockConfig.Object, mockLogger.Object, mockLoggingIDProvider.Object, mockServerConfig.Object, null));
		}
		
		[TestMethod]
		public void TestAutoUpdate()
		{
			var mockConfig = new Mock<IInstanceConfig>();
			mockConfig.Setup(x => x.AutoUpdateInterval).Returns(1);
			var mockLogger = new Mock<ILogger>();
			var mockLoggingIDProvider = new Mock<ILoggingIDProvider>();
			var mockServerConfig = new Mock<IServerConfig>();
			var mockContainer = new Mock<IDependencyInjector>();
			var mockRepo = new Mock<IRepositoryManager>();
			var mockCompiler = new Mock<ICompilerManager>();

			mockContainer.Setup(x => x.GetInstance<IRepositoryManager>()).Returns(mockRepo.Object);
			mockContainer.Setup(x => x.GetInstance<ICompilerManager>()).Returns(mockCompiler.Object);

			new Instance(mockConfig.Object, mockLogger.Object, mockLoggingIDProvider.Object, mockServerConfig.Object, mockContainer.Object).Dispose();
			mockRepo.Verify(x => x.UpdateImpl(true, false), Times.Exactly(1));
			mockCompiler.Verify(x => x.Compile(true), Times.Exactly(1));
		}
		
		[TestMethod]
		public void TestPostDisposeAutoUpdate()
		{
			var mockConfig = new Mock<IInstanceConfig>();
			var mockLogger = new Mock<ILogger>();
			var mockLoggingIDProvider = new Mock<ILoggingIDProvider>();
			var mockServerConfig = new Mock<IServerConfig>();
			var mockContainer = new Mock<IDependencyInjector>();

			var mockRepo = new Mock<IRepositoryManager>();

			mockContainer.Setup(x => x.GetInstance<IRepositoryManager>()).Returns(mockRepo.Object);

			var I = new Instance(mockConfig.Object, mockLogger.Object, mockLoggingIDProvider.Object, mockServerConfig.Object, mockContainer.Object);
			I.Dispose();
			var po = new PrivateObject(I);
			po.Invoke("HandleAutoUpdate", null);
			Assert.ThrowsException<MockException>(() => mockRepo.Verify(x => x.UpdateImpl(true, false), Times.Once()));
		}
		
		[TestMethod]
		public void TestLoggingVersionAndVerifyConnection()
		{
			var mockConfig = new Mock<IInstanceConfig>();
			var mockLogger = new Mock<ILogger>();
			var mockLoggingIDProvider = new Mock<ILoggingIDProvider>();
			const byte mockLoggingID = 42;
			mockLoggingIDProvider.Setup(x => x.Get()).Returns(mockLoggingID);
			var mockServerConfig = new Mock<IServerConfig>();
			var mockContainer = new Mock<IDependencyInjector>();
			using (var I = new Instance(mockConfig.Object, mockLogger.Object, mockLoggingIDProvider.Object, mockServerConfig.Object, mockContainer.Object))
			{
				I.WriteAccess("user", true);
				I.WriteAccess("useewr", false);
				I.WriteError("asdf", EventID.APIVersionMismatch);
				I.WriteInfo("asdewf", EventID.BridgeDLLUpdateFail);
				I.WriteWarning("fsad", EventID.BYONDUpdateStaged);

				Assert.AreEqual(I.Version(), Server.VersionString);
				I.VerifyConnection();
			}

			mockLogger.Verify(x => x.WriteAccess(It.IsAny<string>(), true, mockLoggingID), Times.Once());
			mockLogger.Verify(x => x.WriteAccess(It.IsAny<string>(), false, mockLoggingID), Times.Once());
			mockLogger.Verify(x => x.WriteError("asdf", EventID.APIVersionMismatch, mockLoggingID), Times.Once());
			mockLogger.Verify(x => x.WriteInfo("asdewf", EventID.BridgeDLLUpdateFail, mockLoggingID), Times.Once());
			mockLogger.Verify(x => x.WriteWarning("fsad", EventID.BYONDUpdateStaged, mockLoggingID), Times.Once());
		}
	}
}
