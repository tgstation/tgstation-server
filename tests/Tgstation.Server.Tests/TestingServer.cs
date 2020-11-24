using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Setup;

namespace Tgstation.Server.Tests
{
	sealed class TestingServer : IServer, IDisposable
	{
		public Uri Url { get; }

		public string Directory { get; }

		public string UpdatePath { get; }

		public string DatabaseType { get; }

		public bool DumpOpenApiSpecpath { get; }

		public bool RestartRequested => realServer.RestartRequested;

		string[] args;

		IServer realServer;

		public TestingServer(bool enableOAuth)
		{
			Directory = Environment.GetEnvironmentVariable("TGS4_TEST_TEMP_DIRECTORY");
			if (String.IsNullOrWhiteSpace(Directory))
			{
				Directory = Path.Combine(Path.GetTempPath(), "TGS4_INTEGRATION_TEST");
				if (System.IO.Directory.Exists(Directory))
					try
					{
						System.IO.Directory.Delete(Directory, true);
					}
					catch { }

			}

			Directory = Path.Combine(Directory, Guid.NewGuid().ToString());
			System.IO.Directory.CreateDirectory(Directory);
			const string UrlString = "http://localhost:5010";
			Url = new Uri(UrlString);

			//so we need a db
			//we have to rely on env vars
			DatabaseType = Environment.GetEnvironmentVariable("TGS4_TEST_DATABASE_TYPE");
			var connectionString = Environment.GetEnvironmentVariable("TGS4_TEST_CONNECTION_STRING");
			var gitHubAccessToken = Environment.GetEnvironmentVariable("TGS4_TEST_GITHUB_TOKEN");
			var dumpOpenAPISpecPathEnvVar = Environment.GetEnvironmentVariable("TGS4_TEST_DUMP_API_SPEC");

			if (String.IsNullOrEmpty(DatabaseType))
				Assert.Inconclusive("No database type configured in env var TGS4_TEST_DATABASE_TYPE!");

			if (String.IsNullOrEmpty(connectionString))
				Assert.Inconclusive("No connection string configured in env var TGS4_TEST_CONNECTION_STRING!");

			if (String.IsNullOrEmpty(gitHubAccessToken))
				Console.WriteLine("WARNING: No GitHub access token configured, test may fail due to rate limits!");

			DumpOpenApiSpecpath = !String.IsNullOrEmpty(dumpOpenAPISpecPathEnvVar);

			var args = new List<string>()
			{
				String.Format(CultureInfo.InvariantCulture, "Database:DropDatabase={0}", true),
				String.Format(CultureInfo.InvariantCulture, "General:ApiPort={0}", 5010),
				String.Format(CultureInfo.InvariantCulture, "Database:DatabaseType={0}", DatabaseType),
				String.Format(CultureInfo.InvariantCulture, "Database:ConnectionString={0}", connectionString),
				String.Format(CultureInfo.InvariantCulture, "General:SetupWizardMode={0}", SetupWizardMode.Never),
				String.Format(CultureInfo.InvariantCulture, "General:MinimumPasswordLength={0}", 10),
				String.Format(CultureInfo.InvariantCulture, "General:InstanceLimit={0}", 11),
				String.Format(CultureInfo.InvariantCulture, "General:UserLimit={0}", 150),
				String.Format(CultureInfo.InvariantCulture, "General:HostApiDocumentation={0}", DumpOpenApiSpecpath),
				String.Format(CultureInfo.InvariantCulture, "FileLogging:Directory={0}", Path.Combine(Directory, "Logs")),
				String.Format(CultureInfo.InvariantCulture, "FileLogging:LogLevel={0}", "Trace"),
				String.Format(CultureInfo.InvariantCulture, "General:ValidInstancePaths:0={0}", Directory),
				"General:ByondTopicTimeout=3000"
			};

			// enable all oauth providers
			if (enableOAuth)
				foreach (var I in Enum.GetValues(typeof(OAuthProvider)))
				{
					args.Add($"Security:OAuth:{I}:ClientId=Fake");
					args.Add($"Security:OAuth:{I}:ClientSecret=Faker");
				}

			// SPECIFICALLY DELETE THE DEV APPSETTINGS, WE DON'T WANT IT IN THE WAY
			File.Delete("appsettings.Development.json");

			if (!String.IsNullOrEmpty(gitHubAccessToken))
				args.Add(String.Format(CultureInfo.InvariantCulture, "General:GitHubAccessToken={0}", gitHubAccessToken));

			if (DumpOpenApiSpecpath)
				Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

			UpdatePath = Path.Combine(Directory, Guid.NewGuid().ToString());
			this.args = args.ToArray();
		}

		public void Dispose()
		{
			for (int i = 0; i < 5; ++i)
				try
				{
					System.IO.Directory.Delete(Directory, true);
				}
				catch
				{
					GC.Collect(Int32.MaxValue, GCCollectionMode.Forced, false);
					Thread.Sleep(3000);
				}
		}

		public async Task Run(CancellationToken cancellationToken)
		{
			Console.WriteLine("TEST SERVER START");
			var firstRun = realServer == null;
			realServer = await Application
				.CreateDefaultServerFactory()
				.CreateServer(
					args,
					UpdatePath,
					default);

			if (firstRun)
			{
				var tmp = args.Skip(1).ToList();
				tmp.Add(String.Format(CultureInfo.InvariantCulture, "Database:DropDatabase={0}", false));
				args = tmp.ToArray();
			}

			await realServer.Run(cancellationToken);
			Console.WriteLine("TEST SERVER END");
		}
	}
}
