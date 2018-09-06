using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Client;

namespace Tgstation.Server.Tests
{
	sealed class AdministrationTest
	{
		readonly IAdministrationClient client;

		public AdministrationTest(IAdministrationClient client)
		{
			this.client = client ?? throw new ArgumentNullException(nameof(client));
		}

		public async Task Run(CancellationToken cancellationToken)
		{
			await TestRead(cancellationToken).ConfigureAwait(false);
		}

		async Task TestRead(CancellationToken cancellationToken)
		{
			var model = await client.Read(cancellationToken).ConfigureAwait(false);
			Assert.AreEqual(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), model.WindowsHost);

			//uhh not much else to do
		}
	}
}
