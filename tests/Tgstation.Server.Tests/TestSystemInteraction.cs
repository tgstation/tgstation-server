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
		public async Task TestScriptExecution()
		{
			var platformIdentifier = new PlatformIdentifier();
			var processExecutor = new ProcessExecutor(
				Mock.Of<IProcessFeatures>(),
				Mock.Of<IIOManager>(),
				Mock.Of<ILogger<ProcessExecutor>>(),
				LoggerFactory.Create(x => { }));

			await using var process = await processExecutor.LaunchProcess("test." + platformIdentifier.ScriptFileExtension, ".", string.Empty, null, true, true);
			using var cts = new CancellationTokenSource();
			cts.CancelAfter(3000);
			var exitCode = await process.Lifetime.WithToken(cts.Token);

			Assert.AreEqual(0, exitCode);
			Assert.AreEqual("Hello World!", (await process.GetCombinedOutput(default)).Trim());
		}
	}
}
