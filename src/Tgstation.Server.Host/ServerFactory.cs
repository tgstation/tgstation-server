using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host
{
	/// <inheritdoc />
	public sealed class ServerFactory : IServerFactory
	{
		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="ServerFactory"/>.
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

		/// <inheritdoc />
		public IIOManager IOManager { get; }

		/// <summary>
		/// Create the default <see cref="IServerFactory"/>.
		/// </summary>
		/// <returns>A new <see cref="IServerFactory"/> with the default settings.</returns>
		public static IServerFactory CreateDefault()
			=> new ServerFactory(
				new AssemblyInformationProvider(),
				new DefaultIOManager());

		/// <summary>
		/// Initializes a new instance of the <see cref="ServerFactory"/>.
		/// </summary>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/>.</param>
		/// <param name="ioManager">The value of <see cref="IOManager"/>.</param>
		internal ServerFactory(IAssemblyInformationProvider assemblyInformationProvider, IIOManager ioManager)
		{
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
			IOManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
		}

		/// <inheritdoc />
		public IServer CreateServer(string[] args, string updatePath)
		{
			var hostBuilder = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(
				args ?? throw new ArgumentNullException(nameof(args)))
				.ConfigureAppConfiguration((context, configurationBuilder) => configurationBuilder.SetBasePath(Directory.GetCurrentDirectory()))
				.ConfigureServices(serviceCollection =>
				{
					serviceCollection.AddSingleton(IOManager);
					serviceCollection.AddSingleton(assemblyInformationProvider);
				})
				.ConfigureWebHostDefaults(webHostBuilder =>
				{
					webHostBuilder
						.UseStartup<Application>()
						.SuppressStatusMessages(true)
						.UseShutdownTimeout(TimeSpan.FromMinutes(1));
				});

			if(updatePath != null)
				hostBuilder.UseContentRoot(Path.GetDirectoryName(assemblyInformationProvider.Path));

			return new Server(hostBuilder, IOManager, updatePath);
		}
	}
}
