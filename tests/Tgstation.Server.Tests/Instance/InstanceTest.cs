using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Client.Components;

namespace Tgstation.Server.Tests.Instance
{
	sealed class InstanceTest
	{
		readonly IInstanceClient instanceClient;

		public InstanceTest(IInstanceClient instanceClient)
		{
			this.instanceClient = instanceClient ?? throw new ArgumentNullException(nameof(instanceClient));
		}

		public async Task RunTests(CancellationToken cancellationToken)
		{
			var byondTests = new ByondTest(instanceClient.Byond, instanceClient.Jobs);
			var configTests = new ConfigurationTest(instanceClient.Configuration, instanceClient.Metadata);

			var byondTest = byondTests.Run(cancellationToken);
			await configTests.Run(cancellationToken).ConfigureAwait(false);
			await byondTest.ConfigureAwait(false);
		}
	}
}
