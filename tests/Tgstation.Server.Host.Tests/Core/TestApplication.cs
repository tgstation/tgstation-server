using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.IO;

namespace Tgstation.Server.Host.Core.Tests
{
	[TestClass]
	public sealed class TestApplication : IServerControl
	{
		public Task<bool> ApplyUpdate(byte[] updateZipData, IIOManager ioManager, CancellationToken cancellationToken) => throw new NotImplementedException();

		public void RegisterForRestart(Action action)
		{
		}

		public bool Restart() => throw new NotImplementedException();

		[TestMethod]
		public async Task TestSuccessfulStartup()
		{
			var dbName = Path.GetTempFileName();
			try
			{
				using (var webHost = WebHost.CreateDefaultBuilder(new string[] { "Database:DatabaseType=Sqlite", "Database:ConnectionString=Data Source=" + dbName }) //force it to use sqlite
					.UseStartup<Application>()
					.ConfigureServices((serviceCollection) => serviceCollection.AddSingleton<IServerControl>(this))
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
