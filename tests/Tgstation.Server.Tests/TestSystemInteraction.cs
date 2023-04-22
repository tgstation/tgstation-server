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

			await using var process = await processExecutor.LaunchProcess("test." + platformIdentifier.ScriptFileExtension, ".", string.Empty, null, true, true);
			using var cts = new CancellationTokenSource();
			cts.CancelAfter(3000);
			var exitCode = await process.Lifetime.WithToken(cts.Token);

			Assert.AreEqual(0, exitCode);
			var result = (await process.GetCombinedOutput(default)).Trim();
			var expected = $"Hello World!{Environment.NewLine}Hello Error!";
			Assert.AreEqual(expected, result);
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
				await using (var process = await processExecutor.LaunchProcess("test." + platformIdentifier.ScriptFileExtension, ".", string.Empty, tempFile, true, true))
				{
					using var cts = new CancellationTokenSource();
					cts.CancelAfter(3000);
					var exitCode = await process.Lifetime.WithToken(cts.Token);

					await process.GetCombinedOutput(cts.Token);

					Assert.AreEqual(0, exitCode);
				}

				var expected = $"Hello World!{Environment.NewLine}Hello Error!";

				Assert.IsTrue(File.Exists(tempFile));
				var result = File.ReadAllText(tempFile).Trim();
				Assert.AreEqual(expected, result);
			}
			finally
			{
				File.Delete(tempFile);
			}
		}
	}
}
