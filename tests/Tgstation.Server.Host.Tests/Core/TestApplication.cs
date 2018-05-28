using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Core.Tests
{
	[TestClass]
	public sealed class TestApplication : IServerUpdateConsumer
	{
		public void ApplyUpdate(string updatePath) => throw new System.NotImplementedException();

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
