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
		public async Task TestUpdateProtocol()
		{
			using var server = new TestingServer();
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
						adminClient = await clientFactory.CreateFromLogin(server.Url, User.AdminName, User.DefaultAdminPassword).ConfigureAwait(false);
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
				catch (AggregateException ex)
				{
					if (ex.InnerException is NotSupportedException notSupportedException)
						Assert.Inconclusive(notSupportedException.Message);
				}
			}
			Assert.IsTrue(server.RestartRequested, "Server not requesting restart!");
		}

		static void TerminateAllDDs()
		{
			foreach (var proc in System.Diagnostics.Process.GetProcessesByName("DreamDaemon"))
				using (proc)
					proc.Kill();
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

			using var server = new TestingServer();

			using var hardTimeoutCts = new CancellationTokenSource();
			hardTimeoutCts.CancelAfter(new TimeSpan(0, 9, 45));
			var hardTimeoutCancellationToken = hardTimeoutCts.Token;
			hardTimeoutCancellationToken.Register(() => Console.WriteLine($"[{DateTimeOffset.Now}] TEST TIMEOUT HARD!"));

			using var softTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(hardTimeoutCancellationToken);
			softTimeoutCts.CancelAfter(new TimeSpan(0, 9, 15));
			var softTimeoutCancellationToken = softTimeoutCts.Token;
			bool tooLateForSoftTimeout = false;
			softTimeoutCancellationToken.Register(() =>
			{
				if (!tooLateForSoftTimeout)
					Console.WriteLine($"[{DateTimeOffset.Now}] TEST TIMEOUT SOFT!");
			});

			using var serverCts = CancellationTokenSource.CreateLinkedTokenSource(softTimeoutCancellationToken);
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
							return await clientFactory.CreateFromLogin(server.Url, User.AdminName, User.DefaultAdminPassword, attemptLoginRefresh: false).ConfigureAwait(false);
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

					var badClient = clientFactory.CreateFromToken(server.Url, newToken);
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

				var preStartupTime = DateTimeOffset.Now;

				serverTask = server.Run(cancellationToken);
				using (var adminClient = await CreateAdminClient())
				{
					var instanceClient = adminClient.Instances.CreateClient(instance);

					var jobs = await instanceClient.Jobs.ListActive(cancellationToken);
					if (!jobs.Any())
					{
						var entities = await instanceClient.Jobs.List(cancellationToken);
						var getTasks = entities
							.Select(e => instanceClient.Jobs.GetId(e, cancellationToken))
							.ToList();

						await Task.WhenAll(getTasks);
						jobs = getTasks
							.Select(x => x.Result)
							.Where(x => x.StartedAt.Value > preStartupTime)
							.ToList();
					}

					Assert.AreEqual(1, jobs.Count);

					var reattachJob = jobs.Single();
					Assert.IsTrue(reattachJob.StartedAt.Value >= preStartupTime);

					await new JobsRequiredTest(instanceClient.Jobs).WaitForJob(reattachJob, 40, false, cancellationToken);

					var dd = await instanceClient.DreamDaemon.Read(cancellationToken);
					Assert.AreEqual(WatchdogStatus.Online, dd.Status.Value);

					await instanceClient.DreamDaemon.Shutdown(cancellationToken);
					await instanceClient.DreamDaemon.Update(new DreamDaemon
					{
						AutoStart = true
					}, cancellationToken);

					await adminClient.Administration.Restart(cancellationToken);
				}

				await Task.WhenAny(serverTask, Task.Delay(30000, cancellationToken));
				Assert.IsTrue(serverTask.IsCompleted);

				preStartupTime = DateTimeOffset.Now;
				serverTask = server.Run(cancellationToken);
				using (var adminClient = await CreateAdminClient())
				{
					var instanceClient = adminClient.Instances.CreateClient(instance);

					var jobs = await instanceClient.Jobs.ListActive(cancellationToken);
					if (!jobs.Any())
					{
						var entities = await instanceClient.Jobs.List(cancellationToken);
						var getTasks = entities
							.Select(e => instanceClient.Jobs.GetId(e, cancellationToken))
							.ToList();

						await Task.WhenAll(getTasks);
						jobs = getTasks
							.Select(x => x.Result)
							.Where(x => x.StartedAt.Value > preStartupTime)
							.ToList();
					}

					Assert.AreEqual(1, jobs.Count);

					var launchJob = jobs.Single();
					Assert.IsTrue(launchJob.StartedAt.Value >= preStartupTime);

					await new JobsRequiredTest(instanceClient.Jobs).WaitForJob(launchJob, 40, false, cancellationToken);

					var dd = await instanceClient.DreamDaemon.Read(cancellationToken);

					Assert.AreEqual(WatchdogStatus.Online, dd.Status.Value);

					var repoTest = new RepositoryTest(instanceClient.Repository, instanceClient.Jobs).RunPostTest(cancellationToken);
					await new ChatTest(instanceClient.ChatBots, adminClient.Instances, instance).RunPostTest(cancellationToken);
					await repoTest;

					await new InstanceManagerTest(adminClient.Instances, adminClient.Users, server.Directory).RunPostTest(cancellationToken);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[{DateTimeOffset.Now}] TEST ERROR: {ex}");
				throw;
			}
			finally
			{
				tooLateForSoftTimeout = true;
				serverCts.Cancel();
				try
				{
					await serverTask.WithToken(hardTimeoutCancellationToken).ConfigureAwait(false);
				}
				catch (OperationCanceledException) { }

				TerminateAllDDs();

				hardTimeoutCancellationToken.ThrowIfCancellationRequested();
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
			Assert.AreEqual(String.Empty, process.GetErrorOutput().Trim());
			Assert.AreEqual("Hello World!", process.GetStandardOutput().Trim());
		}
	}
}
