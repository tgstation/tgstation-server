using System;
using System.Data.SqlClient;
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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using MySqlConnector;

using Newtonsoft.Json;

using Npgsql;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Client;
using Tgstation.Server.Client.Components;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.System;
using Tgstation.Server.Tests.Live.Instance;

namespace Tgstation.Server.Tests.Live
{
	[TestClass]
	[TestCategory("SkipWhenLiveUnitTesting")]
	public sealed class TestLiveServer
	{
		public static ushort DDPort { get; } = FreeTcpPort();
		public static ushort DMPort { get; } = GetDMPort();

		readonly Version TestUpdateVersion = new(5, 11, 0);

		readonly IServerClientFactory clientFactory = new ServerClientFactory(new ProductHeaderValue(Assembly.GetExecutingAssembly().GetName().Name, Assembly.GetExecutingAssembly().GetName().Version.ToString()));

		static void TerminateAllDDs()
		{
			foreach (var proc in System.Diagnostics.Process.GetProcessesByName("DreamDaemon"))
				using (proc)
					proc.Kill();
		}

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

		[TestInitialize]
		public async Task Initialize()
		{
			await DummyChatProvider.RandomDisconnections(true, default);
			ServerClientFactory.ApiClientFactory = new RateLimitRetryingApiClientFactory();

			var connectionString = Environment.GetEnvironmentVariable("TGS_TEST_CONNECTION_STRING");
			if (String.IsNullOrWhiteSpace(connectionString))
				return;
			// In CI we've run into issues with the sql services not starting
			// check they're ready
			var databaseType = Enum.Parse<DatabaseType>(Environment.GetEnvironmentVariable("TGS_TEST_DATABASE_TYPE"));
			switch (databaseType)
			{
				case DatabaseType.MariaDB:
				case DatabaseType.MySql:
					var mySqlBuilder = new MySqlConnectionStringBuilder(connectionString)
					{
						Database = new MySqlConnectionStringBuilder().Database
					};
					connectionString = mySqlBuilder.ConnectionString;
					break;
				case DatabaseType.PostgresSql:
					var pgSqlBuilder = new NpgsqlConnectionStringBuilder(connectionString)
					{
						Database = new NpgsqlConnectionStringBuilder().Database
					};
					connectionString = pgSqlBuilder.ConnectionString;
					break;
				case DatabaseType.SqlServer:
					var msSqlBuilder = new SqlConnectionStringBuilder(connectionString)
					{
						InitialCatalog = new SqlConnectionStringBuilder().InitialCatalog
					};
					connectionString = msSqlBuilder.ConnectionString;
					break;
				case DatabaseType.Sqlite:
					return; // no test required
				default:
					Assert.Fail($"Unknown DatabaseType {databaseType}!");
					return;
			}

			var connectionFactory = new DatabaseConnectionFactory();

			using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
			var cancellationToken = cts.Token;

			while (true)
			{
				using var connection = connectionFactory.CreateConnection(connectionString, databaseType);
				try
				{
					await connection.OpenAsync(cancellationToken);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"TEST ERROR INIT: Could not connect to database. Retrying after 3s. Exception: {ex}");
					await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
					continue;
				}

				await connection.CloseAsync();
				break;
			}
		}

		[TestMethod]
		public async Task TestUpdateProtocolAndDisabledOAuth()
		{
			using var server = new LiveTestingServer(null, false);
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
						var message = JsonConvert.DeserializeObject<ErrorMessageResponse>(content);
						Assert.AreEqual(ErrorCode.OAuthProviderDisabled, message.ErrorCode);
					}

