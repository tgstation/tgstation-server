using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Client;
using Tgstation.Server.Client.Components;
using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Tests.Live.Instance
{
	sealed class DeploymentTest : JobsRequiredTest
	{
		readonly IDreamMakerClient dreamMakerClient;
		readonly IDreamDaemonClient dreamDaemonClient;
		readonly IInstanceClient instanceClient;

		readonly ushort dmPort;
		readonly ushort ddPort;
		readonly bool lowPriorityDeployments;
		readonly EngineVersion testEngine;

		Task vpTest;

		public DeploymentTest(
			IInstanceClient instanceClient,
			IJobsClient jobsClient,
			ushort dmPort,
			ushort ddPort,
			bool lowPriorityDeployments,
			EngineVersion testEngine) : base(jobsClient)
		{
			this.instanceClient = instanceClient ?? throw new ArgumentNullException(nameof(instanceClient));
			dreamMakerClient = instanceClient.DreamMaker;
			dreamDaemonClient = instanceClient.DreamDaemon;
			this.dmPort = dmPort;
			this.ddPort = ddPort;
			this.lowPriorityDeployments = lowPriorityDeployments;
			this.testEngine = testEngine;
		}

		public async ValueTask RunPreRepoClone(CancellationToken cancellationToken)
		{
			Assert.IsNull(vpTest);
			vpTest = TestVisibilityPermission(cancellationToken);
			var deployJob = await dreamMakerClient.Compile(cancellationToken);
			var deploymentJobWaitTask = WaitForJob(deployJob, 30, true, null, cancellationToken);
			await CheckDreamDaemonPriority(deploymentJobWaitTask, cancellationToken);
			deployJob = await deploymentJobWaitTask;
			Assert.IsTrue(deployJob.ErrorCode == ErrorCode.RepoCloning || deployJob.ErrorCode == ErrorCode.RepoMissing);

			var dmSettings = await dreamMakerClient.Read(cancellationToken);
#pragma warning disable CS0618 // Type or member is obsolete
			Assert.AreEqual(true, dmSettings.RequireDMApiValidation);
#pragma warning restore CS0618 // Type or member is obsolete
			Assert.AreEqual(DMApiValidationMode.Required, dmSettings.DMApiValidationMode);
			Assert.AreEqual(null, dmSettings.ProjectName);

			// test legacy back and forth
			dmSettings = await dreamMakerClient.Update(new DreamMakerRequest
			{
				DMApiValidationMode = DMApiValidationMode.Optional,
			}, cancellationToken);
			Assert.AreEqual(DMApiValidationMode.Optional, dmSettings.DMApiValidationMode);
#pragma warning disable CS0618 // Type or member is obsolete
			Assert.AreEqual(false, dmSettings.RequireDMApiValidation);
#pragma warning restore CS0618 // Type or member is obsolete

			dmSettings = await dreamMakerClient.Update(new DreamMakerRequest
			{
				DMApiValidationMode = DMApiValidationMode.Skipped,
			}, cancellationToken);
			Assert.AreEqual(DMApiValidationMode.Skipped, dmSettings.DMApiValidationMode);
#pragma warning disable CS0618 // Type or member is obsolete
			Assert.AreEqual(false, dmSettings.RequireDMApiValidation);
#pragma warning restore CS0618 // Type or member is obsolete

			dmSettings = await dreamMakerClient.Update(new DreamMakerRequest
			{
				DMApiValidationMode = DMApiValidationMode.Required,
			}, cancellationToken);
			Assert.AreEqual(DMApiValidationMode.Required, dmSettings.DMApiValidationMode);
#pragma warning disable CS0618 // Type or member is obsolete
			Assert.AreEqual(true, dmSettings.RequireDMApiValidation);
#pragma warning restore CS0618 // Type or member is obsolete

			dmSettings = await dreamMakerClient.Update(new DreamMakerRequest
			{
#pragma warning disable CS0618 // Type or member is obsolete
				RequireDMApiValidation = false,
#pragma warning restore CS0618 // Type or member is obsolete
			}, cancellationToken);
			Assert.AreEqual(DMApiValidationMode.Optional, dmSettings.DMApiValidationMode);
#pragma warning disable CS0618 // Type or member is obsolete
			Assert.AreEqual(false, dmSettings.RequireDMApiValidation);
#pragma warning restore CS0618 // Type or member is obsolete

			dmSettings = await dreamMakerClient.Update(new DreamMakerRequest
			{
#pragma warning disable CS0618 // Type or member is obsolete
				RequireDMApiValidation = true,
#pragma warning restore CS0618 // Type or member is obsolete
			}, cancellationToken);
			Assert.AreEqual(DMApiValidationMode.Required, dmSettings.DMApiValidationMode);
#pragma warning disable CS0618 // Type or member is obsolete
			Assert.AreEqual(true, dmSettings.RequireDMApiValidation);
#pragma warning restore CS0618 // Type or member is obsolete

			await ApiAssert.ThrowsExactly<ApiConflictException, DreamMakerResponse>(() => dreamMakerClient.Update(new DreamMakerRequest
			{
#pragma warning disable CS0618 // Type or member is obsolete
				RequireDMApiValidation = true,
				DMApiValidationMode = DMApiValidationMode.Required,
#pragma warning restore CS0618 // Type or member is obsolete
			}, cancellationToken), ErrorCode.ModelValidationFailure);
		}

		async ValueTask CheckDreamDaemonPriority(Task deploymentJobWaitTask, CancellationToken cancellationToken)
		{
			// this doesn't check dm's priority, but it really should
			while (!deploymentJobWaitTask.IsCompleted)
			{
				var allProcesses = TestLiveServer.GetEngineServerProcessesOnPort(testEngine.Engine.Value, dmPort);
				if (allProcesses.Count == 0)
					continue;

				if (allProcesses.Count > 1)
					Assert.Fail("Multiple engine-like processes running!");

				using var process = allProcesses[0];

				int processId;
				try
				{
					processId = process.Id;
				}
				catch
				{
					return; // vOv
				}

				bool good = false;
				while (!process.HasExited)
				{
					// we need to constantly reacquire the handle to invalidate caches
					using var localProcess = System.Diagnostics.Process.GetProcessById(processId);
					if (lowPriorityDeployments)
					{
						if (localProcess.PriorityClass == System.Diagnostics.ProcessPriorityClass.BelowNormal)
						{
							good = true;
							break;
						}
						else
							Assert.AreEqual(System.Diagnostics.ProcessPriorityClass.Normal, localProcess.PriorityClass);
					}
					else
					{
						good = true;
						Assert.AreEqual(System.Diagnostics.ProcessPriorityClass.Normal, localProcess.PriorityClass, "DreamDaemon's process priority changed when it shouldn't have!");
						await Task.Delay(1, cancellationToken);
					}
				}

				if (!good)
					Assert.Fail("Did not detect DreamDaemon lowering its process priority!");

				break;
			}
		}

		public async Task RunPostRepoClone(Task byondTask, CancellationToken cancellationToken)
		{
			Assert.IsNotNull(vpTest);
			// by alphabetization rules, it should discover api_free here
			Console.WriteLine($"PORT REUSE BUG 5: Setting I-{instanceClient.Metadata.Id} DM to {dmPort}");
			if (!new PlatformIdentifier().IsWindows)
			{
				var updatedDM = await dreamMakerClient.Update(new DreamMakerRequest
				{
					ProjectName = "tests/DMAPI/ApiFree/api_free",
					ApiValidationPort = dmPort,
					CompilerAdditionalArguments = "   ",
				}, cancellationToken);
				Assert.AreEqual(dmPort, updatedDM.ApiValidationPort);
				Assert.AreEqual("tests/DMAPI/ApiFree/api_free", updatedDM.ProjectName);
				Assert.IsNull(updatedDM.CompilerAdditionalArguments);
			}
			else
			{
				var canUseDashD = testEngine.Engine == EngineType.Byond && testEngine.Version >= new Version(515, 1597);
				var updatedDM = await dreamMakerClient.Update(new DreamMakerRequest
				{
					ApiValidationPort = dmPort,
					CompilerAdditionalArguments = canUseDashD ? " -DBABABOOEY" : "     ",
				}, cancellationToken);
				Assert.AreEqual(dmPort, updatedDM.ApiValidationPort);
				if (canUseDashD)
					Assert.AreEqual("-DBABABOOEY", updatedDM.CompilerAdditionalArguments);
				else
					Assert.IsNull(updatedDM.CompilerAdditionalArguments);
			}

			Console.WriteLine($"PORT REUSE BUG 1: Setting I-{instanceClient.Metadata.Id} DD to {ddPort}");
			var updatedDD = await dreamDaemonClient.Update(new DreamDaemonRequest
			{
				StartupTimeout = 60,
				Port = ddPort
			}, cancellationToken);
			Assert.AreEqual(60U, updatedDD.StartupTimeout);
			Assert.AreEqual(ddPort, updatedDD.Port);

			async Task<JobResponse> CompileAfterByondInstall()
			{
				await byondTask;
				return await dreamMakerClient.Compile(cancellationToken);
			}

			var deployJobTask = CompileAfterByondInstall();
			var deployJob = await deployJobTask;
			var deploymentJobWaitTask = WaitForJob(deployJob, 120, true, ErrorCode.DeploymentNeverValidated, cancellationToken);

			await CheckDreamDaemonPriority(deploymentJobWaitTask, cancellationToken);

			Console.WriteLine($"PORT REUSE BUG 2: Expect Conflict, Setting I-{instanceClient.Metadata.Id} DD to {dmPort}");
			var t1 = ApiAssert.ThrowsExactly<ConflictException, DreamDaemonResponse>(() => dreamDaemonClient.Update(new DreamDaemonRequest
			{
				Port = dmPort,
			}, cancellationToken), ErrorCode.PortNotAvailable);
			Console.WriteLine($"PORT REUSE BUG 3: Expect Conflict, Setting I-{instanceClient.Metadata.Id} DM to {ddPort}");
			var t2 = ApiAssert.ThrowsExactly<ConflictException, DreamMakerResponse>(() => dreamMakerClient.Update(new DreamMakerRequest
			{
				ApiValidationPort = ddPort
			}, cancellationToken), ErrorCode.PortNotAvailable);
			await ValueTaskExtensions.WhenAll(t1, t2);

			await deploymentJobWaitTask;

			const string FailProject = "tests/DMAPI/BuildFail/build_fail";
			var updated = await dreamMakerClient.Update(new DreamMakerRequest
			{
				ProjectName = FailProject
			}, cancellationToken);

			Assert.AreEqual(FailProject, updated.ProjectName);

			deployJob = await dreamMakerClient.Compile(cancellationToken);
			await WaitForJob(deployJob, 40, true, ErrorCode.DeploymentExitCode, cancellationToken);

			await dreamMakerClient.Update(new DreamMakerRequest
			{
				ProjectName = "tests/DMAPI/ThisDoesntExist/this_doesnt_exist"
			}, cancellationToken);

			deployJob = await dreamMakerClient.Compile(cancellationToken);
			await WaitForJob(deployJob, 40, true, ErrorCode.DeploymentMissingDme, cancellationToken);

			// set to an absolute path that does exist
			var tempFile = Path.GetTempFileName().Replace('\\', '/');
			try
			{
				// for testing purposes, assume same drive for windows
				var relativePath = $"../../{String.Join("/", instanceClient.Metadata.Path.Replace('\\', '/').Where(pathChar => pathChar == '/').Select(x => ".."))}{tempFile.Substring(tempFile.IndexOf('/'))}";
				var dmePath = $"{tempFile}.dme";
				File.Move(tempFile, dmePath);
				tempFile = dmePath;
				await dreamMakerClient.Update(new DreamMakerRequest
				{
					ProjectName = relativePath
				}, cancellationToken);
				deployJob = await dreamMakerClient.Compile(cancellationToken);
				await WaitForJob(deployJob, 40, true, ErrorCode.DeploymentWrongDme, cancellationToken);
			}
			finally
			{
				File.Delete(tempFile);
			}

			// check that we can change the visibility

			await vpTest;
		}

		async Task TestVisibilityPermission(CancellationToken cancellationToken)
		{
			var updatedDD = await dreamDaemonClient.Read(cancellationToken);
			Assert.AreEqual(DreamDaemonVisibility.Public, updatedDD.Visibility);
			updatedDD = await dreamDaemonClient.Update(new DreamDaemonRequest
			{
				Visibility = DreamDaemonVisibility.Invisible
			}, cancellationToken);
			Assert.AreEqual(DreamDaemonVisibility.Invisible, updatedDD.Visibility);

			var currentPermissionSet = await instanceClient.PermissionSets.Read(cancellationToken);
			Assert.IsTrue((currentPermissionSet.DreamDaemonRights.Value & DreamDaemonRights.SetVisibility) != 0);
			var updatedPS = await instanceClient.PermissionSets.Update(new InstancePermissionSetRequest
			{
				PermissionSetId = currentPermissionSet.PermissionSetId,
				DreamDaemonRights = currentPermissionSet.DreamDaemonRights.Value & ~DreamDaemonRights.SetVisibility
			}, cancellationToken);
			Assert.IsFalse((updatedPS.DreamDaemonRights.Value & DreamDaemonRights.SetVisibility) != 0);

			await ApiAssert.ThrowsExactly<InsufficientPermissionsException, DreamDaemonResponse>(() => dreamDaemonClient.Update(new DreamDaemonRequest
			{
				Visibility = DreamDaemonVisibility.Private
			}, cancellationToken));

			updatedPS = await instanceClient.PermissionSets.Update(new InstancePermissionSetRequest
			{
				PermissionSetId = updatedPS.PermissionSetId,
				DreamDaemonRights = updatedPS.DreamDaemonRights.Value | DreamDaemonRights.SetVisibility
			}, cancellationToken);
			Assert.IsTrue((updatedPS.DreamDaemonRights.Value & DreamDaemonRights.SetVisibility) != 0);
			updatedDD = await dreamDaemonClient.Update(new DreamDaemonRequest
			{
				Visibility = DreamDaemonVisibility.Public
			}, cancellationToken);
			Assert.AreEqual(DreamDaemonVisibility.Public, updatedDD.Visibility);
		}
	}
}
