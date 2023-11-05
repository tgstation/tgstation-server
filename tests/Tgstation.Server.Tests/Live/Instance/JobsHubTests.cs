using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Tgstation.Server.Api.Hubs;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Client;
using Tgstation.Server.Common.Extensions;

namespace Tgstation.Server.Tests.Live.Instance
{
	sealed class JobsHubTests : IJobsHub
	{
		const int ActiveConnections = 2;

		readonly IServerClient permedUser;
		readonly IServerClient permlessUser;

		readonly TaskCompletionSource finishTcs;

		readonly ConcurrentDictionary<long, JobResponse> seenJobs;

		readonly HashSet<long> permlessSeenJobs;

		HubConnection conn1, conn2;
		int expectedReboots;
		bool permlessIsPermed;

		long? permlessPsId;

		public JobsHubTests(IServerClient permedUser, IServerClient permlessUser)
		{
			this.permedUser = permedUser;
			this.permlessUser = permlessUser;

			Assert.AreNotSame(permedUser, permlessUser);

			finishTcs = new TaskCompletionSource();

			seenJobs = new ConcurrentDictionary<long, JobResponse>();
			permlessSeenJobs = new HashSet<long>();
		}

		public Task ReceiveJobUpdate(JobResponse job, CancellationToken cancellationToken)
		{
			try
			{
				Assert.IsTrue(job.InstanceId.HasValue);
				Assert.IsNotNull(job.StartedBy);
				Assert.IsTrue(job.StartedBy.Id.HasValue);
				Assert.IsTrue(job.StartedAt.HasValue);
				Assert.IsNotNull(job.Description);

				seenJobs.AddOrUpdate(job.Id.Value, job, (_, old) =>
				{
					Assert.IsFalse(old.StoppedAt.HasValue, $"Received update for job {job.Id} after it had completed!");

					return job;
				});
			}
			catch(Exception ex)
			{
				finishTcs.SetException(ex);
			}

			return Task.CompletedTask;
		}


		class ShouldNeverReceiveUpdates : IJobsHub
		{
			public Action<JobResponse> Callback { get; set; }
			public Func<ConnectionAbortReason, CancellationToken, Task> Error { get; set; }

			public Task AbortingConnection(ConnectionAbortReason reason, CancellationToken cancellationToken)
				=> Error(reason, cancellationToken);

			public Task ReceiveJobUpdate(JobResponse job, CancellationToken cancellationToken)
			{
				Callback(job);
				return Task.CompletedTask;
			}
		}

		public async Task Run(CancellationToken cancellationToken)
		{
			var neverReceiver = new ShouldNeverReceiveUpdates()
			{
				Callback = job =>
				{
					if (!permlessIsPermed)
						finishTcs.TrySetException(new Exception($"ShouldNeverReceiveUpdates received an update for job {job.Id}!"));
					else
						lock (permlessSeenJobs)
							permlessSeenJobs.Add(job.Id.Value);
				},
				Error = AbortingConnection,
			};

			await using (conn1 = (HubConnection)await permedUser.SubscribeToJobUpdates(
				this,
				null,
				null,
				cancellationToken))
			await using (conn2 = (HubConnection)await permlessUser.SubscribeToJobUpdates(
				neverReceiver,
				null,
				null,
				cancellationToken))
			{
				Console.WriteLine($"Initial conn1: {conn1.ConnectionId}");
				Console.WriteLine($"Initial conn2: {conn2.ConnectionId}");

				conn1.Reconnected += (newId) =>
				{
					Console.WriteLine($"conn1 reconnected: {newId}");
					return Task.CompletedTask;
				};
				conn2.Reconnected += (newId) =>
				{
					Console.WriteLine($"conn1 reconnected: {newId}");
					return Task.CompletedTask;
				};

				await finishTcs.Task;
			}

			var allInstances = await permedUser.Instances.List(null, cancellationToken);

			async ValueTask<List<JobResponse>> CheckInstance(InstanceResponse instance)
			{
				var wasOffline = !instance.Online.Value;
				if (wasOffline)
					await permedUser.Instances.Update(new InstanceUpdateRequest
					{
						Id = instance.Id,
						Online = true,
					}, cancellationToken);

				var jobs = await permedUser.Instances.CreateClient(instance).Jobs.List(null, cancellationToken);
				if (wasOffline)
					await permedUser.Instances.Update(new InstanceUpdateRequest
					{
						Id = instance.Id,
						Online = false,
					}, cancellationToken);

				return jobs;
			}

			var allJobsTask = allInstances
				.Select(CheckInstance);

			var allJobs = (await ValueTaskExtensions.WhenAll(allJobsTask, allInstances.Count)).SelectMany(x => x).ToList();
			var missableMissedJobs = 0;
			foreach (var job in allJobs)
			{
				var seenThisJob = seenJobs.TryGetValue(job.Id.Value, out var hubJob);
				if (seenThisJob)
				{
					Assert.AreEqual(job.StoppedAt, hubJob.StoppedAt);
					Assert.AreEqual(job.InstanceId, hubJob.InstanceId);
					Assert.AreEqual(job.ExceptionDetails, hubJob.ExceptionDetails);
					Assert.AreEqual(job.Stage, hubJob.Stage);
					Assert.AreEqual(job.CancelledBy?.Id, hubJob.CancelledBy?.Id);
					Assert.AreEqual(job.Cancelled, hubJob.Cancelled);
					Assert.AreEqual(job.StartedBy?.Id, hubJob.StartedBy?.Id);
					Assert.AreEqual(job.CancelRight, hubJob.CancelRight);
					Assert.AreEqual(job.CancelRightsType, hubJob.CancelRightsType);
					Assert.AreEqual(job.Progress, hubJob.Progress);
					Assert.AreEqual(job.Description, hubJob.Description);
					Assert.AreEqual(job.ErrorCode, hubJob.ErrorCode);
					Assert.AreEqual(job.StartedAt, hubJob.StartedAt);
				}
				else
				{
					var wasMissableJob = job.JobCode == JobCode.ReconnectChatBot
						|| job.JobCode == JobCode.StartupWatchdogLaunch
						|| job.JobCode == JobCode.StartupWatchdogReattach;
					Assert.IsTrue(wasMissableJob);
					++missableMissedJobs;
				}
			}

			// some instances may be detached, but our cache remains
			var accountedJobs = allJobs.Count - missableMissedJobs;
			var accountedSeenJobs = seenJobs.Where(x => allInstances.Any(i => i.Id.Value == x.Value.InstanceId)).Count();
			Assert.AreEqual(accountedJobs, accountedSeenJobs);
			Assert.IsTrue(accountedJobs <= seenJobs.Count);
			Assert.AreNotEqual(0, permlessSeenJobs.Count);
			Assert.IsTrue(permlessSeenJobs.Count < seenJobs.Count);
			Assert.IsTrue(permlessSeenJobs.All(id => seenJobs.ContainsKey(id)));

			await using var conn3 = (HubConnection)await permedUser.SubscribeToJobUpdates(
				this,
				null,
				null,
				cancellationToken);

			Assert.AreEqual(HubConnectionState.Connected, conn3.State);
			await permlessUser.DisposeAsync();
			await permedUser.DisposeAsync();
			Assert.AreEqual(0, expectedReboots);
		}

