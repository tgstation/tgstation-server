using System;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Client;
using Tgstation.Server.Client.Components;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Tests.Live.Instance
{
	sealed class InstanceTest
	{
		readonly IInstanceClient instanceClient;
		readonly IInstanceManagerClient instanceManagerClient;
		readonly IFileDownloader fileDownloader;
		readonly InstanceManager instanceManager;
		readonly ushort serverPort;

		public InstanceTest(IInstanceClient instanceClient, IInstanceManagerClient instanceManagerClient, IFileDownloader fileDownloader, InstanceManager instanceManager, ushort serverPort)
		{
			this.instanceClient = instanceClient ?? throw new ArgumentNullException(nameof(instanceClient));
			this.instanceManagerClient = instanceManagerClient ?? throw new ArgumentNullException(nameof(instanceManagerClient));
			this.fileDownloader = fileDownloader ?? throw new ArgumentNullException(nameof(fileDownloader));
			this.instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
			this.serverPort = serverPort;
		}

		public async Task RunTests(CancellationToken cancellationToken, bool highPrioDD, bool lowPrioDeployment)
		{
			var byondTest = new ByondTest(instanceClient.Byond, instanceClient.Jobs, fileDownloader, instanceClient.Metadata);
			var chatTest = new ChatTest(instanceClient.ChatBots, instanceManagerClient, instanceClient.Jobs, instanceClient.Metadata);
			var configTest = new ConfigurationTest(instanceClient.Configuration, instanceClient.Metadata);
			var repoTest = new RepositoryTest(instanceClient.Repository, instanceClient.Jobs);
			var dmTest = new DeploymentTest(instanceClient, instanceClient.Jobs, lowPrioDeployment);

			var byondTask = byondTest.Run(cancellationToken, out var firstInstall);
			var chatTask = chatTest.RunPreWatchdog(cancellationToken);

			var repoLongJob = await repoTest.RunLongClone(cancellationToken);

			await dmTest.RunPreRepoClone(cancellationToken);
			await repoTest.AbortLongCloneAndCloneSomethingQuick(repoLongJob, cancellationToken);
			await configTest.RunPreWatchdog(cancellationToken);
			var dmTask = dmTest.RunPostRepoClone(firstInstall, cancellationToken);

			await chatTask;
			await dmTask;
			await byondTask;

			await new WatchdogTest(instanceClient, instanceManager, serverPort, highPrioDD).Run(cancellationToken);
		}
	}
}
