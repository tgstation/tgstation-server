using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Client;

namespace Tgstation.Server.Tests
{
	sealed class AdministrationTest
	{
		readonly IAdministrationClient client;

		public AdministrationTest(IAdministrationClient client)
		{
			this.client = client ?? throw new ArgumentNullException(nameof(client));
		}

		public async Task Run(CancellationToken cancellationToken)
		{
			var logsTest = TestLogs(cancellationToken);
			await TestRead(cancellationToken).ConfigureAwait(false);
			await logsTest;
		}

		async Task TestLogs(CancellationToken cancellationToken)
		{
			var logs = await client.ListLogs(cancellationToken);
			Assert.AreNotEqual(0, logs.Count);
			var logFile = logs.First();
			Assert.IsNotNull(logFile);
			Assert.IsFalse(String.IsNullOrWhiteSpace(logFile.Name));
			Assert.IsNull(logFile.Content);

			var downloaded = await client.GetLog(logFile, cancellationToken);
			Assert.AreEqual(logFile.Name, downloaded.Name);
			Assert.IsTrue(logFile.LastModified <= downloaded.LastModified);
			Assert.IsNull(logFile.Content);

			await ApiAssert.ThrowsException<ConflictException>(() => client.GetLog(new LogFile
			{
				Name = "very_fake_path.log"
			}, cancellationToken), ErrorCode.IOError);

			await Assert.ThrowsExceptionAsync<InsufficientPermissionsException>(() => client.GetLog(new LogFile
			{
				Name = "../out_of_bounds.file"
			}, cancellationToken));
		}

		async Task TestRead(CancellationToken cancellationToken)
		{
			Administration model;
			try
			{
				model = await client.Read(cancellationToken).ConfigureAwait(false);
			}
			catch (RateLimitException)
			{
				Assert.Inconclusive("GitHub rate limit hit while testing administration endpoint. Set environment variable TGS4_TEST_GITHUB_TOKEN to fix this!");
				return;	//c# needs the equivalent of [noreturn]
			}
			Assert.AreEqual(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), model.WindowsHost);

			//we've released a few 4.x versions now, check the release checker is at least somewhat functional
			Assert.AreEqual(4, model.LatestVersion.Major);
		}
	}
}
