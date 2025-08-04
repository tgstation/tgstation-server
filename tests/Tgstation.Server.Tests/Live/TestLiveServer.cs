using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using MySqlConnector;

using Newtonsoft.Json;

using Npgsql;

using StrawberryShake;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Extensions;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Client;
using Tgstation.Server.Client.Components;
using Tgstation.Server.Client.GraphQL;
using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.System;
using Tgstation.Server.Tests.Live.Instance;

namespace Tgstation.Server.Tests.Live
{
	[TestClass]
	[TestCategory("SkipWhenLiveUnitTesting")]
	[TestCategory("RequiresDatabase")]
	public sealed class TestLiveServer
	{
		public static readonly Version TestUpdateVersion = new(5, 11, 0);

		static readonly Lazy<ushort> odDMPort = new(() => FreeTcpPort());
		static readonly Lazy<ushort> odDDPort = new(() => FreeTcpPort(odDMPort.Value));
		static readonly Lazy<ushort> compatDMPort = new(() => FreeTcpPort(odDDPort.Value, odDMPort.Value));
		static readonly Lazy<ushort> compatDDPort = new(() => FreeTcpPort(odDDPort.Value, odDMPort.Value, compatDMPort.Value));
		static readonly Lazy<ushort> mainDDPort = new(() => FreeTcpPort(odDDPort.Value, odDMPort.Value, compatDMPort.Value, compatDDPort.Value));
		static readonly Lazy<ushort> mainDMPort = new(() => FreeTcpPort(odDDPort.Value, odDMPort.Value, compatDMPort.Value, compatDDPort.Value, mainDDPort.Value));

		static void InitializePorts()
		{
			_ = odDMPort.Value;
			_ = odDDPort.Value;
			_ = compatDMPort.Value;
			_ = compatDDPort.Value;
			_ = mainDDPort.Value;
			_ = mainDMPort.Value;
		}

		readonly RestServerClientFactory restClientFactory;
		readonly GraphQLServerClientFactory graphQLClientFactory;

		public TestLiveServer()
		{
			restClientFactory = new(new ProductHeaderValue(Assembly.GetExecutingAssembly().GetName().Name, Assembly.GetExecutingAssembly().GetName().Version.ToString()));
			graphQLClientFactory = new GraphQLServerClientFactory(restClientFactory);
		}

		public static List<System.Diagnostics.Process> GetEngineServerProcessesOnPort(EngineType engineType, ushort? port)
		{
			var result = new List<System.Diagnostics.Process>();

			switch (engineType) {
				case EngineType.Byond:
					result.AddRange(System.Diagnostics.Process.GetProcessesByName("DreamDaemon"));
					if (new PlatformIdentifier().IsWindows)
						result.AddRange(System.Diagnostics.Process.GetProcessesByName("dd"));
					break;
				case EngineType.OpenDream:
					var potentialProcesses = System.Diagnostics.Process.GetProcessesByName("dotnet")
						.Where(process =>
						{
							if (GetCommandLine(process)?.Contains("Robust.Server") == true)
								return true;

							process.Dispose();
							return false;
						});

					result.AddRange(potentialProcesses);
					break;
				default:
					Assert.Fail($"Unknown engine type: {engineType}");
					return null;
			}

			if (port.HasValue)
				result = result.Where(x =>
				{
					string portString = null;
					switch (engineType)
					{
						case EngineType.OpenDream:
							portString = $"--cvar net.port={port.Value}";
							break;
						case EngineType.Byond:
							portString = $"-port {port.Value}";
							break;
						default:
							Assert.Fail($"Unknown engine type: {engineType}");
							break;
					}

					if (GetCommandLine(x)?.Contains(portString) ?? false)
						return true;

					x.Dispose();
					return false;
				}).ToList();

			return result;
		}

		private static string GetCommandLine(System.Diagnostics.Process process)
		{
			if (new PlatformIdentifier().IsWindows)
			{
				var searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id);
				var objects = searcher.Get();
				var commandLine = objects.Cast<ManagementBaseObject>().SingleOrDefault()?["CommandLine"]?.ToString();
				return commandLine;
			}

			try
			{
				var cmdlineFile = File.ReadAllText($"/proc/{process.Id}/cmdline");
				var parsed = cmdlineFile.Replace('\0', ' ');
				return parsed;
			}
			catch (FileNotFoundException)
			{
				return null;
			}
		}

		static bool TerminateAllEngineServers()
		{
			var result = false;
			foreach (var enumValue in Enum.GetValues<EngineType>())
				foreach (var proc in GetEngineServerProcessesOnPort(enumValue, null))
					using (proc)
					{
						proc.Kill();
						proc.WaitForExit();
						result = true;
					}

			return result;
		}

		static ushort FreeTcpPort(params ushort[] usedPorts)
		{
			ushort result;
			var listeners = new List<TcpListener>();

			try
			{
				do
				{
					var l = new TcpListener(IPAddress.Any, 0);
					l.Start();
					try
					{
						listeners.Add(l);
					}
					catch
					{
						using (l)
							l.Stop();
						throw;
					}

					result = (ushort)((IPEndPoint)l.LocalEndpoint).Port;
				}
				while (usedPorts.Contains(result) || result < 20000);
			}
			finally
			{
				foreach (var l in listeners)
					using (l)
						l.Stop();
			}

			Console.WriteLine($"Allocated port: {result}");
			return result;
		}

