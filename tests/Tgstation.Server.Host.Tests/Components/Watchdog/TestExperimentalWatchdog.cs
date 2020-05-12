using Byond.TopicSender;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Components.Watchdog.Tests
{
	[TestClass]
	public sealed class TestExperimentalWatchdog
	{
		[TestMethod]
		public void TestConstruction()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new ExperimentalWatchdog(null, null, null, null, null, null, null, null, null, null, null, default));

			var mockChat = new Mock<IChatManager>();
			mockChat.Setup(x => x.RegisterCommandHandler(It.IsNotNull<ICustomCommandHandler>())).Verifiable();
			Assert.ThrowsException<ArgumentNullException>(() => new ExperimentalWatchdog(mockChat.Object, null, null, null, null, null, null, null, null, null, null, default));

			var mockSessionControllerFactory = new Mock<ISessionControllerFactory>();
			Assert.ThrowsException<ArgumentNullException>(() => new ExperimentalWatchdog(mockChat.Object, mockSessionControllerFactory.Object, null, null, null, null, null, null, null, null, null, default));

			var mockDmbFactory = new Mock<IDmbFactory>();
			Assert.ThrowsException<ArgumentNullException>(() => new ExperimentalWatchdog(mockChat.Object, mockSessionControllerFactory.Object, mockDmbFactory.Object, null, null, null, null, null, null, null, null, default));

			var mockReattachInfoHandler = new Mock<IReattachInfoHandler>();
			Assert.ThrowsException<ArgumentNullException>(() => new ExperimentalWatchdog(mockChat.Object, mockSessionControllerFactory.Object, mockDmbFactory.Object, mockReattachInfoHandler.Object, null, null, null, null, null, null, null, default));

			var mockDatabaseContextFactory = new Mock<IDatabaseContextFactory>();
			Assert.ThrowsException<ArgumentNullException>(() => new ExperimentalWatchdog(mockChat.Object, mockSessionControllerFactory.Object, mockDmbFactory.Object, mockReattachInfoHandler.Object, mockDatabaseContextFactory.Object, null, null, null, null, null, null, default));

			var mockJobManager = new Mock<IJobManager>();
			Assert.ThrowsException<ArgumentNullException>(() => new ExperimentalWatchdog(mockChat.Object, mockSessionControllerFactory.Object, mockDmbFactory.Object, mockReattachInfoHandler.Object, mockDatabaseContextFactory.Object, mockJobManager.Object, null, null, null, null, null, default));

			var mockRestartRegistration = new Mock<IRestartRegistration>();
			mockRestartRegistration.Setup(x => x.Dispose()).Verifiable();
			var mockServerControl = new Mock<IServerControl>();
			mockServerControl.Setup(x => x.RegisterForRestart(It.IsNotNull<IRestartHandler>())).Returns(mockRestartRegistration.Object).Verifiable();
			Assert.ThrowsException<ArgumentNullException>(() => new ExperimentalWatchdog(mockChat.Object, mockSessionControllerFactory.Object, mockDmbFactory.Object, mockReattachInfoHandler.Object, mockDatabaseContextFactory.Object, mockJobManager.Object, mockServerControl.Object, null, null, null, null, default));

			var mockAsyncDelayer = new Mock<IAsyncDelayer>();
			Assert.ThrowsException<ArgumentNullException>(() => new ExperimentalWatchdog(mockChat.Object, mockSessionControllerFactory.Object, mockDmbFactory.Object, mockReattachInfoHandler.Object, mockDatabaseContextFactory.Object, mockJobManager.Object, mockServerControl.Object, mockAsyncDelayer.Object, null, null, null, default));

			var mockLogger = new Mock<ILogger<ExperimentalWatchdog>>();
			Assert.ThrowsException<ArgumentNullException>(() => new ExperimentalWatchdog(mockChat.Object, mockSessionControllerFactory.Object, mockDmbFactory.Object, mockReattachInfoHandler.Object, mockDatabaseContextFactory.Object, mockJobManager.Object, mockServerControl.Object, mockAsyncDelayer.Object, mockLogger.Object, null, null, default));

			var mockLaunchParameters = new DreamDaemonLaunchParameters();
			Assert.ThrowsException<ArgumentNullException>(() => new ExperimentalWatchdog(mockChat.Object, mockSessionControllerFactory.Object, mockDmbFactory.Object, mockReattachInfoHandler.Object, mockDatabaseContextFactory.Object,  mockJobManager.Object, mockServerControl.Object, mockAsyncDelayer.Object, mockLogger.Object, mockLaunchParameters, null, default));

			var mockInstance = new Models.Instance();
			new ExperimentalWatchdog(mockChat.Object, mockSessionControllerFactory.Object, mockDmbFactory.Object, mockReattachInfoHandler.Object, mockDatabaseContextFactory.Object,  mockJobManager.Object, mockServerControl.Object, mockAsyncDelayer.Object, mockLogger.Object, mockLaunchParameters, mockInstance, default).Dispose();

			mockRestartRegistration.VerifyAll();
			mockServerControl.VerifyAll();
			mockChat.VerifyAll();
		}
	}
}
