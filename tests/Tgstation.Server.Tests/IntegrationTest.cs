using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
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

		static string RequireDiscordToken()
		{
			var discordToken = Environment.GetEnvironmentVariable("TGS4_TEST_DISCORD_TOKEN");
			if (String.IsNullOrWhiteSpace(discordToken))
				Assert.Inconclusive("The TGS4_TEST_DISCORD_TOKEN environment variable must be set to run this test!");

			return discordToken;
		}

		[TestMethod]
		public async Task TestAutomaticDiscordReconnection()
		{
			var discordToken = RequireDiscordToken();

			using var discordProvider = new DiscordProvider(Mock.Of<ILogger<DiscordProvider>>(), discordToken, 1);
			var connectResult = await discordProvider.Connect(default).ConfigureAwait(false);
			Assert.IsTrue(connectResult, "Failed to connect to discord!");
			Assert.IsTrue(discordProvider.Connected, "Discord provider is not connected!");

			// Forcefully close the connection under the provider's nose
			// This will be detected in real life scenarios
			DiscordSocketClient socketClient = typeof(DiscordProvider)
				.GetField("client", BindingFlags.Instance | BindingFlags.NonPublic)
				?.GetValue(discordProvider)
				as DiscordSocketClient;
			Assert.IsNotNull(socketClient, "Reflection unable to read discord socket client!");

			await socketClient.LogoutAsync().ConfigureAwait(false);

			Assert.IsFalse(discordProvider.Connected, "Discord provider is still connected!");

			try
			{
				using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(70));
				do
				{
					var message = await discordProvider.NextMessage(cts.Token).ConfigureAwait(false);
					if (message == null)
						break;
				}
				while (true);

				// Prevents a deadlock coming from having the NextMessage continuation call Dispose
				await Task.Yield();
			}
			catch (OperationCanceledException)
			{
				Assert.Fail("Failed to reconnect within the time period!");
			}

			Assert.IsTrue(discordProvider.Connected, "Discord provider not connected!");
		}

		[TestMethod]
		public async Task TestServerUpdate()
		{
			using var server = new TestingServer();

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

				var testUpdateVersion = new Version(4, 1, 4);
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

		static void TerminateAllDDs()
		{
			foreach (var proc in Process.GetProcessesByName("DreamDaemon"))
				using (proc)
					proc.Kill();
		}

		[TestMethod]
		public async Task TestFullStandardOperation()
		{
			RequireDiscordToken();
			using var server = new TestingServer();
			using var serverCts = new CancellationTokenSource();
			var cancellationToken = serverCts.Token;
			TerminateAllDDs();
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
					if (server.DumpOpenApiSpecpath)
					{
						// Dump swagger to disk
						// This is purely for CI
						var webRequest = WebRequest.Create(server.Url.ToString() + "swagger/v1/swagger.json");
						using var response = webRequest.GetResponse();
						using var content = response.GetResponseStream();
						using var output = new FileStream(@"C:\swagger.json", FileMode.Create);
						await content.CopyToAsync(output);
					}

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
					instance = await new InstanceManagerTest(adminClient.Instances, adminClient.Users, server.Directory).RunPreInstanceTest(cancellationToken).ConfigureAwait(false);

					var instanceClient = adminClient.Instances.CreateClient(instance);

					var instanceTests = new InstanceTest(instanceClient, adminClient.Instances).RunTests(cancellationToken);

					await Task.WhenAll(adminTest, instanceTests, usersTest);

					await adminClient.Administration.Restart(cancellationToken);
				}

				await Task.WhenAny(serverTask, Task.Delay(30000, cancellationToken));
				Assert.IsTrue(serverTask.IsCompleted);

				serverTask = server.Run(cancellationToken);
				using (var adminClient = await CreateAdminClient())
				{
					var instanceClient = adminClient.Instances.CreateClient(instance);

					// reattach job
					var jobs = await instanceClient.Jobs.ListActive(cancellationToken);
					if (jobs.Any())
					{
						Assert.AreEqual(1, jobs.Count);

						await new JobsRequiredTest(instanceClient.Jobs).WaitForJob(jobs.Single(), 40, false, cancellationToken);
					}

					var dd = await instanceClient.DreamDaemon.Read(cancellationToken);
					Assert.IsTrue(dd.Running.Value);

					await instanceClient.DreamDaemon.Shutdown(cancellationToken);
					await instanceClient.DreamDaemon.Update(new DreamDaemon
					{
						AutoStart = true
					}, cancellationToken);

					await adminClient.Administration.Restart(cancellationToken);
				}

				await Task.WhenAny(serverTask, Task.Delay(30000, cancellationToken));
				Assert.IsTrue(serverTask.IsCompleted);

				serverTask = server.Run(cancellationToken);
				using (var adminClient = await CreateAdminClient())
				{
					var instanceClient = adminClient.Instances.CreateClient(instance);

					// launch job
					var jobs = await instanceClient.Jobs.ListActive(cancellationToken);
					if (jobs.Any())
					{
						Assert.AreEqual(1, jobs.Count);

						await new JobsRequiredTest(instanceClient.Jobs).WaitForJob(jobs.Single(), 40, false, cancellationToken);
					}

					var dd = await instanceClient.DreamDaemon.Read(cancellationToken);

					Assert.IsTrue(dd.Running.Value);

					var repoTest = new RepositoryTest(instanceClient.Repository, instanceClient.Jobs).RunPostTest(cancellationToken);
					await new ChatTest(instanceClient.ChatBots, adminClient.Instances, instance).RunPostTest(cancellationToken);
					await repoTest;

					await new InstanceManagerTest(adminClient.Instances, adminClient.Users, server.Directory).RunPostTest(cancellationToken);
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

				TerminateAllDDs();
			}
		}
	}
}
