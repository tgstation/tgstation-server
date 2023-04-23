using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Client;

namespace Tgstation.Server.Tests.Live
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
			await TestRead(cancellationToken);
			await logsTest;
		}

		async Task TestLogs(CancellationToken cancellationToken)
		{
			var logs = await client.ListLogs(null, cancellationToken);
			Assert.AreNotEqual(0, logs.Count);
			var logFile = logs[0];
			Assert.IsNotNull(logFile);
			Assert.IsFalse(string.IsNullOrWhiteSpace(logFile.Name));
			Assert.IsNull(logFile.FileTicket);

			var downloadedTuple = await client.GetLog(logFile, cancellationToken);
			Assert.AreEqual(logFile.Name, downloadedTuple.Item1.Name);
			Assert.IsTrue(logFile.LastModified <= downloadedTuple.Item1.LastModified);
			Assert.IsNull(logFile.FileTicket);

			await ApiAssert.ThrowsException<ConflictException>(() => client.GetLog(new LogFileResponse
			{
				Name = "very_fake_path.log"
			}, cancellationToken), ErrorCode.IOError);

			await Assert.ThrowsExceptionAsync<InsufficientPermissionsException>(() => client.GetLog(new LogFileResponse
			{
				Name = "../out_of_bounds.file"
			}, cancellationToken));
		}

		async Task TestRead(CancellationToken cancellationToken)
		{
			AdministrationResponse model;
			try
			{
				model = await client.Read(cancellationToken);
			}
			catch (RateLimitException)
			{
				if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TGS_TEST_GITHUB_TOKEN")))
				{
					Assert.Inconclusive("GitHub rate limit hit while testing administration endpoint. Set environment variable TGS_TEST_GITHUB_TOKEN to fix this!");
				}

				// CI fails all the time b/c of this, ignore it
				return;
			}

			//we've released a few 4.x versions now, check the release checker is at least somewhat functional
			Assert.IsTrue(4 <= model.LatestVersion.Major);
		}
	}
}
