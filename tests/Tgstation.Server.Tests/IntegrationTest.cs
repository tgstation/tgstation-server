using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Client;
using Tgstation.Server.Host;

namespace Tgstation.Server.Tests
{
	[TestClass]
	public sealed class IntegrationTest
	{
		readonly IServerClientFactory clientFactory = new ServerClientFactory(new ProductHeaderValue(Assembly.GetExecutingAssembly().GetName().Name, Assembly.GetExecutingAssembly().GetName().Version.ToString()));
		
		[TestMethod]
		public async Task FullMonty()
		{
			using (var server = new TestingServer())
			using (var serverCts = new CancellationTokenSource())
			{
				var serverTask = server.RunAsync(serverCts.Token);
				try
				{
					using (var adminClient = await clientFactory.CreateServerClient(server.Url, User.AdminName, User.DefaultAdminPassword).ConfigureAwait(false))
					{
						var serverInfo = await adminClient.Version(default).ConfigureAwait(false);

						Assert.AreEqual(ApiHeaders.Version, serverInfo.ApiVersion);
						Assert.AreEqual(typeof(IServer).Assembly.GetName().Version, serverInfo.Version);

						await new AdministrationTest(adminClient.Administration).Run().ConfigureAwait(false);
					}
				}
				finally
				{
					serverCts.Cancel();
					try
					{
						await serverTask.ConfigureAwait(false);
					}
					catch (OperationCanceledException) { }
				}
			}
		}
	}
}
