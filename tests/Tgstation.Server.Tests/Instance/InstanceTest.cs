using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Client;
using Tgstation.Server.Client.Components;

namespace Tgstation.Server.Tests.Instance
{
	sealed class InstanceTest
	{
		readonly IInstanceClient instanceClient;
		readonly IInstanceManagerClient instanceManagerClient;

		public InstanceTest(IInstanceClient instanceClient, IInstanceManagerClient instanceManagerClient)
		{
			this.instanceClient = instanceClient ?? throw new ArgumentNullException(nameof(instanceClient));
			this.instanceManagerClient = instanceManagerClient ?? throw new ArgumentNullException(nameof(instanceManagerClient));
		}

		public async Task RunTests(CancellationToken cancellationToken)
		{
			var byondTest = new ByondTest(instanceClient.Byond, instanceClient.Jobs, instanceClient.Metadata);
			var chatTest = new ChatTest(instanceClient.ChatBots, instanceManagerClient, instanceClient.Metadata.CloneMetadata());
			var configTest = new ConfigurationTest(instanceClient.Configuration, instanceClient.Metadata);
			var repoTest = new RepositoryTest(instanceClient.Repository, instanceClient.Jobs);

			var repoTests = repoTest.RunPreWatchdog(cancellationToken);
			var byondTests = byondTest.Run(cancellationToken);
			var chatTests = chatTest.RunPreWatchdog(cancellationToken);
			await configTest.Run(cancellationToken).ConfigureAwait(false);
			await byondTests.ConfigureAwait(false);
			await chatTests.ConfigureAwait(false);
			await repoTests;

			// await new WatchdogTest(instanceClient).Run(cancellationToken);

			var chatShutdown = chatTest.RunPostWatchdog(cancellationToken);
			await repoTest.RunPostWatchdog(cancellationToken);
			await chatShutdown;
		}
	}
}
