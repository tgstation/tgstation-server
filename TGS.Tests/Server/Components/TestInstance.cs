using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.ServiceModel;

namespace TGS.Server.Components.Tests
{
	/// <summary>
	/// Unit tests for <see cref="Instance"/>
	/// </summary>
	[TestClass]
	public class TestInstance
	{
		const string TestJSON = "{}";

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

		[TestMethod]
		public void TestOfflining()
		{
			var mockConfig = new Mock<IInstanceConfig>();
			var mockLogger = new Mock<ILogger>();
			var mockLoggingIDProvider = new Mock<ILoggingIDProvider>();
			var mockServerConfig = new Mock<IServerConfig>();
			var mockContainer = new Mock<IDependencyInjector>();
			using (var I = new Instance(mockConfig.Object, mockLogger.Object, mockLoggingIDProvider.Object, mockServerConfig.Object, mockContainer.Object))
				I.Offline();
			mockConfig.VerifySet(x => x.Enabled = false, Times.Once());
		}

		[TestMethod]
		public void TestServerDirectory()
		{
			const string FakeServerDirectory = "asdfasdfdfjk";
			var mockConfig = new Mock<IInstanceConfig>();
			mockConfig.Setup(x => x.Directory).Returns(FakeServerDirectory);
			var mockLogger = new Mock<ILogger>();
			var mockLoggingIDProvider = new Mock<ILoggingIDProvider>();
			var mockServerConfig = new Mock<IServerConfig>();
			var mockContainer = new Mock<IDependencyInjector>();
			using (var I = new Instance(mockConfig.Object, mockLogger.Object, mockLoggingIDProvider.Object, mockServerConfig.Object, mockContainer.Object))
				Assert.AreEqual(I.ServerDirectory(), FakeServerDirectory);
		}

		[TestMethod]
		public void TestReattach()
		{
			const string FakeServerDirectory = "asdfasdfdfjk";
			var mockConfig = new Mock<IInstanceConfig>();
			mockConfig.Setup(x => x.Directory).Returns(FakeServerDirectory);
			var mockLogger = new Mock<ILogger>();
			var mockLoggingIDProvider = new Mock<ILoggingIDProvider>();
			var mockServerConfig = new Mock<IServerConfig>();
			var mockContainer = new Mock<IDependencyInjector>();

			var mockChat = new Mock<IChatManager>();
			mockContainer.Setup(x => x.GetInstance<IChatManager>()).Returns(mockChat.Object);

			using (var I = new Instance(mockConfig.Object, mockLogger.Object, mockLoggingIDProvider.Object, mockServerConfig.Object, mockContainer.Object))
			{
				I.Reattach(false);
				I.Reattach(true);
			}

			mockChat.Verify(x => x.SendMessage(It.IsAny<string>(), MessageType.DeveloperInfo), Times.Once());
			mockConfig.VerifySet(x => x.ReattachRequired = true, Times.Exactly(2));
		}

		[TestMethod]
		public void TestUpdateTGS3JSON()
		{
			var mockConfig = new Mock<IInstanceConfig>();
			var mockLogger = new Mock<ILogger>();
			var mockLoggingIDProvider = new Mock<ILoggingIDProvider>();
			var mockServerConfig = new Mock<IServerConfig>();
			var mockContainer = new Mock<IDependencyInjector>();

			var mockIO = new Mock<IIOManager>();

			mockContainer.Setup(x => x.GetInstance<IIOManager>()).Returns(mockIO.Object);

			using (var I = new Instance(mockConfig.Object, mockLogger.Object, mockLoggingIDProvider.Object, mockServerConfig.Object, mockContainer.Object))
				Assert.IsNull(I.UpdateTGS3Json());

			mockIO.Verify(x => x.CopyFile(It.IsAny<string>(), It.IsAny<string>(), true, false), Times.Once());
			mockIO.ResetCalls();
			mockIO.Setup(x => x.CopyFile(It.IsAny<string>(), It.IsAny<string>(), true, false)).Throws(new InternalTestFailureException());

			using (var I = new Instance(mockConfig.Object, mockLogger.Object, mockLoggingIDProvider.Object, mockServerConfig.Object, mockContainer.Object))
				Assert.IsNotNull(I.UpdateTGS3Json());

			mockIO.Verify(x => x.CopyFile(It.IsAny<string>(), It.IsAny<string>(), true, false), Times.Once());
		}

		[TestMethod]
		public void TestAutoUpdateInterval()
		{
			const ulong FakeAAI = 63;
			var mockConfig = new Mock<IInstanceConfig>();
			mockConfig.Setup(x => x.AutoUpdateInterval).Returns(FakeAAI);
			var mockLogger = new Mock<ILogger>();
			var mockLoggingIDProvider = new Mock<ILoggingIDProvider>();
			var mockServerConfig = new Mock<IServerConfig>();
			var mockContainer = new Mock<IDependencyInjector>();

			using (var I = new Instance(mockConfig.Object, mockLogger.Object, mockLoggingIDProvider.Object, mockServerConfig.Object, mockContainer.Object))
			{
				mockConfig.ResetCalls();
				Assert.AreEqual(I.AutoUpdateInterval(), FakeAAI);
			}

			mockConfig.Verify(x => x.AutoUpdateInterval, Times.Once());
		}

		[TestMethod]
		public void TestCreateServiceHost()
		{
			var mockConfig = new Mock<IInstanceConfig>();
			var mockLogger = new Mock<ILogger>();
			var mockLoggingIDProvider = new Mock<ILoggingIDProvider>();
			var mockServerConfig = new Mock<IServerConfig>();
			var mockContainer = new Mock<IDependencyInjector>();
			var CRT = typeof(object);
			var TestSC = new ServiceHost(CRT, new Uri[] { });
			mockContainer.Setup(x => x.CreateServiceHost(It.IsAny<Type>(), It.IsAny<Uri[]>())).Returns(TestSC);

			using (var I = new Instance(mockConfig.Object, mockLogger.Object, mockLoggingIDProvider.Object, mockServerConfig.Object, mockContainer.Object))
			{
				Assert.AreSame(TestSC, I.CreateServiceHost(new Uri[] { }));
				mockContainer.Verify(x => x.CreateServiceHost(It.IsAny<Type>(), It.IsAny<Uri[]>()), Times.Once());
			}
		}
	}
}
