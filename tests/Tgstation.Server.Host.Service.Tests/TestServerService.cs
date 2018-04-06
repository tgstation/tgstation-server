using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Watchdog;

namespace Tgstation.Server.Host.Service.Tests
{
	/// <summary>
	/// Tests for <see cref="ServerService"/>
	/// </summary>
	[TestClass]
	public sealed class TestServerService
	{
		[TestMethod]
		public void TestConstructionAndDisposal()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new ServerService(null));
			var mockWatchdogFactory = new Mock<IWatchdogFactory>();
			new ServerService(mockWatchdogFactory.Object).Dispose();
		}

		[TestMethod]
		public void TestRun()
		{
			var type = typeof(ServerService);
			var onStart = type.GetMethod("OnStart", BindingFlags.Instance | BindingFlags.NonPublic);
			var onStop = type.GetMethod("OnStop", BindingFlags.Instance | BindingFlags.NonPublic);

			var mockWatchdog = new Mock<IWatchdog>();
			var args = Array.Empty<string>();
			CancellationToken cancellationToken;
			mockWatchdog.Setup(x => x.RunAsync(args, It.IsAny<CancellationToken>())).Callback((string[] _, CancellationToken token) => cancellationToken = token).Returns(Task.CompletedTask).Verifiable();
			var mockWatchdogFactory = new Mock<IWatchdogFactory>();
			mockWatchdogFactory.Setup(x => x.CreateWatchdog()).Returns(mockWatchdog.Object).Verifiable();

			using(var service = new ServerService(mockWatchdogFactory.Object))
			{
				onStart.Invoke(service, new object[] { args });

				Assert.IsFalse(cancellationToken.IsCancellationRequested);

				onStop.Invoke(service, Array.Empty<object>());
				Assert.IsTrue(cancellationToken.IsCancellationRequested);
				mockWatchdog.VerifyAll();
			}

			mockWatchdogFactory.VerifyAll();
		}
	}
}
