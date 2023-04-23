using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Client;
using Tgstation.Server.Client.Components;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Tests.Live.Instance
{
	sealed class ConfigurationTest
	{
		readonly IConfigurationClient configurationClient;
		readonly Api.Models.Instance instance;

		public ConfigurationTest(IConfigurationClient configurationClient, Api.Models.Instance instance)
		{
			this.configurationClient = configurationClient ?? throw new ArgumentNullException(nameof(configurationClient));
			this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
		}

		bool FileExists(IConfigurationFile file)
		{
			var tmp = file.Path?.StartsWith('/') ?? false ? '.' + file.Path : file.Path;
			var path = Path.Combine(instance.Path, "Configuration", tmp);
			var result = File.Exists(path);
			return result;
		}

		async Task TestUploadDownloadAndDeleteDirectory(CancellationToken cancellationToken)
		{
			//try to delete non-existent
			var TestDir = new ConfigurationFileRequest
			{
				Path = "/TestDeleteDir"
			};

			await configurationClient.DeleteEmptyDirectory(TestDir, cancellationToken);

			//try to delete non-empty
			const string TestString = "Hello world!";
			using var uploadMs = new MemoryStream(Encoding.UTF8.GetBytes(TestString));
			var file = await configurationClient.Write(new ConfigurationFileRequest
			{
				Path = TestDir.Path + "/test.txt"
			}, uploadMs, cancellationToken);

			Assert.IsTrue(FileExists(file));
			Assert.IsNull(file.LastReadHash);

			var updatedFileTuple = await configurationClient.Read(file, cancellationToken);
			var updatedFile = updatedFileTuple.Item1;
			Assert.IsNotNull(updatedFile.LastReadHash);
			using (var downloadMemoryStream = new MemoryStream())
			{
				using (var downloadStream = updatedFileTuple.Item2)
				{
					var requestStream = downloadStream as CachedResponseStream;
					Assert.IsNotNull(requestStream);
					var response = (HttpResponseMessage)requestStream.GetType().GetField("response", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(requestStream);
					Assert.AreEqual(response.Content.Headers.ContentType.MediaType, MediaTypeNames.Application.Octet);
					await downloadStream.CopyToAsync(downloadMemoryStream, cancellationToken);
				}
				Assert.AreEqual(TestString, Encoding.UTF8.GetString(downloadMemoryStream.ToArray()).Trim());
			}

			await ApiAssert.ThrowsException<ConflictException>(() => configurationClient.DeleteEmptyDirectory(TestDir, cancellationToken), ErrorCode.ConfigurationDirectoryNotEmpty);

			file.FileTicket = null;
			await configurationClient.Write(new ConfigurationFileRequest
			{
				Path = updatedFile.Path,
				LastReadHash = updatedFile.LastReadHash
			}, null, cancellationToken);
			Assert.IsFalse(FileExists(file));

			await configurationClient.DeleteEmptyDirectory(TestDir, cancellationToken);

			var tmp = TestDir.Path?.StartsWith('/') ?? false ? '.' + TestDir.Path : TestDir.Path;
			var path = Path.Combine(instance.Path, "Configuration", tmp);
			Assert.IsFalse(Directory.Exists(path));

			// leave a directory there to test the deployment process
			var staticDir = new ConfigurationFileRequest
			{
				Path = "/GameStaticFiles/data"
			};

			await configurationClient.CreateDirectory(staticDir, cancellationToken);
		}

		Task SetupDMApiTests(CancellationToken cancellationToken)
		{
			// just use an I/O manager here
			var ioManager = new DefaultIOManager();
			return Task.WhenAll(
				ioManager.CopyDirectory(
					"../../../../DMAPI",
					ioManager.ConcatPath(instance.Path, "Repository", "tests", "DMAPI"),
					Enumerable.Empty<string>(),
					null,
					cancellationToken),
				ioManager.CopyDirectory(
					"../../../../../src/DMAPI",
					ioManager.ConcatPath(instance.Path, "Repository", "src", "DMAPI"),
					Enumerable.Empty<string>(),
					null,
					cancellationToken)
				);
		}

		public Task RunPreWatchdog(CancellationToken cancellationToken)
		{
			return Task.WhenAll(TestUploadDownloadAndDeleteDirectory(cancellationToken), SetupDMApiTests(cancellationToken));
		}
	}
}
