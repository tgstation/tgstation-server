using Microsoft.Extensions.Logging;
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
			Assert.ThrowsExactly<ArgumentNullException>(() => new ServerService(null, null, default));
			var mockWatchdogFactory = new Mock<IWatchdogFactory>();
			Assert.ThrowsExactly<ArgumentNullException>(() => new ServerService(mockWatchdogFactory.Object, null, default));
			new ServerService(mockWatchdogFactory.Object, Array.Empty<string>(), default).Dispose();
		}

		[TestMethod]
		public void TestRun()
		{
			var type = typeof(ServerService);
			var onStart = type.GetMethod("OnStart", BindingFlags.Instance | BindingFlags.NonPublic);
			var onStop = type.GetMethod("OnStop", BindingFlags.Instance | BindingFlags.NonPublic);

			var mockWatchdog = new Mock<IWatchdog>();
			var args = Array.Empty<string>();
			CancellationToken cancellationToken = default;
			Task signalCheckerTask = null;
			var childStarted = false;
			ISignalChecker signalChecker = null;

			var hostVersionTcs = new TaskCompletionSource<Version>();
			var hostLifetimeTcs = new TaskCompletionSource<bool>();

			mockWatchdog.Setup(x => x.RunAsync(false, It.IsNotNull<string[]>(), It.IsAny<CancellationToken>())).Returns(async (bool x, string[] _, CancellationToken token) =>
			{
				hostVersionTcs.SetResult(typeof(ServerService).Assembly.GetName().Version);

				cancellationToken = token;
				cancellationToken.Register(() => hostLifetimeTcs.SetResult(true));
				signalCheckerTask = signalChecker.CheckSignals(additionalArgs =>
				{
					childStarted = true;
					return (123, hostLifetimeTcs.Task);
				}, cancellationToken).AsTask();

				await signalCheckerTask;
				return true;
			}).Verifiable();
			mockWatchdog.SetupGet(x => x.InitialHostVersion).Returns(hostVersionTcs.Task);
			var mockWatchdogFactory = new Mock<IWatchdogFactory>();

			mockWatchdogFactory.Setup(x => x.CreateWatchdog(It.IsNotNull<ISignalChecker>(), It.IsNotNull<ILoggerFactory>()))
				.Callback<ISignalChecker, ILoggerFactory>((s, loggerFactory) =>
				{
					signalChecker = s;
				})
				.Returns(mockWatchdog.Object)
				.Verifiable();

			using (var service = new ServerService(mockWatchdogFactory.Object, Array.Empty<string>(), default))
			{
				onStart.Invoke(service, new object[] { args });
				Assert.IsTrue(childStarted);
				onStop.Invoke(service, Array.Empty<object>());
				mockWatchdog.VerifyAll();
			}

			mockWatchdogFactory.VerifyAll();
			Assert.IsTrue(signalCheckerTask.IsCompleted);
			Assert.IsTrue(cancellationToken.IsCancellationRequested);
		}
	}
}
