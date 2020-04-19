using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host
{
	/// <summary>
	/// Implementation of <see cref="IServerFactory"/>.
	/// </summary>
	/// <typeparam name="TStartup">The startup <see langword="class"/> to configure services.</typeparam>
	public sealed class ServerFactory<TStartup> : IServerFactory where TStartup : class
	{
		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="ServerFactory{TStartup}"/>.
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

		/// <inheritdoc />
		public IIOManager IOManager { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ServerFactory{TStartup}"/>.
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
				.ConfigureWebHostDefaults(webHostBuilder =>
					webHostBuilder
						.ConfigureAppConfiguration((context, configurationBuilder) =>
							configurationBuilder.SetBasePath(
								IOManager.ResolvePath(".")))
						.UseStartup<TStartup>()
						.SuppressStatusMessages(true)
						.UseShutdownTimeout(TimeSpan.FromMinutes(1)));

			if(updatePath != null)
				hostBuilder.UseContentRoot(IOManager.GetDirectoryName(assemblyInformationProvider.Path));

			return new Server(hostBuilder, IOManager, updatePath);
		}
	}
}
