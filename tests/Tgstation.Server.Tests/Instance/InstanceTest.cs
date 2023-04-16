using System;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Client;
using Tgstation.Server.Client.Components;
using Tgstation.Server.Host.Components;

namespace Tgstation.Server.Tests.Instance
{
	sealed class InstanceTest
	{
		readonly IInstanceClient instanceClient;
		readonly IInstanceManagerClient instanceManagerClient;
		readonly InstanceManager instanceManager;
		readonly ushort serverPort;

		public InstanceTest(IInstanceClient instanceClient, IInstanceManagerClient instanceManagerClient, InstanceManager instanceManager, ushort serverPort)
		{
			this.instanceClient = instanceClient ?? throw new ArgumentNullException(nameof(instanceClient));
			this.instanceManagerClient = instanceManagerClient ?? throw new ArgumentNullException(nameof(instanceManagerClient));
			this.instanceManager = instanceManager ?? throw new ArgumentNullException(nameof(instanceManager));
			this.serverPort = serverPort;
		}

		public async Task RunTests(CancellationToken cancellationToken)
		{
			var byondTest = new ByondTest(instanceClient.Byond, instanceClient.Jobs, instanceClient.Metadata);
			var chatTest = new ChatTest(instanceClient.ChatBots, instanceManagerClient, instanceClient.Metadata);
			var configTest = new ConfigurationTest(instanceClient.Configuration, instanceClient.Metadata);
			var repoTest = new RepositoryTest(instanceClient.Repository, instanceClient.Jobs);
			var dmTest = new DeploymentTest(instanceClient, instanceClient.Jobs);

			var byondTask = byondTest.Run(cancellationToken);
			var chatTask = chatTest.RunPreWatchdog(cancellationToken);

			var repoLongJob = repoTest.RunLongClone(cancellationToken);

			await dmTest.RunPreRepoClone(cancellationToken);
			await repoTest.AbortLongCloneAndCloneSomethingQuick(await repoLongJob, cancellationToken);
			await configTest.RunPreWatchdog(cancellationToken);
			var dmTask = dmTest.RunPostRepoClone(cancellationToken);

			await byondTask;
			await chatTask;
			await dmTask;

			await new WatchdogTest(instanceClient, instanceManager, serverPort).Run(cancellationToken);
		}
	}
}
