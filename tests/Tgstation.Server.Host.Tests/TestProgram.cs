using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.Common;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.System;

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
			await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => Program.Main(new string[] { "garbage", "0.0.1" }));
		}

		[TestMethod]
		public async Task TestStandardRun()
		{
			var mockServer = new Mock<IServer>();
			mockServer.Setup(x => x.Run(It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
			mockServer.SetupGet(x => x.RestartRequested).Returns(false);
			var mockServerFactory = new Mock<IServerFactory>();
			mockServerFactory.Setup(x => x.CreateServer(It.IsNotNull<string[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockServer.Object);

			var program = new Program
			{
				ServerFactory = mockServerFactory.Object
			};

			var result = await program.Main(Array.Empty<string>(), null);
			Assert.AreEqual(HostExitCode.CompleteExecution, result);
		}

		[TestMethod]
		public async Task TestStandardRunWithRestart()
		{
			var mockServer = new Mock<IServer>();
			mockServer.Setup(x => x.Run(It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
			mockServer.SetupGet(x => x.RestartRequested).Returns(true);
			var mockServerFactory = new Mock<IServerFactory>();
			mockServerFactory.Setup(x => x.CreateServer(It.IsNotNull<string[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockServer.Object);

			var program = new Program
			{
				ServerFactory = mockServerFactory.Object
			};

			var result = await program.Main(Array.Empty<string>(), null);
			Assert.AreEqual(HostExitCode.RestartRequested, result);
		}

		[TestMethod]
		public async Task TestStandardRunWithException()
		{
			var mockServer = new Mock<IServer>();
			mockServer.Setup(x => x.Run(It.IsAny<CancellationToken>())).Throws(new DivideByZeroException());
			mockServer.SetupGet(x => x.RestartRequested).Returns(true);
			var mockServerFactory = new Mock<IServerFactory>();
			mockServerFactory.Setup(x => x.CreateServer(It.IsNotNull<string[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockServer.Object);

			var program = new Program
			{
				ServerFactory = mockServerFactory.Object
			};

			await Assert.ThrowsExactlyAsync<DivideByZeroException>(() => program.Main(Array.Empty<string>(), null).AsTask());
		}

		[TestMethod]
		public async Task TestStandardRunWithExceptionAndWatchdog()
		{
			var mockServer = new Mock<IServer>();
			var mockFs = new MockFileSystem();
			var exception = new DivideByZeroException();
			mockServer.Setup(x => x.Run(It.IsAny<CancellationToken>())).Throws(exception);
			mockServer.SetupGet(x => x.RestartRequested).Returns(true);
			var mockServerFactory = new Mock<IServerFactory>();
			mockServerFactory.SetupGet(x => x.IOManager).Returns(new DefaultIOManager(mockFs));
			mockServerFactory.Setup(x => x.CreateServer(It.IsNotNull<string[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockServer.Object);
			var program = new Program
			{
				ServerFactory = mockServerFactory.Object
			};

			var tempFileName = mockFs.Path.Combine(mockFs.Path.GetTempPath(), mockFs.Path.GetRandomFileName());
			mockFs.File.Delete(tempFileName);
			try
			{
				var result = await program.Main(Array.Empty<string>(), tempFileName);
				Assert.AreEqual(HostExitCode.Error, result);
				Assert.AreEqual(exception.ToString(), mockFs.File.ReadAllText(tempFileName));
			}
			finally
			{
				mockFs.File.Delete(tempFileName);
			}
		}
	}
}
