using System;
using System.IO;
using System.IO.Abstractions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.System.Tests
{
	[TestClass]
	public sealed class TestPosixSignalHandler
	{
		[TestMethod]
		public void TestConstruction()
		{
			Assert.ThrowsExactly<ArgumentNullException>(() => new PosixSignalHandler(null, null, null));

			var mockServerControl = Mock.Of<IServerControl>();
			Assert.ThrowsExactly<ArgumentNullException>(() => new PosixSignalHandler(mockServerControl, null, null));

			var mockAsyncDelayer = Mock.Of<IAsyncDelayer>();
			Assert.ThrowsExactly<ArgumentNullException>(() => new PosixSignalHandler(mockServerControl, mockAsyncDelayer, null));

			new PosixSignalHandler(mockServerControl, mockAsyncDelayer, Mock.Of<ILogger<PosixSignalHandler>>()).Dispose();
		}

		[TestMethod]
		public async Task TestSignalListening()
		{
			if (new PlatformIdentifier().IsWindows)
				Assert.Inconclusive("POSIX only test.");

			Assert.Inconclusive("This test fucking doesn't work (hangs on stdout/stderr processing). If this functionality breaks I will find you and devise a very temporarily traumatizing torture for you.");

			// `kill`ing the test process results in it hanging, no idea why
			// we need to run it as a standard dotnet process#if DEBUG
#if DEBUG
			const string CurrentConfig = "Debug";
#else
			const string CurrentConfig = "Release";
#endif

			var pathToSignalTestApp = $"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}/../../../../Tgstation.Server.Host.Tests.Signals";
			var loggerFactory = LoggerFactory.Create(builder =>
			{
				builder.AddConsole();
				builder.SetMinimumLevel(LogLevel.Trace);
			});

			ProcessExecutor processExecutor = null;
			processExecutor = new ProcessExecutor(
				new PosixProcessFeatures(
					new Lazy<IProcessExecutor>(() => processExecutor),
					new DefaultIOManager(
						new FileSystem()),
					loggerFactory.CreateLogger<PosixProcessFeatures>()),
				Mock.Of<IIOManager>(),
				loggerFactory.CreateLogger<ProcessExecutor>(),
				loggerFactory);
			await using var subProc = await processExecutor
				.LaunchProcess(
					"dotnet",
					pathToSignalTestApp,
					$"run -c {CurrentConfig} --no-build",
					CancellationToken.None,
					null,
					null,
					true,
					true,
					false);

			await Task.Delay(TimeSpan.FromSeconds(10));

			using var killProc = global::System.Diagnostics.Process.Start("kill", $"-SIGUSR1 {subProc.Id}");
			killProc.WaitForExit();

			var exitTask = subProc.Lifetime;

			await Task.WhenAny(exitTask, Task.Delay(TimeSpan.FromSeconds(10)));

			if (!exitTask.IsCompleted)
				subProc.Terminate();

			var exitCode = await exitTask;

			using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
			global::System.Console.WriteLine(await subProc.GetCombinedOutput(cts.Token));

			Assert.AreEqual(0, exitCode);
		}
	}
}
