using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Client;
using Tgstation.Server.Client.Components;
using Tgstation.Server.Host.System;
using Tgstation.Server.Tests.Live;

namespace Tgstation.Server.Tests.Live.Instance
{
	sealed class DeploymentTest : JobsRequiredTest
	{
		readonly IDreamMakerClient dreamMakerClient;
		readonly IDreamDaemonClient dreamDaemonClient;
		readonly IInstanceClient instanceClient;

		readonly bool lowPriorityDeployments;

		Task vpTest;

		public DeploymentTest(IInstanceClient instanceClient, IJobsClient jobsClient, bool lowPriorityDeployments) : base(jobsClient)
		{
			this.instanceClient = instanceClient ?? throw new ArgumentNullException(nameof(instanceClient));
			dreamMakerClient = instanceClient.DreamMaker;
			dreamDaemonClient = instanceClient.DreamDaemon;
			this.lowPriorityDeployments = lowPriorityDeployments;
		}

		public async Task RunPreRepoClone(CancellationToken cancellationToken)
		{
			Assert.IsNull(vpTest);
			vpTest = TestVisibilityPermission(cancellationToken);
			var deployJob = await dreamMakerClient.Compile(cancellationToken);
			var deploymentJobWaitTask = WaitForJob(deployJob, 30, true, null, cancellationToken);
			await CheckDreamDaemonPriority(deploymentJobWaitTask, cancellationToken);
			deployJob = await deploymentJobWaitTask;
			Assert.IsTrue(deployJob.ErrorCode == ErrorCode.RepoCloning || deployJob.ErrorCode == ErrorCode.RepoMissing);

			var dmSettings = await dreamMakerClient.Read(cancellationToken);
			Assert.AreEqual(true, dmSettings.RequireDMApiValidation);
			Assert.AreEqual(null, dmSettings.ProjectName);
		}

		async Task CheckDreamDaemonPriority(Task deploymentJobWaitTask, CancellationToken cancellationToken)
		{
			// this doesn't check dm's priority, but it really should

			while (!deploymentJobWaitTask.IsCompleted)
			{
				var ddProcessName = new PlatformIdentifier().IsWindows && ByondTest.TestVersion >= new Version(515, 1598)
					? "dd"
					: "DreamDaemon";

				var allProcesses = System.Diagnostics.Process.GetProcessesByName(ddProcessName);
				if (allProcesses.Length == 0)
					continue;

				if (allProcesses.Length > 1)
					Assert.Fail("Multiple DreamDaemon-like processes running!");

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
			if (!new PlatformIdentifier().IsWindows)
			{
				var updatedDM = await dreamMakerClient.Update(new DreamMakerRequest
				{
					ProjectName = "tests/DMAPI/ApiFree/api_free",
					ApiValidationPort = TestLiveServer.DMPort
				}, cancellationToken);
				Assert.AreEqual(TestLiveServer.DMPort, updatedDM.ApiValidationPort);
				Assert.AreEqual("tests/DMAPI/ApiFree/api_free", updatedDM.ProjectName);
			}
			else
			{
				var updatedDM = await dreamMakerClient.Update(new DreamMakerRequest
				{
					ApiValidationPort = TestLiveServer.DMPort
				}, cancellationToken);
				Assert.AreEqual(TestLiveServer.DMPort, updatedDM.ApiValidationPort);
			}

			var updatedDD = await dreamDaemonClient.Update(new DreamDaemonRequest
			{
				StartupTimeout = 15,
				Port = TestLiveServer.DDPort
			}, cancellationToken);
			Assert.AreEqual(15U, updatedDD.StartupTimeout);
			Assert.AreEqual(TestLiveServer.DDPort, updatedDD.Port);

			async Task<JobResponse> CompileAfterByondInstall()
			{
				await byondTask;
				return await dreamMakerClient.Compile(cancellationToken);
			}

			var deployJobTask = CompileAfterByondInstall();
			var deployJob = await deployJobTask;
			var deploymentJobWaitTask = WaitForJob(deployJob, 40, true, ErrorCode.DreamMakerNeverValidated, cancellationToken);

			await CheckDreamDaemonPriority(deploymentJobWaitTask, cancellationToken);

			await Task.WhenAll(
				ApiAssert.ThrowsException<ConflictException>(() => dreamDaemonClient.Update(new DreamDaemonRequest
				{
					Port = TestLiveServer.DMPort
				}, cancellationToken), ErrorCode.PortNotAvailable),
				ApiAssert.ThrowsException<ConflictException>(() => dreamMakerClient.Update(new DreamMakerRequest
				{
					ApiValidationPort = TestLiveServer.DDPort
				}, cancellationToken), ErrorCode.PortNotAvailable),
				deploymentJobWaitTask);

			const string FailProject = "tests/DMAPI/BuildFail/build_fail";
			var updated = await dreamMakerClient.Update(new DreamMakerRequest
			{
				ProjectName = FailProject
			}, cancellationToken);

			Assert.AreEqual(FailProject, updated.ProjectName);

			deployJob = await dreamMakerClient.Compile(cancellationToken);
			await WaitForJob(deployJob, 40, true, ErrorCode.DreamMakerExitCode, cancellationToken);

			await dreamMakerClient.Update(new DreamMakerRequest
			{
				ProjectName = "tests/DMAPI/ThisDoesntExist/this_doesnt_exist"
			}, cancellationToken);

			deployJob = await dreamMakerClient.Compile(cancellationToken);
			await WaitForJob(deployJob, 40, true, ErrorCode.DreamMakerMissingDme, cancellationToken);

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

			await ApiAssert.ThrowsException<InsufficientPermissionsException>(() => dreamDaemonClient.Update(new DreamDaemonRequest
			{
				Visibility = DreamDaemonVisibility.Private
			}, cancellationToken), null);

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
