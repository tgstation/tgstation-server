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
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Tests.Instance
{
	sealed class FunctionalTest : JobsRequiredTest
	{
		readonly IInstanceClient instanceClient;

		public FunctionalTest(IInstanceClient instanceClient)
			: base(instanceClient.Jobs)
		{
			this.instanceClient = instanceClient ?? throw new ArgumentNullException(nameof(instanceClient));
		}

		public async Task Run(CancellationToken cancellationToken)
		{
			await RunBasicTest(cancellationToken);
			await RunLongRunningTestThenUpdate(cancellationToken);
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

			await SendCommandHack("reboot", false, cancellationToken);

			await Task.Delay(10000, cancellationToken);

			daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.AreNotEqual(initialCompileJob.Id, daemonStatus.ActiveCompileJob.Id);
			Assert.AreEqual(daemonStatus.ActiveCompileJob.Id, daemonStatus.StagedCompileJob.Id);

			await instanceClient.DreamDaemon.Shutdown(cancellationToken);

			daemonStatus = await instanceClient.DreamDaemon.Read(cancellationToken);
			Assert.IsFalse(daemonStatus.Running.Value);
		}

		async Task SendCommandHack(string message, bool useTgsGlobal, CancellationToken cancellationToken)
		{
			// tricky part, we need to get tgs to reboot.
			// We have a chat command to do this
			// But oh god i should be drawn and quartered for how i'm invoking it here
			// this assumes a purely open channel stored in the TGS4_TEST_IRC_CONNECTION_STRING

			var connectionString = Environment.GetEnvironmentVariable("TGS4_TEST_IRC_CONNECTION_STRING");
			var builder = new IrcConnectionStringBuilder(connectionString);

			using var provider = new IrcProvider(
				new AssemblyInformationProvider(),
				new AsyncDelayer(),
				Mock.Of<ILogger<IrcProvider>>(),
				builder.Address,
				builder.Port.Value,
				builder.Nickname + "_hack_other",
				null,
				null,
				10,
				builder.UseSsl.Value);

			await provider.Connect(cancellationToken);

			var channels = await provider.MapChannels(
				new List<ChatChannel>
				{
					new ChatChannel
					{
						IrcChannel = Environment.GetEnvironmentVariable("TGS4_IRC_CHAT_CHANNEL"),
						Tag = "ohgodohfuck",
						IsAdminChannel = false,
						IsUpdatesChannel = false,
						IsWatchdogChannel = false
					}
				},
				cancellationToken);

			await provider.SendMessage(channels.First().RealId, $"!{(useTgsGlobal ? "tgs" : builder.Nickname)} {message}", cancellationToken);
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
