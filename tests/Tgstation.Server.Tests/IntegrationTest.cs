using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Client;
using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Database.Migrations;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.System;
using Tgstation.Server.Tests.Instance;

namespace Tgstation.Server.Tests
{
	[TestClass]
	[TestCategory("SkipWhenLiveUnitTesting")]
	public sealed class IntegrationTest
	{
		readonly IServerClientFactory clientFactory = new ServerClientFactory(new ProductHeaderValue(Assembly.GetExecutingAssembly().GetName().Name, Assembly.GetExecutingAssembly().GetName().Version.ToString()));

		[TestMethod]
		public async Task TestUpdateProtocolAndDisabledOAuth()
		{
			using var server = new TestingServer(null, false);
			using var serverCts = new CancellationTokenSource();
			var cancellationToken = serverCts.Token;
			var serverTask = server.Run(cancellationToken);
			try
			{
				var testUpdateVersion = new Version(4, 3, 0);
				using (var adminClient = await CreateAdminClient(server.Url, cancellationToken))
				{
					// Disabled OAuth test
					using (var httpClient = new HttpClient())
					using (var request = new HttpRequestMessage(HttpMethod.Post, server.Url.ToString()))
					{
						request.Headers.Accept.Clear();
						request.Headers.UserAgent.Add(new ProductInfoHeaderValue("RootTest", "1.0.0"));
						request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
						request.Headers.Add(ApiHeaders.ApiVersionHeader, "Tgstation.Server.Api/" + ApiHeaders.Version);
						request.Headers.Authorization = new AuthenticationHeaderValue(ApiHeaders.OAuthAuthenticationScheme, adminClient.Token.Bearer);
						request.Headers.Add(ApiHeaders.OAuthProviderHeader, OAuthProvider.GitHub.ToString());
						using var response = await httpClient.SendAsync(request, cancellationToken);
						Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
						var content = await response.Content.ReadAsStringAsync();
						var message = JsonConvert.DeserializeObject<ErrorMessage>(content);
						Assert.AreEqual(ErrorCode.OAuthProviderDisabled, message.ErrorCode);
					}

					//attempt to update to stable
					await adminClient.Administration.Update(new Administration
					{
						NewVersion = testUpdateVersion
					}, cancellationToken).ConfigureAwait(false);
					var serverInfo = await adminClient.ServerInformation(cancellationToken);
					Assert.IsTrue(serverInfo.UpdateInProgress);
				}

				//wait up to 3 minutes for the dl and install
				await Task.WhenAny(serverTask, Task.Delay(TimeSpan.FromMinutes(3), cancellationToken)).ConfigureAwait(false);

				Assert.IsTrue(serverTask.IsCompleted, "Server still running!");

				Assert.IsTrue(Directory.Exists(server.UpdatePath), "Update directory not present!");

				var updatedAssemblyPath = Path.Combine(server.UpdatePath, "Tgstation.Server.Host.dll");
				Assert.IsTrue(File.Exists(updatedAssemblyPath), "Updated assembly missing!");

				var updatedAssemblyVersion = FileVersionInfo.GetVersionInfo(updatedAssemblyPath);
				Assert.AreEqual(testUpdateVersion, Version.Parse(updatedAssemblyVersion.FileVersion).Semver());
			}
			catch (RateLimitException)
			{
				if (String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TGS4_TEST_GITHUB_TOKEN")))
					throw;

				Assert.Inconclusive("GitHub rate limit hit!");
			}
			finally
			{
				serverCts.Cancel();
				try
				{
					await serverTask.ConfigureAwait(false);
				}
				catch (OperationCanceledException) { }
				catch (AggregateException ex)
				{
					if (ex.InnerException is NotSupportedException notSupportedException)
						Assert.Inconclusive(notSupportedException.Message);
				}
			}
			Assert.IsTrue(server.RestartRequested, "Server not requesting restart!");
		}

		[TestMethod]
		public async Task TestSwarmSynchronizationAndUpdates()
		{
			// cleanup existing directories
			new TestingServer(null, false).Dispose();

			const string PrivateKey = "adlfj73ywifhks7iwrgfegjs";

			var controllerAddress = new Uri("http://localhost:5011");
			using (var controller = new TestingServer(new SwarmConfiguration
			{
				Address = controllerAddress,
				Identifier = "controller",
				PrivateKey = PrivateKey
			}, false, 5011))
			{
				using var node1 = new TestingServer(new SwarmConfiguration
				{
					Address = new Uri("http://localhost:5012"),
					ControllerAddress = controllerAddress,
					Identifier = "node1",
					PrivateKey = PrivateKey
				}, false, 5012);
				using var node2 = new TestingServer(new SwarmConfiguration
				{
					Address = new Uri("http://localhost:5013"),
					ControllerAddress = controllerAddress,
					Identifier = "node2",
					PrivateKey = PrivateKey
				}, false, 5013);
				using var serverCts = new CancellationTokenSource();
				var cancellationToken = serverCts.Token;
				var serverTask = Task.WhenAll(
					node1.Run(cancellationToken),
					node2.Run(cancellationToken),
					controller.Run(cancellationToken));

				try
				{
					using var controllerClient = await CreateAdminClient(controller.Url, cancellationToken);
					using var node1Client = await CreateAdminClient(node1.Url, cancellationToken);
					using var node2Client = await CreateAdminClient(node2.Url, cancellationToken);

					var controllerInfo = await controllerClient.ServerInformation(cancellationToken);

					async Task WaitForSwarmServerUpdate()
					{
						ServerInformation serverInformation;
						do
						{
							await Task.Delay(TimeSpan.FromSeconds(10));
							serverInformation = await node1Client.ServerInformation(cancellationToken);
						}
						while (serverInformation.SwarmServers.Count == 1);
					}

					static void CheckInfo(ServerInformation serverInformation)
					{
						Assert.IsNotNull(serverInformation.SwarmServers);
						Assert.AreEqual(3, serverInformation.SwarmServers.Count);

						var node1 = serverInformation.SwarmServers.SingleOrDefault(x => x.Identifier == "node1");
						Assert.IsNotNull(node1);
						Assert.AreEqual(node1.Address, "http://localhost:5012");
						Assert.IsFalse(node1.Controller);

						var node2 = serverInformation.SwarmServers.SingleOrDefault(x => x.Identifier == "node2");
						Assert.IsNotNull(node2);
						Assert.AreEqual(node2.Address, "http://localhost:5013");
						Assert.IsFalse(node2.Controller);

						var controller = serverInformation.SwarmServers.SingleOrDefault(x => x.Identifier == "controller");
						Assert.IsNotNull(controller);
						Assert.AreEqual(controller.Address, "http://localhost:5011");
						Assert.IsTrue(controller.Controller);
					}

					CheckInfo(controllerInfo);

					// wait a few minutes for the updated server list to dispatch
					await Task.WhenAny(
						WaitForSwarmServerUpdate(),
						Task.Delay(TimeSpan.FromMinutes(4), cancellationToken));

					var node2Info = await node2Client.ServerInformation(cancellationToken);
					var node1Info = await node1Client.ServerInformation(cancellationToken);
					CheckInfo(node1Info);
					CheckInfo(node2Info);

					// check user info is shared
					var newUser = await node2Client.Users.Create(new UserUpdate
					{
						Name = "asdf",
						Password = "asdfasdfasdfasdf",
						Enabled = true,
						PermissionSet = new PermissionSet
						{
							AdministrationRights = AdministrationRights.ChangeVersion
						}
					}, cancellationToken);

					var node1User = await node1Client.Users.GetId(newUser, cancellationToken);
					Assert.AreEqual(newUser.Name, node1User.Name);
					Assert.AreEqual(newUser.Enabled, node1User.Enabled);

					using var controllerUserClient = await clientFactory.CreateFromLogin(
						controllerAddress,
						newUser.Name,
						"asdfasdfasdfasdf");

					using var node1BadClient = clientFactory.CreateFromToken(node1.Url, controllerUserClient.Token);
					await Assert.ThrowsExceptionAsync<UnauthorizedException>(() => node1BadClient.Administration.Read(cancellationToken));

					// check instance info is not shared
					var controllerInstance = await controllerClient.Instances.CreateOrAttach(
						new Api.Models.Instance
						{
							Name = "ControllerInstance",
							Path = Path.Combine(controller.Directory, "ControllerInstance")
						},
						cancellationToken);

					var node2Instance = await node2Client.Instances.CreateOrAttach(
						new Api.Models.Instance
						{
							Name = "Node2Instance",
							Path = Path.Combine(node2.Directory, "Node2Instance")
						},
						cancellationToken);
					var node2InstanceList = await node2Client.Instances.List(null, cancellationToken);
					Assert.AreEqual(1, node2InstanceList.Count);
					Assert.AreEqual(node2Instance.Id, node2InstanceList.First().Id);
					Assert.IsNotNull(await node2Client.Instances.GetId(node2Instance, cancellationToken));
					var controllerInstanceList = await controllerClient.Instances.List(null, cancellationToken);
					Assert.AreEqual(1, controllerInstanceList.Count);
					Assert.AreEqual(controllerInstance.Id, controllerInstanceList.First().Id);
					Assert.IsNotNull(await controllerClient.Instances.GetId(controllerInstance, cancellationToken));

					await Assert.ThrowsExceptionAsync<ConflictException>(() => controllerClient.Instances.GetId(node2Instance, cancellationToken));
					await Assert.ThrowsExceptionAsync<ConflictException>(() => node1Client.Instances.GetId(controllerInstance, cancellationToken));

					// test update
					var testUpdateVersion = new Version(4, 6, 2);
					await node1Client.Administration.Update(
						new Administration
						{
							NewVersion = testUpdateVersion
						},
						cancellationToken);
					await Task.WhenAny(Task.Delay(TimeSpan.FromMinutes(2)), serverTask);
					Assert.IsTrue(serverTask.IsCompleted);

					void CheckServerUpdated(TestingServer server)
					{
						Assert.IsTrue(Directory.Exists(server.UpdatePath), "Update directory not present!");

						var updatedAssemblyPath = Path.Combine(server.UpdatePath, "Tgstation.Server.Host.dll");
						Assert.IsTrue(File.Exists(updatedAssemblyPath), "Updated assembly missing!");

						var updatedAssemblyVersion = FileVersionInfo.GetVersionInfo(updatedAssemblyPath);
						Assert.AreEqual(testUpdateVersion, Version.Parse(updatedAssemblyVersion.FileVersion).Semver());
						Directory.Delete(server.UpdatePath, true);
					}

					CheckServerUpdated(controller);
					CheckServerUpdated(node1);
					CheckServerUpdated(node2);

					// regression: test it also works from the controller
					serverTask = Task.WhenAll(
						node1.Run(cancellationToken),
						node2.Run(cancellationToken),
						controller.Run(cancellationToken));

					using var controllerClient2 = await CreateAdminClient(controller.Url, cancellationToken);
					using var node1Client2 = await CreateAdminClient(node1.Url, cancellationToken);
					using var node2Client2 = await CreateAdminClient(node2.Url, cancellationToken);

					await controllerClient2.Administration.Update(
						new Administration
						{
							NewVersion = testUpdateVersion
						},
						cancellationToken);

					await Task.WhenAny(Task.Delay(TimeSpan.FromMinutes(2)), serverTask);
					Assert.IsTrue(serverTask.IsCompleted);

					CheckServerUpdated(controller);
					CheckServerUpdated(node1);
					CheckServerUpdated(node2);
				}
				finally
				{
					serverCts.Cancel();
					await serverTask;
				}
			}

			new TestingServer(null, false).Dispose();
		}

		[TestMethod]
		public async Task TestSwarmReconnection()
		{
			// cleanup existing directories
			new TestingServer(null, false).Dispose();

			const string PrivateKey = "adlfj73ywifhks7iwrgfegjs";

			var controllerAddress = new Uri("http://localhost:5011");
			using (var controller = new TestingServer(new SwarmConfiguration
			{
				Address = controllerAddress,
				Identifier = "controller",
				PrivateKey = PrivateKey
			}, false, 5011))
			{
				using var node1 = new TestingServer(new SwarmConfiguration
				{
					Address = new Uri("http://localhost:5012"),
					ControllerAddress = controllerAddress,
					Identifier = "node1",
					PrivateKey = PrivateKey
				}, false, 5012);
				using var node2 = new TestingServer(new SwarmConfiguration
				{
					Address = new Uri("http://localhost:5013"),
					ControllerAddress = controllerAddress,
					Identifier = "node2",
					PrivateKey = PrivateKey
				}, false, 5013);
				using var serverCts = new CancellationTokenSource();

				var cancellationToken = serverCts.Token;
				using var node1Cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

				Task node1Task, node2Task, controllerTask;
				var serverTask = Task.WhenAll(
					node1Task = node1.Run(node1Cts.Token),
					node2Task = node2.Run(cancellationToken),
					controllerTask = controller.Run(cancellationToken));

				try
				{
					using var controllerClient = await CreateAdminClient(controller.Url, cancellationToken);
					using var node1Client = await CreateAdminClient(node1.Url, cancellationToken);
					using var node2Client = await CreateAdminClient(node2.Url, cancellationToken);

					var controllerInfo = await controllerClient.ServerInformation(cancellationToken);

					async Task WaitForSwarmServerUpdate(IServerClient client, int currentServerCount)
					{
						ServerInformation serverInformation;
						do
						{
							await Task.Delay(TimeSpan.FromSeconds(10));
							serverInformation = await client.ServerInformation(cancellationToken);
						}
						while (serverInformation.SwarmServers.Count == currentServerCount);
					}

					static void CheckInfo(ServerInformation serverInformation)
					{
						Assert.IsNotNull(serverInformation.SwarmServers);
						Assert.AreEqual(3, serverInformation.SwarmServers.Count);

						var node1 = serverInformation.SwarmServers.SingleOrDefault(x => x.Identifier == "node1");
						Assert.IsNotNull(node1);
						Assert.AreEqual(node1.Address, "http://localhost:5012");
						Assert.IsFalse(node1.Controller);

						var node2 = serverInformation.SwarmServers.SingleOrDefault(x => x.Identifier == "node2");
						Assert.IsNotNull(node2);
						Assert.AreEqual(node2.Address, "http://localhost:5013");
						Assert.IsFalse(node2.Controller);

						var controller = serverInformation.SwarmServers.SingleOrDefault(x => x.Identifier == "controller");
						Assert.IsNotNull(controller);
						Assert.AreEqual(controller.Address, "http://localhost:5011");
						Assert.IsTrue(controller.Controller);
					}

					CheckInfo(controllerInfo);

					// wait a few minutes for the updated server list to dispatch
					await Task.WhenAny(
						WaitForSwarmServerUpdate(node1Client, 1),
						Task.Delay(TimeSpan.FromMinutes(4), cancellationToken));

					var node2Info = await node2Client.ServerInformation(cancellationToken);
					var node1Info = await node1Client.ServerInformation(cancellationToken);
					CheckInfo(node1Info);
					CheckInfo(node2Info);

					// kill node1
					node1Cts.Cancel();
					await Task.WhenAny(
						node1Task,
						Task.Delay(TimeSpan.FromMinutes(1)));
					Assert.IsTrue(node1Task.IsCompleted);

					// it should unregister
					controllerInfo = await controllerClient.ServerInformation(cancellationToken);
					Assert.AreEqual(2, controllerInfo.SwarmServers.Count);
					Assert.IsFalse(controllerInfo.SwarmServers.Any(x => x.Identifier == "node1"));

					// wait a few minutes for the updated server list to dispatch
					await Task.WhenAny(
						WaitForSwarmServerUpdate(node2Client, 3),
						Task.Delay(TimeSpan.FromMinutes(4), cancellationToken));

					node2Info = await node2Client.ServerInformation(cancellationToken);
					Assert.AreEqual(2, node2Info.SwarmServers.Count);
					Assert.IsFalse(node2Info.SwarmServers.Any(x => x.Identifier == "node1"));

					// restart the controller
					await controllerClient.Administration.Restart(cancellationToken);
					await Task.WhenAny(
						controllerTask,
						Task.Delay(TimeSpan.FromMinutes(1), cancellationToken));
					Assert.IsTrue(controllerTask.IsCompleted);

					controllerTask = controller.Run(cancellationToken);
					using var controllerClient2 = await CreateAdminClient(controller.Url, cancellationToken);

					// node 2 should reconnect once it's health check triggers
					await Task.WhenAny(
						WaitForSwarmServerUpdate(controllerClient2, 1),
						Task.Delay(TimeSpan.FromMinutes(5), cancellationToken));

					controllerInfo = await controllerClient2.ServerInformation(cancellationToken);
					Assert.AreEqual(2, controllerInfo.SwarmServers.Count);
					Assert.IsNotNull(controllerInfo.SwarmServers.SingleOrDefault(x => x.Identifier == "node2"));

					// wait a few seconds to dispatch the updated list to node2
					await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

					// restart node2
					await node2Client.Administration.Restart(cancellationToken);
					await Task.WhenAny(
						node2Task,
						Task.Delay(TimeSpan.FromMinutes(1)));
					Assert.IsTrue(node1Task.IsCompleted);

					// should remain registered
					controllerInfo = await controllerClient2.ServerInformation(cancellationToken);
					Assert.AreEqual(2, controllerInfo.SwarmServers.Count);
					Assert.IsNotNull(controllerInfo.SwarmServers.SingleOrDefault(x => x.Identifier == "node2"));

					// update should fail
					await controllerClient2.Administration.Update(new Administration
					{
						NewVersion = new Version(4, 6, 2)
					}, cancellationToken);

					async Task WaitForUpdateFailure()
					{
						ServerInformation serverInformation;
						serverInformation = await controllerClient2.ServerInformation(cancellationToken);
						while (serverInformation.UpdateInProgress)
						{
							await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
							serverInformation = await controllerClient2.ServerInformation(cancellationToken);
						}
					}

					var updateFailureTask = WaitForUpdateFailure();
					await Task.WhenAny(updateFailureTask, Task.Delay(TimeSpan.FromMinutes(5), cancellationToken));

					node2Task = node2.Run(cancellationToken);
					using var node2Client2 = await CreateAdminClient(node2.Url, cancellationToken);

					// should re-register
					await Task.WhenAny(
						WaitForSwarmServerUpdate(node2Client2, 1),
						Task.Delay(TimeSpan.FromMinutes(4), cancellationToken));

					node2Info = await node2Client2.ServerInformation(cancellationToken);
					Assert.AreEqual(2, node2Info.SwarmServers.Count);
					Assert.IsNotNull(node2Info.SwarmServers.SingleOrDefault(x => x.Identifier == "controller"));
				}
				finally
				{
					serverCts.Cancel();
					await serverTask;
				}
			}

			new TestingServer(null, false).Dispose();
		}

		static void TerminateAllDDs()
		{
			foreach (var proc in System.Diagnostics.Process.GetProcessesByName("DreamDaemon"))
				using (proc)
					proc.Kill();
		}

		async Task<IServerClient> CreateAdminClient(Uri url, CancellationToken cancellationToken)
		{
			var giveUpAt = DateTimeOffset.UtcNow.AddMinutes(2);
			for(var I = 1; ; ++I)
			{
				try
				{
					Console.WriteLine($"TEST: CreateAdminClient attempt {I}...");
					return await clientFactory.CreateFromLogin(
						url,
						User.AdminName,
						User.DefaultAdminPassword,
						attemptLoginRefresh: false,
						cancellationToken: cancellationToken)
						.ConfigureAwait(false);
				}
				catch (HttpRequestException)
				{
					//migrating, to be expected
					if (DateTimeOffset.UtcNow > giveUpAt)
						throw;
					await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
				}
				catch (ServiceUnavailableException)
				{
					// migrating, to be expected
					if (DateTimeOffset.UtcNow > giveUpAt)
						throw;
					await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
				}
			}
		}

		[TestMethod]
		public async Task TestDownMigrations()
		{
			var connectionString = Environment.GetEnvironmentVariable("TGS4_TEST_CONNECTION_STRING");

			if (String.IsNullOrEmpty(connectionString))
				Assert.Inconclusive("No connection string configured in env var TGS4_TEST_CONNECTION_STRING!");

			var databaseTypeString = Environment.GetEnvironmentVariable("TGS4_TEST_DATABASE_TYPE");
			if (!Enum.TryParse<DatabaseType>(databaseTypeString, out var databaseType))
				Assert.Inconclusive("No/invalid database type configured in env var TGS4_TEST_DATABASE_TYPE!");

			string migrationName = null;
			DatabaseContext CreateContext()
			{
				string serverVersion = Environment.GetEnvironmentVariable($"{DatabaseConfiguration.Section}__{nameof(DatabaseConfiguration.ServerVersion)}");
				if (String.IsNullOrWhiteSpace(serverVersion))
					serverVersion = null;
				switch (databaseType)
				{
					case DatabaseType.MySql:
					case DatabaseType.MariaDB:
						migrationName = nameof(MYInitialCreate);
						return new MySqlDatabaseContext(
							Host.Database.Design.DesignTimeDbContextFactoryHelpers.CreateDatabaseContextOptions<MySqlDatabaseContext>(
								databaseType,
								connectionString,
								serverVersion));
					case DatabaseType.PostgresSql:
						migrationName = nameof(PGCreate);
						return new PostgresSqlDatabaseContext(
							Host.Database.Design.DesignTimeDbContextFactoryHelpers.CreateDatabaseContextOptions<PostgresSqlDatabaseContext>(
								databaseType,
								connectionString,
								serverVersion));
					case DatabaseType.SqlServer:
						migrationName = nameof(MSInitialCreate);
						return new SqlServerDatabaseContext(
							Host.Database.Design.DesignTimeDbContextFactoryHelpers.CreateDatabaseContextOptions<SqlServerDatabaseContext>(
								databaseType,
								connectionString,
								serverVersion));
					case DatabaseType.Sqlite:
						migrationName = nameof(SLRebuild);
						return new SqliteDatabaseContext(
							Host.Database.Design.DesignTimeDbContextFactoryHelpers.CreateDatabaseContextOptions<SqliteDatabaseContext>(
								databaseType,
								connectionString,
								serverVersion));
				}

				return null;
			}

			using var context = CreateContext();
			await context.Database.EnsureDeletedAsync();
			await context.Database.MigrateAsync(default);

			// add usergroups and dummy instances for testing purposes
			var group = new Host.Models.UserGroup
			{
				PermissionSet = new Host.Models.PermissionSet
				{
					AdministrationRights = AdministrationRights.ChangeVersion,
					InstanceManagerRights = InstanceManagerRights.GrantPermissions
				},
				Name = "TestGroup",
			};

			const string TestUserName = "TestUser42";
			var user = new Host.Models.User
			{
				Name = TestUserName,
				CreatedAt = DateTimeOffset.UtcNow,
				OAuthConnections = new List<Host.Models.OAuthConnection>(),
				CanonicalName = Host.Models.User.CanonicalizeName(TestUserName),
				Enabled = false,
				Group = group,
				PasswordHash = "_",
			};

			var instance = new Host.Models.Instance
			{
				AutoUpdateInterval = 0,
				ChatBotLimit = 1,
				ChatSettings = new List<Host.Models.ChatBot>(),
				ConfigurationType = ConfigurationType.HostWrite,
				DreamDaemonSettings = new Host.Models.DreamDaemonSettings
				{
					AllowWebClient = false,
					AutoStart = false,
					HeartbeatSeconds = 0,
					Port = 1447,
					SecurityLevel = DreamDaemonSecurity.Safe,
					StartupTimeout = 1000,
					TopicRequestTimeout = 1000,
					AdditionalParameters = String.Empty,
				},
				DreamMakerSettings = new Host.Models.DreamMakerSettings
				{
					ApiValidationPort = 1557,
					ApiValidationSecurityLevel = DreamDaemonSecurity.Trusted,
					RequireDMApiValidation = false,
				},
				InstancePermissionSets = new List<Host.Models.InstancePermissionSet>
				{
					new Host.Models.InstancePermissionSet
					{
						ByondRights = ByondRights.InstallCustomVersion,
						ChatBotRights = ChatBotRights.None,
						ConfigurationRights = ConfigurationRights.Read,
						DreamDaemonRights = DreamDaemonRights.ReadRevision,
						DreamMakerRights = DreamMakerRights.SetApiValidationPort,
						InstancePermissionSetRights = InstancePermissionSetRights.Write,
						PermissionSet = group.PermissionSet,
						RepositoryRights = RepositoryRights.SetReference
					}
				},
				Name = "sfdsadfsa",
				Online = false,
				Path = "/a/b/c/d",
				RepositorySettings = new Host.Models.RepositorySettings
				{
					AutoUpdatesKeepTestMerges = false,
					AutoUpdatesSynchronize = false,
					CommitterEmail = "email@eample.com",
					CommitterName = "blubluh",
					CreateGitHubDeployments = false,
					PostTestMergeComment = false,
					PushTestMergeCommits = false,
					ShowTestMergeCommitters = false,
				},
			};

			context.Users.Add(user);
			context.Groups.Add(group);
			context.Instances.Add(instance);
			await context.Save(default);

			var dbServiceProvider = ((IInfrastructure<IServiceProvider>)context.Database).Instance;
			var migrator = dbServiceProvider.GetRequiredService<IMigrator>();
			await migrator.MigrateAsync(migrationName, default);
			await context.Database.EnsureDeletedAsync();
		}

		[TestMethod]
		public async Task TestServer()
		{
			var procs = System.Diagnostics.Process.GetProcessesByName("byond");
			if(procs.Any())
			{
				foreach (var proc in procs)
					proc.Dispose();
				Assert.Inconclusive("Cannot run server test because DreamDaemon will not start headless while the BYOND pager is running!");
			}

			using var server = new TestingServer(null, true);

			const int MaximumTestMinutes = 20;
			using var hardTimeoutCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(MaximumTestMinutes));
			var hardCancellationToken = hardTimeoutCancellationTokenSource.Token;
			using var serverCts = CancellationTokenSource.CreateLinkedTokenSource(hardCancellationToken);
			var cancellationToken = serverCts.Token;

			TerminateAllDDs();
			var serverTask = server.Run(cancellationToken);

			try
			{
				Api.Models.Instance instance;
				using (var adminClient = await CreateAdminClient(server.Url, cancellationToken))
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

					async Task FailFast(Task task)
					{
						try
						{
							await task;
						}
						catch (OperationCanceledException)
						{
							throw;
						}
						catch (Exception ex)
						{
							Console.WriteLine($"[{DateTimeOffset.UtcNow}] TEST ERROR: {ex}");
							serverCts.Cancel();
							throw;
						}
					}

					var rootTest = FailFast(new RawRequestTests().Run(clientFactory, adminClient, cancellationToken));
					var adminTest = FailFast(new AdministrationTest(adminClient.Administration).Run(cancellationToken));
					var usersTest = FailFast(new UsersTest(adminClient).Run(cancellationToken));
					instance = await new InstanceManagerTest(adminClient.Instances, adminClient.Users, server.Directory).RunPreInstanceTest(cancellationToken);
					Assert.IsTrue(Directory.Exists(instance.Path));
					var instanceClient = adminClient.Instances.CreateClient(instance);

					Assert.IsTrue(Directory.Exists(instanceClient.Metadata.Path));

					var instanceTests = FailFast(new InstanceTest(instanceClient, adminClient.Instances).RunTests(cancellationToken));

					await Task.WhenAll(rootTest, adminTest, instanceTests, usersTest);

					await adminClient.Administration.Restart(cancellationToken);
				}

				await Task.WhenAny(serverTask, Task.Delay(TimeSpan.FromMinutes(1), cancellationToken));
				Assert.IsTrue(serverTask.IsCompleted);

				// http bind test https://github.com/tgstation/tgstation-server/issues/1065
				if (new PlatformIdentifier().IsWindows)
				{
					using var blockingSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
					blockingSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, true);
					blockingSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);
					blockingSocket.Bind(new IPEndPoint(IPAddress.Any, server.Url.Port));
					try
					{
						await server.Run(cancellationToken);
						Assert.Fail("Expected server task to end with a SocketException");
					}
					catch (SocketException ex)
					{
						Assert.AreEqual(ex.SocketErrorCode, SocketError.AddressAlreadyInUse);
					}
				}

