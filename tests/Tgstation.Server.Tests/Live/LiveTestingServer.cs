using Microsoft.VisualStudio.TestTools.UnitTesting;

using Serilog.Context;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Setup;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Tests.Live
{
	sealed class LiveTestingServer : IServer, IDisposable
	{
		static bool needCleanup = false;
		public static string BaseDirectory { get; }

		static LiveTestingServer()
		{
			SerilogContextHelper.AddSwarmNodeIdentifierToTemplate();
			BaseDirectory = Environment.GetEnvironmentVariable("TGS_TEST_TEMP_DIRECTORY");
			if (string.IsNullOrWhiteSpace(BaseDirectory))
			{
				BaseDirectory = Path.Combine(Path.GetTempPath(), "TGS_INTEGRATION_TEST");
				needCleanup = true;
			}
		}

		static async Task Cleanup(string directory)
		{
			for (int i = 0; i < 5; ++i)
				try
				{
					new DefaultIOManager(new FileSystem()).DeleteDirectory(directory, default).GetAwaiter().GetResult();
				}
				catch
				{
					GC.Collect(int.MaxValue, GCCollectionMode.Forced, false);
					await Task.Delay(3000);
				}
		}

		public Uri ApiUrl { get; }

		public Uri RootUrl { get; }

		public Uri OpenDreamUrl { get; }

		public string Directory { get; }

		public string UpdatePath { get; }

		public string DatabaseType { get; }

		public bool DumpOpenApiSpecpath { get; }

		public bool HighPriorityDreamDaemon { get; }
		public bool LowPriorityDeployments { get; }

		public bool UsingBasicWatchdog { get; }

		public bool RestartRequested => RealServer.RestartRequested;

		readonly List<string> args;
		readonly List<string> swarmArgs;

		string swarmNodeId;

		public IServer RealServer { get; private set; }

		public LiveTestingServer(SwarmConfiguration swarmConfiguration, bool enableOAuth, ushort port = 15010)
		{
			if (needCleanup)
			{
				needCleanup = false;
				Cleanup(BaseDirectory).GetAwaiter().GetResult();
			}

			System.Console.WriteLine($"RunningInGitHubActions: {TestingUtils.RunningInGitHubActions}");

			Assert.IsTrue(port >= 10000); // for testing bridge request limit
			Directory = BaseDirectory;

			Directory = Path.Combine(Directory, swarmConfiguration?.Identifier ?? "default");
			System.IO.Directory.CreateDirectory(Directory);
			string urlString = $"http://localhost:{port}";
			RootUrl = new Uri(urlString);
			ApiUrl = new Uri(urlString + Routes.ApiRoot);

			//so we need a db
			//we have to rely on env vars
			DatabaseType = Environment.GetEnvironmentVariable("TGS_TEST_DATABASE_TYPE");
			var connectionString = Environment.GetEnvironmentVariable("TGS_TEST_CONNECTION_STRING");
			var gitHubAccessToken = Environment.GetEnvironmentVariable("TGS_TEST_GITHUB_TOKEN");
			var dumpOpenAPISpecPathEnvVar = Environment.GetEnvironmentVariable("TGS_TEST_DUMP_API_SPEC");

			UsingBasicWatchdog = Boolean.TryParse(Environment.GetEnvironmentVariable("General__UseBasicWatchdog"), out var result) && result;

			if (String.IsNullOrEmpty(DatabaseType))
				Assert.Inconclusive("No database type configured in env var TGS_TEST_DATABASE_TYPE!");

			if (String.IsNullOrEmpty(connectionString))
				Assert.Inconclusive("No connection string configured in env var TGS_TEST_CONNECTION_STRING!");

			if (String.IsNullOrEmpty(gitHubAccessToken))
				System.Console.WriteLine("WARNING: No GitHub access token configured, test may fail due to rate limits!");

			DumpOpenApiSpecpath = !String.IsNullOrEmpty(dumpOpenAPISpecPathEnvVar);

			// neither of these should really matter but it's better that we test them
			// high prio DD might help with some topic flakiness actually
			// github doesn't allow nicing on linux though
			var windows = new Host.System.PlatformIdentifier().IsWindows;
			var nicingAllowed = windows || !TestingUtils.RunningInGitHubActions;
			HighPriorityDreamDaemon = nicingAllowed;
			LowPriorityDeployments = nicingAllowed;

			var odGitDir = Environment.GetEnvironmentVariable("TGS_TEST_OD_GIT_DIRECTORY");
			if (!String.IsNullOrWhiteSpace(odGitDir))
				OpenDreamUrl = new Uri($"file://{Path.GetFullPath(odGitDir).Replace('\\', '/')}");
			else
				OpenDreamUrl = new GeneralConfiguration().OpenDreamGitUrl;

			args = new List<string>()
			{
				String.Format(CultureInfo.InvariantCulture, "Database:DropDatabase={0}", true), // Replaced after first Run
				String.Format(CultureInfo.InvariantCulture, "General:ConfigVersion={0}", GeneralConfiguration.CurrentConfigVersion),
				String.Format(CultureInfo.InvariantCulture, "General:ApiPort={0}", port),
				String.Format(CultureInfo.InvariantCulture, "Database:DatabaseType={0}", DatabaseType),
				String.Format(CultureInfo.InvariantCulture, "Database:ConnectionString={0}", connectionString),
				String.Format(CultureInfo.InvariantCulture, "General:SetupWizardMode={0}", SetupWizardMode.Never),
				String.Format(CultureInfo.InvariantCulture, "General:MinimumPasswordLength={0}", 10),
				String.Format(CultureInfo.InvariantCulture, "General:InstanceLimit={0}", 11),
				String.Format(CultureInfo.InvariantCulture, "General:UserLimit={0}", 150),
				String.Format(CultureInfo.InvariantCulture, "General:UserGroupLimit={0}", 47),
				String.Format(CultureInfo.InvariantCulture, "General:HostApiDocumentation={0}", DumpOpenApiSpecpath),
				String.Format(CultureInfo.InvariantCulture, "FileLogging:Directory={0}", Path.Combine(Directory, "Logs")),
				String.Format(CultureInfo.InvariantCulture, "FileLogging:LogLevel={0}", "Trace"),
				String.Format(CultureInfo.InvariantCulture, "FileLogging:Disable={0}", false),
				String.Format(CultureInfo.InvariantCulture, "General:ValidInstancePaths:0={0}", Directory),
				"General:ByondTopicTimeout=10000",
				$"Session:HighPriorityLiveDreamDaemon={HighPriorityDreamDaemon}",
				$"Session:LowPriorityDeploymentProcesses={LowPriorityDeployments}",
				$"General:SkipAddingByondFirewallException={!TestingUtils.RunningInGitHubActions}",
				$"General:OpenDreamGitUrl={OpenDreamUrl}",
				$"Security:TokenExpiryMinutes=120", // timeouts are useless for us
				$"General:OpenDreamSuppressInstallOutput={TestingUtils.RunningInGitHubActions}",
				"Telemetry:DisableVersionReporting=true",
				$"General:PrometheusPort={port}",
				$"General:ByondZipDownloadTemplate={TestingUtils.ByondZipDownloadTemplate}"
			};

			if (MultiServerClient.UseGraphQL)
				args.Add("Internal:EnableGraphQL=true");

			swarmArgs = new List<string>();
			if (swarmConfiguration != null)
			{
				UpdateSwarmArguments(swarmConfiguration);
			}

			// enable all oauth providers
			if (enableOAuth)
				foreach (var I in Enum.GetValues(typeof(OAuthProvider)))
				{
					args.Add($"Security:OAuth:{I}:ClientId=Fake");
					args.Add($"Security:OAuth:{I}:ClientSecret=Faker");
					args.Add($"Security:OAuth:{I}:RedirectUrl=https://fakest.com");
					args.Add($"Security:OAuth:{I}:ServerUrl=https://fakestest.com");
				}
			else
				args.Add($"Security:OAuth=null");

			// SPECIFICALLY DELETE THE DEV APPSETTINGS, WE DON'T WANT IT IN THE WAY
			File.Delete("appsettings.Development.yml");
			File.Delete("appsettings.Development.json");

			if (!string.IsNullOrEmpty(gitHubAccessToken))
				args.Add(string.Format(CultureInfo.InvariantCulture, "General:GitHubAccessToken={0}", gitHubAccessToken));

			if (DumpOpenApiSpecpath)
				Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

			UpdatePath = Path.Combine(Directory, Guid.NewGuid().ToString());
		}

		public void Dispose()
		{
		}

		public void UpdateSwarmArguments(SwarmConfiguration swarmConfiguration)
		{
			swarmArgs.Clear();
			swarmArgs.Add($"Swarm:PrivateKey={swarmConfiguration.PrivateKey}");
			swarmArgs.Add($"Swarm:Identifier={swarmConfiguration.Identifier}");
			swarmArgs.Add($"Swarm:Address={swarmConfiguration.Address}");
			swarmArgs.Add($"Swarm:PublicAddress={swarmConfiguration.PublicAddress}");
			if (swarmConfiguration.ControllerAddress != null)
				swarmArgs.Add($"Swarm:ControllerAddress={swarmConfiguration.ControllerAddress}");

			if (swarmConfiguration.UpdateRequiredNodeCount != 0)
				swarmArgs.Add($"Swarm:UpdateRequiredNodeCount={swarmConfiguration.UpdateRequiredNodeCount}");

			swarmNodeId = swarmConfiguration.Identifier;
		}

		public async ValueTask RunNoArgumentsTest(CancellationToken cancellationToken)
		{
			Assert.IsNull(Environment.GetEnvironmentVariable("General:SetupWizardMode"));
			Environment.SetEnvironmentVariable("General:SetupWizardMode", "Never");
			await Application
				.CreateDefaultServerFactory()
				.CreateServer(
					Array.Empty<string>(),
					UpdatePath,
					cancellationToken);
			Environment.SetEnvironmentVariable("General:SetupWizardMode", null);
		}

		public async ValueTask Run(CancellationToken cancellationToken)
		{
			var messageAddition = swarmNodeId != null ? $": {swarmNodeId}" : String.Empty;
			System.Console.WriteLine("TEST SERVER START" + messageAddition);
			var firstRun = RealServer == null;
			var arrayArgs = args.Concat(swarmArgs).ToArray();
			RealServer = await Application
				.CreateDefaultServerFactory()
				.CreateServer(
					arrayArgs,
					UpdatePath,
					cancellationToken);

			if (firstRun)
				args[0] = string.Format(CultureInfo.InvariantCulture, "Database:DropDatabase={0}", false);

			var swarmMode = swarmNodeId != null;
			ApplicationBuilderExtensions.LogSwarmIdentifier = swarmMode;
			using (swarmMode
				? LogContext.PushProperty(SerilogContextHelper.SwarmIdentifierContextProperty, swarmNodeId)
				: null)
				await RealServer.Run(cancellationToken);
			System.Console.WriteLine($"TEST SERVER END" + messageAddition);
		}
	}
}
