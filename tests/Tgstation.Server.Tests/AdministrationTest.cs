using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
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
			Administration model;
			try
			{
				model = await client.Read(cancellationToken).ConfigureAwait(false);
			}
			catch (RateLimitException)
			{
				Assert.Inconclusive("GitHub rate limit hit while testing administration endpoint. Set environment variable TGS4_TEST_GITHUB_TOKEN to fix this!");
				return;	//c# needs the equivalent of [noreturn]
			}
			Assert.AreEqual(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), model.WindowsHost);

			//we've released a few 4.x versions now, check the release checker is at least somewhat functional
			Assert.AreEqual(4, model.LatestVersion.Major);
		}
	}
}
