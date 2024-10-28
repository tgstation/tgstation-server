using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Transfer;

namespace Tgstation.Server.Host.Components.StaticFiles.Tests
{
	[TestClass]
	public sealed class TestConfiguration
	{
		/// <summary>
		/// Regression test for https://github.com/tgstation/tgstation-server/issues/1617.
		/// </summary>
		[TestMethod]
		public async Task TestListOrdering()
		{
			using var loggerFactory = LoggerFactory.Create(builder =>
			{
				builder.AddConsole();
				builder.SetMinimumLevel(LogLevel.Trace);
			});

			var tempPath = Path.GetTempFileName();
			File.Delete(tempPath);
			var ioManager = new ResolvingIOManager(new DefaultIOManager(), tempPath);
			await ioManager.CreateDirectory(".", CancellationToken.None);
			try
			{
				await ioManager.WriteAllBytes("a.txt", Array.Empty<byte>(), CancellationToken.None);
				await ioManager.WriteAllBytes("c.txt", Array.Empty<byte>(), CancellationToken.None);
				await ioManager.WriteAllBytes("d.txt", Array.Empty<byte>(), CancellationToken.None);
				await ioManager.CreateDirectory("a", CancellationToken.None);
				await ioManager.CreateDirectory("c", CancellationToken.None);
				await ioManager.CreateDirectory("d", CancellationToken.None);

				var configuration = new Configuration(
					ioManager,
					new SynchronousIOManager(loggerFactory.CreateLogger<SynchronousIOManager>()),
					Mock.Of<IFilesystemLinkFactory>(),
					Mock.Of<IProcessExecutor>(),
					Mock.Of<IPostWriteHandler>(),
					Mock.Of<IPlatformIdentifier>(),
					Mock.Of<IFileTransferTicketProvider>(),
					loggerFactory.CreateLogger<Configuration>(),
					new GeneralConfiguration(),
					new SessionConfiguration());

				await configuration.StartAsync(CancellationToken.None);

				var listResponse = (await configuration.ListDirectory(".", null, CancellationToken.None)).ToList();
				Assert.AreEqual(9, listResponse.Count);
				Assert.AreEqual("a", listResponse[0].Path[2..]);
				Assert.AreEqual("c", listResponse[1].Path[2..]);
				Assert.AreEqual("CodeModifications", listResponse[2].Path[2..]);
				Assert.AreEqual("d", listResponse[3].Path[2..]);
				Assert.AreEqual("EventScripts", listResponse[4].Path[2..]);
				Assert.AreEqual("GameStaticFiles", listResponse[5].Path[2..]);
				Assert.AreEqual("a.txt", listResponse[6].Path[2..]);
				Assert.AreEqual("c.txt", listResponse[7].Path[2..]);
				Assert.AreEqual("d.txt", listResponse[8].Path[2..]);

				await ioManager.CreateDirectory("GameStaticFiles/config/title_screens", CancellationToken.None);
				await ioManager.CreateDirectory("GameStaticFiles/config/title_music", CancellationToken.None);
				await ioManager.WriteAllBytes("GameStaticFiles/config/word_filter.toml", Array.Empty<byte>(), CancellationToken.None);
				await ioManager.WriteAllBytes("GameStaticFiles/config/whitelist.txt", Array.Empty<byte>(), CancellationToken.None);
				await ioManager.WriteAllBytes("GameStaticFiles/config/unbuyableshuttles.txt", Array.Empty<byte>(), CancellationToken.None);
				await ioManager.WriteAllBytes("GameStaticFiles/config/spaceruinblacklist.txt", Array.Empty<byte>(), CancellationToken.None);

				listResponse = (await configuration.ListDirectory("GameStaticFiles/config", null, CancellationToken.None)).ToList();
				var substringStart = "GameStaticFiles/config".Length + 1;
				Assert.AreEqual(6, listResponse.Count);
				Assert.AreEqual("title_music", listResponse[0].Path[substringStart..]);
				Assert.AreEqual("title_screens", listResponse[1].Path[substringStart..]);
				Assert.AreEqual("spaceruinblacklist.txt", listResponse[2].Path[substringStart..]);
				Assert.AreEqual("unbuyableshuttles.txt", listResponse[3].Path[substringStart..]);
				Assert.AreEqual("whitelist.txt", listResponse[4].Path[substringStart..]);
				Assert.AreEqual("word_filter.toml", listResponse[5].Path[substringStart..]);
			}
			finally
			{
				await ioManager.DeleteDirectory(tempPath, CancellationToken.None);
			}
		}
	}
}
