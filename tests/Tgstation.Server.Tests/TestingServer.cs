using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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

		public TestingServer(string updatePath)
		{
			Directory = Path.GetTempFileName();
			File.Delete(Directory);
			System.IO.Directory.CreateDirectory(Directory);
			Url = new Uri("http://localhost:5001");

			//so we need a db
			//we have to rely on env vars
			var databaseType = Environment.GetEnvironmentVariable("TGS4_TEST_DATABASE_TYPE");
			var connectionString = Environment.GetEnvironmentVariable("TGS4_TEST_CONNECTION_STRING");
			var gitHubAccessToken = Environment.GetEnvironmentVariable("TGS4_TEST_GITHUB_TOKEN");

			if (String.IsNullOrEmpty(databaseType))
				Assert.Fail("No database type configured in env var TGS4_TEST_DATABASE_TYPE!");

			if (String.IsNullOrEmpty(connectionString))
				Assert.Fail("No connection string configured in env var TGS4_TEST_CONNECTION_STRING!");

			if (String.IsNullOrEmpty(gitHubAccessToken))
				Console.WriteLine("WARNING: No GitHub access token configured, test may fail due to rate limits!");
			
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

			realServer = new ServerFactory().CreateServer(args.ToArray(), updatePath);
		}

		public void Dispose()
		{
			System.IO.Directory.Delete(Directory, true);
		}

		public Task RunAsync(CancellationToken cancellationToken) => realServer.RunAsync(cancellationToken);
	}
}
