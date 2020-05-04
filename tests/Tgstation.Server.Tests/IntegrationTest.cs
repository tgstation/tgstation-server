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
using Tgstation.Server.Host.Extensions;

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

			using (var discordProvider = new DiscordProvider(Mock.Of<ILogger<DiscordProvider>>(), discordToken, 1))
			{
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
					using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(70)))
					{
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
				}
				catch (OperationCanceledException)
				{
					Assert.Fail("Failed to reconnect within the time period!");
				}

				Assert.IsTrue(discordProvider.Connected, "Discord provider not connected!");
			}
		}

		// Disabled until 4.1.0 due to changes in version handling
		[Ignore]
		[TestMethod]
		public async Task TestServerUpdate()
		{
			var updatePathRoot = Path.GetTempFileName();
			File.Delete(updatePathRoot);
			Directory.CreateDirectory(updatePathRoot);
			try
			{
				var updatePath = Path.Combine(updatePathRoot, Guid.NewGuid().ToString());
				var server = new TestingServer(clientFactory, updatePath);

				if (server.DatabaseType == "Sqlite")
					Assert.Inconclusive("Cannot run this test on SQLite yet!");

				using (var serverCts = new CancellationTokenSource())
				{
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
		public async Task TestFullStandardOperation()
		{
			RequireDiscordToken();
			var server = new TestingServer(clientFactory, null);
			using (var serverCts = new CancellationTokenSource())
			{
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

						await new AdministrationTest(adminClient.Administration).Run(cancellationToken).ConfigureAwait(false);
						await new UsersTest(adminClient.Users).Run(cancellationToken).ConfigureAwait(false);
						await new InstanceManagerTest(adminClient.Instances, server.Directory).Run(cancellationToken).ConfigureAwait(false);
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
		}
	}
}
