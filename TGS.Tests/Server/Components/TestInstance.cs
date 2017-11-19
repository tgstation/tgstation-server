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
		/// <summary>
		/// Test that a basic <see cref="Instance"/> can be constructed
		/// </summary>
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

		/// <summary>
		/// Test that exceptions propagate from <see cref="Instance.Instance(IInstanceConfig, ILogger, ILoggingIDProvider, IServerConfig, IDependencyInjector)"/>
		/// </summary>
		[TestMethod]
		public void TestBadDependencyGraph()
		{
			var mockConfig = new Mock<IInstanceConfig>();
			var mockLogger = new Mock<ILogger>();
			var mockLoggingIDProvider = new Mock<ILoggingIDProvider>();
			var mockServerConfig = new Mock<IServerConfig>();
			Assert.ThrowsException<NullReferenceException>(() => new Instance(mockConfig.Object, mockLogger.Object, mockLoggingIDProvider.Object, mockServerConfig.Object, null));
		}

		/// <summary>
		/// Test that <see cref="Instance.AutoUpdateInterval"/> functions correctly
		/// </summary>
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
			bool repoCalled = false;
			mockRepo.Setup(x => x.UpdateImpl(true, false)).Callback(() => repoCalled = true);

			var mockCompiler = new Mock<ICompilerManager>();
			bool compilerCalled = false;
			mockCompiler.Setup(x => x.Compile(true)).Callback(() => compilerCalled = true);

			mockContainer.Setup(x => x.GetInstance<IRepositoryManager>()).Returns(mockRepo.Object);
			mockContainer.Setup(x => x.GetInstance<ICompilerManager>()).Returns(mockCompiler.Object);

			new Instance(mockConfig.Object, mockLogger.Object, mockLoggingIDProvider.Object, mockServerConfig.Object, mockContainer.Object).Dispose();
			Assert.IsTrue(repoCalled);
			Assert.IsTrue(compilerCalled);
		}
	}
}
