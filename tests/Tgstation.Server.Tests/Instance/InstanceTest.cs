using System;
using System.Collections.Generic;
using System.Text;
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
			var configTests = new ConfigurationTest(instanceClient.Configuration, instanceClient.Metadata);
			await configTests.Run(cancellationToken).ConfigureAwait(false);
		}
	}
}