		[ClassInitialize]
		public static async Task Initialize(TestContext _)
		{
			// Clear problematic environment variables
			Environment.SetEnvironmentVariable("MSBuildExtensionsPath", null);
			Environment.SetEnvironmentVariable("MSBuildSDKsPath", null);

			if (TestingUtils.RunningInGitHubActions || String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TGS_TEST_GITHUB_TOKEN")))
				await TestingGitHubService.InitializeAndInject(default);

			await CachingFileDownloader.InitializeAndInjectForLiveTests(default);

			DummyChatProvider.RandomDisconnections(true);
			RestServerClientFactory.ApiClientFactory = new RateLimitRetryingApiClientFactory();

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

			try
			{
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
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested && TestingUtils.RunningInGitHubActions)
			{
				Assert.Fail("Could not connect to the test database! Try re-running failed jobs.");
			}
		}

		[ClassCleanup(ClassCleanupBehavior.EndOfClass)]
		public static void Cleanup()
		{
			CachingFileDownloader.Cleanup();
		}

		[TestMethod]
		public async Task TestUpdateProtocolAndDisabledOAuth()
		{
			using var server = new LiveTestingServer(null, false);
			using var serverCts = new CancellationTokenSource();
			var cancellationToken = serverCts.Token;
			var serverTask = server.Run(cancellationToken).AsTask();
			try
			{
				async ValueTask<ServerUpdateResponse> TestWithoutAndWithPermission(Func<ValueTask<ServerUpdateResponse>> action, IRestServerClient client, AdministrationRights right)
				{
					var ourUser = await client.Users.Read(cancellationToken);
					var update = new UserUpdateRequest
					{
						Id = ourUser.Id,
						PermissionSet = ourUser.PermissionSet,
					};

					update.PermissionSet.AdministrationRights &= ~right;
					await client.Users.Update(update, cancellationToken);

					await ApiAssert.ThrowsExactly<InsufficientPermissionsException, ServerUpdateResponse>(action);

					update.PermissionSet.AdministrationRights |= right;
					await client.Users.Update(update, cancellationToken);

					return await action();
				}

				await using (var adminClient = await CreateAdminClient(server.ApiUrl, cancellationToken))
				{
					// Disabled OAuth test
					using (var httpClient = new HttpClient())
					using (var request = new HttpRequestMessage(HttpMethod.Post, server.ApiUrl.ToString()))
					{
						request.Headers.Accept.Clear();
						request.Headers.UserAgent.Add(new ProductInfoHeaderValue("RootTest", "1.0.0"));
						request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
						request.Headers.Add(ApiHeaders.ApiVersionHeader, "Tgstation.Server.Api/" + ApiHeaders.Version);
						request.Headers.Authorization = new AuthenticationHeaderValue(ApiHeaders.OAuthAuthenticationScheme, adminClient.RestClient.Token.Bearer);
						request.Headers.Add(ApiHeaders.OAuthProviderHeader, Api.Models.OAuthProvider.GitHub.ToString());
						using var response = await httpClient.SendAsync(request, cancellationToken);
						Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
						var content = await response.Content.ReadAsStringAsync();
						var message = JsonConvert.DeserializeObject<ErrorMessageResponse>(content);
						Assert.AreEqual(Api.Models.ErrorCode.OAuthProviderDisabled, message.ErrorCode);
					}

					//attempt to update to stable
					await adminClient.Execute(
						async restClient =>
						{
							var responseModel = await TestWithoutAndWithPermission(
								() => restClient.Administration.Update(
									new ServerUpdateRequest
									{
										NewVersion = TestUpdateVersion,
										UploadZip = false,
									},
									null,
									cancellationToken),
								adminClient.RestClient,
								AdministrationRights.ChangeVersion);

							Assert.IsNotNull(responseModel);
							Assert.IsNull(responseModel.FileTicket);
							Assert.AreEqual(TestUpdateVersion, responseModel.NewVersion);
						},
						async gqlClient => await gqlClient.RunMutationEnsureNoErrors(
							gql => gql.RepositoryBasedServerUpdate.ExecuteAsync(TestUpdateVersion, cancellationToken),
							result => result.ChangeServerNodeVersionViaTrackedRepository,
							cancellationToken));

					try
					{
						var serverInfoTask = adminClient.RestClient.ServerInformation(cancellationToken).AsTask();
						var completedTask = await Task.WhenAny(serverTask, serverInfoTask);
						if (completedTask == serverInfoTask)
						{
							var serverInfo = await serverInfoTask;
							Assert.IsTrue(serverInfo.UpdateInProgress);
						}
					}
					catch (ServiceUnavailableException) { }
					catch (HttpRequestException) { }
				}

				async Task CheckUpdate()
				{
					//wait up to 3 minutes for the dl and install
					await Task.WhenAny(serverTask, Task.Delay(TimeSpan.FromMinutes(3), cancellationToken));

					Assert.IsTrue(serverTask.IsCompleted, "Server still running!");

					await serverTask;

					Assert.IsTrue(Directory.Exists(server.UpdatePath), "Update directory not present!");

					var updatedAssemblyPath = Path.Combine(server.UpdatePath, "Tgstation.Server.Host.dll");
					Assert.IsTrue(File.Exists(updatedAssemblyPath), "Updated assembly missing!");

					var updatedAssemblyVersion = FileVersionInfo.GetVersionInfo(updatedAssemblyPath);
					Assert.AreEqual(TestUpdateVersion, Version.Parse(updatedAssemblyVersion.FileVersion).Semver());
				}

				await CheckUpdate();

				// Second pass, uploaded updates
				var downloader = CachingFileDownloader.CreateRealDownloader(Mock.Of<ILogger<Host.IO.FileDownloader>>());
				var gitHubToken = Environment.GetEnvironmentVariable("TGS_TEST_GITHUB_TOKEN");
				if (String.IsNullOrWhiteSpace(gitHubToken))
					gitHubToken = null;
				await new Host.IO.DefaultIOManager(new FileSystem()).DeleteDirectory(server.UpdatePath, cancellationToken);
				serverTask = server.Run(cancellationToken).AsTask();

				await using (var adminClient = await CreateAdminClient(server.ApiUrl, cancellationToken))
				{
					// test we can't do this without the correct permission

					await using var download = downloader.DownloadFile(
						new Uri($"https://github.com/tgstation/tgstation-server/releases/download/tgstation-server-v{TestLiveServer.TestUpdateVersion}/ServerUpdatePackage.zip"),
						gitHubToken);

					var downloadStream = await download.GetResult(cancellationToken);
					var responseModel = await TestWithoutAndWithPermission(
						() => adminClient.RestClient.Administration.Update(
							new ServerUpdateRequest
							{
								NewVersion = TestUpdateVersion,
								UploadZip = true,
							},
							downloadStream,
							cancellationToken),
						adminClient.RestClient,
						AdministrationRights.UploadVersion);

					Assert.IsNotNull(responseModel);
					Assert.IsNotNull(responseModel.FileTicket);
					Assert.AreEqual(TestUpdateVersion, responseModel.NewVersion);
				}

				await CheckUpdate();
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
				await using var adminClient = await CreateAdminClient(server.ApiUrl, cancellationToken);
				await ApiAssert.ThrowsExactly<ConflictException, ServerUpdateResponse>(
					() => adminClient.RestClient.Administration.Update(
						new ServerUpdateRequest
						{
							NewVersion = testUpdateVersion
						},
						null,
						cancellationToken),
					Api.Models.ErrorCode.ResourceNotPresent);
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

			var controllerAddress = new Uri("http://localhost:15111");
			using (var controller = new LiveTestingServer(new SwarmConfiguration
			{
				Address = controllerAddress,
				Identifier = "controller",
				PrivateKey = PrivateKey,
				EndPoints = new List<HostingSpecification>
				{
					new HostingSpecification
					{
						Port = 15111,
					}
				}
			}, false, 15011))
			{
				using var serverCts = new CancellationTokenSource();
				serverCts.CancelAfter(TimeSpan.FromHours(3));
				var cancellationToken = serverCts.Token;
				var serverTask = controller.Run(cancellationToken).AsTask();

				try
				{
					await using var controllerClient = await CreateAdminClient(controller.ApiUrl, cancellationToken);

					var controllerInfo = await controllerClient.RestClient.ServerInformation(cancellationToken);

					static void CheckInfo(ServerInformationResponse serverInformation)
					{
						Assert.IsNotNull(serverInformation.SwarmServers);
						Assert.AreEqual(1, serverInformation.SwarmServers.Count);
						var controller = serverInformation.SwarmServers.SingleOrDefault(x => x.Identifier == "controller");
						Assert.IsNotNull(controller);
						Assert.AreEqual(controller.Address, new Uri("http://localhost:15111"));
						Assert.IsTrue(controller.Controller);
					}

					CheckInfo(controllerInfo);

					// test update
					await controllerClient.Execute(
						async restClient =>
						{
							var responseModel = await restClient.Administration.Update(
								new ServerUpdateRequest
								{
									NewVersion = TestUpdateVersion
								},
								null,
								cancellationToken);

							Assert.IsNotNull(responseModel);
							Assert.IsNull(responseModel.FileTicket);
							Assert.AreEqual(TestUpdateVersion, responseModel.NewVersion);
						},
						async gqlClient => await gqlClient.RunMutationEnsureNoErrors(
							gql => gql.RepositoryBasedServerUpdate.ExecuteAsync(TestUpdateVersion, cancellationToken),
							result => result.ChangeServerNodeVersionViaTrackedRepository,
							cancellationToken));

					await Task.WhenAny(Task.Delay(TimeSpan.FromMinutes(2)), serverTask);
					Assert.IsTrue(serverTask.IsCompleted);

					static void CheckServerUpdated(LiveTestingServer server)
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

			var controllerAddress = new Uri("http://localhost:15111");
			using (var controller = new LiveTestingServer(new SwarmConfiguration
			{
				Address = controllerAddress,
				Identifier = "controller",
				PrivateKey = PrivateKey,
				EndPoints = new List<HostingSpecification>
				{
					new HostingSpecification
					{
						Port = 15111,
					}
				}
			}, false, 15011))
			{
				using var node1 = new LiveTestingServer(new SwarmConfiguration
				{
					Address = new Uri("http://localhost:15112"),
					ControllerAddress = controllerAddress,
					Identifier = "node1",
					PrivateKey = PrivateKey,
					EndPoints = new List<HostingSpecification>
				{
					new HostingSpecification
					{
						Port = 15112,
					}
				}
				}, false, 15012);
				using var node2 = new LiveTestingServer(new SwarmConfiguration
				{
					Address = new Uri("http://localhost:15113"),
					ControllerAddress = controllerAddress,
					Identifier = "node2",
					PrivateKey = PrivateKey,
					EndPoints = new List<HostingSpecification>
				{
					new HostingSpecification
					{
						Port = 15113,
					}
				}
				}, false, 15013);
				using var serverCts = new CancellationTokenSource();
				var cancellationToken = serverCts.Token;
				var serverTask = Task.WhenAll(
					node1.Run(cancellationToken).AsTask(),
					node2.Run(cancellationToken).AsTask(),
					controller.Run(cancellationToken).AsTask());

				try
				{
					await using var controllerClient = await CreateAdminClient(controller.ApiUrl, cancellationToken);
					await using var node1Client = await CreateAdminClient(node1.ApiUrl, cancellationToken);
					await using var node2Client = await CreateAdminClient(node2.ApiUrl, cancellationToken);

					var controllerInfo = await controllerClient.RestClient.ServerInformation(cancellationToken);

					async Task WaitForSwarmServerUpdate()
					{
						ServerInformationResponse serverInformation;
						do
						{
							await Task.Delay(TimeSpan.FromSeconds(10));
							serverInformation = await node1Client.RestClient.ServerInformation(cancellationToken);
						}
						while (serverInformation.SwarmServers.Count == 1);
					}

					static void CheckInfo(ServerInformationResponse serverInformation)
					{
						Assert.IsNotNull(serverInformation.SwarmServers);
						Assert.AreEqual(3, serverInformation.SwarmServers.Count);

						var node1 = serverInformation.SwarmServers.SingleOrDefault(x => x.Identifier == "node1");
						Assert.IsNotNull(node1);
						Assert.AreEqual(node1.Address, new Uri("http://localhost:15112"));
						Assert.IsFalse(node1.Controller);

						var node2 = serverInformation.SwarmServers.SingleOrDefault(x => x.Identifier == "node2");
						Assert.IsNotNull(node2);
						Assert.AreEqual(node2.Address, new Uri("http://localhost:15113"));
						Assert.IsFalse(node2.Controller);

						var controller = serverInformation.SwarmServers.SingleOrDefault(x => x.Identifier == "controller");
						Assert.IsNotNull(controller);
						Assert.AreEqual(controller.Address, new Uri("http://localhost:15111"));
						Assert.IsTrue(controller.Controller);
					}

					CheckInfo(controllerInfo);

					// wait a few minutes for the updated server list to dispatch
					await Task.WhenAny(
						WaitForSwarmServerUpdate(),
						Task.Delay(TimeSpan.FromMinutes(4), cancellationToken));

					var node2Info = await node2Client.RestClient.ServerInformation(cancellationToken);
					var node1Info = await node1Client.RestClient.ServerInformation(cancellationToken);
					CheckInfo(node1Info);
					CheckInfo(node2Info);

					// check user info is shared
					var newUser = await node2Client.RestClient.Users.Create(new UserCreateRequest
					{
						Name = "asdf",
						Password = "asdfasdfasdfasdf",
						Enabled = true,
						PermissionSet = new PermissionSet
						{
							AdministrationRights = AdministrationRights.ChangeVersion
						}
					}, cancellationToken);

					var node1User = await node1Client.RestClient.Users.GetId(newUser, cancellationToken);
					Assert.AreEqual(newUser.Name, node1User.Name);
					Assert.AreEqual(newUser.Enabled, node1User.Enabled);

					await using var controllerUserClient = await restClientFactory.CreateFromLogin(
						controller.ApiUrl,
						newUser.Name,
						"asdfasdfasdfasdf");

					await using var node1TokenCopiedClient = restClientFactory.CreateFromToken(node1.RootUrl, controllerUserClient.Token);
					await node1TokenCopiedClient.Administration.Read(false, cancellationToken);

					// check instance info is not shared
					var controllerInstance = await controllerClient.RestClient.Instances.CreateOrAttach(
						new InstanceCreateRequest
						{
							Name = "ControllerInstance",
							Path = Path.Combine(controller.Directory, "ControllerInstance")
						},
						cancellationToken);

					var node2Instance = await node2Client.RestClient.Instances.CreateOrAttach(
						new InstanceCreateRequest
						{
							Name = "Node2Instance",
							Path = Path.Combine(node2.Directory, "Node2Instance")
						},
						cancellationToken);
					var node2InstanceList = await node2Client.RestClient.Instances.List(null, cancellationToken);
					Assert.AreEqual(1, node2InstanceList.Count);
					Assert.AreEqual(node2Instance.Id, node2InstanceList[0].Id);
					Assert.IsNotNull(await node2Client.RestClient.Instances.GetId(node2Instance, cancellationToken));
					var controllerInstanceList = await controllerClient.RestClient.Instances.List(null, cancellationToken);
					Assert.AreEqual(1, controllerInstanceList.Count);
					Assert.AreEqual(controllerInstance.Id, controllerInstanceList[0].Id);
					Assert.IsNotNull(await controllerClient.RestClient.Instances.GetId(controllerInstance, cancellationToken));

					await ApiAssert.ThrowsExactly<ConflictException, InstanceResponse>(() => controllerClient.RestClient.Instances.GetId(node2Instance, cancellationToken), Api.Models.ErrorCode.ResourceNotPresent);
					await ApiAssert.ThrowsExactly<ConflictException, InstanceResponse>(() => node1Client.RestClient.Instances.GetId(controllerInstance, cancellationToken), Api.Models.ErrorCode.ResourceNotPresent);

					// test update
					await node1Client.Execute(
						async restClient => await restClient.Administration.Update(
							new ServerUpdateRequest
							{
								NewVersion = TestUpdateVersion
							},
							null,
							cancellationToken),
						async gqlClient => await gqlClient.RunMutationEnsureNoErrors(
							gql => gql.RepositoryBasedServerUpdate.ExecuteAsync(TestUpdateVersion, cancellationToken),
							result => result.ChangeServerNodeVersionViaTrackedRepository,
							cancellationToken));
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
						EndPoints = new List<HostingSpecification>
						{
							new HostingSpecification
							{
								Port = 15111,
							}
						}
					});
					serverTask = Task.WhenAll(
						controller.Run(cancellationToken).AsTask(),
						node1.Run(cancellationToken).AsTask());

					await using var controllerClient2 = await CreateAdminClient(controller.ApiUrl, cancellationToken);
					await using var node1Client2 = await CreateAdminClient(node1.ApiUrl, cancellationToken);

					await controllerClient2.Execute(
						async restClient => await ApiAssert.ThrowsExactly<ApiConflictException, ServerUpdateResponse>(
							() => restClient.Administration.Update(
								new ServerUpdateRequest
								{
									NewVersion = TestUpdateVersion
								},
								null,
								cancellationToken),
							Api.Models.ErrorCode.SwarmIntegrityCheckFailed),
						async gqlClient => await ApiAssert.OperationFails(
							gqlClient,
							gql => gql.RepositoryBasedServerUpdate.ExecuteAsync(TestUpdateVersion, cancellationToken),
							result => result.ChangeServerNodeVersionViaTrackedRepository,
							Client.GraphQL.ErrorCode.SwarmIntegrityCheckFailed,
							cancellationToken));

					// regression: test updating also works from the controller
					serverTask = Task.WhenAll(
						serverTask,
						node2.Run(cancellationToken).AsTask());

					await using var node2Client2 = await CreateAdminClient(node2.ApiUrl, cancellationToken);

					async Task WaitForSwarmServerUpdate2()
					{
						ServerInformationResponse serverInformation;
						do
						{
							await Task.Delay(TimeSpan.FromSeconds(10));
							serverInformation = await node2Client2.RestClient.ServerInformation(cancellationToken);
						}
						while (serverInformation.SwarmServers.Count == 1);
					}

					await Task.WhenAny(
						WaitForSwarmServerUpdate2(),
						Task.Delay(TimeSpan.FromMinutes(4), cancellationToken));

					var node2Info2 = await node2Client2.RestClient.ServerInformation(cancellationToken);
					var node1Info2 = await node1Client2.RestClient.ServerInformation(cancellationToken);
					CheckInfo(node1Info2);
					CheckInfo(node2Info2);

					// also test with uploaded updates this time
					var downloader = CachingFileDownloader.CreateRealDownloader(Mock.Of<ILogger<Host.IO.FileDownloader>>());
					var gitHubToken = Environment.GetEnvironmentVariable("TGS_TEST_GITHUB_TOKEN");
					if (String.IsNullOrWhiteSpace(gitHubToken))
						gitHubToken = null;
					await using var download = downloader.DownloadFile(
						new Uri($"https://github.com/tgstation/tgstation-server/releases/download/tgstation-server-v{TestLiveServer.TestUpdateVersion}/ServerUpdatePackage.zip"),
						gitHubToken);

					var downloadStream = await download.GetResult(cancellationToken);
					var responseModel = await controllerClient2.RestClient.Administration.Update(
						new ServerUpdateRequest
						{
							NewVersion = TestUpdateVersion,
							UploadZip = true,
						},
						downloadStream,
						cancellationToken);

					Assert.IsNotNull(responseModel);
					Assert.IsNotNull(responseModel.FileTicket);
					Assert.AreEqual(TestUpdateVersion, responseModel.NewVersion);

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

			var controllerAddress = new Uri("http://localhost:15111");
			using (var controller = new LiveTestingServer(new SwarmConfiguration
			{
				Address = controllerAddress,
				Identifier = "controller",
				PublicAddress = new Uri("http://fakecontroller.com"),
				PrivateKey = PrivateKey,
				UpdateRequiredNodeCount = 2,
				EndPoints = new List<HostingSpecification>
				{
					new HostingSpecification
					{
						Port = 15111,
					}
				}
			}, false, 15011))
			{
				using var node1 = new LiveTestingServer(new SwarmConfiguration
				{
					Address = new Uri("http://localhost:15112"),
					ControllerAddress = controllerAddress,
					PublicAddress = new Uri("http://fakenode1.com"),
					Identifier = "node1",
					PrivateKey = PrivateKey,
					EndPoints = new List<HostingSpecification>
					{
						new HostingSpecification
						{
							Port = 15112,
						}
					}
				}, false, 15012);
				using var node2 = new LiveTestingServer(new SwarmConfiguration
				{
					Address = new Uri("http://localhost:15113"),
					ControllerAddress = controllerAddress,
					Identifier = "node2",
					PrivateKey = PrivateKey,
					EndPoints = new List<HostingSpecification>
					{
						new HostingSpecification
						{
							Port = 15113,
						}
					}
				}, false, 15013);
				using var serverCts = new CancellationTokenSource();

				var cancellationToken = serverCts.Token;
				using var node1Cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

				Task node1Task, node2Task, controllerTask;
				var serverTask = Task.WhenAll(
					node1Task = node1.Run(node1Cts.Token).AsTask(),
					node2Task = node2.Run(cancellationToken).AsTask(),
					controllerTask = controller.Run(cancellationToken).AsTask());

				try
				{
					await using var controllerClient = await CreateAdminClient(controller.ApiUrl, cancellationToken);
					await using var node1Client = await CreateAdminClient(node1.ApiUrl, cancellationToken);
					await using var node2Client = await CreateAdminClient(node2.ApiUrl, cancellationToken);

					// test a token signed from any one node will work on another
					var token = node2Client.RestClient.Token;
					var testNode1Client = restClientFactory.CreateFromToken(node1.ApiUrl, token);

					await testNode1Client.ServerInformation(cancellationToken);

					var controllerInfo = await controllerClient.RestClient.ServerInformation(cancellationToken);

					async Task WaitForSwarmServerUpdate(IRestServerClient client, int currentServerCount)
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
						Assert.AreEqual(node1.Address, new Uri("http://localhost:15112"));
						Assert.IsFalse(node1.Controller);
						Assert.AreEqual(node1.PublicAddress, new Uri("http://fakenode1.com"));

						var node2 = serverInformation.SwarmServers.SingleOrDefault(x => x.Identifier == "node2");
						Assert.IsNotNull(node2);
						Assert.AreEqual(node2.Address, new Uri("http://localhost:15113"));
						Assert.IsFalse(node2.Controller);
						Assert.IsNull(node2.PublicAddress);

						var controller = serverInformation.SwarmServers.SingleOrDefault(x => x.Identifier == "controller");
						Assert.IsNotNull(controller);
						Assert.AreEqual(controller.Address, new Uri("http://localhost:15111"));
						Assert.IsTrue(controller.Controller);
						Assert.AreEqual(controller.PublicAddress, new Uri("http://fakecontroller.com"));
					}

					CheckInfo(controllerInfo);

					// wait a few minutes for the updated server list to dispatch
					await Task.WhenAny(
						WaitForSwarmServerUpdate(node1Client.RestClient, 1),
						Task.Delay(TimeSpan.FromMinutes(4), cancellationToken));

					var node2Info = await node2Client.RestClient.ServerInformation(cancellationToken);
					var node1Info = await node1Client.RestClient.ServerInformation(cancellationToken);
					CheckInfo(node1Info);
					CheckInfo(node2Info);

					// kill node1
					node1Cts.Cancel();
					await Task.WhenAny(
						node1Task,
						Task.Delay(TimeSpan.FromMinutes(1)));
					Assert.IsTrue(node1Task.IsCompleted);

					// it should unregister
					controllerInfo = await controllerClient.RestClient.ServerInformation(cancellationToken);
					Assert.AreEqual(2, controllerInfo.SwarmServers.Count);
					Assert.IsFalse(controllerInfo.SwarmServers.Any(x => x.Identifier == "node1"));

					// wait a few minutes for the updated server list to dispatch
					await Task.WhenAny(
						WaitForSwarmServerUpdate(node2Client.RestClient, 3),
						Task.Delay(TimeSpan.FromMinutes(4), cancellationToken));

					node2Info = await node2Client.RestClient.ServerInformation(cancellationToken);
					Assert.AreEqual(2, node2Info.SwarmServers.Count);
					Assert.IsFalse(node2Info.SwarmServers.Any(x => x.Identifier == "node1"));

					// restart the controller
					await controllerClient.Execute(
						restClient => restClient.Administration.Restart(cancellationToken),
						async gqlClient => await gqlClient.RunMutationEnsureNoErrors(
							gql => gql.RestartServer.ExecuteAsync(cancellationToken),
							result => result.RestartServerNode,
							cancellationToken));

					await Task.WhenAny(
						controllerTask,
						Task.Delay(TimeSpan.FromMinutes(1), cancellationToken));
					Assert.IsTrue(controllerTask.IsCompleted);

					controllerTask = controller.Run(cancellationToken).AsTask();
					await using var controllerClient2 = await CreateAdminClient(controller.ApiUrl, cancellationToken);

					// node 2 should reconnect once it's health check triggers
					await Task.WhenAny(
						WaitForSwarmServerUpdate(controllerClient2.RestClient, 1),
						Task.Delay(TimeSpan.FromMinutes(5), cancellationToken));

					controllerInfo = await controllerClient2.RestClient.ServerInformation(cancellationToken);
					Assert.AreEqual(2, controllerInfo.SwarmServers.Count);
					Assert.IsNotNull(controllerInfo.SwarmServers.SingleOrDefault(x => x.Identifier == "node2"));

					// wait a few seconds to dispatch the updated list to node2
					await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

					// restart node2
					await node2Client.Execute(
						restClient => restClient.Administration.Restart(cancellationToken),
						async gqlClient => await gqlClient.RunMutationEnsureNoErrors(
							gql => gql.RestartServer.ExecuteAsync(cancellationToken),
							result => result.RestartServerNode,
							cancellationToken));
					await Task.WhenAny(
						node2Task,
						Task.Delay(TimeSpan.FromMinutes(1)));
					Assert.IsTrue(node1Task.IsCompleted);

					// should have unregistered
					controllerInfo = await controllerClient2.RestClient.ServerInformation(cancellationToken);
					Assert.AreEqual(1, controllerInfo.SwarmServers.Count);
					Assert.IsNull(controllerInfo.SwarmServers.SingleOrDefault(x => x.Identifier == "node2"));

					// update should fail
					await controllerClient2.Execute(
						async restClient => await ApiAssert.ThrowsExactly<ApiConflictException, ServerUpdateResponse>(
							() => restClient.Administration.Update(
								new ServerUpdateRequest
								{
									NewVersion = TestUpdateVersion
								},
								null,
								cancellationToken),
							Api.Models.ErrorCode.SwarmIntegrityCheckFailed),
						async gqlClient => await ApiAssert.OperationFails(
							gqlClient,
							gql => gql.RepositoryBasedServerUpdate.ExecuteAsync(TestUpdateVersion, cancellationToken),
							result => result.ChangeServerNodeVersionViaTrackedRepository,
							Client.GraphQL.ErrorCode.SwarmIntegrityCheckFailed,
							cancellationToken));

					node2Task = node2.Run(cancellationToken).AsTask();
					await using var node2Client2 = await CreateAdminClient(node2.ApiUrl, cancellationToken);

					// should re-register
					await Task.WhenAny(
						WaitForSwarmServerUpdate(node2Client2.RestClient, 1),
						Task.Delay(TimeSpan.FromMinutes(4), cancellationToken));

					node2Info = await node2Client2.RestClient.ServerInformation(cancellationToken);
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
		public async Task TestTgstationHeadless() => await TestTgstation(false);

		async ValueTask TestTgstation(bool interactive)
		{
			// i'm only running this on dev machines, actions is too taxed
			if (TestingUtils.RunningInGitHubActions)
				Assert.Inconclusive("lol. lmao.");

			var discordConnectionString = Environment.GetEnvironmentVariable("TGS_TEST_DISCORD_TOKEN");

			var procs = System.Diagnostics.Process.GetProcessesByName("byond");
			if (procs.Length != 0)
			{
				foreach (var proc in procs)
					proc.Dispose();

				// Inconclusive and not fail because we don't want to unexpectedly kill a dev's BYOND.exe
				Assert.Inconclusive("Cannot run server test because DreamDaemon will not start headless while the BYOND pager is running!");
			}


			using var loggerFactory = LoggerFactory.Create(builder =>
			{
				builder.AddConsole();
				builder.SetMinimumLevel(LogLevel.Trace);
			});
			using var server = new LiveTestingServer(null, true);

			TerminateAllEngineServers();

			using var serverCts = new CancellationTokenSource();
			var cancellationToken = serverCts.Token;
			var serverTask = server.Run(cancellationToken);
			try
			{
				await using var adminClient = await CreateAdminClient(server.ApiUrl, cancellationToken);
				var restAdminClient = adminClient.RestClient;
				var instanceManagerTest = new InstanceManagerTest(restAdminClient, server.Directory);
				var instance = await instanceManagerTest.CreateTestInstance("TgTestInstance", cancellationToken);
				var instanceClient = restAdminClient.Instances.CreateClient(instance);


				var ddUpdateTask = instanceClient.DreamDaemon.Update(new DreamDaemonRequest
				{
					LogOutput = true,
				}, cancellationToken);
				var dmUpdateTask = instanceClient.DreamMaker.Update(new DreamMakerRequest
				{
					ApiValidationSecurityLevel = DreamDaemonSecurity.Trusted,
				}, cancellationToken);

				var ioManager = new Host.IO.DefaultIOManager(new FileSystem());
				var repoPath = ioManager.ConcatPath(instance.Path, "Repository");
				await using var jobsTest = new JobsRequiredTest(instanceClient.Jobs);

				await jobsTest.HubConnectionTask;

				var postWriteHandler = (Host.IO.IPostWriteHandler)(new PlatformIdentifier().IsWindows
					? new Host.IO.WindowsPostWriteHandler()
					: new Host.IO.PosixPostWriteHandler(loggerFactory.CreateLogger<Host.IO.PosixPostWriteHandler>()));
				var localRepoPath = Environment.GetEnvironmentVariable("TGS_LOCAL_TG_REPO");
				Task jobWaitTask;
				if (!String.IsNullOrWhiteSpace(localRepoPath))
				{
					await ioManager.CopyDirectory(
						[],
						(src, dest) =>
						{
							if (postWriteHandler.NeedsPostWrite(src))
								postWriteHandler.HandleWrite(dest);

							return ValueTask.CompletedTask;
						},
						ioManager.ConcatPath(
							localRepoPath,
							".git"),
						ioManager.ConcatPath(
							repoPath,
							".git"),
						null,
						cancellationToken);

					ProcessExecutor processExecutor = null;
					processExecutor = new ProcessExecutor(
						new PlatformIdentifier().IsWindows
							? new WindowsProcessFeatures(loggerFactory.CreateLogger<WindowsProcessFeatures>())
							: new PosixProcessFeatures(new Lazy<IProcessExecutor>(() => processExecutor), ioManager, loggerFactory.CreateLogger<PosixProcessFeatures>()),
						ioManager,
						loggerFactory.CreateLogger<ProcessExecutor>(),
						loggerFactory);

					async ValueTask RunGitCommand(string args)
					{
						await using var gitRemoteOriginFixProc = await processExecutor.LaunchProcess(
							"git",
							repoPath,
							args,
							cancellationToken,
							null,
							null,
							true,
							true);

						int? exitCode;
						using (cancellationToken.Register(gitRemoteOriginFixProc.Terminate))
							exitCode = await gitRemoteOriginFixProc.Lifetime;

						loggerFactory.CreateLogger("TgTest").LogInformation("git {args} output:{newLine}{output}",args, Environment.NewLine, await gitRemoteOriginFixProc.GetCombinedOutput(cancellationToken));
						Assert.AreEqual(0, exitCode);
					}

					await RunGitCommand("remote set-url origin https://github.com/tgstation/tgstation");
					await RunGitCommand("checkout -f master");
					await RunGitCommand("reset --hard origin/master");

					jobWaitTask = Task.CompletedTask;
				}
				else
				{
					var repoResponse = await instanceClient.Repository.Clone(new RepositoryCreateRequest
					{
						Origin = new Uri("https://github.com/tgstation/tgstation"),
					}, cancellationToken);
					jobWaitTask = jobsTest.WaitForJob(repoResponse.ActiveJob, 300, false, null, cancellationToken);
				}

				await Task.WhenAll(jobWaitTask, ddUpdateTask.AsTask(), dmUpdateTask.AsTask());

				var depsBytesTask = ioManager.ReadAllBytes(
					ioManager.ConcatPath(repoPath, "dependencies.sh"),
					cancellationToken);

				var scriptsCopyTask = ioManager.CopyDirectory(
					[],
					(src, dest) =>
					{
						if (postWriteHandler.NeedsPostWrite(src))
							postWriteHandler.HandleWrite(dest);

						return ValueTask.CompletedTask;
					},
					ioManager.ConcatPath(
						repoPath,
						"tools",
						"tgs_scripts"),
					ioManager.ConcatPath(
						instance.Path,
						"Configuration",
						"EventScripts"),
					null,
					cancellationToken);

				var dependenciesSh = Encoding.UTF8.GetString((await depsBytesTask).Span);
				var lines = dependenciesSh.Split("\n", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
				const string MajorPrefix = "export BYOND_MAJOR=";
				var major = Int32.Parse(lines.First(x => x.StartsWith(MajorPrefix))[MajorPrefix.Length..]);
				const string MinorPrefix = "export BYOND_MINOR=";
				var minor = Int32.Parse(lines.First(x => x.StartsWith(MinorPrefix))[MinorPrefix.Length..]);

				var byondJob = await instanceClient.Engine.SetActiveVersion(new EngineVersionRequest
				{
					EngineVersion = new EngineVersion
					{
						Version = new Version(major, minor),
						Engine = EngineType.Byond,
					},
				}, null, cancellationToken);

				var byondJobTask = jobsTest.WaitForJob(byondJob.InstallJob, 60, false, null, cancellationToken);

				await Task.WhenAll(scriptsCopyTask.AsTask(), byondJobTask);

				var compileJob = await instanceClient.DreamMaker.Compile(cancellationToken);

				await jobsTest.WaitForJob(compileJob, 180, false, null, cancellationToken);

				var startJob = await instanceClient.DreamDaemon.Start(cancellationToken);
				await jobsTest.WaitForJob(startJob, 30, false, null, cancellationToken);

				var compileJob2 = await instanceClient.DreamMaker.Compile(cancellationToken);

				await jobsTest.WaitForJob(compileJob2, 360, false, null, cancellationToken);

				if (interactive)
				{
					bool updated = false;
					while (true)
					{
						await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

						var status = await instanceClient.DreamDaemon.Read(cancellationToken);

						if (updated)
						{
							if (status.Status == WatchdogStatus.Offline)
								break;
						}
						else if (status.StagedCompileJob == null)
						{
							updated = true;
							await instanceClient.DreamDaemon.Update(new DreamDaemonRequest
							{
								SoftShutdown = true
							}, cancellationToken);
						}
					}
				}
			}
			finally
			{
				serverCts.Cancel();
				await serverTask;
			}
		}

		[TestMethod]
		public Task TestStandardTgsOperation() => TestStandardTgsOperation(false);

		[TestMethod]
		public Task TestOpenDreamExclusiveTgsOperation()
		{
			if (!Boolean.TryParse(Environment.GetEnvironmentVariable("TGS_TEST_OD_EXCLUSIVE"), out var odExclusive) || !odExclusive)
				Assert.Inconclusive("This test is covered by TestStandardTgsOperation");

			return TestStandardTgsOperation(true);
		}

		async Task TestStandardTgsOperation(bool openDreamOnly)
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
				Assert.AreEqual(ProcessPriorityClass.Normal, currentProcess.PriorityClass);

			InitializePorts();

			var maximumTestMinutes = TestingUtils.RunningInGitHubActions ? 90 : 20;
			using var hardCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(maximumTestMinutes));
			var hardCancellationToken = hardCancellationTokenSource.Token;

			hardCancellationToken.Register(() => Console.WriteLine("TGS TEST CANCELLED TOKEN DUE TO TIMEOUT"));

			ServiceCollectionExtensions.UseAdditionalLoggerProvider<HardFailLoggerProvider>();

			var failureTask = HardFailLoggerProvider.FailureSource;
			var internalTask = TestTgsInternal(openDreamOnly, hardCancellationToken);
			await Task.WhenAny(
				internalTask,
				failureTask);

			if (!internalTask.IsCompleted)
			{
				Console.WriteLine("TGS TEST CANCELLING TOKEN DUE TO ERROR");
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

		async Task TestTgsInternal(bool openDreamOnly, CancellationToken hardCancellationToken)
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

			// uncomment to force this test to run with basic watchdog
			// Environment.SetEnvironmentVariable("General__UseBasicWatchdog", "true");

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
			if (procs.Length != 0)
			{
				foreach (var proc in procs)
					proc.Dispose();

				// Inconclusive and not fail because we don't want to unexpectedly kill a dev's BYOND.exe
				Assert.Inconclusive("Cannot run server test because DreamDaemon will not start headless while the BYOND pager is running!");
			}

			if (TerminateAllEngineServers())
				await Task.Delay(TimeSpan.FromSeconds(5), hardCancellationToken);

			using var server = new LiveTestingServer(null, true);

			using var serverCts = CancellationTokenSource.CreateLinkedTokenSource(hardCancellationToken);
			var cancellationToken = serverCts.Token;

			for (var i = 0; i < 50; ++i)
				await Task.Yield();

			InstanceManager GetInstanceManager() => ((Host.Server)server.RealServer).Host.Services.GetRequiredService<InstanceManager>();
			ILogger GetLogger() => ((Host.Server)server.RealServer).Host.Services.GetRequiredService<ILogger<TestLiveServer>>();

			// main run
			var serverTask = server.Run(cancellationToken).AsTask();

			Host.IO.IFileDownloader GetFileDownloader() => ((Host.Server)server.RealServer).Host.Services.GetRequiredService<Host.IO.IFileDownloader>();
			if (serverTask.IsFaulted)
				await serverTask;

			var graphQLClientFactory = new GraphQLServerClientFactory(restClientFactory);
			try
			{
				Api.Models.Instance instance;
				long initialStaged, initialActive, initialSessionId;

				await using var firstAdminMultiClient = await CreateAdminClient(server.ApiUrl, cancellationToken);

				var firstAdminRestClient = firstAdminMultiClient.RestClient;

				if (MultiServerClient.UseGraphQL)
					await using (var tokenOnlyGraphQLClient = graphQLClientFactory.CreateFromToken(server.RootUrl, firstAdminRestClient.Token.Bearer))
					{
						// just testing auth works the same here
						var result = await tokenOnlyGraphQLClient.RunOperation(client => client.ServerVersion.ExecuteAsync(cancellationToken), cancellationToken);
						Assert.IsTrue(result.IsSuccessResult());
					}

				await using (var tokenOnlyRestClient = restClientFactory.CreateFromToken(server.RootUrl, firstAdminRestClient.Token))
				{
					// regression test for password change issue
					var currentUser = await tokenOnlyRestClient.Users.Read(cancellationToken);
					var updatedUser = await tokenOnlyRestClient.Users.Update(new UserUpdateRequest
					{
						Id = currentUser.Id,
						Password = DefaultCredentials.DefaultAdminUserPassword,
					}, cancellationToken);

					await ApiAssert.ThrowsExactly<UnauthorizedException, UserResponse>(() => tokenOnlyRestClient.Users.Read(cancellationToken), null);
				}

				// basic graphql test, to be used everywhere eventually
				if (MultiServerClient.UseGraphQL)
					await using (var unauthenticatedGraphQLClient = graphQLClientFactory.CreateUnauthenticated(server.RootUrl))
					{
						// check auth works as expected
						var result = await unauthenticatedGraphQLClient.RunOperation(client => client.ServerVersion.ExecuteAsync(cancellationToken), cancellationToken);
						Assert.IsTrue(result.IsErrorResult());

						// test getting server info
						var unAuthedMultiClient = new MultiServerClient(firstAdminRestClient, unauthenticatedGraphQLClient);

						await unauthenticatedGraphQLClient.RunQueryEnsureNoErrors(
							gqlClient => gqlClient.UnauthenticatedServerInformation.ExecuteAsync(cancellationToken),
							cancellationToken);

						var testObserver = new HoldLastObserver<IOperationResult<ISessionInvalidationResult>>();
						using var subscription = await unauthenticatedGraphQLClient.Subscribe(
							gql => gql.SessionInvalidation.Watch(),
							testObserver,
							cancellationToken);

						await Task.Delay(1000, cancellationToken);

						Assert.AreEqual(0U, testObserver.ErrorCount);
						Assert.AreEqual(1U, testObserver.ResultCount);
						Assert.IsTrue(testObserver.LastValue.IsAuthenticationError());
						Assert.IsTrue(testObserver.Completed);
					}

				async ValueTask<MultiServerClient> CreateUserWithNoInstancePerms()
				{
					var createRequest = new UserCreateRequest()
					{
						Name = "SomePermlessChum",
						Password = "alidfjuwh84322r4yrkajhfdqh38hrfiouw4",
						Enabled = true,
						PermissionSet = new PermissionSet
						{
							InstanceManagerRights = InstanceManagerRights.Read,
						}
					};

					var user = await firstAdminRestClient.Users.Create(createRequest, cancellationToken);
					Assert.IsTrue(user.Enabled);

					return await CreateClient(server.RootUrl, createRequest.Name, createRequest.Password, false, cancellationToken);
				}

				var restartObserver = new HoldLastObserver<IOperationResult<ISessionInvalidationResult>>();
				IDisposable restartSubscription;
				var jobsHubTest = new JobsHubTests(firstAdminMultiClient, await CreateUserWithNoInstancePerms());
				Task jobsHubTestTask;
				{
					if (server.DumpOpenApiSpecpath)
					{
						// Dump swagger to disk
						// This is purely for CI
						using var httpClient = new HttpClient();
						var webRequestTask = httpClient.GetAsync(server.ApiUrl.ToString() + "doc/tgs_api.json", cancellationToken);
						using var response = await webRequestTask;
						response.EnsureSuccessStatusCode();
						await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
						await using var output = new FileStream(@"C:\tgs_api.json", FileMode.Create);
						await content.CopyToAsync(output, cancellationToken);
					}

					async Task FailFast(Task task)
					{
						try
						{
							await task;
						}
						catch (Exception ex) when (ex is not OperationCanceledException)
						{
							Console.WriteLine($"[{DateTimeOffset.UtcNow}] TEST ERROR: {ex}");
							serverCts.Cancel();
							throw;
						}
					}

					Task nonInstanceTests;
					IInstanceClient instanceClient = null;
					InstanceResponse odInstance, compatInstance;
					if (!openDreamOnly)
					{
						// force a session refresh if necessary
						if (MultiServerClient.UseGraphQL)
							await firstAdminMultiClient.GraphQLClient.RunQueryEnsureNoErrors(
								gql => gql.ReadCurrentUser.ExecuteAsync(cancellationToken),
								cancellationToken);

						jobsHubTestTask = FailFast(await jobsHubTest.Run(cancellationToken)); // returns Task<Task>
						var rootTest = FailFast(RawRequestTests.Run(restClientFactory, firstAdminRestClient, cancellationToken, out var signalRTestTask));
						var adminTest = FailFast(new AdministrationTest(firstAdminMultiClient).Run(cancellationToken));
						var usersTest = FailFast(new UsersTest(firstAdminMultiClient).Run(cancellationToken, signalRTestTask).AsTask());

						var instanceManagerTest = new InstanceManagerTest(firstAdminRestClient, server.Directory);
						var compatInstanceTask = instanceManagerTest.CreateTestInstance("CompatTestsInstance", cancellationToken);
						var odInstanceTask = instanceManagerTest.CreateTestInstance("OdTestsInstance", cancellationToken);
						var byondApiCompatInstanceTask = instanceManagerTest.CreateTestInstance("BCAPITestsInstance", cancellationToken);
						instance = await instanceManagerTest.CreateTestInstance("LiveTestsInstance", cancellationToken);
						compatInstance = await compatInstanceTask;
						odInstance = await odInstanceTask;
						var byondApiCompatInstance = await byondApiCompatInstanceTask;
						var instancesTest = FailFast(instanceManagerTest.RunPreTest(cancellationToken));
						Assert.IsTrue(Directory.Exists(instance.Path));
						instanceClient = firstAdminRestClient.Instances.CreateClient(instance);

						Assert.IsTrue(Directory.Exists(instanceClient.Metadata.Path));
						nonInstanceTests = Task.WhenAll(instancesTest, adminTest, rootTest, usersTest);
					}
					else
					{
						compatInstance = null;
						nonInstanceTests = Task.CompletedTask;
						jobsHubTestTask = null;
						instance = null;
						var instanceManagerTest = new InstanceManagerTest(firstAdminRestClient, server.Directory);
						var odInstanceTask = instanceManagerTest.CreateTestInstance("OdTestsInstance", cancellationToken);
						odInstance = await odInstanceTask;
					}

					var instanceTest = new InstanceTest(
						firstAdminRestClient.Instances,
						GetFileDownloader(),
						GetInstanceManager(),
						(ushort)server.ApiUrl.Port);

					async Task RunInstanceTests()
					{
						var testSerialized = TestingUtils.RunningInGitHubActions; // they only have 2 cores, can't handle intense parallelization
						async Task ODCompatTests()
						{
							var edgeODVersionTask = EngineTest.GetEdgeVersion(EngineType.OpenDream, GetLogger(), GetFileDownloader(), cancellationToken);

							var ex = await Assert.ThrowsExactlyAsync<JobException>(
								() => InstanceTest.DownloadEngineVersion(
									new EngineVersion
									{
										Engine = EngineType.OpenDream,
										SourceSHA = "f1dc153caf9d84cd1d0056e52286cc0163e3f4d3", // 1 before verified version
									},
									GetFileDownloader(),
									server.OpenDreamUrl,
									cancellationToken).AsTask());

							Assert.AreEqual(Api.Models.ErrorCode.OpenDreamTooOld, ex.ErrorCode);

							await instanceTest
								.RunCompatTests(
									await edgeODVersionTask,
									server.OpenDreamUrl,
									firstAdminRestClient.Instances.CreateClient(odInstance),
									odDMPort.Value,
									odDDPort.Value,
									server.HighPriorityDreamDaemon,
									server.UsingBasicWatchdog,
									cancellationToken);
						}

						var odCompatTests = FailFast(ODCompatTests());

						if (openDreamOnly || testSerialized)
							await odCompatTests;

						if (openDreamOnly)
							return;

						var windowsMinCompat = new Version(510, 1346);
						var linuxMinCompat = new Version(512, 1451); // http://www.byond.com/forum/?forum=5&command=search&scope=local&text=resolved%3a512.1451
						await CachingFileDownloader.InitializeByondVersion(
							GetLogger(),
							new PlatformIdentifier().IsWindows
								? windowsMinCompat
								: linuxMinCompat,
							new PlatformIdentifier().IsWindows,
							cancellationToken);

						var compatTests = FailFast(
							instanceTest
								.RunCompatTests(
									new EngineVersion
									{
										Engine = EngineType.Byond,
										Version = new PlatformIdentifier().IsWindows
											? windowsMinCompat
											: linuxMinCompat,
									},
									server.OpenDreamUrl,
									firstAdminRestClient.Instances.CreateClient(compatInstance),
									compatDMPort.Value,
									compatDDPort.Value,
									server.HighPriorityDreamDaemon,
									server.UsingBasicWatchdog,
									cancellationToken));

						if (testSerialized)
							await compatTests;

						await FailFast(
							instanceTest
								.RunTests(
									GetLogger(),
									instanceClient,
									mainDMPort.Value,
									mainDDPort.Value,
									server.HighPriorityDreamDaemon,
									server.LowPriorityDeployments,
									server.UsingBasicWatchdog,
									cancellationToken));

						await compatTests;
						await odCompatTests;
					}

					var instanceTests = RunInstanceTests();

					await Task.WhenAll(nonInstanceTests, instanceTests);

					if (openDreamOnly)
						return;

					var dd = await instanceClient.DreamDaemon.Read(cancellationToken);
					Assert.AreEqual(WatchdogStatus.Online, dd.Status.Value);
					Assert.IsNotNull(dd.StagedCompileJob);
					Assert.AreNotEqual(dd.StagedCompileJob.Id, dd.ActiveCompileJob.Id);
					initialActive = dd.ActiveCompileJob.Id.Value;
					initialStaged = dd.StagedCompileJob.Id.Value;
					initialSessionId = dd.SessionId.Value;

					// force a session refresh if necessary
					if (MultiServerClient.UseGraphQL)
					{
						await firstAdminMultiClient.GraphQLClient.RunQueryEnsureNoErrors(
							gql => gql.ReadCurrentUser.ExecuteAsync(cancellationToken),
							cancellationToken);

						restartSubscription = await firstAdminMultiClient.GraphQLClient.Subscribe(
							gql => gql.SessionInvalidation.Watch(),
							restartObserver,
							cancellationToken);
					}
					else
						restartSubscription = null;

					try
					{
						await Task.Delay(1000, cancellationToken);

						jobsHubTest.ExpectShutdown();
						await firstAdminMultiClient.Execute(
							restClient => restClient.Administration.Restart(cancellationToken),
							async gqlClient => await gqlClient.RunMutationEnsureNoErrors(
								gql => gql.RestartServer.ExecuteAsync(cancellationToken),
								result => result.RestartServerNode,
								cancellationToken));
					}
					catch
					{
						restartSubscription?.Dispose();
						throw;
					}
				}

				try
				{
					await Task.WhenAny(serverTask, Task.Delay(TimeSpan.FromMinutes(1), cancellationToken));
					Assert.IsTrue(serverTask.IsCompleted);

					if (MultiServerClient.UseGraphQL)
					{
						Assert.AreEqual(0U, restartObserver.ErrorCount);
						Assert.AreEqual(1U, restartObserver.ResultCount);
						restartObserver.LastValue.EnsureNoErrors();
						Assert.IsTrue(restartObserver.Completed);
						Assert.AreEqual(SessionInvalidationReason.ServerShutdown, restartObserver.LastValue.Data.SessionInvalidated);
					}
				}
				finally
				{
					restartSubscription?.Dispose();
				}

				// test the reattach message queueing
				// for the code coverage really...
				var topicRequestResult = await WatchdogTest.SendTestTopic(
					"tgs_integration_test_tactics6=1",
					WatchdogTest.StaticTopicClient,
					null,
					mainDDPort.Value,
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
						blockingSocket.Bind(new IPEndPoint(IPAddress.Any, server.ApiUrl.Port));
						// bind test run
						await server.Run(cancellationToken);
						Assert.Fail("Expected server task to end with a SocketException");
					}
					catch (SocketException ex)
					{
						Assert.AreEqual(SocketError.AccessDenied, ex.SocketErrorCode);
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
				serverTask = server.Run(cancellationToken).AsTask();
				await using (var multiClient = await CreateAdminClient(server.ApiUrl, cancellationToken))
				{
					var adminClient = multiClient.RestClient;

					await jobsHubTest.WaitForReconnect(cancellationToken);
					var instanceClient = adminClient.Instances.CreateClient(instance);

					var jobs = await instanceClient.Jobs.ListActive(null, cancellationToken);
					if (jobs.Count == 0)
					{
						var entities = await instanceClient.Jobs.List(null, cancellationToken);
						var getTasks = entities
							.Select(e => instanceClient.Jobs.GetId(e, cancellationToken))
							.ToList();

						jobs = (await ValueTaskExtensions.WhenAll(getTasks))
							.Where(x => x.StartedAt.Value >= preStartupTime)
							.ToList();
					}

					await using var jrt = new JobsRequiredTest(instanceClient.Jobs);

					await jrt.HubConnectionTask;

					foreach (var job in jobs)
					{
						Assert.IsTrue(job.StartedAt.Value >= preStartupTime);
						await jrt.WaitForJob(job, 130, job.Description.Contains("Reconnect chat bot") ? null : false, null, cancellationToken);
					}

					var dd = await instanceClient.DreamDaemon.Read(cancellationToken);
					Assert.AreEqual(WatchdogStatus.Online, dd.Status.Value);
					Assert.IsNotNull(dd.StagedCompileJob);
					Assert.AreNotEqual(dd.StagedCompileJob.Id, dd.ActiveCompileJob.Id);
					Assert.AreEqual(initialStaged, dd.StagedCompileJob.Id);
					Assert.AreEqual(initialActive, dd.ActiveCompileJob.Id);
					Assert.AreEqual(initialSessionId, dd.SessionId);

					var chatReadTask = instanceClient.ChatBots.List(null, cancellationToken);

					// Check the DMAPI got the channels again https://github.com/tgstation/tgstation-server/issues/1490
					topicRequestResult = await WatchdogTest.SendTestTopic(
						"tgs_integration_test_tactics7=1",
						WatchdogTest.StaticTopicClient,
						GetInstanceManager().GetInstanceReference(instanceClient.Metadata),
						mainDDPort.Value,
						cancellationToken);

					Assert.IsNotNull(topicRequestResult);
					Assert.IsTrue(topicRequestResult.FloatData.HasValue);

					var currentChatBots = await chatReadTask;
					var connectedChannelCount = currentChatBots.Where(x => x.Enabled.Value).SelectMany(x => x.Channels).Count();

					Assert.AreEqual(connectedChannelCount, topicRequestResult.FloatData.Value);

					dd = await WatchdogTest.TellWorldToReboot2(
						instanceClient,
						GetInstanceManager(),
						WatchdogTest.StaticTopicClient,
						mainDDPort.Value,
						true,
						cancellationToken);

					Assert.AreEqual(WatchdogStatus.Online, dd.Status.Value); // if this assert fails, you likely have to crack open the debugger and read test_fail_reason.txt manually
					Assert.IsNull(dd.StagedCompileJob);
					Assert.AreEqual(initialStaged, dd.ActiveCompileJob.Id);

					await instanceClient.DreamDaemon.Shutdown(cancellationToken);
					dd = await instanceClient.DreamDaemon.Update(new DreamDaemonRequest
					{
						AutoStart = true
					}, cancellationToken);

					Assert.AreEqual(WatchdogStatus.Offline, dd.Status);

					jobsHubTest.ExpectShutdown();
					await multiClient.Execute(
						restClient => restClient.Administration.Restart(cancellationToken),
						async gqlClient => await gqlClient.RunMutationEnsureNoErrors(
							gql => gql.RestartServer.ExecuteAsync(cancellationToken),
							result => result.RestartServerNode,
							cancellationToken));
				}

				await Task.WhenAny(serverTask, Task.Delay(TimeSpan.FromMinutes(1), cancellationToken));
				Assert.IsTrue(serverTask.IsCompleted);

				preStartupTime = DateTimeOffset.UtcNow;

				async Task WaitForInitialJobs(IInstanceClient instanceClient)
				{
					var jobs = await instanceClient.Jobs.ListActive(null, cancellationToken);
					if (jobs.Count == 0)
						jobs = (await instanceClient.Jobs.List(null, cancellationToken))
							.Where(x => x.StartedAt.Value > preStartupTime)
							.ToList();
					else
						jobs = jobs.Where(x => x.JobCode.Value.IsServerStartupJob()).ToList();

					await using var jrt = new JobsRequiredTest(instanceClient.Jobs);

					await jrt.HubConnectionTask;

					foreach (var job in jobs)
					{
						Assert.IsTrue(job.StartedAt.Value >= preStartupTime);
						await jrt.WaitForJob(job, 140, job.JobCode == JobCode.ReconnectChatBot ? null : false, null, cancellationToken);
					}
				}

				// chat bot start, dd autostart, and reboot with different initial job test
				preStartupTime = DateTimeOffset.UtcNow;
				serverTask = server.Run(cancellationToken).AsTask();
				long expectedCompileJobId, expectedStaged;
				var edgeVersion = await EngineTest.GetEdgeVersion(EngineType.Byond, GetLogger(), GetFileDownloader(), cancellationToken);
				await using (var adminClient = await CreateAdminClient(server.ApiUrl, cancellationToken))
				{
					var restAdminClient = adminClient.RestClient;
					await jobsHubTest.WaitForReconnect(cancellationToken);
					var instanceClient = restAdminClient.Instances.CreateClient(instance);
					await WaitForInitialJobs(instanceClient);

					var dd = await instanceClient.DreamDaemon.Read(cancellationToken);

					Assert.AreEqual(WatchdogStatus.Online, dd.Status.Value);

					var compileJob = await instanceClient.DreamMaker.Compile(cancellationToken);
					await using var wdt = new WatchdogTest(edgeVersion, instanceClient, GetInstanceManager(), (ushort)server.ApiUrl.Port, server.HighPriorityDreamDaemon, mainDDPort.Value, server.UsingBasicWatchdog);
					await wdt.WaitForJob(compileJob, 30, false, null, cancellationToken);

					dd = await instanceClient.DreamDaemon.Read(cancellationToken);
					Assert.AreEqual(dd.StagedCompileJob.Job.Id, compileJob.Id);

					expectedCompileJobId = compileJob.Id.Value;
					dd = await wdt.TellWorldToReboot(true, cancellationToken);

					Assert.AreEqual(expectedCompileJobId, dd.ActiveCompileJob.Job.Id);
					Assert.AreEqual(WatchdogStatus.Online, dd.Status.Value);

					expectedCompileJobId = dd.ActiveCompileJob.Id.Value;

					await instanceClient.DreamDaemon.Update(new DreamDaemonRequest
					{
						AutoStart = false,
					}, cancellationToken);

					compileJob = await instanceClient.DreamMaker.Compile(cancellationToken);
					await wdt.WaitForJob(compileJob, 30, false, null, cancellationToken);
					expectedStaged = compileJob.Id.Value;

					jobsHubTest.ExpectShutdown();
					await adminClient.Execute(
						restClient => restClient.Administration.Restart(cancellationToken),
						async gqlClient => await gqlClient.RunMutationEnsureNoErrors(
							gql => gql.RestartServer.ExecuteAsync(cancellationToken),
							result => result.RestartServerNode,
							cancellationToken));
				}

				await Task.WhenAny(serverTask, Task.Delay(TimeSpan.FromMinutes(1), cancellationToken));
				Assert.IsTrue(serverTask.IsCompleted);

				// post/entity deletion tests
				serverTask = server.Run(cancellationToken).AsTask();
				await using (var adminClient = await CreateAdminClient(server.ApiUrl, cancellationToken))
				{
					var restAdminClient = adminClient.RestClient;
					await jobsHubTest.WaitForReconnect(cancellationToken);
					var instanceClient = restAdminClient.Instances.CreateClient(instance);
					await WaitForInitialJobs(instanceClient);

					var currentDD = await instanceClient.DreamDaemon.Read(cancellationToken);
					Assert.AreEqual(expectedCompileJobId, currentDD.ActiveCompileJob.Id.Value);
					Assert.AreEqual(WatchdogStatus.Online, currentDD.Status);
					Assert.AreEqual(expectedStaged, currentDD.StagedCompileJob.Job.Id.Value);

					await using var wdt = new WatchdogTest(edgeVersion, instanceClient, GetInstanceManager(), (ushort)server.ApiUrl.Port, server.HighPriorityDreamDaemon, mainDDPort.Value, server.UsingBasicWatchdog);
					currentDD = await wdt.TellWorldToReboot(false, cancellationToken);
					Assert.AreEqual(expectedStaged, currentDD.ActiveCompileJob.Job.Id.Value);
					Assert.IsNull(currentDD.StagedCompileJob);

					await using var repoTestObj = new RepositoryTest(instanceClient, instanceClient.Repository, instanceClient.Jobs);
					var repoTest = repoTestObj.RunPostTest(cancellationToken);
					await using var chatTestObj = new ChatTest(instanceClient.ChatBots, restAdminClient.Instances, instanceClient.Jobs, instance);
					await chatTestObj.RunPostTest(cancellationToken);
					await repoTest;

					DummyChatProvider.RandomDisconnections(false);

					jobsHubTest.CompleteNow();
					await jobsHubTestTask;

					await new InstanceManagerTest(restAdminClient, server.Directory).RunPostTest(instance, cancellationToken);
				}
			}
			catch (ApiException ex)
			{
				Console.WriteLine($"[{DateTimeOffset.UtcNow}] TEST ERROR: {ex.ErrorCode}: {ex.Message}\n{ex.AdditionalServerData}");
				throw;
			}
			catch(OperationCanceledException ex)
			{
				Console.WriteLine($"[{DateTimeOffset.UtcNow}] TEST ABORTED: {ex}");
				throw;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[{DateTimeOffset.UtcNow}] TEST ERROR: {ex}");
				throw;
			}
			finally
			{
				Console.WriteLine("TGS TEST CANCELLING TOKEN AS FINAL STEP");
				serverCts.Cancel();
				try
				{
					await serverTask.WaitAsync(hardCancellationToken);
				}
				catch (OperationCanceledException) { }

				TerminateAllEngineServers();
			}

			Assert.IsTrue(serverTask.IsCompleted);
			await serverTask;
		}

		ValueTask<MultiServerClient> CreateAdminClient(Uri url, CancellationToken cancellationToken)
			=> CreateClient(url, DefaultCredentials.AdminUserName, DefaultCredentials.DefaultAdminUserPassword, true, cancellationToken);

		async ValueTask<MultiServerClient> CreateClient(
			Uri url,
			string username,
			string password,
			bool retry,
			CancellationToken cancellationToken = default)
		{
			url = new Uri(url.ToString().Replace(Routes.ApiRoot, String.Empty));
			var giveUpAt = DateTimeOffset.UtcNow.AddMinutes(2);
			for (var I = 1; ; ++I)
			{
				ValueTask<IRestServerClient> restClientTask;
				ValueTask<IAuthenticatedGraphQLServerClient> graphQLClientTask;
				try
				{
					Console.WriteLine($"TEST: CreateAdminClient attempt {I}...");

					restClientTask = restClientFactory.CreateFromLogin(
						url,
						username,
						password,
						cancellationToken: cancellationToken);
					using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
					graphQLClientTask = MultiServerClient.UseGraphQL
						? graphQLClientFactory.CreateFromLogin(
							url,
							username,
							password,
							cancellationToken: cts.Token)
						: ValueTask.FromResult<IAuthenticatedGraphQLServerClient>(null);

					IRestServerClient restClient;
					try
					{
						restClient = await restClientTask;
					}
					catch (Exception restException) when (restException is not HttpRequestException && restException is not ServiceUnavailableException)
					{
						cts.Cancel();
						try
						{
							await (await graphQLClientTask).DisposeAsync();
						}
						catch (OperationCanceledException)
						{
						}
						catch (Exception graphQLException)
						{
							throw new AggregateException(restException, graphQLException);
						}

						throw;
					}

					try
					{
						return new MultiServerClient(
							restClient,
							await graphQLClientTask);
					}
					catch
					{
						await restClient.DisposeAsync();
						throw;
					}
				}
				catch (HttpRequestException)
				{
					//migrating, to be expected
					if (DateTimeOffset.UtcNow > giveUpAt || !retry)
						throw;
					await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
				}
				catch (ServiceUnavailableException)
				{
					// migrating, to be expected
					if (DateTimeOffset.UtcNow > giveUpAt || !retry)
						throw;
					await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
				}
			}
		}
	}
}