				await Task.WhenAny(serverTask, Task.Delay(TimeSpan.FromMinutes(1), cancellationToken));
				Assert.IsTrue(serverTask.IsCompleted);

				var preStartupTime = DateTimeOffset.UtcNow;

				serverTask = server.Run(cancellationToken);
				using (var adminClient = await CreateAdminClient(server.Url, cancellationToken))
				{
					var instanceClient = adminClient.Instances.CreateClient(instance);

					var jobs = await instanceClient.Jobs.ListActive(null, cancellationToken);
					if (!jobs.Any())
					{
						var entities = await instanceClient.Jobs.List(null, cancellationToken);
						var getTasks = entities
							.Select(e => instanceClient.Jobs.GetId(e, cancellationToken))
							.ToList();

						await Task.WhenAll(getTasks);
						jobs = getTasks
							.Select(x => x.Result)
							.Where(x => x.StartedAt.Value >= preStartupTime)
							.ToList();
					}

					var jrt = new JobsRequiredTest(instanceClient.Jobs);
					foreach (var job in jobs)
					{
						Assert.IsTrue(job.StartedAt.Value >= preStartupTime);
						await jrt.WaitForJob(job, 130, false, null, cancellationToken);
					}

					var dd = await instanceClient.DreamDaemon.Read(cancellationToken);
					Assert.AreEqual(WatchdogStatus.Online, dd.Status.Value);

					await instanceClient.DreamDaemon.Shutdown(cancellationToken);
					await instanceClient.DreamDaemon.Update(new DreamDaemon
					{
						AutoStart = true
					}, cancellationToken);

					await adminClient.Administration.Restart(cancellationToken);
				}

