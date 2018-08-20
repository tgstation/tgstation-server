using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Runtime.InteropServices;
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

		public async Task Run()
		{
			await TestRead().ConfigureAwait(false);
		}

		async Task TestRead()
		{
			var model = await client.Read(default).ConfigureAwait(false);
			Assert.AreEqual(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), model.WindowsHost);

			//uhh not much else to do
		}
	}
}