		public void ExpectShutdown()
		{
			Assert.AreEqual(0, Interlocked.Exchange(ref expectedReboots, ActiveConnections));
			Assert.AreEqual(HubConnectionState.Connected, conn1.State);
			Assert.AreEqual(HubConnectionState.Connected, conn2.State);
		}

		public async ValueTask WaitForReconnect(CancellationToken cancellationToken)
		{
			Assert.AreEqual(0, expectedReboots);
			await Task.WhenAll(conn1.StopAsync(cancellationToken), conn2.StopAsync(cancellationToken));

			Assert.AreEqual(HubConnectionState.Disconnected, conn1.State);
			Assert.AreEqual(HubConnectionState.Disconnected, conn2.State);

			// force token refreshs
			await Task.WhenAll(permedUser.Administration.Read(cancellationToken).AsTask(), permlessUser.Instances.List(null, cancellationToken).AsTask());

			await Task.WhenAll(conn1.StartAsync(cancellationToken), conn2.StartAsync(cancellationToken));

			Assert.AreEqual(HubConnectionState.Connected, conn1.State);
			Assert.AreEqual(HubConnectionState.Connected, conn2.State);
			Console.WriteLine($"New conn1: {conn1.ConnectionId}");
			Console.WriteLine($"New conn2: {conn2.ConnectionId}");

			if (!permlessPsId.HasValue)
			{
				var permlessUserId = long.Parse(permlessUser.Token.ParseJwt().Subject);
				permlessPsId = (await permedUser.Users.GetId(new Api.Models.EntityId
				{
					Id = permlessUserId
				}, cancellationToken)).PermissionSet.Id;
			}

			var instancesTask = permedUser.Instances.List(null, cancellationToken);

			permlessIsPermed = !permlessIsPermed;

			var instances = await instancesTask;
			await ValueTaskExtensions.WhenAll(
				instances
				.Where(instance => instance.Online.Value)
				.Select<InstanceResponse, ValueTask>(async instance =>
				{
					var ic = permedUser.Instances.CreateClient(instance);
					if (permlessIsPermed)
						await ic.PermissionSets.Create(new InstancePermissionSetRequest
						{
							PermissionSetId = permlessPsId.Value,
						}, cancellationToken);
					else
						await ic.PermissionSets.Delete(new InstancePermissionSetRequest
						{
							PermissionSetId = permlessPsId.Value
						}, cancellationToken);
				}));
		}

		public void CompleteNow() => finishTcs.TrySetResult();

		public Task AbortingConnection(ConnectionAbortReason reason, CancellationToken cancellationToken)
		{
			try
			{
				Assert.AreEqual(ConnectionAbortReason.ServerRestart, reason);
				var remaining = Interlocked.Decrement(ref expectedReboots);
				Assert.IsTrue(remaining >= 0);
			}
			catch (Exception ex)
			{
				finishTcs.TrySetException(ex);
			}
			return Task.CompletedTask;
		}
	}
}
