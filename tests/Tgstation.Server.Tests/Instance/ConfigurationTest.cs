using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
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

		bool FileExists(ConfigurationFile file)
		{
			var tmp = (file.Path?.StartsWith('/') ?? false) ? '.' + file.Path : file.Path;
			var path = Path.Combine(instance.Path, "Configuration", tmp);
			var result = File.Exists(path);
			return result;
		}

		async Task TestDeleteDirectory(CancellationToken cancellationToken)
		{
			//try to delete non-existent
			var TestDir = new ConfigurationFile
			{
				Path = "/TestDeleteDir"
			};

			await configurationClient.DeleteEmptyDirectory(TestDir, cancellationToken).ConfigureAwait(false);

			//try to delete non-empty
			using var uploadMs = new MemoryStream(Encoding.UTF8.GetBytes("Hello world!"));
			var file = await configurationClient.Write(new ConfigurationFile
			{
				Path = TestDir.Path + "/test.txt"
			}, uploadMs, cancellationToken).ConfigureAwait(false);

			Assert.IsTrue(FileExists(file));

			var updatedFile = await configurationClient.Read(file, cancellationToken).ConfigureAwait(false);
			Assert.AreEqual(file.LastReadHash, updatedFile.Item1.LastReadHash);

			await ApiAssert.ThrowsException<ConflictException>(() => configurationClient.DeleteEmptyDirectory(TestDir, cancellationToken), ErrorCode.ConfigurationDirectoryNotEmpty).ConfigureAwait(false);

			file.FileTicket = null;
			await configurationClient.Write(file, null, cancellationToken).ConfigureAwait(false);

			await configurationClient.DeleteEmptyDirectory(TestDir, cancellationToken).ConfigureAwait(false);
		}

		public async Task Run(CancellationToken cancellationToken)
		{
			await TestDeleteDirectory(cancellationToken).ConfigureAwait(false);
		}
	}
}
