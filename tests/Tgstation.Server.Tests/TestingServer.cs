using System;
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
		public bool RestartRequested => realServer.RestartRequested;

		readonly IServer realServer;
		readonly string databasePath;

		public TestingServer()
		{
			databasePath = Path.GetTempFileName();
			File.Delete(databasePath);
			Url = new Uri("http://localhost:5001");
			realServer = new ServerFactory().CreateServer(new string[]
			{
				"--urls",
				Url.ToString(),
				"Database:DatabaseType=Sqlite",
				String.Format(CultureInfo.InvariantCulture, "Database:ConnectionString=Data Source={0}", databasePath)
				,"Database:NoMigrations=true"	//TODO: remove this when migrations are added
			}, null);
		}

		public void Dispose()
		{
			realServer.Dispose();
			File.Delete(databasePath);
		}

		public Task RunAsync(CancellationToken cancellationToken) => realServer.RunAsync(cancellationToken);
	}
}
