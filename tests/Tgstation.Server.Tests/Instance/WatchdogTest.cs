using Byond.TopicSender;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Client.Components;
using Tgstation.Server.Host.Components.Chat.Providers;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Components.Interop.Topic;
using Tgstation.Server.Host.Core;
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
			// Increase startup timeout
			await instanceClient.DreamDaemon.Update(new DreamDaemon
			{
				StartupTimeout = 45
			}, cancellationToken);

			await RunBasicTest(cancellationToken);
			await RunLongRunningTestThenUpdate(cancellationToken);
			await RunLongRunningTestThenUpdateWithByondVersionSwitch(cancellationToken);
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
			Assert.AreNotEqual(initialCompileJob.Id, daemonStatus.StagedCompileJob.Id);
			Assert.AreEqual(DreamDaemonSecurity.Ultrasafe, daemonStatus.StagedCompileJob.MinimumSecurityLevel);

			await TellWorldToReboot(cancellationToken);

			await Task.Delay(10000, cancellationToken);

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

			var startJob = await instanceClient.DreamDaemon.Start(cancellationToken).ConfigureAwait(false);

			var byondInstallJob = await byondInstallJobTask;

			await WaitForJob(byondInstallJob.InstallJob, 30, false, cancellationToken);

			const string DmeName = "LongRunning/long_running_test";

			var daemonStatus = await DeployTestDme(DmeName, DreamDaemonSecurity.Safe, cancellationToken);

			await WaitForJob(startJob, 10, false, cancellationToken);

			Assert.IsTrue(daemonStatus.Running.Value);
			Assert.IsNotNull(daemonStatus.ActiveCompileJob);
			Assert.IsNotNull(daemonStatus.StagedCompileJob);
			Assert.AreNotEqual(daemonStatus.ActiveCompileJob.ByondVersion, daemonStatus.StagedCompileJob.ByondVersion);
			Assert.AreEqual(versionToInstall, daemonStatus.StagedCompileJob.ByondVersion);
			Assert.AreEqual(true, daemonStatus.SoftRestart);

			await TellWorldToReboot(cancellationToken);

			daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.AreEqual(versionToInstall, daemonStatus.ActiveCompileJob.ByondVersion);
			Assert.IsNull(daemonStatus.StagedCompileJob);

			await instanceClient.DreamDaemon.Shutdown(cancellationToken);

			daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.IsFalse(daemonStatus.Running.Value);
		}

		async Task TellWorldToReboot(CancellationToken cancellationToken)
		{
			//we've wired long_running_test to reboot if its reboot mode is changed TO normal

			await instanceClient.DreamDaemon.Update(new DreamDaemon
			{
				SoftRestart = true
			}, cancellationToken);

			await Task.Delay(6000, cancellationToken);
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

			// Compile job isn't loaded until after the job completes
			await Task.Delay(TimeSpan.FromSeconds(3));

			return await instanceClient.DreamDaemon.Read(cancellationToken);
		}

		async Task GracefulWatchdogShutdown(uint timeout, CancellationToken cancellationToken)
		{
			await instanceClient.DreamDaemon.Update(new DreamDaemon
			{
				SoftShutdown = true
			}, cancellationToken);

			var newStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.AreEqual(true, newStatus.SoftShutdown);

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
