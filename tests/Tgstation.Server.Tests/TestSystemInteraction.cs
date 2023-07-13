using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Tests
{
	[TestClass]
	public sealed class TestSystemInteraction
	{
		[TestMethod]
		public async Task TestScriptExecutionWithStdRead()
		{
			using var loggerFactory = LoggerFactory.Create(x => { });
			var platformIdentifier = new PlatformIdentifier();
			var processExecutor = new ProcessExecutor(
				Mock.Of<IProcessFeatures>(),
				new DefaultIOManager(),
				Mock.Of<ILogger<ProcessExecutor>>(),
				loggerFactory);

			await using var process = processExecutor.LaunchProcess("test." + platformIdentifier.ScriptFileExtension, ".", string.Empty, null, true, true);
			using var cts = new CancellationTokenSource();
			cts.CancelAfter(3000);
			var exitCode = await process.Lifetime.WaitAsync(cts.Token);

			Assert.AreEqual(0, exitCode);
			var result = (await process.GetCombinedOutput(default)).Trim();

			// no guarantees about order
			Assert.IsTrue(result.Contains("Hello World!"));
			Assert.IsTrue(result.Contains("Hello Error!"));
		}

		[TestMethod]
		public async Task TestScriptExecutionWithFileOutput()
		{
			using var loggerFactory = LoggerFactory.Create(x => { });
			var platformIdentifier = new PlatformIdentifier();
			var processExecutor = new ProcessExecutor(
				Mock.Of<IProcessFeatures>(),
				new DefaultIOManager(),
				Mock.Of<ILogger<ProcessExecutor>>(),
				loggerFactory);

			var tempFile = Path.GetTempFileName();
			File.Delete(tempFile);
			try
			{
				await using (var process = processExecutor.LaunchProcess("test." + platformIdentifier.ScriptFileExtension, ".", string.Empty, tempFile, true, true))
				{
					using var cts = new CancellationTokenSource();
					cts.CancelAfter(3000);
					var exitCode = await process.Lifetime.WaitAsync(cts.Token);

					await process.GetCombinedOutput(cts.Token);

					Assert.AreEqual(0, exitCode);
				}

				Assert.IsTrue(File.Exists(tempFile));
				var result = File.ReadAllText(tempFile).Trim();

				// no guarantees about order
				Assert.IsTrue(result.Contains("Hello World!"));
				Assert.IsTrue(result.Contains("Hello Error!"));
			}
			finally
			{
				File.Delete(tempFile);
			}
		}
	}
}
