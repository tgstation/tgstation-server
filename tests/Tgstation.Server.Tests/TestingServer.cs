using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Client;
using Tgstation.Server.Host;
using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Tests
{
	sealed class TestingServer : IServer
	{
		public Uri Url { get; }

		public string Directory { get; }
		public bool RestartRequested => realServer.RestartRequested;

		readonly IServer realServer;

		readonly IServerClientFactory serverClientFactory;

		readonly bool dumpOpenAPISpecpath;

		public TestingServer(IServerClientFactory serverClientFactory, string updatePath)
		{
			this.serverClientFactory = serverClientFactory;

			Directory = Path.GetTempFileName();
			File.Delete(Directory);
			System.IO.Directory.CreateDirectory(Directory);
			Url = new Uri("http://localhost:5001");

			//so we need a db
			//we have to rely on env vars
			var databaseType = Environment.GetEnvironmentVariable("TGS4_TEST_DATABASE_TYPE");
			var connectionString = Environment.GetEnvironmentVariable("TGS4_TEST_CONNECTION_STRING");
			var gitHubAccessToken = Environment.GetEnvironmentVariable("TGS4_TEST_GITHUB_TOKEN");
			var dumpOpenAPISpecPathEnvVar = Environment.GetEnvironmentVariable("TGS4_TEST_DUMP_API_SPEC");

			if (String.IsNullOrEmpty(databaseType))
				Assert.Inconclusive("No database type configured in env var TGS4_TEST_DATABASE_TYPE!");

			if (String.IsNullOrEmpty(connectionString))
				Assert.Inconclusive("No connection string configured in env var TGS4_TEST_CONNECTION_STRING!");

			if (String.IsNullOrEmpty(gitHubAccessToken))
				Console.WriteLine("WARNING: No GitHub access token configured, test may fail due to rate limits!");

			dumpOpenAPISpecpath = !String.IsNullOrEmpty(dumpOpenAPISpecPathEnvVar);
			
			var args = new List<string>()
			{
				String.Format(CultureInfo.InvariantCulture, "Kestrel:EndPoints:Http:Url={0}", Url),
				String.Format(CultureInfo.InvariantCulture, "Database:DatabaseType={0}", databaseType),
				String.Format(CultureInfo.InvariantCulture, "Database:ConnectionString={0}", connectionString),
				String.Format(CultureInfo.InvariantCulture, "Database:DropDatabase={0}", true),
				String.Format(CultureInfo.InvariantCulture, "General:SetupWizardMode={0}", SetupWizardMode.Never)
			};

			if (!String.IsNullOrEmpty(gitHubAccessToken))
				args.Add(String.Format(CultureInfo.InvariantCulture, "General:GitHubAccessToken={0}", gitHubAccessToken));

			if (dumpOpenAPISpecpath)
				Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

			realServer = ServerFactory.CreateDefault().CreateServer(args.ToArray(), updatePath);
		}

		public void Dispose()
		{
			System.IO.Directory.Delete(Directory, true);
		}

		public async Task RunAsync(CancellationToken cancellationToken)
		{
			Task runTask = realServer.RunAsync(cancellationToken);

			if (dumpOpenAPISpecpath)
			{
				var giveUpAt = DateTimeOffset.Now.AddSeconds(60);
				do
				{
					try
					{
						var client = await serverClientFactory.CreateServerClient(Url, User.AdminName, User.DefaultAdminPassword).ConfigureAwait(false);
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

				// Dump swagger to disk
				// This is purely for CI
				var webRequest = WebRequest.Create(Url.ToString() + "swagger/v1/swagger.json");
				using (var response = webRequest.GetResponse())
				using (var content = response.GetResponseStream())
				using (var output = new FileStream(@"C:\swagger.json", FileMode.Create))
				{
					await content.CopyToAsync(output);
				}
			}

			await runTask;
		}
	}
}