					//attempt to update to stable
					await adminClient.Administration.Update(new ServerUpdateRequest
					{
						NewVersion = testUpdateVersion
					}, cancellationToken);
					var serverInfo = await adminClient.ServerInformation(cancellationToken);
					Assert.IsTrue(serverInfo.UpdateInProgress);
				}

				//wait up to 3 minutes for the dl and install
				await Task.WhenAny(serverTask, Task.Delay(TimeSpan.FromMinutes(3), cancellationToken));

				Assert.IsTrue(serverTask.IsCompleted, "Server still running!");

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
					await serverTask;
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
		public async Task TestUpdateBadVersion()
		{
			using var server = new LiveTestingServer(null, false);
			using var serverCts = new CancellationTokenSource();
			var cancellationToken = serverCts.Token;
			var serverTask = server.Run(cancellationToken);
			try
			{
				var testUpdateVersion = new Version(5, 11, 20);
				using var adminClient = await CreateAdminClient(server.Url, cancellationToken);
				await ApiAssert.ThrowsException<ConflictException>(() => adminClient.Administration.Update(new ServerUpdateRequest
				{
					NewVersion = testUpdateVersion
				}, cancellationToken), ErrorCode.ResourceNotPresent);
			}
			finally
			{
				serverCts.Cancel();
				try
				{
					await serverTask;
				}
				catch (OperationCanceledException) { }
				catch (AggregateException ex)
				{
					if (ex.InnerException is NotSupportedException notSupportedException)
						Assert.Inconclusive(notSupportedException.Message);
				}
			}
		}

		[TestMethod]
		public async Task TestOneServerSwarmUpdate()
		{
			// cleanup existing directories
			new LiveTestingServer(null, false).Dispose();

			const string PrivateKey = "adlfj73ywifhks7iwrgfegjs";

			var controllerAddress = new Uri("http://localhost:5011");
			using (var controller = new LiveTestingServer(new SwarmConfiguration
			{
				Address = controllerAddress,
				Identifier = "controller",
				PrivateKey = PrivateKey
			}, false, 5011))
			{
				using var serverCts = new CancellationTokenSource();
				serverCts.CancelAfter(TimeSpan.FromHours(3));
				var cancellationToken = serverCts.Token;
				var serverTask = controller.Run(cancellationToken);

				try
				{
					using var controllerClient = await CreateAdminClient(controller.Url, cancellationToken);

					var controllerInfo = await controllerClient.ServerInformation(cancellationToken);

					static void CheckInfo(ServerInformationResponse serverInformation)
					{
						Assert.IsNotNull(serverInformation.SwarmServers);
						Assert.AreEqual(1, serverInformation.SwarmServers.Count);
						var controller = serverInformation.SwarmServers.SingleOrDefault(x => x.Identifier == "controller");
						Assert.IsNotNull(controller);
						Assert.AreEqual(controller.Address, new Uri("http://localhost:5011"));
						Assert.IsTrue(controller.Controller);
					}

					CheckInfo(controllerInfo);

					// test update
					await controllerClient.Administration.Update(
						new ServerUpdateRequest
						{
							NewVersion = TestUpdateVersion
						},
						cancellationToken);
					await Task.WhenAny(Task.Delay(TimeSpan.FromMinutes(2)), serverTask);
					Assert.IsTrue(serverTask.IsCompleted);

					void CheckServerUpdated(LiveTestingServer server)
					{
						Assert.IsTrue(Directory.Exists(server.UpdatePath), "Update directory not present!");

						var updatedAssemblyPath = Path.Combine(server.UpdatePath, "Tgstation.Server.Host.dll");
						Assert.IsTrue(File.Exists(updatedAssemblyPath), "Updated assembly missing!");

						var updatedAssemblyVersion = FileVersionInfo.GetVersionInfo(updatedAssemblyPath);
						Assert.AreEqual(TestUpdateVersion, Version.Parse(updatedAssemblyVersion.FileVersion).Semver());
						Directory.Delete(server.UpdatePath, true);
					}

					CheckServerUpdated(controller);
				}
				finally
				{
					serverCts.Cancel();
					await serverTask;
				}
			}

			new LiveTestingServer(null, false).Dispose();
		}

		[TestMethod]
		public async Task TestCreateServerWithNoArguments()
		{
			using var server = new LiveTestingServer(null, false);
			await server.RunNoArgumentsTest(default);
		}

		[TestMethod]
		public async Task TestSwarmSynchronizationAndUpdates()
		{
			// cleanup existing directories
			new LiveTestingServer(null, false).Dispose();

			const string PrivateKey = "adlfj73ywifhks7iwrgfegjs";

			var controllerAddress = new Uri("http://localhost:5011");
			using (var controller = new LiveTestingServer(new SwarmConfiguration
			{
				Address = controllerAddress,
				Identifier = "controller",
				PrivateKey = PrivateKey
			}, false, 5011))
			{
				using var node1 = new LiveTestingServer(new SwarmConfiguration
				{
					Address = new Uri("http://localhost:5012"),
					ControllerAddress = controllerAddress,
					Identifier = "node1",
					PrivateKey = PrivateKey
				}, false, 5012);
				using var node2 = new LiveTestingServer(new SwarmConfiguration
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
						ServerInformationResponse serverInformation;
						do
						{
							await Task.Delay(TimeSpan.FromSeconds(10));
							serverInformation = await node1Client.ServerInformation(cancellationToken);
						}
						while (serverInformation.SwarmServers.Count == 1);
					}

					static void CheckInfo(ServerInformationResponse serverInformation)
					{
						Assert.IsNotNull(serverInformation.SwarmServers);
						Assert.AreEqual(3, serverInformation.SwarmServers.Count);

						var node1 = serverInformation.SwarmServers.SingleOrDefault(x => x.Identifier == "node1");
						Assert.IsNotNull(node1);
						Assert.AreEqual(node1.Address, new Uri("http://localhost:5012"));
						Assert.IsFalse(node1.Controller);

						var node2 = serverInformation.SwarmServers.SingleOrDefault(x => x.Identifier == "node2");
						Assert.IsNotNull(node2);
						Assert.AreEqual(node2.Address, new Uri("http://localhost:5013"));
						Assert.IsFalse(node2.Controller);

						var controller = serverInformation.SwarmServers.SingleOrDefault(x => x.Identifier == "controller");
						Assert.IsNotNull(controller);
						Assert.AreEqual(controller.Address, new Uri("http://localhost:5011"));
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
					var newUser = await node2Client.Users.Create(new UserCreateRequest
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
						new InstanceCreateRequest
						{
							Name = "ControllerInstance",
							Path = Path.Combine(controller.Directory, "ControllerInstance")
						},
						cancellationToken);

					var node2Instance = await node2Client.Instances.CreateOrAttach(
						new InstanceCreateRequest
						{
							Name = "Node2Instance",
							Path = Path.Combine(node2.Directory, "Node2Instance")
						},
						cancellationToken);
					var node2InstanceList = await node2Client.Instances.List(null, cancellationToken);
					Assert.AreEqual(1, node2InstanceList.Count);
					Assert.AreEqual(node2Instance.Id, node2InstanceList[0].Id);
					Assert.IsNotNull(await node2Client.Instances.GetId(node2Instance, cancellationToken));
					var controllerInstanceList = await controllerClient.Instances.List(null, cancellationToken);
					Assert.AreEqual(1, controllerInstanceList.Count);
					Assert.AreEqual(controllerInstance.Id, controllerInstanceList[0].Id);
					Assert.IsNotNull(await controllerClient.Instances.GetId(controllerInstance, cancellationToken));

					await Assert.ThrowsExceptionAsync<ConflictException>(() => controllerClient.Instances.GetId(node2Instance, cancellationToken));
					await Assert.ThrowsExceptionAsync<ConflictException>(() => node1Client.Instances.GetId(controllerInstance, cancellationToken));

					// test update
					await node1Client.Administration.Update(
						new ServerUpdateRequest
						{
							NewVersion = TestUpdateVersion
						},
						cancellationToken);
					await Task.WhenAny(Task.Delay(TimeSpan.FromMinutes(2)), serverTask);
					Assert.IsTrue(serverTask.IsCompleted);

					void CheckServerUpdated(LiveTestingServer server)
					{
						Assert.IsTrue(Directory.Exists(server.UpdatePath), "Update directory not present!");

						var updatedAssemblyPath = Path.Combine(server.UpdatePath, "Tgstation.Server.Host.dll");
						Assert.IsTrue(File.Exists(updatedAssemblyPath), "Updated assembly missing!");

						var updatedAssemblyVersion = FileVersionInfo.GetVersionInfo(updatedAssemblyPath);
						Assert.AreEqual(TestUpdateVersion, Version.Parse(updatedAssemblyVersion.FileVersion).Semver());
						Directory.Delete(server.UpdatePath, true);
					}

					CheckServerUpdated(controller);
					CheckServerUpdated(node1);
					CheckServerUpdated(node2);

					// test it respects the update configuration
					controller.UpdateSwarmArguments(new SwarmConfiguration
					{
						Address = controllerAddress,
						Identifier = "controller",
						PrivateKey = PrivateKey,
						UpdateRequiredNodeCount = 2,
					});
					serverTask = Task.WhenAll(
						controller.Run(cancellationToken),
						node1.Run(cancellationToken));

					using var controllerClient2 = await CreateAdminClient(controller.Url, cancellationToken);
					using var node1Client2 = await CreateAdminClient(node1.Url, cancellationToken);

					await ApiAssert.ThrowsException<ApiConflictException>(() => controllerClient2.Administration.Update(
						new ServerUpdateRequest
						{
							NewVersion = TestUpdateVersion
						},
						cancellationToken), ErrorCode.SwarmIntegrityCheckFailed);

					// regression: test updating also works from the controller
					serverTask = Task.WhenAll(
						serverTask,
						node2.Run(cancellationToken));

					using var node2Client2 = await CreateAdminClient(node2.Url, cancellationToken);

					await controllerClient2.Administration.Update(
						new ServerUpdateRequest
						{
							NewVersion = TestUpdateVersion
						},
						cancellationToken);

					await Task.WhenAny(Task.Delay(TimeSpan.FromMinutes(2)), serverTask);
					Assert.IsTrue(serverTask.IsCompleted);

					CheckServerUpdated(controller);
					CheckServerUpdated(node1);
					CheckServerUpdated(node2);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"[{DateTimeOffset.UtcNow}] TEST ERROR: {ex}");
					throw;
				}
				finally
				{
					serverCts.Cancel();
					await serverTask;
				}
			}
		}

		[TestMethod]
		public async Task TestSwarmReconnection()
		{
			// cleanup existing directories
			new LiveTestingServer(null, false).Dispose();

			const string PrivateKey = "adlfj73ywifhks7iwrgfegjs";

			var controllerAddress = new Uri("http://localhost:5011");
			using (var controller = new LiveTestingServer(new SwarmConfiguration
			{
				Address = controllerAddress,
				Identifier = "controller",
				PrivateKey = PrivateKey,
				UpdateRequiredNodeCount = 2,
			}, false, 5011))
			{
				using var node1 = new LiveTestingServer(new SwarmConfiguration
				{
					Address = new Uri("http://localhost:5012"),
					ControllerAddress = controllerAddress,
					Identifier = "node1",
					PrivateKey = PrivateKey
				}, false, 5012);
				using var node2 = new LiveTestingServer(new SwarmConfiguration
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
						ServerInformationResponse serverInformation;
						do
						{
							await Task.Delay(TimeSpan.FromSeconds(10));
							serverInformation = await client.ServerInformation(cancellationToken);
						}
						while (serverInformation.SwarmServers.Count == currentServerCount);
					}

					static void CheckInfo(ServerInformationResponse serverInformation)
					{
						Assert.IsNotNull(serverInformation.SwarmServers);
						Assert.AreEqual(3, serverInformation.SwarmServers.Count);

						var node1 = serverInformation.SwarmServers.SingleOrDefault(x => x.Identifier == "node1");
						Assert.IsNotNull(node1);
						Assert.AreEqual(node1.Address, new Uri("http://localhost:5012"));
						Assert.IsFalse(node1.Controller);

						var node2 = serverInformation.SwarmServers.SingleOrDefault(x => x.Identifier == "node2");
						Assert.IsNotNull(node2);
						Assert.AreEqual(node2.Address, new Uri("http://localhost:5013"));
						Assert.IsFalse(node2.Controller);

						var controller = serverInformation.SwarmServers.SingleOrDefault(x => x.Identifier == "controller");
						Assert.IsNotNull(controller);
						Assert.AreEqual(controller.Address, new Uri("http://localhost:5011"));
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

					// should have unregistered
					controllerInfo = await controllerClient2.ServerInformation(cancellationToken);
					Assert.AreEqual(1, controllerInfo.SwarmServers.Count);
					Assert.IsNull(controllerInfo.SwarmServers.SingleOrDefault(x => x.Identifier == "node2"));

					// update should fail
					await ApiAssert.ThrowsException<ApiConflictException>(() => controllerClient2.Administration.Update(new ServerUpdateRequest
					{
						NewVersion = new Version(4, 6, 2)
					}, cancellationToken), ErrorCode.SwarmIntegrityCheckFailed);

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

			new LiveTestingServer(null, false).Dispose();
		}

		[TestMethod]
		public async Task TestStandardTgsOperation()
		{
			using(var currentProcess = System.Diagnostics.Process.GetCurrentProcess())
			{
				var currentPriorityClass = currentProcess.PriorityClass;
				if (currentPriorityClass != ProcessPriorityClass.Normal)
				{
					// attempt to adjust it
					Console.WriteLine($"TEST PROCESS PRIORITY: Attempting to normalize process priority from {currentPriorityClass}...");
					currentProcess.PriorityClass = ProcessPriorityClass.Normal;
				}
			}

			using (var currentProcess = System.Diagnostics.Process.GetCurrentProcess())
			{
				Assert.AreEqual(ProcessPriorityClass.Normal, currentProcess.PriorityClass);
			}

			var maximumTestMinutes = LiveTestUtils.RunningInGitHubActions ? 90 : 20;
			using var hardCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(maximumTestMinutes));
			var hardCancellationToken = hardCancellationTokenSource.Token;

			ServiceCollectionExtensions.UseAdditionalLoggerProvider<HardFailLoggerProvider>();

			var failureTask = HardFailLoggerProvider.FailureSource;
			var internalTask = TestTgsInternal(hardCancellationToken);
			await Task.WhenAny(
				internalTask,
				failureTask);

			if (!internalTask.IsCompleted)
			{
				hardCancellationTokenSource.Cancel();
				try
				{
					await failureTask;
				}
				finally
				{
					try
					{
						await internalTask;
					}
					catch (OperationCanceledException)
					{
						Console.WriteLine("TEST CANCELLED!");
					}
					catch (Exception ex)
					{
						Console.WriteLine($"ADDITIONAL TEST ERROR: {ex}");
					}
				}
			}
			else
				await internalTask;
		}

		async Task TestTgsInternal(CancellationToken hardCancellationToken)
		{
			var discordConnectionString = Environment.GetEnvironmentVariable("TGS_TEST_DISCORD_TOKEN");
			var ircConnectionString = Environment.GetEnvironmentVariable("TGS_TEST_IRC_CONNECTION_STRING");
			var missingChatVarsCount = Convert.ToInt32(String.IsNullOrWhiteSpace(discordConnectionString))
				+ Convert.ToInt32(String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TGS_TEST_DISCORD_CHANNEL")))
				+ Convert.ToInt32(String.IsNullOrWhiteSpace(ircConnectionString))
				+ Convert.ToInt32(String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TGS_TEST_IRC_CHANNEL")));

			const int TotalChatVars = 4;

			// uncomment to force this test to run with DummyChatProviders
			// missingChatVarsCount = TotalChatVars;

			if (missingChatVarsCount != 0)
			{
				if (missingChatVarsCount != TotalChatVars)
					Assert.Fail("All TGS_TEST_* chat environment variables must be present or none at all!");

				ServiceCollectionExtensions.UseChatProviderFactory<DummyChatProviderFactory>();
			}
			else
			{
				// prevalidate
				Assert.IsTrue(new DiscordConnectionStringBuilder(discordConnectionString).Valid);
				Assert.IsTrue(new IrcConnectionStringBuilder(ircConnectionString).Valid);
			}

			var procs = System.Diagnostics.Process.GetProcessesByName("byond");
			if (procs.Any())
			{
				foreach (var proc in procs)
					proc.Dispose();
				Assert.Inconclusive("Cannot run server test because DreamDaemon will not start headless while the BYOND pager is running!");
			}

			using var server = new LiveTestingServer(null, true);

			using var serverCts = CancellationTokenSource.CreateLinkedTokenSource(hardCancellationToken);
			var cancellationToken = serverCts.Token;

			TerminateAllDDs();

			InstanceManager GetInstanceManager() => ((Host.Server)server.RealServer).Host.Services.GetRequiredService<InstanceManager>();

			// main run
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
						using var httpClient = new HttpClient();
						var webRequestTask = httpClient.GetAsync(server.Url.ToString() + "swagger/v1/swagger.json");
						using var response = await webRequestTask;
						response.EnsureSuccessStatusCode();
						using var content = await response.Content.ReadAsStreamAsync();
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

					var rootTest = FailFast(RawRequestTests.Run(clientFactory, adminClient, cancellationToken));
					var adminTest = FailFast(new AdministrationTest(adminClient.Administration).Run(cancellationToken));
					var usersTest = FailFast(new UsersTest(adminClient).Run(cancellationToken));
					var instanceMangagerTest = new InstanceManagerTest(adminClient, server.Directory);
					instance = await instanceMangagerTest.CreateTestInstance(cancellationToken);
					var instancesTest = FailFast(instanceMangagerTest.RunPreTest(cancellationToken));
					Assert.IsTrue(Directory.Exists(instance.Path));
					var instanceClient = adminClient.Instances.CreateClient(instance);

					Assert.IsTrue(Directory.Exists(instanceClient.Metadata.Path));

					var instanceTests = FailFast(new InstanceTest(instanceClient, adminClient.Instances, GetInstanceManager(), (ushort)server.Url.Port).RunTests(cancellationToken, server.HighPriorityDreamDaemon, server.LowPriorityDeployments));

					await Task.WhenAll(rootTest, adminTest, instancesTest, instanceTests, usersTest);

					await adminClient.Administration.Restart(cancellationToken);
				}

				await Task.WhenAny(serverTask, Task.Delay(TimeSpan.FromMinutes(1), cancellationToken));
				Assert.IsTrue(serverTask.IsCompleted);

				// test the reattach message queueing
				// for the code coverage really...
				var topicRequestResult = await WatchdogTest.TopicClient.SendTopic(
					IPAddress.Loopback,
					$"tgs_integration_test_tactics6=1",
					DDPort,
					cancellationToken);

				Assert.IsNotNull(topicRequestResult);
				Assert.AreEqual("queued", topicRequestResult.StringData);

				// http bind test https://github.com/tgstation/tgstation-server/issues/1065
				if (new PlatformIdentifier().IsWindows)
				{
					HardFailLoggerProvider.BlockFails = true;
					try
					{
						using var blockingSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
						blockingSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, true);
						blockingSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);
						blockingSocket.Bind(new IPEndPoint(IPAddress.Any, server.Url.Port));
						// bind test run
						await server.Run(cancellationToken);
						Assert.Fail("Expected server task to end with a SocketException");
					}
					catch (SocketException ex)
					{
						Assert.AreEqual(ex.SocketErrorCode, SocketError.AddressAlreadyInUse);
					}
					finally
					{
						HardFailLoggerProvider.BlockFails = false;
					}
				}

				await Task.WhenAny(serverTask, Task.Delay(TimeSpan.FromMinutes(1), cancellationToken));
				Assert.IsTrue(serverTask.IsCompleted);

				var preStartupTime = DateTimeOffset.UtcNow;

				// chat bot start and DD reattach test
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
						await jrt.WaitForJob(job, 130, job.Description.Contains("Reconnect chat bot") ? null : false, null, cancellationToken);
					}

					var dd = await instanceClient.DreamDaemon.Read(cancellationToken);
					Assert.AreEqual(WatchdogStatus.Online, dd.Status.Value);

					var chatReadTask = instanceClient.ChatBots.List(null, cancellationToken);

					// Check the DMAPI got the channels again https://github.com/tgstation/tgstation-server/issues/1490
					topicRequestResult = await WatchdogTest.TopicClient.SendTopic(
						IPAddress.Loopback,
						$"tgs_integration_test_tactics7=1",
						DDPort,
						cancellationToken);

					Assert.IsNotNull(topicRequestResult);
					if(!Int32.TryParse(topicRequestResult.StringData, out var channelsPresent))
					{
						Assert.Fail("Expected DD to send us an int!");
					}

					var currentChatBots = await chatReadTask;
					var connectedChannelCount = currentChatBots.Where(x => x.Enabled.Value).SelectMany(x => x.Channels).Count();

					Assert.AreEqual(connectedChannelCount, channelsPresent);

					await instanceClient.DreamDaemon.Shutdown(cancellationToken);
					dd = await instanceClient.DreamDaemon.Update(new DreamDaemonRequest
					{
						AutoStart = true
					}, cancellationToken);

					Assert.AreEqual(WatchdogStatus.Offline, dd.Status);

					await adminClient.Administration.Restart(cancellationToken);
				}

				await Task.WhenAny(serverTask, Task.Delay(TimeSpan.FromMinutes(1), cancellationToken));
				Assert.IsTrue(serverTask.IsCompleted);

				preStartupTime = DateTimeOffset.UtcNow;

				async Task WaitForInitialJobs(IInstanceClient instanceClient)
				{
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
						await jrt.WaitForJob(job, 140, job.Description.Contains("Reconnect chat bot") ? null : false, null, cancellationToken);
					}
				}

				// chat bot start, dd autostart, and reboot with different initial job test
				preStartupTime = DateTimeOffset.UtcNow;
				serverTask = server.Run(cancellationToken);
				long expectedCompileJobId, expectedStaged;
				using (var adminClient = await CreateAdminClient(server.Url, cancellationToken))
				{
					var instanceClient = adminClient.Instances.CreateClient(instance);
					await WaitForInitialJobs(instanceClient);

					var dd = await instanceClient.DreamDaemon.Read(cancellationToken);

					Assert.AreEqual(WatchdogStatus.Online, dd.Status.Value);

					var compileJob = await instanceClient.DreamMaker.Compile(cancellationToken);
					var wdt = new WatchdogTest(instanceClient, GetInstanceManager(), (ushort)server.Url.Port, server.HighPriorityDreamDaemon);
					await wdt.WaitForJob(compileJob, 30, false, null, cancellationToken);

					dd = await instanceClient.DreamDaemon.Read(cancellationToken);
					Assert.AreEqual(dd.StagedCompileJob.Job.Id, compileJob.Id);

					expectedCompileJobId = compileJob.Id.Value;
					dd = await wdt.TellWorldToReboot(cancellationToken);

					while (dd.Status.Value == WatchdogStatus.Restoring)
					{
						await Task.Delay(TimeSpan.FromSeconds(1));
						dd = await instanceClient.DreamDaemon.Read(cancellationToken);
					}

					Assert.AreEqual(dd.ActiveCompileJob.Job.Id, expectedCompileJobId);
					Assert.AreEqual(WatchdogStatus.Online, dd.Status.Value);

					expectedCompileJobId = dd.ActiveCompileJob.Id.Value;

					await instanceClient.DreamDaemon.Update(new DreamDaemonRequest
					{
						AutoStart = false,
					}, cancellationToken);

					compileJob = await instanceClient.DreamMaker.Compile(cancellationToken);
					await wdt.WaitForJob(compileJob, 30, false, null, cancellationToken);
					expectedStaged = compileJob.Id.Value;

					await adminClient.Administration.Restart(cancellationToken);
				}

				await Task.WhenAny(serverTask, Task.Delay(TimeSpan.FromMinutes(1), cancellationToken));
				Assert.IsTrue(serverTask.IsCompleted);

				// post/entity deletion tests
				serverTask = server.Run(cancellationToken);
				using (var adminClient = await CreateAdminClient(server.Url, cancellationToken))
				{
					var instanceClient = adminClient.Instances.CreateClient(instance);
					await WaitForInitialJobs(instanceClient);

					var currentDD = await instanceClient.DreamDaemon.Read(cancellationToken);
					Assert.AreEqual(expectedCompileJobId, currentDD.ActiveCompileJob.Id.Value);
					Assert.AreEqual(WatchdogStatus.Online, currentDD.Status);
					Assert.AreEqual(expectedStaged, currentDD.StagedCompileJob.Job.Id.Value);

					var wdt = new WatchdogTest(instanceClient, GetInstanceManager(), (ushort)server.Url.Port, server.HighPriorityDreamDaemon);
					currentDD = await wdt.TellWorldToReboot(cancellationToken);
					Assert.AreEqual(expectedStaged, currentDD.ActiveCompileJob.Job.Id.Value);
					Assert.IsNull(currentDD.StagedCompileJob);

					var repoTest = new RepositoryTest(instanceClient.Repository, instanceClient.Jobs).RunPostTest(cancellationToken);
					await new ChatTest(instanceClient.ChatBots, adminClient.Instances, instanceClient.Jobs, instance).RunPostTest(cancellationToken);
					await repoTest;

					await new InstanceManagerTest(adminClient, server.Directory).RunPostTest(instance, cancellationToken);
				}
			}
			catch (ApiException ex)
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
					await serverTask.WithToken(hardCancellationToken);
				}
				catch (OperationCanceledException) { }

				TerminateAllDDs();
			}

			Assert.IsTrue(serverTask.IsCompleted);
			await serverTask;
		}

		async Task<IServerClient> CreateAdminClient(Uri url, CancellationToken cancellationToken)
		{
			var giveUpAt = DateTimeOffset.UtcNow.AddMinutes(2);
			for (var I = 1; ; ++I)
			{
				try
				{
					System.Console.WriteLine($"TEST: CreateAdminClient attempt {I}...");
					return await clientFactory.CreateFromLogin(
						url,
						DefaultCredentials.AdminUserName,
						DefaultCredentials.DefaultAdminUserPassword,
						attemptLoginRefresh: false,
						cancellationToken: cancellationToken)
						;
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
	}
}
