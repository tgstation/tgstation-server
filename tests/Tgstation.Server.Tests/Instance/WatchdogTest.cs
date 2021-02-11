using Byond.TopicSender;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Client;
using Tgstation.Server.Client.Components;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Tests.Instance
{
	sealed class WatchdogTest : JobsRequiredTest
	{
		readonly IInstanceClient instanceClient;

		public WatchdogTest(IInstanceClient instanceClient)
			: base(instanceClient.Jobs)
		{
			this.instanceClient = instanceClient ?? throw new ArgumentNullException(nameof(instanceClient));
		}

		public async Task Run(CancellationToken cancellationToken)
		{
			global::System.Console.WriteLine("TEST: START WATCHDOG TESTS");
			// Increase startup timeout, disable heartbeats
			var initialSettings = await instanceClient.DreamDaemon.Update(new DreamDaemonRequest
			{
				StartupTimeout = 60,
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
			await RunLongRunningTestThenUpdateWithNewDme(cancellationToken);
			await RunLongRunningTestThenUpdateWithByondVersionSwitch(cancellationToken);

			await RunHeartbeatTest(cancellationToken);

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
			TaskCompletionSource<object> jobTcs = new TaskCompletionSource<object>();
			TaskCompletionSource<object> killTaskStarted = new TaskCompletionSource<object>();
			var killTask = Task.Run(() =>
			{
				killTaskStarted.SetResult(null);
				while (!jobTcs.Task.IsCompleted)
					KillDD(false);
			});

			JobResponse job;
			try
			{
				await killTaskStarted.Task;
				var dumpTask = instanceClient.DreamDaemon.CreateDump(cancellationToken);
				job = await WaitForJob(await dumpTask, 20, true, null, cancellationToken);
			}
			finally
			{
				jobTcs.SetResult(null);
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

			var startJob = await StartDD(cancellationToken).ConfigureAwait(false);

			await WaitForJob(startJob, 40, false, null, cancellationToken);

			daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.AreEqual(WatchdogStatus.Online, daemonStatus.Status.Value);
			Assert.AreEqual(false, daemonStatus.SoftRestart);
			Assert.AreEqual(false, daemonStatus.SoftShutdown);
			Assert.AreEqual(String.Empty, daemonStatus.AdditionalParameters);
			var initialCompileJob = daemonStatus.ActiveCompileJob;

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
					startJob = await instanceClient.DreamDaemon.Start(cancellationToken).ConfigureAwait(false);

					await WaitForJob(startJob, 40, true, ErrorCode.DreamDaemonPortInUse, cancellationToken);
				}

			startJob = await StartDD(cancellationToken).ConfigureAwait(false);

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

		async Task RunHeartbeatTest(CancellationToken cancellationToken)
		{
			System.Console.WriteLine("TEST: WATCHDOG HEARTBEAT TEST");
			// enable heartbeats
			await instanceClient.DreamDaemon.Update(new DreamDaemonRequest
			{
				HeartbeatSeconds = 1,
			}, cancellationToken);

			var startJob = await StartDD(cancellationToken).ConfigureAwait(false);

			await WaitForJob(startJob, 40, false, null, cancellationToken);

			// lock on to DD and pause it so it can't heartbeat
			var ddProcs = System.Diagnostics.Process.GetProcessesByName("DreamDaemon").ToList();
			if (ddProcs.Count != 1)
				Assert.Fail($"Incorrect number of DD processes: {ddProcs.Count}");

			using var ddProc = ddProcs.Single();
			IProcessExecutor executor = null;
			executor = new ProcessExecutor(
				new PlatformIdentifier().IsWindows
					? (IProcessFeatures)new WindowsProcessFeatures()
					: new PosixProcessFeatures(new Lazy<IProcessExecutor>(() => executor), Mock.Of<IIOManager>(), Mock.Of<ILogger<PosixProcessFeatures>>()),
				Mock.Of<ILogger<ProcessExecutor>>(),
				LoggerFactory.Create(x => { }));
			using var ourProcessHandler = executor
				.GetProcess(ddProc.Id);

			// Ensure it's responding to heartbeats
			await Task.WhenAny(Task.Delay(20000), ourProcessHandler.Lifetime);
			Assert.IsFalse(ddProc.HasExited);

			await instanceClient.DreamDaemon.Update(new DreamDaemonRequest
			{
				SoftShutdown = true
			}, cancellationToken);

			ourProcessHandler.Suspend();

			await Task.WhenAny(ourProcessHandler.Lifetime, Task.Delay(TimeSpan.FromMinutes(1)));

			var timeout = 20;
			do
			{
				await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
				var ddStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
				Assert.AreEqual(1U, ddStatus.HeartbeatSeconds.Value);
				if (ddStatus.Status.Value == WatchdogStatus.Offline)
					break;

				if (--timeout == 0)
					Assert.Fail("DreamDaemon didn't shutdown within the timeout!");
			}
			while (timeout > 0);

			// disable heartbeats
			await instanceClient.DreamDaemon.Update(new DreamDaemonRequest
			{
				HeartbeatSeconds = 0,
			}, cancellationToken);
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

			var startJob = await StartDD(cancellationToken).ConfigureAwait(false);

			await WaitForJob(startJob, 40, false, null, cancellationToken);

			daemonStatus = await DeployTestDme(DmeName, DreamDaemonSecurity.Safe, true, cancellationToken);

			Assert.AreEqual(WatchdogStatus.Online, daemonStatus.Status.Value);

			Assert.AreEqual(initialCompileJob.Id, daemonStatus.ActiveCompileJob.Id);
			var newerCompileJob = daemonStatus.StagedCompileJob;

			Assert.IsNotNull(newerCompileJob);
			Assert.AreNotEqual(initialCompileJob.Id, newerCompileJob.Id);
			Assert.AreEqual(DreamDaemonSecurity.Ultrasafe, newerCompileJob.MinimumSecurityLevel);

			daemonStatus = await TellWorldToReboot(cancellationToken);

			Assert.AreNotEqual(initialCompileJob.Id, daemonStatus.ActiveCompileJob.Id);
			Assert.IsNull(daemonStatus.StagedCompileJob);

			await instanceClient.DreamDaemon.Shutdown(cancellationToken);

			daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.AreEqual(WatchdogStatus.Offline, daemonStatus.Status.Value);
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

			var startJob = await StartDD(cancellationToken).ConfigureAwait(false);

			await WaitForJob(startJob, 40, false, null, cancellationToken);

			daemonStatus = await DeployTestDme(DmeName + "_copy", DreamDaemonSecurity.Safe, true, cancellationToken);

			Assert.AreEqual(WatchdogStatus.Online, daemonStatus.Status.Value);

			Assert.AreEqual(initialCompileJob.Id, daemonStatus.ActiveCompileJob.Id);
			var newerCompileJob = daemonStatus.StagedCompileJob;

			Assert.IsNotNull(newerCompileJob);
			Assert.AreNotEqual(initialCompileJob.Id, newerCompileJob.Id);
			Assert.AreEqual(DreamDaemonSecurity.Ultrasafe, newerCompileJob.MinimumSecurityLevel);

			daemonStatus = await TellWorldToReboot(cancellationToken);

			Assert.AreNotEqual(initialCompileJob.Id, daemonStatus.ActiveCompileJob.Id);
			Assert.IsNull(daemonStatus.StagedCompileJob);

			await instanceClient.DreamDaemon.Shutdown(cancellationToken);

			daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.AreEqual(WatchdogStatus.Offline, daemonStatus.Status.Value);
		}

		async Task RunLongRunningTestThenUpdateWithByondVersionSwitch(CancellationToken cancellationToken)
		{
			global::System.Console.WriteLine("TEST: WATCHDOG BYOND VERSION UPDATE TEST");
			var versionToInstall = ByondTest.TestVersion;

			versionToInstall = versionToInstall.Semver();
			var currentByondVersion = await instanceClient.Byond.ActiveVersion(cancellationToken);
			Assert.AreNotEqual(versionToInstall, currentByondVersion.Version);

			var initialStatus = await instanceClient.DreamDaemon.Read(cancellationToken);

			var startJob = await StartDD(cancellationToken).ConfigureAwait(false);

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

			daemonStatus = await TellWorldToReboot(cancellationToken);

			Assert.AreEqual(versionToInstall, daemonStatus.ActiveCompileJob.ByondVersion);
			Assert.IsNull(daemonStatus.StagedCompileJob);

			await instanceClient.DreamDaemon.Shutdown(cancellationToken);

			daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.AreEqual(WatchdogStatus.Offline, daemonStatus.Status.Value);
		}

		public async Task StartAndLeaveRunning(CancellationToken cancellationToken)
		{
			global::System.Console.WriteLine("TEST: WATCHDOG STARTING ENDLESS");
			var dd = await instanceClient.DreamDaemon.Read(cancellationToken);
			if(dd.ActiveCompileJob == null)
				await DeployTestDme("LongRunning/long_running_test", DreamDaemonSecurity.Trusted, true, cancellationToken);

			var startJob = await StartDD(cancellationToken).ConfigureAwait(false);

			await WaitForJob(startJob, 40, false, null, cancellationToken);

			var daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.AreEqual(WatchdogStatus.Online, daemonStatus.Status.Value);
			Assert.AreEqual(IntegrationTest.DDPort, daemonStatus.CurrentPort);

			// The measure we use to test dream daemon startup doesn't work on linux currently
			if (new PlatformIdentifier().IsWindows)
			{
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
			}
		}

		static bool KillDD(bool require)
		{
			var ddProcs = System.Diagnostics.Process.GetProcessesByName("DreamDaemon").ToList();
			if ((require && ddProcs.Count == 0) || ddProcs.Count > 1)
				Assert.Fail($"Incorrect number of DD processes: {ddProcs.Count}");

			using var ddProc = ddProcs.SingleOrDefault();
			ddProc?.Kill();
			ddProc?.WaitForExit();

			return ddProc != null;
		}

		async Task<DreamDaemonResponse> TellWorldToReboot(CancellationToken cancellationToken)
		{
			var daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			var initialCompileJob = daemonStatus.ActiveCompileJob;

			var bts = new TopicClient(new SocketParameters
			{
				SendTimeout = TimeSpan.FromSeconds(30),
				ReceiveTimeout = TimeSpan.FromSeconds(30),
				ConnectTimeout = TimeSpan.FromSeconds(30),
				DisconnectTimeout = TimeSpan.FromSeconds(30)
			});

			try
			{
				System.Console.WriteLine("TEST: Sending world reboot topic...");
				var result = await bts.SendTopic(IPAddress.Loopback, "tgs_integration_test_special_tactics=1", IntegrationTest.DDPort, cancellationToken);
				Assert.AreEqual("ack", result.StringData);

				using (var tempCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
				using (tempCts.Token.Register(() => System.Console.WriteLine("TEST ERROR: Timeout in TellWorldToReboot!")))
				{
					tempCts.CancelAfter(TimeSpan.FromMinutes(2));
					var tempToken = tempCts.Token;

					do
					{
						await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
						daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
					}
					while (initialCompileJob.Id == daemonStatus.ActiveCompileJob.Id && !tempToken.IsCancellationRequested);
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
				RequireDMApiValidation = requireApi
			}, cancellationToken);

			Assert.AreEqual(deploymentSecurity, refreshed.ApiValidationSecurityLevel);
			Assert.AreEqual(requireApi, refreshed.RequireDMApiValidation);

			var compileJobJob = await instanceClient.DreamMaker.Compile(cancellationToken);

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
				await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
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
			var failFile = Path.Combine(instanceClient.Metadata.Path, "Game", compileJob.DirectoryName.Value.ToString(), "A", Path.GetDirectoryName(compileJob.DmeName), "test_fail_reason.txt");
			if (!File.Exists(failFile))
				return;

			var text = await File.ReadAllTextAsync(failFile, cancellationToken);
			Assert.Fail(text);
		}
	}
}
