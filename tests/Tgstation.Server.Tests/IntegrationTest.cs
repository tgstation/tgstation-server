using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Client;
using Tgstation.Server.Host;
using Tgstation.Server.Host.Components.Chat.Providers;
using Tgstation.Server.Tests.Instance;

namespace Tgstation.Server.Tests
{
	[TestClass]
	[TestCategory("SkipWhenLiveUnitTesting")]
	public sealed class IntegrationTest
	{
		readonly IServerClientFactory clientFactory = new ServerClientFactory(new ProductHeaderValue(Assembly.GetExecutingAssembly().GetName().Name, Assembly.GetExecutingAssembly().GetName().Version.ToString()));

		[TestMethod]
		public async Task TestServerUpdate()
		{
			using var server = new TestingServer(clientFactory);

			if (server.DatabaseType == "Sqlite")
				Assert.Inconclusive("Cannot run this test on SQLite yet!");

			using var serverCts = new CancellationTokenSource();
			var cancellationToken = serverCts.Token;
			var serverTask = server.Run(cancellationToken);
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
					catch (HttpRequestException)
					{
						//migrating, to be expected
						if (DateTimeOffset.Now > giveUpAt)
							throw;
						await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
					}
					catch (ServiceUnavailableException)
					{
						//migrating, to be expected
						if (DateTimeOffset.Now > giveUpAt)
							throw;
						await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
					}
				} while (true);

				var testUpdateVersion = new Version(4, 1, 0);
				using (adminClient)
					//attempt to update to stable
					await adminClient.Administration.Update(new Administration
					{
						NewVersion = testUpdateVersion
					}, cancellationToken).ConfigureAwait(false);

				//wait up to 3 minutes for the dl and install
				await Task.WhenAny(serverTask, Task.Delay(TimeSpan.FromMinutes(3), cancellationToken)).ConfigureAwait(false);

				Assert.IsTrue(serverTask.IsCompleted, "Sever still running!");

				Assert.IsTrue(Directory.Exists(server.UpdatePath), "Update directory not present!");

				var updatedAssemblyPath = Path.Combine(server.UpdatePath, "Tgstation.Server.Host.dll");
				Assert.IsTrue(File.Exists(updatedAssemblyPath), "Updated assembly missing!");

				var updatedAssemblyVersion = FileVersionInfo.GetVersionInfo(updatedAssemblyPath);
				Assert.AreEqual(testUpdateVersion, Version.Parse(updatedAssemblyVersion.FileVersion).Semver());
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

		[TestMethod]
		public async Task TestFullStandardOperation()
		{
			using var server = new TestingServer(clientFactory);
			using var serverCts = new CancellationTokenSource();
			var cancellationToken = serverCts.Token;
			var serverTask = server.Run(cancellationToken);
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
					catch (HttpRequestException)
					{
						//migrating, to be expected
						if (DateTimeOffset.Now > giveUpAt)
							throw;
						await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
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
					var assemblyVersion = typeof(IServer).Assembly.GetName().Version.Semver();
					Assert.AreEqual(assemblyVersion, serverInfo.Version);
					Assert.AreEqual(10U, serverInfo.MinimumPasswordLength);
					Assert.AreEqual(11U, serverInfo.InstanceLimit);
					Assert.AreEqual(150U, serverInfo.UserLimit);

					//check that modifying the token even slightly fucks up the auth
					var newToken = new Token
					{
						ExpiresAt = adminClient.Token.ExpiresAt,
						Bearer = adminClient.Token.Bearer + '0'
					};

					var badClient = clientFactory.CreateServerClient(server.Url, newToken);
					await Assert.ThrowsExceptionAsync<UnauthorizedException>(() => badClient.Version(cancellationToken)).ConfigureAwait(false);

					var adminTest = new AdministrationTest(adminClient.Administration).Run(cancellationToken);
					var usersTest = new UsersTest(adminClient.Users).Run(cancellationToken);
					await new InstanceManagerTest(adminClient.Instances, adminClient.Users, server.Directory).Run(cancellationToken).ConfigureAwait(false);

					await adminTest.ConfigureAwait(false);
					await usersTest.ConfigureAwait(false);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"TEST ERROR: {ex.GetType()} in flight!");
				throw;
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

		[TestMethod]
		public async Task TestRebootAndAttach()
		{
			using var server = new TestingServer(clientFactory);
			using var serverCts = new CancellationTokenSource();
			var cancellationToken = serverCts.Token;
			var serverTask = server.Run(cancellationToken);
			try
			{
				async Task<IServerClient> CreateAdminClient()
				{
					var giveUpAt = DateTimeOffset.Now.AddSeconds(60);
					do
					{
						try
						{
							return await clientFactory.CreateServerClient(server.Url, User.AdminName, User.DefaultAdminPassword).ConfigureAwait(false);
						}
						catch (HttpRequestException)
						{
							//migrating, to be expected
							if (DateTimeOffset.Now > giveUpAt)
								throw;
							await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
						}
						catch (ServiceUnavailableException)
						{
							// migrating, to be expected
							if (DateTimeOffset.Now > giveUpAt)
								throw;
							await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
						}
					} while (true);
				}

				Api.Models.Instance instance;
				using (var adminClient = await CreateAdminClient())
				{
					var instanceTest = new InstanceManagerTest(adminClient.Instances, adminClient.Users, server.Directory);
					instance = await instanceTest.CreateTestInstance(cancellationToken);
					instance.Online = true;
					instance = await adminClient.Instances.Update(instance, cancellationToken);
					var instanceClient = adminClient.Instances.CreateClient(instance);
					var repoTest = new RepositoryTest(instanceClient.Repository, instanceClient.Jobs);
					var byondTest = new ByondTest(instanceClient.Byond, instanceClient.Jobs, instance);

					var repoTask = repoTest.RunPreWatchdog(cancellationToken);
					await byondTest.Run(cancellationToken);
					await repoTask;

					await new WatchdogTest(instanceClient).StartAndLeaveRunning(cancellationToken);

					await adminClient.Administration.Restart(cancellationToken);
				}

				await Task.WhenAny(serverTask, Task.Delay(30000, cancellationToken));
				Assert.IsTrue(serverTask.IsCompleted);

				serverTask = server.Run(cancellationToken);
				using (var adminClient = await CreateAdminClient())
				{
					var instanceClient = adminClient.Instances.CreateClient(instance);
					var dd = await instanceClient.DreamDaemon.Read(cancellationToken);

					await new RepositoryTest(instanceClient.Repository, instanceClient.Jobs).RunPostWatchdog(cancellationToken);

					Assert.IsTrue(dd.Running.Value);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"TEST: ERROR: {ex.GetType()} in flight!");
				throw;
			}
			finally
			{
				Console.WriteLine($"TEST: STOPPING SERVER!");
				serverCts.Cancel();
				try
				{
					Console.WriteLine($"TEST: WAITING FOR SERVER!");
					await serverTask.ConfigureAwait(false);
				}
				catch (OperationCanceledException) { }

				foreach (var proc in System.Diagnostics.Process.GetProcessesByName("DreamDaemon"))
					using (proc)
						proc.Kill();
			}
		}
	}
}