				await Task.WhenAny(serverTask, Task.Delay(TimeSpan.FromMinutes(1), cancellationToken));
				Assert.IsTrue(serverTask.IsCompleted);

				preStartupTime = DateTimeOffset.UtcNow;
				serverTask = server.Run(cancellationToken);
				using (var adminClient = await CreateAdminClient(server.Url, cancellationToken))
				{
					var instanceClient = adminClient.Instances.CreateClient(instance);

					var jobs = await instanceClient.Jobs.ListActive(null, cancellationToken);
					if (!jobs.Any())
					{
						var entities = await instanceClient.Jobs.List(null, cancellationToken);
						var getTasks = entities
							.Select(e => instanceClient.Jobs.GetId(e, cancellationToken))
							.ToList();

						await Task.WhenAll(getTasks);
						jobs = getTasks
							.Select(x => x.Result)
							.Where(x => x.StartedAt.Value > preStartupTime)
							.ToList();
					}

					var jrt = new JobsRequiredTest(instanceClient.Jobs);
					foreach (var job in jobs)
					{
						Assert.IsTrue(job.StartedAt.Value >= preStartupTime);
						await jrt.WaitForJob(job, 140, false, null, cancellationToken);
					}

					var dd = await instanceClient.DreamDaemon.Read(cancellationToken);

					Assert.AreEqual(WatchdogStatus.Online, dd.Status.Value);

					var repoTest = new RepositoryTest(instanceClient.Repository, instanceClient.Jobs).RunPostTest(cancellationToken);
					await new ChatTest(instanceClient.ChatBots, adminClient.Instances, instance).RunPostTest(cancellationToken);
					await repoTest;

					await new InstanceManagerTest(adminClient.Instances, adminClient.Users, server.Directory).RunPostTest(cancellationToken);
				}
			}
			catch(ApiException ex)
			{
				Console.WriteLine($"[{DateTimeOffset.UtcNow}] TEST ERROR: {ex.ErrorCode}: {ex.Message}\n{ex.AdditionalServerData}");
				throw;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[{DateTimeOffset.UtcNow}] TEST ERROR: {ex}");
				throw;
			}
			finally
			{
				serverCts.Cancel();
				try
				{
					await serverTask.WithToken(hardCancellationToken).ConfigureAwait(false);
				}
				catch (OperationCanceledException) { }

				TerminateAllDDs();
			}

			Assert.IsTrue(serverTask.IsCompleted);
			await serverTask;
		}

		public static ushort DDPort = FreeTcpPort();
		public static ushort DMPort = GetDMPort();

		static ushort GetDMPort()
		{
			ushort result;
			do
			{
				result = FreeTcpPort();
			} while (result == DDPort);
			return result;
		}

		static ushort FreeTcpPort()
		{
			var l = new TcpListener(IPAddress.Loopback, 0);
			l.Start();
			try
			{
				return (ushort)((IPEndPoint)l.LocalEndpoint).Port;
			}
			finally
			{
				l.Stop();
			}
		}

		[TestMethod]
		public async Task TestScriptExecution()
		{
			var platformIdentifier = new PlatformIdentifier();
			var processExecutor = new ProcessExecutor(
				Mock.Of<IProcessFeatures>(),
				Mock.Of<ILogger<ProcessExecutor>>(),
				LoggerFactory.Create(x => { }));

			using var process = processExecutor.LaunchProcess("test." + platformIdentifier.ScriptFileExtension, ".", String.Empty, true, true, true);
			using var cts = new CancellationTokenSource();
			cts.CancelAfter(3000);
			var exitCode = await process.Lifetime.WithToken(cts.Token);

			Assert.AreEqual(0, exitCode);
			Assert.AreEqual(String.Empty, (await process.GetErrorOutput(default)).Trim());
			Assert.AreEqual("Hello World!", (await process.GetStandardOutput(default)).Trim());
		}

		[TestMethod]
		public async Task TestRepoParentLookup()
		{
			using var testingServer = new TestingServer(null, false);
			LibGit2Sharp.Repository.Clone("https://github.com/Cyberboss/test", testingServer.Directory);
			var libGit2Repo = new LibGit2Sharp.Repository(testingServer.Directory);
			using var repo = new Host.Components.Repository.Repository(
				libGit2Repo,
				new LibGit2Commands(),
				Mock.Of<Host.IO.IIOManager>(),
				Mock.Of<IEventConsumer>(),
				Mock.Of<ICredentialsProvider>(),
				Mock.Of<IGitRemoteFeaturesFactory>(),
				Mock.Of<ILogger<Host.Components.Repository.Repository>>(),
				() => { });

			const string StartSha = "af4da8beb9f9b374b04a3cc4d65acca662e8cc1a";
			await repo.CheckoutObject(StartSha, progress => { }, default);
			var result = await repo.ShaIsParent("2f8588a3ca0f6b027704a2a04381215619de3412", default);
			Assert.IsTrue(result);
			Assert.AreEqual(StartSha, repo.Head);
			result = await repo.ShaIsParent("f636418bf47d238d33b0e4a34f0072b23a8aad0e", default);
			Assert.IsFalse(result); ;
			Assert.AreEqual(StartSha, repo.Head);
		}
	}
}
