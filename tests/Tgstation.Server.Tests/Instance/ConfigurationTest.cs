using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Client;
using Tgstation.Server.Client.Components;

namespace Tgstation.Server.Tests.Instance
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
			var tmp = (file.Path?.StartsWith('/') ?? false) ? '.' + file.Path : file.Path;
			var path = Path.Combine(instance.Path, "Configuration", tmp);
			var result = File.Exists(path);
			return result;
		}

		async Task TestDeleteDirectory(CancellationToken cancellationToken)
		{
			//try to delete non-existent
			var TestDir = new ConfigurationFileRequest
			{
				Path = "/TestDeleteDir"
			};

			await configurationClient.DeleteEmptyDirectory(TestDir, cancellationToken).ConfigureAwait(false);

			//try to delete non-empty
			const string TestString = "Hello world!";
			using var uploadMs = new MemoryStream(Encoding.UTF8.GetBytes(TestString));
			var file = await configurationClient.Write(new ConfigurationFileRequest
			{
				Path = TestDir.Path + "/test.txt"
			}, uploadMs, cancellationToken).ConfigureAwait(false);

			Assert.IsTrue(FileExists(file));
			Assert.IsNull(file.LastReadHash);

			var updatedFileTuple = await configurationClient.Read(file, cancellationToken).ConfigureAwait(false);
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
					await downloadStream.CopyToAsync(downloadMemoryStream);
				}
				Assert.AreEqual(TestString, Encoding.UTF8.GetString(downloadMemoryStream.ToArray()).Trim());
			}

			await ApiAssert.ThrowsException<ConflictException>(() => configurationClient.DeleteEmptyDirectory(TestDir, cancellationToken), ErrorCode.ConfigurationDirectoryNotEmpty).ConfigureAwait(false);

			file.FileTicket = null;
			await configurationClient.Write(new ConfigurationFileRequest
			{
				Path = updatedFile.Path,
				LastReadHash = updatedFile.LastReadHash
			}, null, cancellationToken).ConfigureAwait(false);
			Assert.IsFalse(FileExists(file));

			await configurationClient.DeleteEmptyDirectory(TestDir, cancellationToken).ConfigureAwait(false);

			var tmp = (TestDir.Path?.StartsWith('/') ?? false) ? '.' + TestDir.Path : TestDir.Path;
			var path = Path.Combine(instance.Path, "Configuration", tmp);
			Assert.IsFalse(Directory.Exists(path));
		}

		public async Task Run(CancellationToken cancellationToken)
		{
			await TestDeleteDirectory(cancellationToken).ConfigureAwait(false);
		}
	}
}
