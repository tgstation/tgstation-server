using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Client;

namespace Tgstation.Server.Tests.Live
{
	sealed class AdministrationTest
	{
		readonly IMultiServerClient client;
		readonly IAdministrationClient restClient;

		public AdministrationTest(MultiServerClient client)
		{
			this.client = client ?? throw new ArgumentNullException(nameof(client));
			this.restClient = client.RestClient.Administration;
		}

		public async Task Run(CancellationToken cancellationToken)
		{
			var logsTest = TestLogs(cancellationToken);
			await TestRead(cancellationToken);
			await logsTest;
		}

		async Task TestLogs(CancellationToken cancellationToken)
		{
			var logs = await restClient.ListLogs(null, cancellationToken);
			Assert.AreNotEqual(0, logs.Count);
			var logFile = logs[0];
			Assert.IsNotNull(logFile);
			Assert.IsFalse(string.IsNullOrWhiteSpace(logFile.Name));
			Assert.IsNull(logFile.FileTicket);

			var downloadedTuple = await restClient.GetLog(logFile, cancellationToken);
			Assert.AreEqual(logFile.Name, downloadedTuple.Item1.Name);
			Assert.IsTrue(logFile.LastModified <= downloadedTuple.Item1.LastModified);
			Assert.IsNull(logFile.FileTicket);

			await ApiAssert.ThrowsExactly<ConflictException, Tuple<LogFileResponse, Stream>>(() => restClient.GetLog(new LogFileResponse
			{
				Name = "very_fake_path.log"
			}, cancellationToken), ErrorCode.IOError);

			await ApiAssert.ThrowsExactly<InsufficientPermissionsException, Tuple<LogFileResponse, Stream>>(() => restClient.GetLog(new LogFileResponse
			{
				Name = "../out_of_bounds.file"
			}, cancellationToken));
		}

		async Task TestRead(CancellationToken cancellationToken)
		{
			await client.Execute(
				async restServerClient =>
				{
					var restClient = restServerClient.Administration;

					var model = await restClient.Read(false, cancellationToken);

					//we've released a few 5.x versions now, check the release checker is at least somewhat functional
					Assert.IsNotNull(model.LatestVersion);
					Assert.IsTrue(4 < model.LatestVersion.Major);
					Assert.IsNotNull(model.TrackedRepositoryUrl);
					Assert.IsTrue(model.GeneratedAt.HasValue);
					Assert.IsTrue(model.GeneratedAt.Value <= DateTimeOffset.UtcNow);

					// test the cache
					var newerModel = await restClient.Read(false, cancellationToken);
					Assert.AreEqual(model.GeneratedAt, newerModel.GeneratedAt);

					await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

					var newestModel = await restClient.Read(true, cancellationToken);
					Assert.AreNotEqual(model.GeneratedAt, newestModel.GeneratedAt);
					Assert.IsNotNull(newestModel.GeneratedAt);
					Assert.IsTrue(model.GeneratedAt < newestModel.GeneratedAt);
				},
				async gqlClient =>
				{
					var queryResult = await gqlClient.RunQueryEnsureNoErrors(
						gql => gql.GetUpdateInformation.ExecuteAsync(false, cancellationToken),
						cancellationToken);

					// we've released a few 5.x versions now, check the release checker is at least somewhat functional
					Assert.IsNotNull(queryResult.Swarm.UpdateInformation.LatestVersion);
					Assert.IsTrue(4 < queryResult.Swarm.UpdateInformation.LatestVersion.Major);
					Assert.IsNotNull(queryResult.Swarm.UpdateInformation.TrackedRepositoryUrl);
					Assert.IsTrue(queryResult.Swarm.UpdateInformation.GeneratedAt.HasValue);
					Assert.IsTrue(queryResult.Swarm.UpdateInformation.GeneratedAt.Value <= DateTimeOffset.UtcNow);

					// test the cache
					var queryResult2 = await gqlClient.RunQueryEnsureNoErrors(
						gql => gql.GetUpdateInformation.ExecuteAsync(false, cancellationToken),
						cancellationToken);
					Assert.AreEqual(queryResult.Swarm.UpdateInformation.GeneratedAt, queryResult2.Swarm.UpdateInformation.GeneratedAt);

					await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
					var queryResult3 = await gqlClient.RunQueryEnsureNoErrors(
						gql => gql.GetUpdateInformation.ExecuteAsync(true, cancellationToken),
						cancellationToken);

					Assert.AreNotEqual(queryResult.Swarm.UpdateInformation.GeneratedAt, queryResult3.Swarm.UpdateInformation.GeneratedAt);
					Assert.IsNotNull(queryResult3.Swarm.UpdateInformation.GeneratedAt);
					Assert.IsTrue(queryResult.Swarm.UpdateInformation.GeneratedAt.Value < queryResult3.Swarm.UpdateInformation.GeneratedAt.Value);
				});
		}
	}
}
