using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Client.Components;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Tests.Instance
{
	sealed class DeploymentTest : JobsRequiredTest
	{
		readonly IDreamMakerClient dreamMakerClient;
		readonly IDreamDaemonClient dreamDaemonClient;

		public DeploymentTest(IDreamMakerClient dreamMakerClient, IDreamDaemonClient dreamDaemonClient, IJobsClient jobsClient) : base(jobsClient)
		{
			this.dreamMakerClient = dreamMakerClient ?? throw new ArgumentNullException(nameof(dreamMakerClient));
			this.dreamDaemonClient = dreamDaemonClient ?? throw new ArgumentNullException(nameof(dreamDaemonClient));
		}

		public async Task Run(Task repositoryTask, CancellationToken cancellationToken)
		{
			Assert.IsFalse(repositoryTask.IsCompleted);
			var deployJob = await dreamMakerClient.Compile(cancellationToken);
			deployJob = await WaitForJob(deployJob, 30, true, null, cancellationToken);
			Assert.IsTrue(deployJob.ErrorCode == ErrorCode.RepoCloning || deployJob.ErrorCode == ErrorCode.RepoMissing);

			var dmSettings = await dreamMakerClient.Read(cancellationToken);
			Assert.AreEqual(true, dmSettings.RequireDMApiValidation);
			Assert.AreEqual(null, dmSettings.ProjectName);

			await repositoryTask;

			// by alphabetization rules, it should discover api_free here
			if (!new PlatformIdentifier().IsWindows)
				await dreamMakerClient.Update(new DreamMaker
				{
					ProjectName = "tests/DMAPI/ApiFree/api_free"
				}, cancellationToken);

			var updatedDD = await dreamDaemonClient.Update(new DreamDaemon
			{
				StartupTimeout = 5
			}, cancellationToken);
			Assert.AreEqual(5U, updatedDD.StartupTimeout);
			deployJob = await dreamMakerClient.Compile(cancellationToken);
			await WaitForJob(deployJob, 30, true, ErrorCode.DreamMakerNeverValidated, cancellationToken);

			const string FailProject = "tests/DMAPI/BuildFail/build_fail";
			var updated = await dreamMakerClient.Update(new DreamMaker
			{
				ProjectName = FailProject
			}, cancellationToken);

			Assert.AreEqual(FailProject, updated.ProjectName);

			deployJob = await dreamMakerClient.Compile(cancellationToken);
			await WaitForJob(deployJob, 30, true, ErrorCode.DreamMakerExitCode, cancellationToken);

			await dreamMakerClient.Update(new DreamMaker
			{
				ProjectName = "tests/DMAPI/ThisDoesntExist/this_doesnt_exist"
			}, cancellationToken);

			deployJob = await dreamMakerClient.Compile(cancellationToken);
			await WaitForJob(deployJob, 30, true, ErrorCode.DreamMakerMissingDme, cancellationToken);
		}
	}
}
