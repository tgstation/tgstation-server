using Byond.TopicSender;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Client;
using Tgstation.Server.Client.Components;
using Tgstation.Server.Host.Components.Interop;
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
			// Increase startup timeout, disable heartbeats
			await instanceClient.DreamDaemon.Update(new DreamDaemon
			{
				StartupTimeout = 45,
				HeartbeatSeconds = 0,
			}, cancellationToken);

			await ApiAssert.ThrowsException<ApiConflictException>(() => instanceClient.DreamDaemon.Update(new DreamDaemon
			{
				SoftShutdown = true,
				SoftRestart = true
			}, cancellationToken), ErrorCode.DreamDaemonDoubleSoft);

			await RunBasicTest(cancellationToken);

			await RunLongRunningTestThenUpdate(cancellationToken);
			await RunLongRunningTestThenUpdateWithByondVersionSwitch(cancellationToken);

			await RunHeartbeatTest(cancellationToken);

			await StartAndLeaveRunning(cancellationToken);
		}

		async Task RunBasicTest(CancellationToken cancellationToken)
		{
			var daemonStatus = await DeployTestDme("BasicOperation/basic_operation_test", DreamDaemonSecurity.Ultrasafe, cancellationToken);

			Assert.IsFalse(daemonStatus.Running.Value);
			Assert.IsNotNull(daemonStatus.ActiveCompileJob);
			Assert.IsNull(daemonStatus.StagedCompileJob);
			Assert.AreEqual(DMApiConstants.Version, daemonStatus.ActiveCompileJob.DMApiVersion);
			Assert.AreEqual(DreamDaemonSecurity.Safe, daemonStatus.ActiveCompileJob.MinimumSecurityLevel);

			var startJob = await instanceClient.DreamDaemon.Start(cancellationToken).ConfigureAwait(false);

			await WaitForJob(startJob, 10, false, cancellationToken);

			daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.IsTrue(daemonStatus.Running.Value);
			Assert.AreEqual(false, daemonStatus.SoftRestart);
			Assert.AreEqual(false, daemonStatus.SoftShutdown);

			await GracefulWatchdogShutdown(30, cancellationToken);

			daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.IsFalse(daemonStatus.Running.Value);

			await CheckDMApiFail(daemonStatus.ActiveCompileJob, cancellationToken);
		}

		async Task RunHeartbeatTest(CancellationToken cancellationToken)
		{
			// enable heartbeats
			await instanceClient.DreamDaemon.Update(new DreamDaemon
			{
				HeartbeatSeconds = 1,
			}, cancellationToken);

			var startJob = await instanceClient.DreamDaemon.Start(cancellationToken).ConfigureAwait(false);

			await WaitForJob(startJob, 10, false, cancellationToken);

			// lock on to DD and pause it so it can't heartbeat
			var ddProcs = System.Diagnostics.Process.GetProcessesByName("DreamDaemon").ToList();
			if (ddProcs.Count != 1)
				Assert.Inconclusive($"Incorrect number of DD processes: {ddProcs.Count}");

			using var ddProc = ddProcs.Single();
			using var ourProcessHandler = new ProcessExecutor(
				new PlatformIdentifier().IsWindows
					? (IProcessFeatures)new WindowsProcessFeatures(Mock.Of<ILogger<WindowsProcessFeatures>>())
					: new PosixProcessFeatures(Mock.Of<IIOManager>(), Mock.Of<ILogger<PosixProcessFeatures>>()),
				Mock.Of<ILogger<ProcessExecutor>>(),
				LoggerFactory.Create(x => { }))
				.GetProcess(ddProc.Id);

			// Ensure it's responding to heartbeats
			await Task.WhenAny(Task.Delay(20000), ourProcessHandler.Lifetime);
			Assert.IsFalse(ddProc.HasExited);

			await instanceClient.DreamDaemon.Update(new DreamDaemon
			{
				SoftShutdown = true
			}, cancellationToken);

			ourProcessHandler.Suspend();

			await Task.WhenAny(ourProcessHandler.Lifetime, Task.Delay(TimeSpan.FromSeconds(20)));

			var timeout = 10;
			do
			{
				await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
				var ddStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
				Assert.AreEqual(1U, ddStatus.HeartbeatSeconds.Value);
				if (!ddStatus.Running.Value)
					break;

				if (--timeout == 0)
					Assert.Fail("DreamDaemon didn't shutdown within the timeout!");
			}
			while (timeout > 0);

			// disable heartbeats
			await instanceClient.DreamDaemon.Update(new DreamDaemon
			{
				HeartbeatSeconds = 0,
			}, cancellationToken);
		}

		async Task RunLongRunningTestThenUpdate(CancellationToken cancellationToken)
		{
			const string DmeName = "LongRunning/long_running_test";

			var daemonStatus = await DeployTestDme(DmeName, DreamDaemonSecurity.Trusted, cancellationToken);

			var initialCompileJob = daemonStatus.ActiveCompileJob;
			Assert.IsFalse(daemonStatus.Running.Value);
			Assert.IsNotNull(daemonStatus.ActiveCompileJob);
			Assert.IsNull(daemonStatus.StagedCompileJob);
			Assert.AreEqual(DMApiConstants.Version, daemonStatus.ActiveCompileJob.DMApiVersion);
			Assert.AreEqual(DreamDaemonSecurity.Ultrasafe, daemonStatus.ActiveCompileJob.MinimumSecurityLevel);

			var startJob = await instanceClient.DreamDaemon.Start(cancellationToken).ConfigureAwait(false);

			await WaitForJob(startJob, 10, false, cancellationToken);

			daemonStatus = await DeployTestDme(DmeName, DreamDaemonSecurity.Safe, cancellationToken);

			Assert.IsTrue(daemonStatus.Running.Value);

			Assert.AreEqual(initialCompileJob.Id, daemonStatus.ActiveCompileJob.Id);
			var newerCompileJob = daemonStatus.StagedCompileJob;

			Assert.IsNotNull(newerCompileJob);
			Assert.AreNotEqual(initialCompileJob.Id, newerCompileJob.Id);
			Assert.AreEqual(DreamDaemonSecurity.Ultrasafe, newerCompileJob.MinimumSecurityLevel);

			await TellWorldToReboot(cancellationToken);

			daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.AreNotEqual(initialCompileJob.Id, daemonStatus.ActiveCompileJob.Id);
			Assert.IsNull(daemonStatus.StagedCompileJob);

			await instanceClient.DreamDaemon.Shutdown(cancellationToken);

			daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.IsFalse(daemonStatus.Running.Value);
		}

		async Task RunLongRunningTestThenUpdateWithByondVersionSwitch(CancellationToken cancellationToken)
		{
			var versionToInstall = new Version(511, 1384, 0);
			var byondInstallJobTask = instanceClient.Byond.SetActiveVersion(
				new Api.Models.Byond
				{
					Version = versionToInstall
				},
				cancellationToken);

			var initialStatus = await instanceClient.DreamDaemon.Read(cancellationToken);

			var startJob = await instanceClient.DreamDaemon.Start(cancellationToken).ConfigureAwait(false);

			var byondInstallJob = await byondInstallJobTask;

			await WaitForJob(startJob, 40, false, cancellationToken);

			await WaitForJob(byondInstallJob.InstallJob, 30, false, cancellationToken);

			const string DmeName = "LongRunning/long_running_test";

			await DeployTestDme(DmeName, DreamDaemonSecurity.Safe, cancellationToken);

			var daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.IsTrue(daemonStatus.Running.Value);
			Assert.IsNotNull(daemonStatus.ActiveCompileJob);


			Assert.AreEqual(initialStatus.ActiveCompileJob.Id, daemonStatus.ActiveCompileJob.Id);
			var newerCompileJob = daemonStatus.StagedCompileJob;
			Assert.AreNotEqual(daemonStatus.ActiveCompileJob.ByondVersion, newerCompileJob.ByondVersion);
			Assert.AreEqual(versionToInstall, newerCompileJob.ByondVersion);

			Assert.AreEqual(true, daemonStatus.SoftRestart);

			await TellWorldToReboot(cancellationToken);

			daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.AreEqual(versionToInstall, daemonStatus.ActiveCompileJob.ByondVersion);
			Assert.IsNull(daemonStatus.StagedCompileJob);

			await instanceClient.DreamDaemon.Shutdown(cancellationToken);

			daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.IsFalse(daemonStatus.Running.Value);
		}

		public async Task StartAndLeaveRunning(CancellationToken cancellationToken)
		{
			var dd = await instanceClient.DreamDaemon.Read(cancellationToken);
			if(dd.ActiveCompileJob == null)
				await DeployTestDme("LongRunning/long_running_test", DreamDaemonSecurity.Trusted, cancellationToken);

			var startJob = await instanceClient.DreamDaemon.Start(cancellationToken).ConfigureAwait(false);

			await WaitForJob(startJob, 40, false, cancellationToken);
		}

		async Task TellWorldToReboot(CancellationToken cancellationToken)
		{
			var bts = new TopicClient(new SocketParameters
			{
				SendTimeout = TimeSpan.FromSeconds(5),
				ReceiveTimeout = TimeSpan.FromSeconds(5),
				ConnectTimeout = TimeSpan.FromSeconds(5),
				DisconnectTimeout = TimeSpan.FromSeconds(5)
			});

			try
			{
				global::System.Console.WriteLine("TEST: Sending world reboot topic...");
				var result = await bts.SendTopic(IPAddress.Loopback, "tgs_integration_test_special_tactics=1", 1337, cancellationToken);
				Assert.AreEqual("ack", result.StringData);

				await Task.Delay(10000, cancellationToken);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch
			{
			}
		}

		async Task<DreamDaemon> DeployTestDme(string dmeName, DreamDaemonSecurity deploymentSecurity, CancellationToken cancellationToken)
		{
			await instanceClient.DreamMaker.Update(new DreamMaker
			{
				ApiValidationSecurityLevel = deploymentSecurity,
				ProjectName = $"tests/DMAPI/{dmeName}"
			}, cancellationToken);

			var compileJobJob = await instanceClient.DreamMaker.Compile(cancellationToken);

			await WaitForJob(compileJobJob, 90, false, cancellationToken);

			return await instanceClient.DreamDaemon.Read(cancellationToken);
		}

		async Task GracefulWatchdogShutdown(uint timeout, CancellationToken cancellationToken)
		{
			await instanceClient.DreamDaemon.Update(new DreamDaemon
			{
				SoftShutdown = true
			}, cancellationToken);

			var newStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.IsTrue(newStatus.SoftShutdown.Value || !newStatus.Running.Value);

			do
			{
				await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
				var ddStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
				if (!ddStatus.Running.Value)
					break;

				if (--timeout == 0)
					Assert.Fail("DreamDaemon didn't shutdown within the timeout!");
			}
			while (timeout > 0);
		}

		async Task CheckDMApiFail(CompileJob compileJob, CancellationToken cancellationToken)
		{
			var failFile = Path.Combine(instanceClient.Metadata.Path, "Game", compileJob.DirectoryName.Value.ToString(), "A", Path.GetDirectoryName(compileJob.DmeName), "test_fail_reason.txt");
			if (!File.Exists(failFile))
				return;

			var text = await File.ReadAllTextAsync(failFile, cancellationToken);
			Assert.Fail(text);
		}
	}
}
