using Byond.TopicSender;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Mono.Unix;
using Mono.Unix.Native;

using Moq;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Client;
using Tgstation.Server.Client.Components;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Components.Session;
using Tgstation.Server.Host.Components.Watchdog;
using Tgstation.Server.Host.Controllers;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Utils;

using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Tgstation.Server.Tests.Live.Instance
{
	sealed class WatchdogTest : JobsRequiredTest
	{
		static readonly ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
		{
			builder.AddConsole();
			builder.SetMinimumLevel(LogLevel.Trace);
		});

		public static readonly TopicClient StaticTopicClient = new(new SocketParameters
		{
			SendTimeout = TimeSpan.FromSeconds(30),
			ReceiveTimeout = TimeSpan.FromSeconds(30),
			ConnectTimeout = TimeSpan.FromSeconds(30),
			DisconnectTimeout = TimeSpan.FromSeconds(30)
		}, loggerFactory.CreateLogger($"WatchdogTest.TopicClient.Static"));

		readonly IInstanceClient instanceClient;
		readonly InstanceManager instanceManager;
		readonly ushort serverPort;
		readonly ushort ddPort;
		readonly bool highPrioDD;
		readonly TopicClient topicClient;
		readonly EngineVersion testVersion;
		readonly bool watchdogRestartsProcess;

		bool ranTimeoutTest = false;
		const string BaseAdditionalParameters = "expect_chat_channels=1&expect_static_files=1";

		public WatchdogTest(EngineVersion testVersion, IInstanceClient instanceClient, InstanceManager instanceManager, ushort serverPort, bool highPrioDD, ushort ddPort, bool watchdogRestartsProcess)
			: base(instanceClient.Jobs)
		{
			this.instanceClient = instanceClient ?? throw new ArgumentNullException(nameof(instanceClient));
			this.instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
			this.serverPort = serverPort;
			this.highPrioDD = highPrioDD;
			this.ddPort = ddPort;
			this.testVersion = testVersion ?? throw new ArgumentNullException(nameof(testVersion));
			this.watchdogRestartsProcess = watchdogRestartsProcess || testVersion.Engine.Value == EngineType.OpenDream;

			topicClient = new(new SocketParameters
			{
				SendTimeout = TimeSpan.FromSeconds(30),
				ReceiveTimeout = TimeSpan.FromSeconds(30),
				ConnectTimeout = TimeSpan.FromSeconds(30),
				DisconnectTimeout = TimeSpan.FromSeconds(30)
			}, loggerFactory.CreateLogger($"WatchdogTest.TopicClient.{instanceClient.Metadata.Name}"));
		}
		public async Task Run(CancellationToken cancellationToken)
		{
			try
			{
				await RunInt(cancellationToken);
			}
			catch
			{
				System.Console.WriteLine($"WATCHDOG TEST FAILING INSTANCE ID {instanceClient.Metadata.Id.Value}");
				throw;
			}
		}

		async Task RunInt(CancellationToken cancellationToken)
		{
			System.Console.WriteLine($"TEST: START WATCHDOG TESTS {instanceClient.Metadata.Name}");

			async Task CheckByondVersions()
			{
				var listTask = instanceClient.Engine.InstalledVersions(null, cancellationToken);

				var list = await listTask;

				Assert.AreEqual(1, list.Count);
				var byondVersion = list[0];

				Assert.AreEqual(1, byondVersion.EngineVersion.CustomIteration);
				Assert.AreEqual(testVersion.Engine, byondVersion.EngineVersion.Engine);
				if (testVersion.Version != null)
				{
					Assert.AreEqual(testVersion.Version.Major, byondVersion.EngineVersion.Version.Major);
					Assert.AreEqual(testVersion.Version.Minor, byondVersion.EngineVersion.Version.Minor);
				}
				else
				{
					Assert.IsNull(byondVersion.EngineVersion.Version);
					Assert.AreEqual(testVersion.SourceSHA, byondVersion.EngineVersion.SourceSHA);
				}
			}

			async Task UpdateDDSettings()
			{
				for (var i = 0; i < 5; ++i)
					try
					{
						// Increase startup timeout, disable heartbeats, enable map threads because we've tested without for years
						global::System.Console.WriteLine($"PORT REUSE BUG 4: Setting I-{instanceClient.Metadata.Id} DD to {ddPort}");
						var updated = await instanceClient.DreamDaemon.Update(new DreamDaemonRequest
						{
							StartupTimeout = 60,
							HealthCheckSeconds = 0,
							Port = ddPort,
							MapThreads = 2,
							OpenDreamTopicPort = 47,
							LogOutput = false,
							AdditionalParameters = BaseAdditionalParameters
						}, cancellationToken);

						Assert.AreEqual<ushort?>(47, updated.OpenDreamTopicPort);
						Assert.IsFalse(updated.ImmediateMemoryUsage.HasValue);
					}
					catch (ConflictException ex) when (ex.ErrorCode == ErrorCode.PortNotAvailable)
					{
						if (i == 4)
							throw;

						// I have no idea why this happens sometimes
						await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
					}

				var updated2 = await instanceClient.DreamDaemon.Update(new DreamDaemonRequest
				{
					OpenDreamTopicPort = 0,
				}, cancellationToken);

				Assert.AreEqual<ushort?>(0, updated2.OpenDreamTopicPort);
			}

			global::System.Console.WriteLine($"PORT REUSE BUG 4: Expect error. Setting I-{instanceClient.Metadata.Id} DD to 0");
			await Task.WhenAll(
				UpdateDDSettings(),
				CheckByondVersions(),
				ApiAssert.ThrowsException<ApiConflictException, DreamDaemonResponse>(() => instanceClient.DreamDaemon.Update(new DreamDaemonRequest
				{
					SoftShutdown = true,
					SoftRestart = true
				}, cancellationToken), ErrorCode.GameServerDoubleSoft).AsTask(),
				ApiAssert.ThrowsException<ApiConflictException, DreamDaemonResponse>(() => instanceClient.DreamDaemon.Update(new DreamDaemonRequest
				{
					Port = 0
				}, cancellationToken), ErrorCode.ModelValidationFailure).AsTask(),
				ApiAssert.ThrowsException<ConflictException, JobResponse>(() => instanceClient.DreamDaemon.CreateDump(cancellationToken), ErrorCode.WatchdogNotRunning).AsTask(),
				ApiAssert.ThrowsException<ConflictException, JobResponse>(() => instanceClient.DreamDaemon.Restart(cancellationToken), ErrorCode.WatchdogNotRunning).AsTask());

			await RunBasicTest(false, cancellationToken);

			// hardlinks and DMAPI checks don't play well together
			bool linuxAdvancedWatchdogWeirdness = testVersion.Engine.Value == EngineType.Byond
				&& !new PlatformIdentifier().IsWindows
				&& !watchdogRestartsProcess;
			if (!linuxAdvancedWatchdogWeirdness)
				await RunBasicTest(true, cancellationToken);

			await TestDMApiFreeDeploy(cancellationToken);

			// long running test likes consistency with the channels
			DummyChatProvider.RandomDisconnections(false);

			await RunLongRunningTestThenUpdate(cancellationToken);

			await RunLongRunningTestThenUpdateWithNewDme(cancellationToken);
			await RunLongRunningTestThenUpdateWithByondVersionSwitch(cancellationToken);

			// no chatty bullshit while we test health checks
			var tcs = new TaskCompletionSource();
			var oldTask = Interlocked.Exchange(ref DummyChatProvider.MessageGuard, tcs.Task);

			await RunHealthCheckTest(true, cancellationToken);
			await RunHealthCheckTest(false, cancellationToken);

			async void Cleanup()
			{
				await oldTask;
				tcs.SetResult();
			}

			Cleanup();

			await InteropTestsForLongRunningDme(cancellationToken);

			await instanceClient.DreamDaemon.Update(new DreamDaemonRequest
			{
				AdditionalParameters = String.Empty
			}, cancellationToken);

			// for the restart staging tests
			await DeployTestDme("LongRunning/long_running_test", DreamDaemonSecurity.Trusted, DMApiValidationMode.Required, cancellationToken);

			System.Console.WriteLine($"TEST: END WATCHDOG TESTS {instanceClient.Metadata.Name}");
		}

		async ValueTask RegressionTest1686(CancellationToken cancellationToken)
		{
			async ValueTask RunTest(bool useTrusted)
			{
				System.Console.WriteLine($"TEST: RegressionTest1686 {useTrusted}...");
				var ddUpdateTask = instanceClient.DreamDaemon.Update(new DreamDaemonRequest
				{
					SecurityLevel = useTrusted ? DreamDaemonSecurity.Trusted : DreamDaemonSecurity.Safe,
				}, cancellationToken);
				var currentStatus = await DeployTestDme("long_running_test_rooted", DreamDaemonSecurity.Trusted, DMApiValidationMode.Required, cancellationToken);
				await ddUpdateTask;

				Assert.AreEqual(WatchdogStatus.Offline, currentStatus.Status);

				var startJob = await StartDD(cancellationToken);

				await WaitForJob(startJob, 40, false, null, cancellationToken);

				currentStatus = await instanceClient.DreamDaemon.Update(new DreamDaemonRequest
				{
					SoftShutdown = true,
				}, cancellationToken);
				ValidateSessionId(currentStatus, true);

				Assert.AreEqual(WatchdogStatus.Online, currentStatus.Status);

				// reimplement TellWorldToReboot because it expects a new deployment and we don't care
				System.Console.WriteLine("TEST: Hack world reboot topic...");
				var result = await SendTestTopic(
					"tgs_integration_test_special_tactics=1",
					cancellationToken);
				Assert.AreEqual("ack", result.StringData);

				using var tempCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
				var tempToken = tempCts.Token;
				using (tempToken.Register(() => System.Console.WriteLine("TEST ERROR: Timeout in RegressionTest1686!")))
				{
					tempCts.CancelAfter(TimeSpan.FromMinutes(2));

					do
					{
						await Task.Delay(TimeSpan.FromSeconds(1), tempToken);
						currentStatus = await instanceClient.DreamDaemon.Read(tempToken);
					}
					while (currentStatus.Status != WatchdogStatus.Offline);
				}

				await CheckDMApiFail(currentStatus.ActiveCompileJob, cancellationToken);
			}

			await RunTest(true);

			if (new PlatformIdentifier().IsWindows || !watchdogRestartsProcess)
				await RunTest(false);
		}

		ValueTask<TopicResponse> SendTestTopic(string queryString, CancellationToken cancellationToken)
			=> SendTestTopic(queryString, topicClient, instanceManager.GetInstanceReference(instanceClient.Metadata), FindTopicPort(), cancellationToken);

		public static async ValueTask<TopicResponse> SendTestTopic(string queryString, ITopicClient topicClient, IInstanceReference instanceReference, ushort topicPort, CancellationToken cancellationToken)
		{
			using (instanceReference)
			{
				using var loggerFactory = LoggerFactory.Create(builder =>
				{
					builder.AddConsole();
					builder.SetMinimumLevel(LogLevel.Trace);
				});

				var watchdog = instanceReference?.Watchdog;
				var session = (SessionController)watchdog?.GetType().GetMethod("GetActiveController", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(watchdog, null);

				using (session != null
					? await session.TopicSendSemaphore.Lock(cancellationToken)
					: null)
					return await topicClient.SendWithOptionalPriority(
						new AsyncDelayer(loggerFactory.CreateLogger<AsyncDelayer>()),
						loggerFactory.CreateLogger<WatchdogTest>(),
						queryString,
						topicPort,
						true,
						cancellationToken);
			}
		}

		async ValueTask BroadcastTest(CancellationToken cancellationToken)
		{
			var topicRequestResult = await SendTestTopic("tgs_integration_test_tactics_broadcast=1", cancellationToken);

			Assert.IsNotNull(topicRequestResult);
			Assert.AreEqual("!!NULL!!", topicRequestResult.StringData);

			const string TestBroadcastMessage = "TGS: THIS IS A TEST OF THE EMERGENCY BROADCAST SYSTEM!";
			await instanceClient.DreamDaemon.Update(new DreamDaemonRequest
			{
				BroadcastMessage = TestBroadcastMessage,
			}, cancellationToken);

			topicRequestResult = await SendTestTopic(
				"tgs_integration_test_tactics_broadcast=1",
				cancellationToken);

			Assert.IsNotNull(topicRequestResult);
			Assert.AreEqual(TestBroadcastMessage, topicRequestResult.StringData);
		}

		async Task InteropTestsForLongRunningDme(CancellationToken cancellationToken)
		{
			await RegressionTest1686(cancellationToken);

			await ApiAssert.ThrowsException<ConflictException, DreamDaemonResponse>(() => instanceClient.DreamDaemon.Update(new DreamDaemonRequest
			{
				BroadcastMessage = "ksjfdksjf",
			}, cancellationToken), ErrorCode.BroadcastFailure);

			await StartAndLeaveRunning(cancellationToken);

			await BroadcastTest(cancellationToken);

			await RegressionTest1550(cancellationToken);

			await TestLegacyBridgeEndpoint(cancellationToken);

			var deleteJobTask = TestDeleteByondInstallErrorCasesAndQueing(cancellationToken);

			SessionController.LogTopicRequests = false;
			await WhiteBoxChatCommandTest(cancellationToken);
			await SendChatOverloadCommand(cancellationToken);
			await ValidateTopicLimits(cancellationToken);
			SessionController.LogTopicRequests = true;

			// This one fucks with the access_identifer, run it in isolation
			await WhiteBoxValidateBridgeRequestLimitAndTestChunking(cancellationToken);

			var ddInfo = await instanceClient.DreamDaemon.Read(cancellationToken);
			await CheckDMApiFail(ddInfo.ActiveCompileJob, cancellationToken);

			var deleteJob = await deleteJobTask;

			// And this freezes DD (also restarts it)
			await DumpTests(false, cancellationToken);
			await DumpTests(true, cancellationToken);

			await WaitForJob(deleteJob, 15, false, null, cancellationToken);
		}

		async ValueTask RegressionTest1550(CancellationToken cancellationToken)
		{
			// Previous test, StartAndLeaveRunning, has SoftRestart set. We don't want that.
			var restartJob = await instanceClient.DreamDaemon.Restart(cancellationToken);
			await WaitForJob(restartJob, 10, false, null, cancellationToken);

			// we need to cycle deployments twice because TGS holds the initial deployment
			var currentStatus = await DeployTestDme("LongRunning/long_running_test", DreamDaemonSecurity.Trusted, DMApiValidationMode.Required, cancellationToken);

			Assert.AreEqual(WatchdogStatus.Online, currentStatus.Status);
			Assert.IsNotNull(currentStatus.StagedCompileJob);
			ValidateSessionId(currentStatus, true);
			var expectedStaged = currentStatus.StagedCompileJob;
			Assert.AreNotEqual(expectedStaged.Id, currentStatus.ActiveCompileJob.Id);

			Assert.IsFalse(currentStatus.SoftShutdown);

			currentStatus = await TellWorldToReboot(true, cancellationToken);
			ValidateSessionId(currentStatus, true);
			Assert.AreEqual(expectedStaged.Id, currentStatus.ActiveCompileJob.Id);

			var topicRequestResult = await SendTestTopic(
				"shadow_wizard_money_gang=1",
				cancellationToken);

			Assert.IsNotNull(topicRequestResult);
			Assert.AreEqual("we love casting spells", topicRequestResult.StringData);

			currentStatus = await DeployTestDme("LongRunning/long_running_test", DreamDaemonSecurity.Trusted, DMApiValidationMode.Required, cancellationToken);
			Assert.AreEqual(watchdogRestartsProcess, currentStatus.SoftRestart);
			ValidateSessionId(currentStatus, false);

			Assert.AreEqual(WatchdogStatus.Online, currentStatus.Status);
			Assert.IsNotNull(currentStatus.StagedCompileJob);
			Assert.AreEqual(expectedStaged.Id, currentStatus.ActiveCompileJob.Id);
			expectedStaged = currentStatus.StagedCompileJob;
			Assert.AreNotEqual(expectedStaged.Id, currentStatus.ActiveCompileJob.Id);

			currentStatus = await TellWorldToReboot(true, cancellationToken);

			ValidateSessionId(currentStatus, watchdogRestartsProcess);
			Assert.AreEqual(WatchdogStatus.Online, currentStatus.Status);
			Assert.IsNull(currentStatus.StagedCompileJob);
			Assert.AreEqual(expectedStaged.Id, currentStatus.ActiveCompileJob.Id);

			await CheckDMApiFail(currentStatus.ActiveCompileJob, cancellationToken, false);
			await CheckDMApiFail(expectedStaged, cancellationToken, false);
		}

		async Task<JobResponse> TestDeleteByondInstallErrorCasesAndQueing(CancellationToken cancellationToken)
		{
			var currentByond = await instanceClient.Engine.ActiveVersion(cancellationToken);
			Assert.IsNotNull(currentByond);
			Assert.AreEqual(testVersion, currentByond.EngineVersion);

			// Change the active version and check we get delayed while deleting the old one because the watchdog is using it
			var setActiveResponse = await instanceClient.Engine.SetActiveVersion(
				new EngineVersionRequest
				{
					EngineVersion = new EngineVersion
					{
						Version = testVersion.Version,
						SourceSHA = testVersion.SourceSHA,
						Engine = testVersion.Engine,
						CustomIteration = 1,
					}
				},
				null,
				cancellationToken);

			Assert.IsNotNull(setActiveResponse);
			Assert.IsNull(setActiveResponse.InstallJob);

			var deleteJob = await instanceClient.Engine.DeleteVersion(
				new EngineVersionDeleteRequest
				{
					EngineVersion = new EngineVersion
					{
						Version = testVersion.Version,
						SourceSHA = testVersion.SourceSHA,
						Engine = testVersion.Engine,
					}
				},
				cancellationToken);

			Assert.IsNotNull(deleteJob);

			deleteJob = await WaitForJobProgress(deleteJob, 15, cancellationToken);
			Assert.IsNotNull(deleteJob);
			Assert.IsNotNull(deleteJob.Stage);
			Assert.IsTrue(deleteJob.Stage.Contains("Waiting"));

			// then change it back and check it fails the job because it's active again
			setActiveResponse = await instanceClient.Engine.SetActiveVersion(
				new EngineVersionRequest
				{
					EngineVersion = new EngineVersion
					{
						Version = testVersion.Version,
						Engine = testVersion.Engine,
						SourceSHA = testVersion.SourceSHA
					}
				},
				null,
				cancellationToken);

			Assert.IsNotNull(setActiveResponse);
			Assert.IsNull(setActiveResponse.InstallJob);

			await WaitForJob(deleteJob, 5, true, ErrorCode.EngineCannotDeleteActiveVersion, cancellationToken);

			// finally, queue the last delete job which should complete when the watchdog restarts with a newly deployed .dmb
			// queue the byond change followed by the deployment for that first
			setActiveResponse = await instanceClient.Engine.SetActiveVersion(
				new EngineVersionRequest
				{
					EngineVersion = new EngineVersion
					{
						Version = testVersion.Version,
						Engine = testVersion.Engine,
						SourceSHA = testVersion.SourceSHA,
						CustomIteration = 1,
					}
				},
				null,
				cancellationToken);

			Assert.IsNotNull(setActiveResponse);
			Assert.IsNull(setActiveResponse.InstallJob);

			deleteJob = await instanceClient.Engine.DeleteVersion(
				new EngineVersionDeleteRequest
				{
					EngineVersion = new EngineVersion
					{
						Version = testVersion.Version,
						Engine = testVersion.Engine,
						SourceSHA = testVersion.SourceSHA,
					}
				},
				cancellationToken);

			Assert.IsNotNull(deleteJob);

			await DeployTestDme("LongRunning/long_running_test", DreamDaemonSecurity.Safe, DMApiValidationMode.Required, cancellationToken);
			return deleteJob;
		}

		async Task SendChatOverloadCommand(CancellationToken cancellationToken)
		{
			// for the code coverage really...
			var topicRequestResult = await SendTestTopic(
				"tgs_integration_test_tactics5=1",
				cancellationToken);

			Assert.IsNotNull(topicRequestResult);
			Assert.AreEqual("sent", topicRequestResult.StringData);
		}

		async Task DumpTests(bool mini, CancellationToken cancellationToken)
		{
			System.Console.WriteLine("TEST: WATCHDOG DUMP TESTS");
			var updated = await instanceClient.DreamDaemon.Update(new DreamDaemonRequest
			{
				Minidumps = mini,
			}, cancellationToken);
			Assert.AreEqual(mini, updated.Minidumps);
			var dumpJob = await instanceClient.DreamDaemon.CreateDump(cancellationToken);
			await WaitForJob(dumpJob, 60, false, null, cancellationToken);

			var dumpFiles = Directory.GetFiles(Path.Combine(
				instanceClient.Metadata.Path, "Diagnostics", "ProcessDumps"), testVersion.Engine == EngineType.OpenDream ? "*.net.dmp" : "*.dmp");
			Assert.AreEqual(1, dumpFiles.Length);
			File.Delete(dumpFiles.Single());

			// fuck this test, it's flakey as a motherfucker
			if (Environment.NewLine == null)
			{
				// failed dump
				JobResponse job;
				while (true)
				{
					KillDD(true);
					var jobTcs = new TaskCompletionSource();
					var killTaskStarted = new TaskCompletionSource();
					var killThread = new Thread(() =>
					{
						killTaskStarted.SetResult();
						while (!jobTcs.Task.IsCompleted)
							KillDD(false);
					})
					{
						Priority = ThreadPriority.AboveNormal
					};

					killThread.Start();
					try
					{
						await killTaskStarted.Task;
						var dumpTask = instanceClient.DreamDaemon.CreateDump(cancellationToken);
						job = await WaitForJob(await dumpTask, 20, true, null, cancellationToken);
					}
					finally
					{
						jobTcs.SetResult();
						killThread.Join();
					}

					// these can also happen

					if (!(new PlatformIdentifier().IsWindows
						&& (job.ExceptionDetails.Contains("Access is denied.")
						|| job.ExceptionDetails.Contains("The handle is invalid.")
						|| job.ExceptionDetails.Contains("Unknown error")
						|| job.ExceptionDetails.Contains("No process is associated with this object.")
						|| job.ExceptionDetails.Contains("The program issued a command but the command length is incorrect.")
						|| job.ExceptionDetails.Contains("Only part of a ReadProcessMemory or WriteProcessMemory request was completed.")
						|| job.ExceptionDetails.Contains("Unknown error"))))
						break;

					var restartJob = await instanceClient.DreamDaemon.Restart(cancellationToken);
					await WaitForJob(restartJob, 20, false, null, cancellationToken);
				}

				Assert.IsTrue(job.ErrorCode == ErrorCode.GameServerOffline || job.ErrorCode == ErrorCode.GCoreFailure, $"{job.ErrorCode}: {job.ExceptionDetails}");
			}

			var restartJob2 = await instanceClient.DreamDaemon.Restart(cancellationToken);
			await WaitForJob(restartJob2, 20, false, null, cancellationToken);
		}

		async Task TestDMApiFreeDeploy(CancellationToken cancellationToken)
		{
			System.Console.WriteLine("TEST: WATCHDOG API FREE TEST");
			var daemonStatus = await DeployTestDme("ApiFree/api_free", DreamDaemonSecurity.Safe, DMApiValidationMode.Optional, cancellationToken);

			Assert.AreEqual(WatchdogStatus.Offline, daemonStatus.Status.Value);
			Assert.IsNotNull(daemonStatus.ActiveCompileJob);
			Assert.IsNull(daemonStatus.StagedCompileJob);
			Assert.IsNull(daemonStatus.ActiveCompileJob.DMApiVersion);
			Assert.AreEqual(DreamDaemonSecurity.Ultrasafe, daemonStatus.ActiveCompileJob.MinimumSecurityLevel);

			await ExpectGameDirectoryCount(1, cancellationToken);

			var startJob = await StartDD(cancellationToken);

			await WaitForJob(startJob, 40, false, null, cancellationToken);

			daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);

			Assert.AreEqual(WatchdogStatus.Online, daemonStatus.Status.Value);
			ValidateSessionId(daemonStatus, true);
			await CheckDDPriority();
			Assert.AreEqual(false, daemonStatus.SoftRestart);
			Assert.AreEqual(false, daemonStatus.SoftShutdown);
			Assert.AreEqual(string.Empty, daemonStatus.AdditionalParameters);
			Assert.AreEqual(false, daemonStatus.SoftRestart);
			var initialCompileJob = daemonStatus.ActiveCompileJob;
			await CheckDMApiFail(daemonStatus.ActiveCompileJob, cancellationToken);

			daemonStatus = await DeployTestDme("BasicOperation/basic operation_test", DreamDaemonSecurity.Trusted, DMApiValidationMode.Required, cancellationToken);

			Assert.AreEqual(WatchdogStatus.Online, daemonStatus.Status.Value);
			Assert.AreEqual(false, daemonStatus.SoftRestart); // dme name change triggered, instant reboot
			Assert.AreEqual(initialCompileJob.Id, daemonStatus.ActiveCompileJob.Id);
			var newerCompileJob = daemonStatus.StagedCompileJob;

			await ExpectGameDirectoryCount(2, cancellationToken);

			Assert.IsNotNull(newerCompileJob);
			Assert.AreNotEqual(initialCompileJob.Id, newerCompileJob.Id);
			Assert.AreEqual(DreamDaemonSecurity.Trusted, newerCompileJob.MinimumSecurityLevel);
			Assert.AreEqual(DMApiConstants.InteropVersion, daemonStatus.StagedCompileJob.DMApiVersion);
			await instanceClient.DreamDaemon.Shutdown(cancellationToken);

			await ExpectGameDirectoryCount(1, cancellationToken);
		}

		public async ValueTask ExpectGameDirectoryCount(int expected, CancellationToken cancellationToken)
		{
			string[] lastDirectories;
			int CountNonLiveDirs()
			{
				lastDirectories = Directory.GetDirectories(Path.Combine(instanceClient.Metadata.Path, "Game"));
				return lastDirectories.Where(directory => Path.GetFileName(directory) != "Live").Count();
			}

			int nonLiveDirs = 0;
			// cleanup task is async
			for(var i = 0; i < 20; ++i)
			{
				nonLiveDirs = CountNonLiveDirs();
				if (expected == nonLiveDirs)
					return;

				await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
			}

			nonLiveDirs = CountNonLiveDirs();
			Assert.AreEqual(expected, nonLiveDirs, $"Directories present: {String.Join(", ", lastDirectories.Select(Path.GetFileName))}");
		}

		async Task RunBasicTest(bool skipApiValidation, CancellationToken cancellationToken)
		{
			System.Console.WriteLine("TEST: WATCHDOG BASIC TEST");

			var daemonStatus = await instanceClient.DreamDaemon.Update(new DreamDaemonRequest
			{
				AdditionalParameters = "test=bababooey"
			}, cancellationToken);
			Assert.AreEqual("test=bababooey", daemonStatus.AdditionalParameters);
			daemonStatus = await DeployTestDme($"BasicOperation/basic operation_test{(skipApiValidation ? "_nov3" : String.Empty)}", DreamDaemonSecurity.Trusted, skipApiValidation ? DMApiValidationMode.Skipped : DMApiValidationMode.Required, cancellationToken);

			Assert.AreEqual(WatchdogStatus.Offline, daemonStatus.Status.Value);
			Assert.IsNotNull(daemonStatus.ActiveCompileJob);

			if (skipApiValidation)
			{
				Assert.IsNull(daemonStatus.ActiveCompileJob.DMApiVersion);
				Assert.AreEqual(DreamDaemonSecurity.Ultrasafe, daemonStatus.ActiveCompileJob.MinimumSecurityLevel);
			}
			else
			{
				Assert.AreEqual(DMApiConstants.InteropVersion, daemonStatus.ActiveCompileJob.DMApiVersion);
				Assert.AreEqual(DreamDaemonSecurity.Trusted, daemonStatus.ActiveCompileJob.MinimumSecurityLevel);
			}

			Assert.IsFalse(daemonStatus.SessionId.HasValue);
			Assert.IsFalse(daemonStatus.LaunchTime.HasValue);
			Assert.IsNull(daemonStatus.StagedCompileJob);
			Assert.AreNotEqual(DreamDaemonSecurity.Ultrasafe, daemonStatus.SecurityLevel);

			await ExpectGameDirectoryCount(1, cancellationToken);

			JobResponse startJob;
			if (new PlatformIdentifier().IsWindows) // Can't get address reuse to trigger on linux for some reason
				using (var blockSocket = new Socket(
					testVersion.Engine.Value == EngineType.OpenDream
						? SocketType.Dgram
						: SocketType.Stream,
					testVersion.Engine.Value == EngineType.OpenDream
						? ProtocolType.Udp
						: ProtocolType.Tcp))
				{
					blockSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, true);
					blockSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);
					if (testVersion.Engine.Value != EngineType.OpenDream)
						blockSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);

					blockSocket.Bind(new IPEndPoint(IPAddress.Any, ddPort));

					// Don't use StartDD here
					startJob = await instanceClient.DreamDaemon.Start(cancellationToken);

					await WaitForJob(startJob, 40, true, ErrorCode.GameServerPortInUse, cancellationToken);
				}

			startJob = await StartDD(cancellationToken);

			await WaitForJob(startJob, 40, false, null, cancellationToken);

			daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);

			Assert.AreEqual(WatchdogStatus.Online, daemonStatus.Status.Value);
			ValidateSessionId(daemonStatus, true);
			await CheckDDPriority();
			Assert.AreEqual(false, daemonStatus.SoftRestart);
			Assert.AreEqual(false, daemonStatus.SoftShutdown);
			Assert.IsTrue(daemonStatus.ImmediateMemoryUsage.HasValue);
			Assert.AreNotEqual(0, daemonStatus.ImmediateMemoryUsage.Value);

			if (skipApiValidation)
				Assert.IsFalse(daemonStatus.ClientCount.HasValue);

			await GracefulWatchdogShutdown(cancellationToken);

			daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.AreEqual(WatchdogStatus.Offline, daemonStatus.Status.Value);
			Assert.IsFalse(daemonStatus.SessionId.HasValue);
			Assert.IsFalse(daemonStatus.LaunchTime.HasValue);
			await ExpectGameDirectoryCount(1, cancellationToken);

			await CheckDMApiFail(daemonStatus.ActiveCompileJob, cancellationToken, false, skipApiValidation);

			daemonStatus = await instanceClient.DreamDaemon.Update(new DreamDaemonRequest
			{
				AdditionalParameters = string.Empty,
				LogOutput = true,
			}, cancellationToken);
			Assert.AreEqual(string.Empty, daemonStatus.AdditionalParameters);
			Assert.IsFalse(daemonStatus.SessionId.HasValue);
			Assert.IsFalse(daemonStatus.LaunchTime.HasValue);
		}

		long? sessionIdTracker;
		void ValidateSessionId(DreamDaemonResponse daemonStatus, bool? knownIncrease)
		{
			Assert.IsTrue(daemonStatus.SessionId.HasValue, $"Expected a session ID in the DreamDaemonResponse");
			Assert.IsTrue(daemonStatus.LaunchTime.HasValue);
			Assert.IsTrue(daemonStatus.LaunchTime.Value >= DateTimeOffset.UtcNow.AddHours(-1));

			if (daemonStatus.ClientCount.HasValue)
				Assert.AreEqual(0U, daemonStatus.ClientCount.Value);

			if (sessionIdTracker.HasValue)
				if (knownIncrease.HasValue)
					if (knownIncrease.Value)
						Assert.IsTrue(daemonStatus.SessionId.Value > sessionIdTracker.Value, $"Expected a session ID > {sessionIdTracker.Value}, got {daemonStatus.SessionId.Value} instead");
					else
						Assert.AreEqual(sessionIdTracker.Value, daemonStatus.SessionId.Value);
				else
					Assert.IsTrue(daemonStatus.SessionId.Value >= sessionIdTracker.Value, $"Expected a session ID >= {sessionIdTracker.Value}, got {daemonStatus.SessionId.Value} instead");

			sessionIdTracker = daemonStatus.SessionId.Value;
		}

		void TestLinuxIsntBeingFuckingCheekyAboutFilePaths(DreamDaemonResponse currentStatus, CompileJobResponse previousStatus)
		{
			if (new PlatformIdentifier().IsWindows || watchdogRestartsProcess)
				return;

			Assert.IsNotNull(currentStatus.ActiveCompileJob);
			Assert.IsTrue(currentStatus.ActiveCompileJob.DmeName.Contains("long_running_test"));
			Assert.AreEqual(WatchdogStatus.Online, currentStatus.Status);

			var procs = TestLiveServer.GetEngineServerProcessesOnPort(testVersion.Engine.Value, currentStatus.Port.Value);
			Assert.AreEqual(1, procs.Count);
			var failingLinks = new List<string>();
			using var proc = procs[0];
			var pid = proc.Id;
			var foundLivePath = false;
			var allPaths = new List<string>();

			var features = new PosixProcessFeatures(
				new Lazy<IProcessExecutor>(Mock.Of<IProcessExecutor>()),
				new DefaultIOManager(new FileSystem()),
				Mock.Of<ILogger<PosixProcessFeatures>>());

			features.SuspendProcess(proc);
			try
			{
				Assert.IsFalse(proc.HasExited);
				foreach (var fd in Directory.GetFiles($"/proc/{pid}/fd"))
				{
					var sb = new StringBuilder(UInt16.MaxValue);
					if (Syscall.readlink(fd, sb) == -1)
						throw new UnixIOException(Stdlib.GetLastError());

					var path = sb.ToString();

					allPaths.Add($"Path: {path}");
					if (path.Contains($"Game/{previousStatus.DirectoryName}"))
						failingLinks.Add($"Found fd {fd} resolving to previous absolute path game dir path: {path}");

					if (path.Contains($"Game/{currentStatus.ActiveCompileJob.DirectoryName}"))
						failingLinks.Add($"Found fd {fd} resolving to current absolute path game dir path: {path}");

					if (path.Contains($"Game/Live"))
						foundLivePath = true;
				}

				if (!foundLivePath)
					failingLinks.Add($"Failed to find a path containing the 'Live' directory!");
			}
			finally
			{
				features.ResumeProcess(proc);
			}

			Assert.IsTrue(failingLinks.Count == 0, String.Join(Environment.NewLine, failingLinks.Concat(allPaths)));
		}

		async Task RunHealthCheckTest(bool checkDump, CancellationToken cancellationToken)
		{
			System.Console.WriteLine("TEST: WATCHDOG HEALTH CHECK TEST");

			// enable health checks
			var status = await instanceClient.DreamDaemon.Update(new DreamDaemonRequest
			{
				HealthCheckSeconds = 1,
				DumpOnHealthCheckRestart = checkDump,
			}, cancellationToken);

			Assert.AreEqual(checkDump, status.DumpOnHealthCheckRestart);
			Assert.AreEqual(1U, status.HealthCheckSeconds.Value);

			var startJob = await StartDD(cancellationToken);

			await WaitForJob(startJob, 40, false, null, cancellationToken);

			await CheckDDPriority();

			// lock on to DD and pause it so it can't health check
			var ddProcs = TestLiveServer.GetEngineServerProcessesOnPort(testVersion.Engine.Value, ddPort).Where(x => !x.HasExited).ToList();
			if (ddProcs.Count != 1)
				Assert.Fail($"Incorrect number of DD processes: {ddProcs.Count}");

			using var ddProc = ddProcs.Single();
			ProcessExecutor executor = null;
			executor = new ProcessExecutor(
				RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
					? new WindowsProcessFeatures(Mock.Of<ILogger<WindowsProcessFeatures>>())
					: new PosixProcessFeatures(new Lazy<IProcessExecutor>(() => executor), new DefaultIOManager(new FileSystem()), Mock.Of<ILogger<PosixProcessFeatures>>()),
				Mock.Of<IIOManager>(),
				Mock.Of<ILogger<ProcessExecutor>>(),
				LoggerFactory.Create(x => { }));
			await using var ourProcessHandler = executor
				.GetProcess(ddProc.Id);

			// Ensure it's responding to health checks
			await Task.WhenAny(Task.Delay(7000, cancellationToken), ourProcessHandler.Lifetime);
			Assert.IsFalse(ddProc.HasExited);
			var daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.AreEqual(0U, daemonStatus.ClientCount);

			// check DD agrees
			var topicRequestResult = await SendTestTopic(
				"tgs_integration_test_tactics8=1",
				cancellationToken);

			Assert.IsNotNull(topicRequestResult);
			Assert.AreEqual(TopicResponseType.StringResponse, topicRequestResult.ResponseType);
			Assert.IsNotNull(topicRequestResult.StringData);
			Assert.AreEqual("received health check", topicRequestResult.StringData);

			var ddStatus = await instanceClient.DreamDaemon.Update(new DreamDaemonRequest
			{
				SoftShutdown = true
			}, cancellationToken);
			ValidateSessionId(ddStatus, true);

			global::System.Console.WriteLine($"WATCHDOG TEST {instanceClient.Metadata.Id}: COMMENCE PROCESS SUSPEND FOR HEALTH CHECK DEATH PID {ourProcessHandler.Id}.");
			ourProcessHandler.SuspendProcess();
			global::System.Console.WriteLine($"WATCHDOG TEST {instanceClient.Metadata.Id}: FINISH PROCESS SUSPEND FOR HEALTH CHECK DEATH. WAITING FOR LIFETIME {ourProcessHandler.Id}.");

			if (testVersion.Engine == EngineType.OpenDream && checkDump)
			{
				// because dotnet diagnostics relies on the engine process to write its own dump, we actually have to unpause it after the watchdog has decided to kill it
				// incredibly cursed, because we don't have the means to accurately tell when that will happen. ESP in CI
				return; // CBA rn
				/*
				await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
				ourProcessHandler.ResumeProcess();
				global::System.Console.WriteLine($"WATCHDOG TEST {instanceClient.Metadata.Id}: PROCESS RESUMING FOR DOTNET DUMP. WAITING FOR LIFETIME {ourProcessHandler.Id}.");*/
			}

			await Task.WhenAny(ourProcessHandler.Lifetime, Task.Delay(TimeSpan.FromMinutes(4), cancellationToken));
			if (testVersion.Engine == EngineType.OpenDream && checkDump && !ourProcessHandler.Lifetime.IsCompleted)
				return;

			Assert.IsTrue(ourProcessHandler.Lifetime.IsCompleted);

			var timeout = 20;
			do
			{
				ddStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
				Assert.AreEqual(1U, ddStatus.HealthCheckSeconds.Value);
				if (ddStatus.Status.Value == WatchdogStatus.Offline)
				{
					await CheckDMApiFail(ddStatus.ActiveCompileJob, cancellationToken);
					break;
				}

				if (--timeout == 0)
					Assert.Fail("DreamDaemon didn't shutdown within the timeout!");

				await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
			}
			while (timeout > 0);

			// disable health checks
			ddStatus = await instanceClient.DreamDaemon.Update(new DreamDaemonRequest
			{
				HealthCheckSeconds = 0,
			}, cancellationToken);
			Assert.AreEqual(0U, ddStatus.HealthCheckSeconds.Value);

			if (checkDump)
			{
				// check the dump happened
				var dumpFiles = Directory.GetFiles(Path.Combine(
					instanceClient.Metadata.Path, "Diagnostics", "ProcessDumps"), "*.dmp");
				Assert.AreEqual(1, dumpFiles.Length);
				File.Delete(dumpFiles.Single());
			}
		}

		async Task<JobResponse> StartDD(CancellationToken cancellationToken)
		{
			// integration tests may take a while to release the port
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			cts.CancelAfter(TimeSpan.FromMinutes(1));
			while (true)
			{
				try
				{
					SocketExtensions.BindTest(new PlatformIdentifier(), ddPort, false, testVersion.Engine == EngineType.OpenDream);
					break;
				}
				catch
				{
					try
					{
						await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
						continue;
					}
					catch (OperationCanceledException)
					{
					}

					throw;
				}
			}

			return await instanceClient.DreamDaemon.Start(cancellationToken);
		}

		class TestData
		{
			public string Size { get; set; }
			public string Payload { get; set; }
		}

		// - Uses instance manager concrete
		// - Injects a custom bridge handler into the bridge registrar and makes the test hack into the DMAPI and change its access_identifier
		async Task WhiteBoxValidateBridgeRequestLimitAndTestChunking(CancellationToken cancellationToken)
		{
			// first check the bridge limits
			var bridgeTestsTcs = new TaskCompletionSource();
			BridgeController.TemporarilyDisableContentLogging();
			using (var loggerFactory = LoggerFactory.Create(builder =>
			{
				builder.AddConsole();
				builder.SetMinimumLevel(LogLevel.Trace);
			}))
			{
				var accessIdentifier = $"tgs_integration_test_for_instance_{instanceClient.Metadata.Name}";
				var bridgeProcessor = new TestBridgeHandler(bridgeTestsTcs, loggerFactory.CreateLogger<TestBridgeHandler>(), accessIdentifier, serverPort);
				using var bridgeRegistration = instanceManager.RegisterHandler(bridgeProcessor);

				System.Console.WriteLine("TEST: Sending Bridge tests topic...");

				var bridgeTestTopicResult = await SendTestTopic(
					$"tgs_integration_test_tactics2={accessIdentifier}",
					cancellationToken);
				Assert.AreEqual("ack2", bridgeTestTopicResult.StringData);

				await bridgeTestsTcs.Task.WaitAsync(cancellationToken);
			}

			BridgeController.ReenableContentLogging();

			// Time for DD to revert the bridge access identifier change
			await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
		}

		async Task ValidateTopicLimits(CancellationToken cancellationToken)
		{
			// Time for topic tests
			// Request

			System.Console.WriteLine("TEST: Sending Topic tests topics...");

			var nextPow = 0;
			var lastSize = 0;

			var baseTopic = new TestData
			{
				Size = 0.ToString().PadLeft(6, '0'),
				Payload = "",
			};

			var json = JsonConvert.SerializeObject(baseTopic, DMApiConstants.SerializerSettings);

			var baseSize = (int)(DMApiConstants.MaximumTopicRequestLength - 1);

			var topicString = $"tgs_integration_test_tactics3={topicClient.SanitizeString(json)}";
			var wrappingSize = topicString.Length;

			while (!cancellationToken.IsCancellationRequested)
			{
				var currentSize = baseSize + (int)Math.Pow(2, nextPow);
				var payloadSize = currentSize - wrappingSize;

				var topic = new TestData
				{
					Size = payloadSize.ToString().PadLeft(6, '0'),
					Payload = new string('a', payloadSize),
				};

				TopicResponse topicRequestResult = null;
				try
				{
					System.Console.WriteLine($"Topic send limit test S:{currentSize}...");
					topicRequestResult = await SendTestTopic(
						$"tgs_integration_test_tactics3={topicClient.SanitizeString(JsonConvert.SerializeObject(topic, DMApiConstants.SerializerSettings))}",
						cancellationToken);
				}
				catch (ArgumentOutOfRangeException)
				{
				}

				if (topicRequestResult == null
					|| topicRequestResult.ResponseType != TopicResponseType.StringResponse
					|| topicRequestResult.StringData != "pass")
				{
					if (topicRequestResult != null)
					{
						Assert.AreEqual(TopicResponseType.StringResponse, topicRequestResult.ResponseType, $"String data is: {topicRequestResult.StringData ?? "<<NULL>>"}");
						Assert.AreEqual("fail", topicRequestResult.StringData);
					}

					if (currentSize == lastSize + 1)
						break;
					baseSize = lastSize;
					nextPow = 0;
					continue;
				}

				lastSize = currentSize;
				++nextPow;
			}

			cancellationToken.ThrowIfCancellationRequested();

			Assert.AreEqual(DMApiConstants.MaximumTopicRequestLength, (uint)lastSize);

			System.Console.WriteLine("TEST: Receiving Topic tests topics...");

			// Receive
			baseSize = (int)(DMApiConstants.MaximumTopicResponseLength - 1);
			nextPow = 0;
			lastSize = 0;
			while (!cancellationToken.IsCancellationRequested)
			{
				var currentSize = baseSize + (int)Math.Pow(2, nextPow);
				System.Console.WriteLine($"Topic recieve limit test S:{currentSize}...");
				var topicRequestResult = await SendTestTopic(
					$"tgs_integration_test_tactics4={topicClient.SanitizeString(currentSize.ToString())}",
					cancellationToken);

				if (topicRequestResult.ResponseType != TopicResponseType.StringResponse
					|| new string('a', currentSize) != topicRequestResult.StringData)
				{
					if (currentSize == lastSize + 1)
						break;
					baseSize = lastSize;
					nextPow = 0;
					continue;
				}

				lastSize = currentSize;
				++nextPow;
			}

			cancellationToken.ThrowIfCancellationRequested();
			Assert.AreEqual(DMApiConstants.MaximumTopicResponseLength, (uint)lastSize);
		}

		ushort FindTopicPort()
		{
			using var instanceReference = instanceManager.GetInstanceReference(instanceClient.Metadata);
			var watchdog = instanceReference.Watchdog;

			var sessionObj = watchdog.GetType().GetProperty("Server", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(watchdog);
			Assert.IsNotNull(sessionObj);

			var session = (ISessionController)sessionObj;
			return session.ReattachInformation.TopicPort ?? session.ReattachInformation.Port;
		}

		// - Uses instance manager concrete
		// - Injects a custom bridge handler into the bridge registrar and makes the test hack into the DMAPI and change its access_identifier
		async Task WhiteBoxChatCommandTest(CancellationToken cancellationToken)
		{
			var ddInfo = await instanceClient.DreamDaemon.Read(cancellationToken);
			for (int i = 0; ddInfo.Status != WatchdogStatus.Online && i < 15; ++i)
			{
				await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
				ddInfo = await instanceClient.DreamDaemon.Read(cancellationToken);
			}

			Assert.AreEqual(WatchdogStatus.Online, ddInfo.Status);

			MessageContent embedsResponse, overloadResponse, overloadResponse2, embedsResponse2;
			var startTime = DateTimeOffset.UtcNow - TimeSpan.FromSeconds(5);
			using (var instanceReference = instanceManager.GetInstanceReference(instanceClient.Metadata))
			{
				var mockChatUser = new ChatUser(
					new ChannelRepresentation("test_connection", "Test Connection", 42)
					{
						IsAdminChannel = true,
						EmbedsSupported = true,
						IsPrivateChannel = false,
					},
					"Test Sender",
					"test_user_mention",
					1234);

				var embedsResponseTask = ((WatchdogBase)instanceReference.Watchdog).HandleChatCommand(
					"embeds_test",
					string.Empty,
					mockChatUser,
					cancellationToken);

				var embedsResponseTask2 = ((WatchdogBase)instanceReference.Watchdog).HandleChatCommand(
					"embeds_test",
					new string('a', (int)DMApiConstants.MaximumTopicRequestLength * 3),
					mockChatUser,
					cancellationToken);

				var overloadResponseTask2 = ((WatchdogBase)instanceReference.Watchdog).HandleChatCommand(
					"response_overload_test",
					string.Empty,
					mockChatUser,
					cancellationToken);

				overloadResponse = await ((WatchdogBase)instanceReference.Watchdog).HandleChatCommand(
					"response_overload_test",
					string.Empty,
					mockChatUser,
					cancellationToken);

				overloadResponse2 = await overloadResponseTask2;
				embedsResponse = await embedsResponseTask;
				embedsResponse2 = await embedsResponseTask2;
			}

			var endTime = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(5);

			ddInfo = await instanceClient.DreamDaemon.Read(cancellationToken);
			await CheckDMApiFail(ddInfo.ActiveCompileJob, cancellationToken);

			CheckEmbedsTest(embedsResponse, startTime, endTime);
			CheckEmbedsTest(embedsResponse2, startTime, endTime);

			var expectedString = new string('a', (int)DMApiConstants.MaximumTopicResponseLength * 3);
			Assert.IsNotNull(overloadResponse);
			Assert.AreEqual(expectedString, overloadResponse.Text);
			Assert.IsNotNull(overloadResponse2);
			Assert.AreEqual(expectedString, overloadResponse2.Text);
		}

		static void CheckEmbedsTest(MessageContent embedsResponse, DateTimeOffset startTime, DateTimeOffset endTime)
		{
			Assert.IsNotNull(embedsResponse);
			Assert.AreEqual("Embed support test2", embedsResponse.Text);
			Assert.AreEqual("desc", embedsResponse.Embed.Description);
			Assert.AreEqual("title", embedsResponse.Embed.Title);
			Assert.AreEqual("#0000FF", embedsResponse.Embed.Colour);
			Assert.AreEqual("Dominion", embedsResponse.Embed.Author?.Name);
			Assert.AreEqual("https://github.com/Cyberboss", embedsResponse.Embed.Author.Url);
			Assert.IsTrue(DateTimeOffset.TryParse(embedsResponse.Embed.Timestamp, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var timestamp));
			var timestampCheck = startTime < timestamp && endTime > timestamp;
			Assert.IsTrue(timestampCheck);
			Assert.AreEqual("https://github.com/tgstation/tgstation-server", embedsResponse.Embed.Url);
			Assert.AreEqual(3, embedsResponse.Embed.Fields?.Count);
			Assert.AreEqual("field1", embedsResponse.Embed.Fields.ElementAt(0).Name);
			Assert.AreEqual("value1", embedsResponse.Embed.Fields.ElementAt(0).Value);
			Assert.IsNull(embedsResponse.Embed.Fields.ElementAt(0).IsInline);
			Assert.AreEqual("field2", embedsResponse.Embed.Fields.ElementAt(1).Name);
			Assert.AreEqual("value2", embedsResponse.Embed.Fields.ElementAt(1).Value);
			Assert.IsTrue(embedsResponse.Embed.Fields.ElementAt(1).IsInline);
			Assert.AreEqual("field3", embedsResponse.Embed.Fields.ElementAt(2).Name);
			Assert.AreEqual("value3", embedsResponse.Embed.Fields.ElementAt(2).Value);
			Assert.IsTrue(embedsResponse.Embed.Fields.ElementAt(2).IsInline);
			Assert.AreEqual("Footer text", embedsResponse.Embed.Footer?.Text);
		}

		async ValueTask CheckDDPriority()
		{
			await Task.Yield();
			var allProcesses = TestLiveServer.GetEngineServerProcessesOnPort(testVersion.Engine.Value, ddPort).Where(x => !x.HasExited).ToList();
			if (allProcesses.Count == 0)
				Assert.Fail("Expected engine server to be running here");

			if (allProcesses.Count > 1)
				Assert.Fail("Multiple engine server-like processes running!");

			using var process = allProcesses[0];

			Assert.AreEqual(
				highPrioDD
					? System.Diagnostics.ProcessPriorityClass.AboveNormal
					: System.Diagnostics.ProcessPriorityClass.Normal,
				process.PriorityClass);
		}

		async Task RunLongRunningTestThenUpdate(CancellationToken cancellationToken)
		{
			System.Console.WriteLine("TEST: WATCHDOG LONG RUNNING WITH UPDATE TEST");
			const string DmeName = "LongRunning/long_running_test";

			var daemonStatus = await DeployTestDme(DmeName, DreamDaemonSecurity.Trusted, DMApiValidationMode.Required, cancellationToken);

			var initialCompileJob = daemonStatus.ActiveCompileJob;
			Assert.IsNotNull(initialCompileJob);
			Assert.AreEqual(WatchdogStatus.Offline, daemonStatus.Status.Value);
			Assert.IsNotNull(daemonStatus.ActiveCompileJob);
			Assert.IsNull(daemonStatus.StagedCompileJob);
			Assert.AreEqual(DMApiConstants.InteropVersion, daemonStatus.ActiveCompileJob.DMApiVersion);
			Assert.AreEqual(DreamDaemonSecurity.Safe, daemonStatus.ActiveCompileJob.MinimumSecurityLevel);

			var startJob = await StartDD(cancellationToken);

			await WaitForJob(startJob, 40, false, null, cancellationToken);

			daemonStatus = await DeployTestDme(DmeName, DreamDaemonSecurity.Safe, DMApiValidationMode.Required, cancellationToken);

			Assert.AreEqual(WatchdogStatus.Online, daemonStatus.Status.Value);
			ValidateSessionId(daemonStatus, true);
			await CheckDDPriority();

			Assert.AreEqual(initialCompileJob.Id, daemonStatus.ActiveCompileJob.Id);
			var newerCompileJob = daemonStatus.StagedCompileJob;

			Assert.IsNotNull(newerCompileJob);
			Assert.AreNotEqual(initialCompileJob.Id, newerCompileJob.Id);
			Assert.AreEqual(DreamDaemonSecurity.Safe, newerCompileJob.MinimumSecurityLevel);

			await CheckDMApiFail(daemonStatus.ActiveCompileJob, cancellationToken);
			daemonStatus = await TellWorldToReboot(true, cancellationToken);

			ValidateSessionId(daemonStatus, watchdogRestartsProcess);
			Assert.AreNotEqual(initialCompileJob.Id, daemonStatus.ActiveCompileJob.Id);
			Assert.IsNull(daemonStatus.StagedCompileJob);

			TestLinuxIsntBeingFuckingCheekyAboutFilePaths(daemonStatus, initialCompileJob);

			await instanceClient.DreamDaemon.Shutdown(cancellationToken);

			daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.AreEqual(WatchdogStatus.Offline, daemonStatus.Status.Value);
			await CheckDMApiFail(daemonStatus.ActiveCompileJob, cancellationToken);
		}

		async Task RunLongRunningTestThenUpdateWithNewDme(CancellationToken cancellationToken)
		{
			System.Console.WriteLine("TEST: WATCHDOG LONG RUNNING WITH NEW DME TEST");

			var daemonStatus = await DeployTestDme("LongRunning/long_running_test", DreamDaemonSecurity.Trusted, DMApiValidationMode.Required, cancellationToken);

			var initialCompileJob = daemonStatus.ActiveCompileJob;
			Assert.AreEqual(WatchdogStatus.Offline, daemonStatus.Status.Value);
			Assert.IsNotNull(daemonStatus.ActiveCompileJob);
			Assert.IsNull(daemonStatus.StagedCompileJob);
			Assert.AreEqual(DMApiConstants.InteropVersion, daemonStatus.ActiveCompileJob.DMApiVersion);
			Assert.AreEqual(DreamDaemonSecurity.Safe, daemonStatus.ActiveCompileJob.MinimumSecurityLevel);
			Assert.AreEqual(false, daemonStatus.SoftRestart);

			var startJob = await StartDD(cancellationToken);

			await WaitForJob(startJob, 40, false, null, cancellationToken);

			daemonStatus = await DeployTestDme("LongRunning/long_running_test_copy", DreamDaemonSecurity.Safe, DMApiValidationMode.Required, cancellationToken);

			ValidateSessionId(daemonStatus, true);
			Assert.AreEqual(WatchdogStatus.Online, daemonStatus.Status.Value);
			Assert.AreEqual(true, daemonStatus.SoftRestart);
			await CheckDDPriority();

			Assert.AreEqual(initialCompileJob.Id, daemonStatus.ActiveCompileJob.Id);
			var newerCompileJob = daemonStatus.StagedCompileJob;

			Assert.IsNotNull(newerCompileJob);
			Assert.AreNotEqual(initialCompileJob.Id, newerCompileJob.Id);
			Assert.AreEqual(DreamDaemonSecurity.Safe, newerCompileJob.MinimumSecurityLevel);

			await CheckDMApiFail(daemonStatus.ActiveCompileJob, cancellationToken);
			daemonStatus = await TellWorldToReboot(true, cancellationToken);

			ValidateSessionId(daemonStatus, true); // remember, dme name change triggers reboot
			Assert.AreNotEqual(initialCompileJob.Id, daemonStatus.ActiveCompileJob.Id);
			Assert.AreEqual(false, daemonStatus.SoftRestart);
			Assert.IsNull(daemonStatus.StagedCompileJob);

			await instanceClient.DreamDaemon.Shutdown(cancellationToken);

			daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.AreEqual(WatchdogStatus.Offline, daemonStatus.Status.Value);
			await CheckDMApiFail(daemonStatus.ActiveCompileJob, cancellationToken);
		}

		async Task RunLongRunningTestThenUpdateWithByondVersionSwitch(CancellationToken cancellationToken)
		{
			System.Console.WriteLine("TEST: WATCHDOG BYOND VERSION UPDATE TEST");
			var versionToInstall = testVersion;

			var currentByondVersion = await instanceClient.Engine.ActiveVersion(cancellationToken);
			Assert.AreNotEqual(versionToInstall, currentByondVersion.EngineVersion);

			var initialStatus = await instanceClient.DreamDaemon.Read(cancellationToken);

			var startJob = await StartDD(cancellationToken);

			await WaitForJob(startJob, 70, false, null, cancellationToken);

			await CheckDDPriority();

			var byondInstallJobTask = instanceClient.Engine.SetActiveVersion(
				new EngineVersionRequest
				{
					EngineVersion = new EngineVersion
					{
						Version = versionToInstall.Version,
						Engine = versionToInstall.Engine,
						SourceSHA = versionToInstall.SourceSHA,
					}
				},
				null,
				cancellationToken);
			var byondInstallJob = await byondInstallJobTask;

			// This used to be the case but it gets deleted now that we have and test that
			// Assert.IsNull(byondInstallJob.InstallJob);
			await WaitForJob(byondInstallJob.InstallJob, EngineTest.EngineInstallationTimeout(versionToInstall) + 30, false, null, cancellationToken);

			const string DmeName = "LongRunning/long_running_test";

			await DeployTestDme(DmeName, DreamDaemonSecurity.Safe, DMApiValidationMode.Required, cancellationToken);

			var daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.AreEqual(WatchdogStatus.Online, daemonStatus.Status.Value);
			Assert.IsNotNull(daemonStatus.ActiveCompileJob);
			ValidateSessionId(daemonStatus, true);

			Assert.IsTrue(daemonStatus.ImmediateMemoryUsage.HasValue);
			Assert.AreNotEqual(0, daemonStatus.ImmediateMemoryUsage.Value);

			Assert.AreEqual(initialStatus.ActiveCompileJob.Id, daemonStatus.ActiveCompileJob.Id);
			var newerCompileJob = daemonStatus.StagedCompileJob;
			Assert.AreNotEqual(daemonStatus.ActiveCompileJob.EngineVersion, newerCompileJob.EngineVersion);

			Assert.AreEqual(versionToInstall, newerCompileJob.EngineVersion);

			Assert.AreEqual(true, daemonStatus.SoftRestart);

			await CheckDMApiFail(daemonStatus.ActiveCompileJob, cancellationToken);
			daemonStatus = await TellWorldToReboot(true, cancellationToken);

			Assert.AreEqual(versionToInstall, daemonStatus.ActiveCompileJob.EngineVersion);
			Assert.IsNull(daemonStatus.StagedCompileJob);
			ValidateSessionId(daemonStatus, true);

			await instanceClient.DreamDaemon.Shutdown(cancellationToken);
			await CheckDMApiFail(daemonStatus.ActiveCompileJob, cancellationToken);

			daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.AreEqual(WatchdogStatus.Offline, daemonStatus.Status.Value);
		}

		public async Task StartAndLeaveRunning(CancellationToken cancellationToken)
		{
			System.Console.WriteLine("TEST: WATCHDOG STARTING ENDLESS");
			var startJob = await StartDD(cancellationToken);

			await WaitForJob(startJob, 40, false, null, cancellationToken);

			var daemonStatus = await instanceClient.DreamDaemon.Update(new DreamDaemonRequest
			{
				AdditionalParameters = "slow_start=1",
			},
			cancellationToken);

			Assert.AreEqual(WatchdogStatus.Online, daemonStatus.Status);
			Assert.IsTrue(daemonStatus.SoftRestart);
			await CheckDDPriority();
			Assert.AreEqual(ddPort, daemonStatus.CurrentPort);

			// Try killing the DD process to ensure it gets set to the restoring state
			bool firstTime = true;
			do
			{
				if(!firstTime)
					Assert.IsFalse(daemonStatus.SoftRestart);

				ValidateSessionId(daemonStatus, true);
				KillDD(firstTime);
				firstTime = false;
				await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
				daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			}
			while (daemonStatus.Status == WatchdogStatus.Online);
			Assert.AreEqual(WatchdogStatus.Restoring, daemonStatus.Status);

			// Kill it again
			do
			{
				KillDD(false);
				daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			}
			while (daemonStatus.Status == WatchdogStatus.Online || daemonStatus.Status == WatchdogStatus.Restoring);
			Assert.AreEqual(WatchdogStatus.DelayedRestart, daemonStatus.Status);

			await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);

			daemonStatus = await instanceClient.DreamDaemon.Update(new DreamDaemonRequest
			{
				AdditionalParameters = String.Empty,
			},
			cancellationToken);
			ValidateSessionId(daemonStatus, true);
			Assert.AreEqual(WatchdogStatus.Online, daemonStatus.Status);
			Assert.IsTrue(daemonStatus.SoftRestart);
			await CheckDMApiFail(daemonStatus.ActiveCompileJob, cancellationToken);
		}

		bool KillDD(bool require)
		{
			var ddProcs = TestLiveServer.GetEngineServerProcessesOnPort(testVersion.Engine.Value, ddPort).Where(x => !x.HasExited).ToList();
			if (require && ddProcs.Count == 0 || ddProcs.Count > 1)
				Assert.Fail($"Incorrect number of DD processes: {ddProcs.Count}");

			using var ddProc = ddProcs.SingleOrDefault();
			ddProc?.Kill();
			ddProc?.WaitForExit();

			return ddProc != null;
		}

		public Task<DreamDaemonResponse> TellWorldToReboot(bool waitForOnlineIfRestoring, CancellationToken cancellationToken, [CallerLineNumber]int source = 0)
			=> TellWorldToReboot2(instanceClient, instanceManager, topicClient, FindTopicPort(), waitForOnlineIfRestoring || testVersion.Engine.Value == EngineType.OpenDream, cancellationToken, source);
		public static async Task<DreamDaemonResponse> TellWorldToReboot2(IInstanceClient instanceClient, IInstanceManager instanceManager, ITopicClient topicClient, ushort topicPort, bool waitForOnlineIfRestoring, CancellationToken cancellationToken, [CallerLineNumber]int source = 0, [CallerFilePath]string path = null)
		{
			var daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.IsNotNull(daemonStatus.StagedCompileJob);
			var initialSession = daemonStatus.ActiveCompileJob;

			System.Console.WriteLine($"TEST: Sending world reboot topic @ {path}#L{source}");

			var result = await SendTestTopic("tgs_integration_test_special_tactics=1", topicClient, instanceManager.GetInstanceReference(instanceClient.Metadata), topicPort, cancellationToken);
			Assert.AreEqual("ack", result.StringData);

			using var tempCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			var tempToken = tempCts.Token;
			using (tempToken.Register(() => System.Console.WriteLine("TEST ERROR: Timeout in TellWorldToReboot!")))
			{
				tempCts.CancelAfter(TimeSpan.FromMinutes(2));

				do
				{
					await Task.Delay(TimeSpan.FromSeconds(1), tempToken);
					daemonStatus = await instanceClient.DreamDaemon.Read(tempToken);
				}
				while (initialSession.Id == daemonStatus.ActiveCompileJob.Id);
			}

			if (waitForOnlineIfRestoring && daemonStatus.Status == WatchdogStatus.Restoring)
			{
				do
				{
					await Task.Delay(TimeSpan.FromSeconds(1), tempToken);
					daemonStatus = await instanceClient.DreamDaemon.Read(tempToken);
				}
				while (daemonStatus.Status == WatchdogStatus.Restoring);
			}

			return daemonStatus;
		}

		async Task<DreamDaemonResponse> DeployTestDme(string dmeName, DreamDaemonSecurity deploymentSecurity, DMApiValidationMode dmApiValidationMode, CancellationToken cancellationToken)
		{
			var refreshed = await instanceClient.DreamMaker.Update(new DreamMakerRequest
			{
				ApiValidationSecurityLevel = deploymentSecurity,
				ProjectName = dmeName.Contains("rooted") ? dmeName : $"tests/DMAPI/{dmeName}",
				DMApiValidationMode = dmApiValidationMode,
				Timeout = !ranTimeoutTest ? TimeSpan.FromMilliseconds(1) : TimeSpan.FromMinutes(5),
			}, cancellationToken);

			JobResponse compileJobJob;
			if (!ranTimeoutTest)
			{
				Assert.AreEqual(deploymentSecurity, refreshed.ApiValidationSecurityLevel);
				Assert.AreEqual(dmApiValidationMode, refreshed.DMApiValidationMode);
				Assert.AreEqual(TimeSpan.FromMilliseconds(1), refreshed.Timeout);

				compileJobJob = await instanceClient.DreamMaker.Compile(cancellationToken);

				await WaitForJob(compileJobJob, 90, true, ErrorCode.DeploymentTimeout, cancellationToken);

				await instanceClient.DreamMaker.Update(new DreamMakerRequest
				{
					Timeout = TimeSpan.FromMinutes(5),
				}, cancellationToken);
				ranTimeoutTest = true;
			}

			compileJobJob = await instanceClient.DreamMaker.Compile(cancellationToken);
			await WaitForJob(compileJobJob, 90, false, null, cancellationToken);

			// annoying but, with signalR instant job updates, this running task can get queued before the task that processes the watchdog's monitor activation
			for (var i = 0; i < 10; ++i)
				await Task.Yield();

			var ddInfo = await instanceClient.DreamDaemon.Read(cancellationToken);
			var targetJob = ddInfo.StagedCompileJob ?? ddInfo.ActiveCompileJob;
			Assert.IsNotNull(targetJob);
			if (dmApiValidationMode == DMApiValidationMode.Required)
				Assert.IsNotNull(targetJob.DMApiVersion);
			else
				Assert.IsNull(targetJob.DMApiVersion);

			return ddInfo;
		}

		async Task GracefulWatchdogShutdown(CancellationToken cancellationToken)
		{
			await instanceClient.DreamDaemon.Update(new DreamDaemonRequest
			{
				SoftShutdown = true
			}, cancellationToken);

			var newStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.IsTrue(newStatus.SoftShutdown.Value || newStatus.Status.Value == WatchdogStatus.Offline);

			var timeout = 40;
			do
			{
				await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
				var ddStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
				if (ddStatus.Status.Value == WatchdogStatus.Offline)
					break;

				if (--timeout == 0)
					Assert.Fail("DreamDaemon didn't shutdown within the timeout!");
			}
			while (timeout > 0);
		}

		async Task CheckDMApiFail(CompileJobResponse compileJob, CancellationToken cancellationToken, bool checkLogs = true, bool expectInitialBridgeFailure = false)
		{
			var gameDir = Path.Combine(instanceClient.Metadata.Path, "Game", compileJob.DirectoryName.Value.ToString(), Path.GetDirectoryName(compileJob.DmeName));
			var failFile = Path.Combine(gameDir, "test_fail_reason.txt");

			if (!File.Exists(failFile))
			{
				var bridgeFailFile = Path.Combine(gameDir, "initial_bridge_failed.txt");
				var initialBridgeFailed = File.Exists(bridgeFailFile);

				System.Console.WriteLine($"Files in game dir:{Environment.NewLine}{String.Join(Environment.NewLine, Directory.GetFiles(gameDir))}");

				Assert.AreEqual(expectInitialBridgeFailure, initialBridgeFailed, $"Initial bridge failure expectancy not met in {gameDir}");

				var successFile = Path.Combine(gameDir, "test_success.txt");
				Assert.IsTrue(File.Exists(successFile));
			}
			else
			{
				var text = await File.ReadAllTextAsync(failFile, cancellationToken);
				Assert.Fail(text);
			}

			if (!checkLogs)
				return;

			var daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			if (daemonStatus.Status != WatchdogStatus.Offline || !daemonStatus.LogOutput.Value)
				return;

			var outerLogsDir = Path.Combine(instanceClient.Metadata.Path, "Diagnostics", "DreamDaemonLogs");
			var logsDir = new DirectoryInfo(outerLogsDir).GetDirectories().OrderByDescending(x => x.CreationTime).FirstOrDefault();
			Assert.IsNotNull(logsDir);

			var logfile = logsDir.GetFiles().OrderByDescending(x => x.CreationTime).FirstOrDefault();
			Assert.IsNotNull(logfile);

			var logtext = await File.ReadAllTextAsync(logfile.FullName, cancellationToken);
			Assert.IsFalse(String.IsNullOrWhiteSpace(logtext));
		}

		async ValueTask TestLegacyBridgeEndpoint(CancellationToken cancellationToken)
		{
			System.Console.WriteLine("TEST: TestLegacyBridgeEndpoint");
			var result = await SendTestTopic(
				"im_out_of_memes=1",
				cancellationToken);
			Assert.IsNotNull(result);
			Assert.AreEqual("all gucci", result.StringData);
			await CheckDMApiFail((await instanceClient.DreamDaemon.Read(cancellationToken)).ActiveCompileJob, cancellationToken);
		}
	}
}
