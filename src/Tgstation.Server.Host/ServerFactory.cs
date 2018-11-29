using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Reflection;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host
{
	/// <inheritdoc />
	public sealed class ServerFactory : IServerFactory
	{
		/// <inheritdoc />
		public IServer CreateServer(string[] args, string updatePath)
		{
			var webHost = WebHost.CreateDefaultBuilder(args ?? throw new ArgumentNullException(nameof(args)))
				.ConfigureAppConfiguration((context, configurationBuilder) => configurationBuilder.SetBasePath(Directory.GetCurrentDirectory()))
				.UseStartup<Application>()
				.SuppressStatusMessages(true)
				.UseShutdownTimeout(TimeSpan.FromMinutes(1));

			if(updatePath != null)
				webHost.UseContentRoot(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

			return new Server(webHost, updatePath);
		}
	}
}
