using Byond.TopicSender;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

using Newtonsoft.Json;

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Client;
using Tgstation.Server.Client.Components;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Components.Interop.Bridge;
using Tgstation.Server.Host.Components.Watchdog;
using Tgstation.Server.Host.Controllers;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Tests.Instance
{
	sealed class WatchdogTest : JobsRequiredTest, IBridgeHandler
	{
		readonly IInstanceClient instanceClient;
		readonly InstanceManager instanceManager;
		readonly ushort serverPort;

		bool ranTimeoutTest = false;

		public WatchdogTest(IInstanceClient instanceClient, InstanceManager instanceManager, ushort serverPort)
			: base(instanceClient.Jobs)
		{
			this.instanceClient = instanceClient ?? throw new ArgumentNullException(nameof(instanceClient));
			this.instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
			this.serverPort = serverPort;
		}

		public async Task Run(CancellationToken cancellationToken)
		{
			global::System.Console.WriteLine("TEST: START WATCHDOG TESTS");
			// Increase startup timeout, disable heartbeats
			var initialSettings = await instanceClient.DreamDaemon.Update(new DreamDaemonRequest
			{
				StartupTimeout = 15,
				HeartbeatSeconds = 0,
				Port = IntegrationTest.DDPort
			}, cancellationToken);

			await ApiAssert.ThrowsException<ApiConflictException>(() => instanceClient.DreamDaemon.Update(new DreamDaemonRequest
			{
				Port = 0
			}, cancellationToken), ErrorCode.ModelValidationFailure);

			await ApiAssert.ThrowsException<ApiConflictException>(() => instanceClient.DreamDaemon.Update(new DreamDaemonRequest
			{
				SoftShutdown = true,
				SoftRestart = true
			}, cancellationToken), ErrorCode.DreamDaemonDoubleSoft);

			await ApiAssert.ThrowsException<ConflictException>(() => instanceClient.DreamDaemon.CreateDump(cancellationToken), ErrorCode.WatchdogNotRunning);
			await ApiAssert.ThrowsException<ConflictException>(() => instanceClient.DreamDaemon.Restart(cancellationToken), ErrorCode.WatchdogNotRunning);

			await RunBasicTest(cancellationToken);

			await TestDMApiFreeDeploy(cancellationToken);

			await RunLongRunningTestThenUpdate(cancellationToken);

			await GhettoChatCommandTest(cancellationToken);
			await GhettoValidateDMApiLimits(cancellationToken);

			await RunLongRunningTestThenUpdateWithNewDme(cancellationToken);
			await RunLongRunningTestThenUpdateWithByondVersionSwitch(cancellationToken);

			await RunHeartbeatTest(true, cancellationToken);
			await RunHeartbeatTest(false, cancellationToken);

			await StartAndLeaveRunning(cancellationToken);

			await DumpTests(cancellationToken);

			System.Console.WriteLine("TEST: END WATCHDOG TESTS");
		}

		async Task DumpTests(CancellationToken cancellationToken)
		{
			System.Console.WriteLine("TEST: WATCHDOG DUMP TESTS");
			var dumpJob = await instanceClient.DreamDaemon.CreateDump(cancellationToken);
			await WaitForJob(dumpJob, 30, false, null, cancellationToken);

			var dumpFiles = Directory.GetFiles(Path.Combine(
				instanceClient.Metadata.Path, "Diagnostics", "ProcessDumps"), "*.dmp");
			Assert.AreEqual(1, dumpFiles.Length);
			File.Delete(dumpFiles.Single());

			KillDD(true);
			var jobTcs = new TaskCompletionSource();
			var killTaskStarted = new TaskCompletionSource();
			var killTask = Task.Run(() =>
			{
				killTaskStarted.SetResult();
				while (!jobTcs.Task.IsCompleted)
					KillDD(false);
			}, cancellationToken);

			JobResponse job;
			try
			{
				await killTaskStarted.Task;
				var dumpTask = instanceClient.DreamDaemon.CreateDump(cancellationToken);
				job = await WaitForJob(await dumpTask, 20, true, null, cancellationToken);
			}
			finally
			{
				jobTcs.SetResult();
				await killTask;
			}
			Assert.IsTrue(job.ErrorCode == ErrorCode.DreamDaemonOffline || job.ErrorCode == ErrorCode.GCoreFailure, $"{job.ErrorCode}: {job.ExceptionDetails}");
			await Task.Delay(TimeSpan.FromSeconds(20), cancellationToken);

			var ddStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.AreEqual(WatchdogStatus.Online, ddStatus.Status.Value);
		}

		async Task TestDMApiFreeDeploy(CancellationToken cancellationToken)
		{
			global::System.Console.WriteLine("TEST: WATCHDOG API FREE TEST");
			var daemonStatus = await DeployTestDme("ApiFree/api_free", DreamDaemonSecurity.Safe, false, cancellationToken);

			Assert.AreEqual(WatchdogStatus.Offline, daemonStatus.Status.Value);
			Assert.IsNotNull(daemonStatus.ActiveCompileJob);
			Assert.IsNull(daemonStatus.StagedCompileJob);
			Assert.IsNull(daemonStatus.ActiveCompileJob.DMApiVersion);
			Assert.AreEqual(DreamDaemonSecurity.Ultrasafe, daemonStatus.ActiveCompileJob.MinimumSecurityLevel);

			var startJob = await StartDD(cancellationToken);

			await WaitForJob(startJob, 40, false, null, cancellationToken);

			daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.AreEqual(WatchdogStatus.Online, daemonStatus.Status.Value);
			Assert.AreEqual(false, daemonStatus.SoftRestart);
			Assert.AreEqual(false, daemonStatus.SoftShutdown);
			Assert.AreEqual(String.Empty, daemonStatus.AdditionalParameters);
			var initialCompileJob = daemonStatus.ActiveCompileJob;
			await CheckDMApiFail(daemonStatus.ActiveCompileJob, cancellationToken);

			daemonStatus = await DeployTestDme("BasicOperation/basic_operation_test", DreamDaemonSecurity.Trusted, true, cancellationToken);

			Assert.AreEqual(WatchdogStatus.Online, daemonStatus.Status.Value);

			Assert.AreEqual(initialCompileJob.Id, daemonStatus.ActiveCompileJob.Id);
			var newerCompileJob = daemonStatus.StagedCompileJob;

			Assert.IsNotNull(newerCompileJob);
			Assert.AreNotEqual(initialCompileJob.Id, newerCompileJob.Id);
			Assert.AreEqual(DreamDaemonSecurity.Trusted, newerCompileJob.MinimumSecurityLevel);
			Assert.AreEqual(DMApiConstants.InteropVersion, daemonStatus.StagedCompileJob.DMApiVersion);
			await instanceClient.DreamDaemon.Shutdown(cancellationToken);
		}

		async Task RunBasicTest(CancellationToken cancellationToken)
		{
			global::System.Console.WriteLine("TEST: WATCHDOG BASIC TEST");

			var daemonStatus = await instanceClient.DreamDaemon.Update(new DreamDaemonRequest
			{
				AdditionalParameters = "test=bababooey"
			}, cancellationToken);
			Assert.AreEqual("test=bababooey", daemonStatus.AdditionalParameters);
			daemonStatus = await DeployTestDme("BasicOperation/basic_operation_test", DreamDaemonSecurity.Trusted, true, cancellationToken);

			Assert.AreEqual(WatchdogStatus.Offline, daemonStatus.Status.Value);
			Assert.IsNotNull(daemonStatus.ActiveCompileJob);
			Assert.IsNull(daemonStatus.StagedCompileJob);
			Assert.AreEqual(DMApiConstants.InteropVersion, daemonStatus.ActiveCompileJob.DMApiVersion);
			Assert.AreEqual(DreamDaemonSecurity.Trusted, daemonStatus.ActiveCompileJob.MinimumSecurityLevel);

			JobResponse startJob;
			if (new PlatformIdentifier().IsWindows) // Can't get address reuse to trigger on linux for some reason
				using (var blockSocket = new Socket(SocketType.Stream, ProtocolType.Tcp))
				{
					blockSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, true);
					blockSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);
					blockSocket.Bind(new IPEndPoint(IPAddress.Any, IntegrationTest.DDPort));

					// Don't use StartDD here
					startJob = await instanceClient.DreamDaemon.Start(cancellationToken);

					await WaitForJob(startJob, 40, true, ErrorCode.DreamDaemonPortInUse, cancellationToken);
				}

			startJob = await StartDD(cancellationToken);

			await WaitForJob(startJob, 40, false, null, cancellationToken);

			daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.AreEqual(WatchdogStatus.Online, daemonStatus.Status.Value);
			Assert.AreEqual(false, daemonStatus.SoftRestart);
			Assert.AreEqual(false, daemonStatus.SoftShutdown);

			await GracefulWatchdogShutdown(60, cancellationToken);

			daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.AreEqual(WatchdogStatus.Offline, daemonStatus.Status.Value);

			await CheckDMApiFail(daemonStatus.ActiveCompileJob, cancellationToken);

			daemonStatus = await instanceClient.DreamDaemon.Update(new DreamDaemonRequest
			{
				AdditionalParameters = String.Empty
			}, cancellationToken);
			Assert.AreEqual(String.Empty, daemonStatus.AdditionalParameters);
		}

		async Task RunHeartbeatTest(bool checkDump, CancellationToken cancellationToken)
		{
			System.Console.WriteLine("TEST: WATCHDOG HEARTBEAT TEST");
			// enable heartbeats
			await instanceClient.DreamDaemon.Update(new DreamDaemonRequest
			{
				HeartbeatSeconds = 1,
				DumpOnHeartbeatRestart = checkDump,
			}, cancellationToken);

			var startJob = await StartDD(cancellationToken);

			await WaitForJob(startJob, 40, false, null, cancellationToken);

			// lock on to DD and pause it so it can't heartbeat
			var ddProcs = System.Diagnostics.Process.GetProcessesByName("DreamDaemon").Where(x => !x.HasExited).ToList();
			if (ddProcs.Count != 1)
				Assert.Fail($"Incorrect number of DD processes: {ddProcs.Count}");

			using var ddProc = ddProcs.Single();
			IProcessExecutor executor = null;
			executor = new ProcessExecutor(
                System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
					? new WindowsProcessFeatures(Mock.Of<ILogger<WindowsProcessFeatures>>())
					: new PosixProcessFeatures(new Lazy<IProcessExecutor>(() => executor), Mock.Of<IIOManager>(), Mock.Of<ILogger<PosixProcessFeatures>>()),
				Mock.Of<IIOManager>(),
				Mock.Of<ILogger<ProcessExecutor>>(),
				LoggerFactory.Create(x => { }));
			await using var ourProcessHandler = executor
				.GetProcess(ddProc.Id);

			// Ensure it's responding to heartbeats
			await Task.WhenAny(Task.Delay(20000, cancellationToken), ourProcessHandler.Lifetime);
			Assert.IsFalse(ddProc.HasExited);

			await instanceClient.DreamDaemon.Update(new DreamDaemonRequest
			{
				SoftShutdown = true
			}, cancellationToken);

			ourProcessHandler.Suspend();

			await Task.WhenAny(ourProcessHandler.Lifetime, Task.Delay(TimeSpan.FromMinutes(1), cancellationToken));

			var timeout = 20;
			do
			{
				await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
				var ddStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
				Assert.AreEqual(1U, ddStatus.HeartbeatSeconds.Value);
				if (ddStatus.Status.Value == WatchdogStatus.Offline)
				{
					await CheckDMApiFail(ddStatus.ActiveCompileJob, cancellationToken);
					break;
				}

				if (--timeout == 0)
					Assert.Fail("DreamDaemon didn't shutdown within the timeout!");
			}
			while (timeout > 0);

			// disable heartbeats
			await instanceClient.DreamDaemon.Update(new DreamDaemonRequest
			{
				HeartbeatSeconds = 0,
			}, cancellationToken);

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
					SocketExtensions.BindTest(IntegrationTest.DDPort, false);
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
			await Task.Delay(TimeSpan.FromSeconds(3), cts.Token);

			return await instanceClient.DreamDaemon.Start(cancellationToken);
		}

		class DMApiParametersImpl : DMApiParameters { }
		public DMApiParameters DMApiParameters => new DMApiParametersImpl
		{
			AccessIdentifier = "tgs_integration_test"
		};

		TaskCompletionSource bridgeTestsTcs;

		public class BridgeResponseHack : BridgeResponse
		{
			public string IntegrationHack { get; set; }
		}

		public class ResponseTestData : TestData
		{
			public bool Continue { get; set; }
			public string PayloadId { get; set; }
		}

		bool bridgeStageResponse;
		long lastBridgeRequestSize = 0;

		uint lastPayloadId = 0;

		public Task<BridgeResponse> ProcessBridgeRequest(BridgeParameters parameters, CancellationToken cancellationToken)
		{
			try
			{
				Assert.AreEqual(DMApiParameters.AccessIdentifier, parameters.AccessIdentifier);
				Assert.AreEqual((BridgeCommandType)0, parameters.CommandType);
				Assert.IsNotNull(parameters.ChatMessage?.Text);
				var splits = parameters.ChatMessage.Text.Split(':', StringSplitOptions.RemoveEmptyEntries);
				var coreMessage = splits[0];
				Assert.IsFalse(String.IsNullOrWhiteSpace(coreMessage));
				if (!bridgeStageResponse)
					if (coreMessage == "done")
					{
						Assert.AreEqual(DMApiConstants.MaximumBridgeRequestLength, lastBridgeRequestSize);

						bridgeTestsTcs.SetResult();
						return Task.FromResult<BridgeResponse>(
							new BridgeResponseHack
							{
								IntegrationHack = "ok"
							});
					}

				Assert.AreEqual("payload", coreMessage);
				lastBridgeRequestSize = $"http://127.0.0.1:{serverPort}/Bridge?data=".Length + HttpUtility.UrlEncode(
					JsonConvert.SerializeObject(parameters, DMApiConstants.SerializerSettings)).Length;
				return Task.FromResult<BridgeResponse>(
					new BridgeResponseHack
					{
						IntegrationHack = "ok"
					});
			}
			catch (Exception ex)
			{
				bridgeTestsTcs.SetException(ex);
				return Task.FromResult<BridgeResponse>(null);
			}
		}

		public class TestData
		{
			public string Size { get; set; }
			public string Payload { get; set; }
		}

		async Task GhettoValidateDMApiLimits(CancellationToken cancellationToken)
		{
			var startJob = await StartDD(cancellationToken);

			await WaitForJob(startJob, 40, false, null, cancellationToken);

			// first check the bridge limits
			bridgeTestsTcs = new TaskCompletionSource();
			BridgeController.LogContent = false;
			using (var bridgeRegistration = instanceManager.RegisterHandler(this))
			{
				System.Console.WriteLine("TEST: Sending Bridge tests topic...");
				var bridgeTestTopicResult = await topicClient.SendTopic(IPAddress.Loopback, "tgs_integration_test_tactics2=1", IntegrationTest.DDPort, cancellationToken);
				Assert.AreEqual("ack2", bridgeTestTopicResult.StringData);

				using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
				// this test doesn't take long
				await bridgeTestsTcs.Task.WithToken(cancellationToken);
			}

			BridgeController.LogContent = true;

			// Time for DD to revert the bridge access identifier change
			await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);

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
			var topicString = $"tgs_integration_test_tactics3={topicClient.SanitizeString(json)}";

			var baseSize = topicString.Length;
			var wrappingSize = baseSize;

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
					topicRequestResult = await topicClient.SendTopic(
						IPAddress.Loopback,
						$"tgs_integration_test_tactics3={topicClient.SanitizeString(JsonConvert.SerializeObject(topic, DMApiConstants.SerializerSettings))}",
						IntegrationTest.DDPort,
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
						Assert.AreEqual("fail", topicRequestResult.StringData);
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

			// Receive
			baseSize = 1;
			nextPow = 0;
			lastSize = 0;
			while (!cancellationToken.IsCancellationRequested)
			{
				var currentSize = baseSize + (int)Math.Pow(2, nextPow);
				var topicRequestResult = await topicClient.SendTopic(
					IPAddress.Loopback,
					$"tgs_integration_test_tactics4={topicClient.SanitizeString(currentSize.ToString())}",
					IntegrationTest.DDPort,
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
			await instanceClient.DreamDaemon.Shutdown(cancellationToken);
		}

		async Task GhettoChatCommandTest(CancellationToken cancellationToken)
		{
			var startJob = await StartDD(cancellationToken);

			await WaitForJob(startJob, 40, false, null, cancellationToken);

			// oh god, oh fuck, blackbox testing
			MessageContent response;
			var startTime = DateTimeOffset.UtcNow - TimeSpan.FromSeconds(5);
			using (var instanceReference = instanceManager.GetInstanceReference(instanceClient.Metadata))
			{
				response = await ((BasicWatchdog)instanceReference.Watchdog).HandleChatCommand(
					"embeds_test",
					String.Empty,
					new Host.Components.Chat.ChatUser
					{
						Channel = new Host.Components.Chat.ChannelRepresentation
						{
							IsAdminChannel = true,
							ConnectionName = "test_connection",
							EmbedsSupported = true,
							FriendlyName = "Test Connection",
							Id = "test_channel_id",
							IsPrivateChannel = false,
						},
						FriendlyName = "Test Sender",
						Id = "test_user_id",
						Mention = "test_user_mention",
						RealId = 1234,
					},
					cancellationToken);
			}

			var endTime = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(5);

			Assert.IsNotNull(response);
			Assert.AreEqual("Embed support test2", response.Text);
			Assert.AreEqual("desc", response.Embed.Description);
			Assert.AreEqual("title", response.Embed.Title);
			Assert.AreEqual("#0000FF", response.Embed.Colour);
			Assert.AreEqual("Dominion", response.Embed.Author?.Name);
			Assert.AreEqual("https://github.com/Cyberboss", response.Embed.Author.Url);
			Assert.IsTrue(DateTimeOffset.TryParse(response.Embed.Timestamp, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var timestamp));
			Assert.IsTrue(startTime < timestamp && endTime > timestamp);
			Assert.AreEqual("https://github.com/tgstation/tgstation-server", response.Embed.Url);
			Assert.AreEqual(3, response.Embed.Fields?.Count);
			Assert.AreEqual("field1", response.Embed.Fields.ElementAt(0).Name);
			Assert.AreEqual("value1", response.Embed.Fields.ElementAt(0).Value);
			Assert.IsNull(response.Embed.Fields.ElementAt(0).IsInline);
			Assert.AreEqual("field2", response.Embed.Fields.ElementAt(1).Name);
			Assert.AreEqual("value2", response.Embed.Fields.ElementAt(1).Value);
			Assert.IsTrue(response.Embed.Fields.ElementAt(1).IsInline);
			Assert.AreEqual("field3", response.Embed.Fields.ElementAt(2).Name);
			Assert.AreEqual("value3", response.Embed.Fields.ElementAt(2).Value);
			Assert.IsTrue(response.Embed.Fields.ElementAt(2).IsInline);
			Assert.AreEqual("Footer text", response.Embed.Footer?.Text);

			await instanceClient.DreamDaemon.Shutdown(cancellationToken);
		}

		async Task RunLongRunningTestThenUpdate(CancellationToken cancellationToken)
		{
			global::System.Console.WriteLine("TEST: WATCHDOG LONG RUNNING WITH UPDATE TEST");
			const string DmeName = "LongRunning/long_running_test";

			var daemonStatus = await DeployTestDme(DmeName, DreamDaemonSecurity.Trusted, true, cancellationToken);

			var initialCompileJob = daemonStatus.ActiveCompileJob;
			Assert.AreEqual(WatchdogStatus.Offline, daemonStatus.Status.Value);
			Assert.IsNotNull(daemonStatus.ActiveCompileJob);
			Assert.IsNull(daemonStatus.StagedCompileJob);
			Assert.AreEqual(DMApiConstants.InteropVersion, daemonStatus.ActiveCompileJob.DMApiVersion);
			Assert.AreEqual(DreamDaemonSecurity.Ultrasafe, daemonStatus.ActiveCompileJob.MinimumSecurityLevel);

			var startJob = await StartDD(cancellationToken);

			await WaitForJob(startJob, 40, false, null, cancellationToken);

			daemonStatus = await DeployTestDme(DmeName, DreamDaemonSecurity.Safe, true, cancellationToken);

			Assert.AreEqual(WatchdogStatus.Online, daemonStatus.Status.Value);

			Assert.AreEqual(initialCompileJob.Id, daemonStatus.ActiveCompileJob.Id);
			var newerCompileJob = daemonStatus.StagedCompileJob;

			Assert.IsNotNull(newerCompileJob);
			Assert.AreNotEqual(initialCompileJob.Id, newerCompileJob.Id);
			Assert.AreEqual(DreamDaemonSecurity.Ultrasafe, newerCompileJob.MinimumSecurityLevel);

			await CheckDMApiFail(daemonStatus.ActiveCompileJob, cancellationToken);
			daemonStatus = await TellWorldToReboot(cancellationToken);

			Assert.AreNotEqual(initialCompileJob.Id, daemonStatus.ActiveCompileJob.Id);
			Assert.IsNull(daemonStatus.StagedCompileJob);

			await instanceClient.DreamDaemon.Shutdown(cancellationToken);

			daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.AreEqual(WatchdogStatus.Offline, daemonStatus.Status.Value);
			await CheckDMApiFail(daemonStatus.ActiveCompileJob, cancellationToken);
		}

		async Task RunLongRunningTestThenUpdateWithNewDme(CancellationToken cancellationToken)
		{
			global::System.Console.WriteLine("TEST: WATCHDOG LONG RUNNING WITH NEW DME TEST");
			const string DmeName = "LongRunning/long_running_test";

			var daemonStatus = await DeployTestDme(DmeName, DreamDaemonSecurity.Trusted, true, cancellationToken);

			var initialCompileJob = daemonStatus.ActiveCompileJob;
			Assert.AreEqual(WatchdogStatus.Offline, daemonStatus.Status.Value);
			Assert.IsNotNull(daemonStatus.ActiveCompileJob);
			Assert.IsNull(daemonStatus.StagedCompileJob);
			Assert.AreEqual(DMApiConstants.InteropVersion, daemonStatus.ActiveCompileJob.DMApiVersion);
			Assert.AreEqual(DreamDaemonSecurity.Ultrasafe, daemonStatus.ActiveCompileJob.MinimumSecurityLevel);

			var startJob = await StartDD(cancellationToken);

			await WaitForJob(startJob, 40, false, null, cancellationToken);

			daemonStatus = await DeployTestDme(DmeName + "_copy", DreamDaemonSecurity.Safe, true, cancellationToken);

			Assert.AreEqual(WatchdogStatus.Online, daemonStatus.Status.Value);

			Assert.AreEqual(initialCompileJob.Id, daemonStatus.ActiveCompileJob.Id);
			var newerCompileJob = daemonStatus.StagedCompileJob;

			Assert.IsNotNull(newerCompileJob);
			Assert.AreNotEqual(initialCompileJob.Id, newerCompileJob.Id);
			Assert.AreEqual(DreamDaemonSecurity.Ultrasafe, newerCompileJob.MinimumSecurityLevel);

			await CheckDMApiFail(daemonStatus.ActiveCompileJob, cancellationToken);
			daemonStatus = await TellWorldToReboot(cancellationToken);

			Assert.AreNotEqual(initialCompileJob.Id, daemonStatus.ActiveCompileJob.Id);
			Assert.IsNull(daemonStatus.StagedCompileJob);

			await instanceClient.DreamDaemon.Shutdown(cancellationToken);

			daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.AreEqual(WatchdogStatus.Offline, daemonStatus.Status.Value);
			await CheckDMApiFail(daemonStatus.ActiveCompileJob, cancellationToken);
		}

		async Task RunLongRunningTestThenUpdateWithByondVersionSwitch(CancellationToken cancellationToken)
		{
			global::System.Console.WriteLine("TEST: WATCHDOG BYOND VERSION UPDATE TEST");
			var versionToInstall = ByondTest.TestVersion;

			versionToInstall = versionToInstall.Semver();
			var currentByondVersion = await instanceClient.Byond.ActiveVersion(cancellationToken);
			Assert.AreNotEqual(versionToInstall, currentByondVersion.Version);

			var initialStatus = await instanceClient.DreamDaemon.Read(cancellationToken);

			var startJob = await StartDD(cancellationToken);

			await WaitForJob(startJob, 70, false, null, cancellationToken);

			var byondInstallJobTask = instanceClient.Byond.SetActiveVersion(
				new ByondVersionRequest
				{
					Version = versionToInstall
				},
				null,
				cancellationToken);
			var byondInstallJob = await byondInstallJobTask;

			Assert.IsNull(byondInstallJob.InstallJob);

			const string DmeName = "LongRunning/long_running_test";

			await DeployTestDme(DmeName, DreamDaemonSecurity.Safe, true, cancellationToken);

			var daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.AreEqual(WatchdogStatus.Online, daemonStatus.Status.Value);
			Assert.IsNotNull(daemonStatus.ActiveCompileJob);


			Assert.AreEqual(initialStatus.ActiveCompileJob.Id, daemonStatus.ActiveCompileJob.Id);
			var newerCompileJob = daemonStatus.StagedCompileJob;
			Assert.AreNotEqual(daemonStatus.ActiveCompileJob.ByondVersion, newerCompileJob.ByondVersion);
			Assert.AreEqual(versionToInstall, newerCompileJob.ByondVersion);

			Assert.AreEqual(true, daemonStatus.SoftRestart);

			await CheckDMApiFail(daemonStatus.ActiveCompileJob, cancellationToken);
			daemonStatus = await TellWorldToReboot(cancellationToken);

			Assert.AreEqual(versionToInstall, daemonStatus.ActiveCompileJob.ByondVersion);
			Assert.IsNull(daemonStatus.StagedCompileJob);

			await instanceClient.DreamDaemon.Shutdown(cancellationToken);
			await CheckDMApiFail(daemonStatus.ActiveCompileJob, cancellationToken);

			daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.AreEqual(WatchdogStatus.Offline, daemonStatus.Status.Value);
		}

		public async Task StartAndLeaveRunning(CancellationToken cancellationToken)
		{
			global::System.Console.WriteLine("TEST: WATCHDOG STARTING ENDLESS");
			var dd = await instanceClient.DreamDaemon.Read(cancellationToken);
			if (dd.ActiveCompileJob == null)
				await DeployTestDme("LongRunning/long_running_test", DreamDaemonSecurity.Trusted, true, cancellationToken);

			var startJob = await StartDD(cancellationToken);

			await WaitForJob(startJob, 40, false, null, cancellationToken);

			var daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.AreEqual(WatchdogStatus.Online, daemonStatus.Status.Value);
			Assert.AreEqual(IntegrationTest.DDPort, daemonStatus.CurrentPort);

			// Try killing the DD process to ensure it gets set to the restoring state
			do
			{
				KillDD(true);
				await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
				daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			}
			while (daemonStatus.Status == WatchdogStatus.Online);
			Assert.AreEqual(WatchdogStatus.Restoring, daemonStatus.Status.Value);

			// Kill it again
			do
			{
				KillDD(false);
				daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			}
			while (daemonStatus.Status == WatchdogStatus.Online || daemonStatus.Status == WatchdogStatus.Restoring);
			Assert.AreEqual(WatchdogStatus.DelayedRestart, daemonStatus.Status.Value);

			await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);

			daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.AreEqual(WatchdogStatus.Online, daemonStatus.Status.Value);
			await CheckDMApiFail(daemonStatus.ActiveCompileJob, cancellationToken);
		}

		static bool KillDD(bool require)
		{
			var ddProcs = System.Diagnostics.Process.GetProcessesByName("DreamDaemon").Where(x => !x.HasExited).ToList();
			if ((require && ddProcs.Count == 0) || ddProcs.Count > 1)
				Assert.Fail($"Incorrect number of DD processes: {ddProcs.Count}");

			using var ddProc = ddProcs.SingleOrDefault();
			ddProc?.Kill();
			ddProc?.WaitForExit();

			return ddProc != null;
		}

		TopicClient topicClient = new (new SocketParameters
		{
			SendTimeout = TimeSpan.FromSeconds(30),
			ReceiveTimeout = TimeSpan.FromSeconds(30),
			ConnectTimeout = TimeSpan.FromSeconds(30),
			DisconnectTimeout = TimeSpan.FromSeconds(30)
		});

		public async Task<DreamDaemonResponse> TellWorldToReboot(CancellationToken cancellationToken)
		{
			var daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			var initialCompileJob = daemonStatus.ActiveCompileJob;

			try
			{
				System.Console.WriteLine("TEST: Sending world reboot topic...");
				var result = await topicClient.SendTopic(IPAddress.Loopback, "tgs_integration_test_special_tactics=1", IntegrationTest.DDPort, cancellationToken);
				Assert.AreEqual("ack", result.StringData);

				using (var tempCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
				using (tempCts.Token.Register(() => System.Console.WriteLine("TEST ERROR: Timeout in TellWorldToReboot!")))
				{
					tempCts.CancelAfter(TimeSpan.FromMinutes(2));
					var tempToken = tempCts.Token;

					do
					{
						await Task.Delay(TimeSpan.FromSeconds(1), tempToken);
						daemonStatus = await instanceClient.DreamDaemon.Read(tempToken);
					}
					while (initialCompileJob.Id == daemonStatus.ActiveCompileJob.Id);
				}
			}
			catch (OperationCanceledException)
			{
				throw;
			}

			return daemonStatus;
		}

		async Task<DreamDaemonResponse> DeployTestDme(string dmeName, DreamDaemonSecurity deploymentSecurity, bool requireApi, CancellationToken cancellationToken)
		{
			var refreshed = await instanceClient.DreamMaker.Update(new DreamMakerRequest
			{
				ApiValidationSecurityLevel = deploymentSecurity,
				ProjectName = $"tests/DMAPI/{dmeName}",
				RequireDMApiValidation = requireApi,
				Timeout = TimeSpan.FromMilliseconds(1),
			}, cancellationToken);

			JobResponse compileJobJob;
			if (!ranTimeoutTest)
			{
				Assert.AreEqual(deploymentSecurity, refreshed.ApiValidationSecurityLevel);
				Assert.AreEqual(requireApi, refreshed.RequireDMApiValidation);
				Assert.AreEqual(TimeSpan.FromMilliseconds(1), refreshed.Timeout);

				compileJobJob = await instanceClient.DreamMaker.Compile(cancellationToken);

				await WaitForJob(compileJobJob, 90, true, ErrorCode.DeploymentTimeout, cancellationToken);
				ranTimeoutTest = true;
			}

			await instanceClient.DreamMaker.Update(new DreamMakerRequest
			{
				Timeout = TimeSpan.FromMinutes(5),
			}, cancellationToken);

			compileJobJob = await instanceClient.DreamMaker.Compile(cancellationToken);
			await WaitForJob(compileJobJob, 90, false, null, cancellationToken);

			var ddInfo = await instanceClient.DreamDaemon.Read(cancellationToken);
			if (requireApi)
				Assert.IsNotNull((ddInfo.StagedCompileJob ?? ddInfo.ActiveCompileJob).DMApiVersion);
			return ddInfo;
		}

		async Task GracefulWatchdogShutdown(uint timeout, CancellationToken cancellationToken)
		{
			await instanceClient.DreamDaemon.Update(new DreamDaemonRequest
			{
				SoftShutdown = true
			}, cancellationToken);

			var newStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.IsTrue(newStatus.SoftShutdown.Value || (newStatus.Status.Value == WatchdogStatus.Offline));

			do
			{
				await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
				var ddStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
				if (ddStatus.Status.Value == WatchdogStatus.Offline)
					break;

				if (--timeout == 0)
					Assert.Fail("DreamDaemon didn't shutdown within the timeout!");
			}
			while (timeout > 0);
		}

		async Task CheckDMApiFail(CompileJobResponse compileJob, CancellationToken cancellationToken)
		{
			var gameDir = Path.Combine(instanceClient.Metadata.Path, "Game", compileJob.DirectoryName.Value.ToString(), Path.GetDirectoryName(compileJob.DmeName));
			var failFile = Path.Combine(gameDir, "test_fail_reason.txt");
			if (!File.Exists(failFile))
			{
				var successFile = Path.Combine(gameDir, "test_success.txt");
				Assert.IsTrue(File.Exists(successFile));
				return;
			}

			var text = await File.ReadAllTextAsync(failFile, cancellationToken);
			Assert.Fail(text);
		}
	}
}
