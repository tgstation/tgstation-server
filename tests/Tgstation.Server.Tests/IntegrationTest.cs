using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
		[TestCategory("SkipWhenLiveUnitTesting")]
		public async Task TestUpdate()
		{
			var updatePathRoot = Path.GetTempFileName();
			File.Delete(updatePathRoot);
			Directory.CreateDirectory(updatePathRoot);
			try
			{
				var updatePath = Path.Combine(updatePathRoot, Guid.NewGuid().ToString());
				var server = new TestingServer(updatePath);
				using (var serverCts = new CancellationTokenSource())
				{
					var cancellationToken = serverCts.Token;
					var serverTask = server.RunAsync(cancellationToken);
					try
					{
						IServerClient adminClient;

						var giveUpAt = DateTimeOffset.Now.AddSeconds(60);
						do
						{
							try
							{
								adminClient = await clientFactory.CreateServerClient(server.Url, User.AdminName, User.DefaultAdminPassword).ConfigureAwait(false);
								break;
							}
							catch (ServiceUnavailableException)
							{
								//migrating, to be expected
								if (DateTimeOffset.Now > giveUpAt)
									throw;
								await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
							}
						} while (true);

						var testUpdateVersion = new Version(4, 0, 0, 6);
						using (adminClient)
							//attempt to update to stable
							await adminClient.Administration.Update(new Administration
							{
								NewVersion = testUpdateVersion
							}, cancellationToken).ConfigureAwait(false);

						//wait up to 3 minutes for the dl and install
						await Task.WhenAny(serverTask, Task.Delay(TimeSpan.FromMinutes(3), cancellationToken)).ConfigureAwait(false);

						Assert.IsTrue(serverTask.IsCompleted, "Sever still running!");

						Assert.IsTrue(Directory.Exists(updatePath), "Update directory not present!");

						var updatedAssemblyPath = Path.Combine(updatePath, "Tgstation.Server.Host.dll");
						Assert.IsTrue(File.Exists(updatedAssemblyPath), "Updated assembly missing!");

						var updatedAssemblyVersion = FileVersionInfo.GetVersionInfo(updatedAssemblyPath);
						Assert.AreEqual(testUpdateVersion, Version.Parse(updatedAssemblyVersion.FileVersion));
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
					Assert.IsTrue(server.RestartRequested, "Server not requesting restart!");
				}
			}
			finally
			{
				Directory.Delete(updatePathRoot, true);
			}
		}

		[TestMethod]
		[TestCategory("SkipWhenLiveUnitTesting")]
		public async Task TestStandardOperation()
		{
			var server = new TestingServer(null);
			using (var serverCts = new CancellationTokenSource())
			{
				var cancellationToken = serverCts.Token;
				var serverTask = server.RunAsync(cancellationToken);
				try
				{
					IServerClient adminClient;

					var giveUpAt = DateTimeOffset.Now.AddSeconds(60);
					do
					{
						try
						{
							adminClient = await clientFactory.CreateServerClient(server.Url, User.AdminName, User.DefaultAdminPassword).ConfigureAwait(false);
							break;
						}
						catch (ServiceUnavailableException)
						{
							// migrating, to be expected
							if (DateTimeOffset.Now > giveUpAt)
								throw;
							await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
						}
					} while (true);

					using (adminClient)
					{
						var serverInfo = await adminClient.Version(default).ConfigureAwait(false);

						Assert.AreEqual(ApiHeaders.Version, serverInfo.ApiVersion);
						Assert.AreEqual(typeof(IServer).Assembly.GetName().Version, serverInfo.Version);

						//check that modifying the token even slightly fucks up the auth
						var newToken = new Token
						{
							ExpiresAt = adminClient.Token.ExpiresAt,
							Bearer = adminClient.Token.Bearer + '0'
						};

						var badClient = clientFactory.CreateServerClient(server.Url, newToken);
						await Assert.ThrowsExceptionAsync<UnauthorizedException>(() => badClient.Version(cancellationToken)).ConfigureAwait(false);

						await new AdministrationTest(adminClient.Administration).Run(cancellationToken).ConfigureAwait(false);
						await new InstanceManagerTest(adminClient.Instances, server.Directory).Run(cancellationToken).ConfigureAwait(false);
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
