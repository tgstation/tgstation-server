using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Tests
{
	/// <summary>
	/// Tests for <see cref="Program"/>.
	/// </summary>
	[TestClass]
	public sealed class TestProgram
	{
		[TestMethod]
		public async Task TestIncompatibleWatchdog()
		{
			await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => Program.Main(new string[] { "garbage", "0.0.1" })).ConfigureAwait(false);
		}

		[TestMethod]
		public async Task TestStandardRun()
		{
			var mockServer = new Mock<IServer>();
			mockServer.Setup(x => x.Run(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
			mockServer.SetupGet(x => x.RestartRequested).Returns(false);
			var mockServerFactory = new Mock<IServerFactory>();
			mockServerFactory.Setup(x => x.CreateServer(It.IsNotNull<string[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockServer.Object);

			var result = await Program.Run(mockServerFactory.Object, Array.Empty<string>()).ConfigureAwait(false);
			Assert.AreEqual(0, result);
		}

		[TestMethod]
		public async Task TestStandardRunWithRestart()
		{
			var mockServer = new Mock<IServer>();
			mockServer.Setup(x => x.Run(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
			mockServer.SetupGet(x => x.RestartRequested).Returns(true);
			var mockServerFactory = new Mock<IServerFactory>();
			mockServerFactory.Setup(x => x.CreateServer(It.IsNotNull<string[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockServer.Object);

			var result = await Program.Run(mockServerFactory.Object, Array.Empty<string>()).ConfigureAwait(false);
			Assert.AreEqual(1, result);
		}

		[TestMethod]
		public async Task TestStandardRunWithException()
		{
			var mockServer = new Mock<IServer>();
			mockServer.Setup(x => x.Run(It.IsAny<CancellationToken>())).Throws(new DivideByZeroException());
			mockServer.SetupGet(x => x.RestartRequested).Returns(true);
			var mockServerFactory = new Mock<IServerFactory>();
			mockServerFactory.Setup(x => x.CreateServer(It.IsNotNull<string[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockServer.Object);

			await Assert.ThrowsExceptionAsync<DivideByZeroException>(() => Program.Run(mockServerFactory.Object, Array.Empty<string>())).ConfigureAwait(false);
		}

		[TestMethod]
		public async Task TestStandardRunWithExceptionAndWatchdog()
		{
			var mockServer = new Mock<IServer>();
			var exception = new DivideByZeroException();
			mockServer.Setup(x => x.Run(It.IsAny<CancellationToken>())).Throws(exception);
			mockServer.SetupGet(x => x.RestartRequested).Returns(true);
			var mockServerFactory = new Mock<IServerFactory>();
			mockServerFactory.SetupGet(x => x.IOManager).Returns(new DefaultIOManager());
			mockServerFactory.Setup(x => x.CreateServer(It.IsNotNull<string[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockServer.Object);

			var tempFileName = Path.GetTempFileName();
			File.Delete(tempFileName);
			try
			{
				var result = await Program.Run(mockServerFactory.Object, new string[] { tempFileName }).ConfigureAwait(false);
				Assert.AreEqual(2, result);
				Assert.AreEqual(exception.ToString(), File.ReadAllText(tempFileName));
			}
			finally
			{
				File.Delete(tempFileName);
			}
		}
	}
}
