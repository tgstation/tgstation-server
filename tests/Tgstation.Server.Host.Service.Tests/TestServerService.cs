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
		public async Task TestConstructionAndDisposal()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new ServerService(null, null));
			var mockWatchdog = new Mock<IWatchdog>();
			Assert.ThrowsException<ArgumentNullException>(() => new ServerService(mockWatchdog.Object, null));
			var mockLoggerFactory = Mock.Of<ILogger<ServerService>>();
			await new ServerService(mockWatchdog.Object, mockLoggerFactory).DisposeAsync();
		}

		[TestMethod]
		public async Task TestRun()
		{
			var type = typeof(ServerService);
			var onStart = type.GetMethod("OnStart", BindingFlags.Instance | BindingFlags.NonPublic);
			var onStop = type.GetMethod("OnStop", BindingFlags.Instance | BindingFlags.NonPublic);

			var mockWatchdog = new Mock<IWatchdog>();
			var args = Array.Empty<string>();
			CancellationToken cancellationToken;
			mockWatchdog.Setup(x => x.RunAsync(false, args, It.IsAny<CancellationToken>())).Callback((bool x, string[] _, CancellationToken token) => cancellationToken = token).Returns(Task.CompletedTask).Verifiable();
			var mockLogger = Mock.Of<ILogger<ServerService>>();

			var service = new ServerService(mockWatchdog.Object, mockLogger);
			await using (service)
			{
				await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.StopAsync(default));
				await service.StartAsync(default);
				await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.StartAsync(default));
				await service.StopAsync(default);
				await service.DisposeAsync();
			}

			await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.StopAsync(default));

			mockWatchdog.VerifyAll();
		}
	}
}
