using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Core.Tests
{
	[TestClass]
	public sealed class TestApplication : IServerUpdater
	{
		public void ApplyUpdate(string updatePath) => throw new NotImplementedException();

		public void RegisterForUpdate(Action action) => throw new NotImplementedException();
		
		[TestMethod]
		public async Task TestSuccessfulStartup()
		{
			var dbName = Path.GetTempFileName();
			try
			{
				using (var webHost = WebHost.CreateDefaultBuilder(new string[] { "Database:DatabaseType=Sqlite", "Database:ConnectionString=Data Source=" + dbName }) //force it to use sqlite
					.UseStartup<Application>()
					.Build()
				)
				{
					await webHost.StartAsync().ConfigureAwait(false);
					await webHost.StopAsync().ConfigureAwait(false);
				}
			}
			finally
			{
				File.Delete(dbName);
			}
		}
	}
}
