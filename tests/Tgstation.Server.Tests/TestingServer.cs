using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host;

namespace Tgstation.Server.Tests
{
	sealed class TestingServer : IServer
	{
		public Uri Url { get; }

		public string Directory { get; }
		public bool RestartRequested => realServer.RestartRequested;

		readonly IServer realServer;

		public TestingServer()
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

			var args = new List<string>()
			{
				"--urls",
				Url.ToString(),
				String.Format(CultureInfo.InvariantCulture, "Database:DatabaseType={0}", databaseType),
				String.Format(CultureInfo.InvariantCulture, "Database:ConnectionString={0}", connectionString),
				"Database:DropDatabase=true"
			};

			if (!String.IsNullOrEmpty(gitHubAccessToken))
				args.Add(String.Format(CultureInfo.InvariantCulture, "General:GitHubAccessToken={0}", gitHubAccessToken));

			realServer = new ServerFactory().CreateServer(args.ToArray(), null);
		}

		public void Dispose()
		{
			realServer.Dispose();
			System.IO.Directory.Delete(Directory, true);
		}

		public Task RunAsync(CancellationToken cancellationToken) => realServer.RunAsync(cancellationToken);
	}
}
