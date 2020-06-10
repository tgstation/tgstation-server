using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Setup;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host
{
	/// <summary>
	/// Implementation of <see cref="IServerFactory"/>.
	/// </summary>
	sealed class ServerFactory : IServerFactory
	{
		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="ServerFactory"/>.
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

		/// <inheritdoc />
		public IIOManager IOManager { get; }

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
		public async Task<IServer> CreateServer(string[] args, string updatePath, CancellationToken cancellationToken)
		{
			if (args == null)
				throw new ArgumentNullException(nameof(args));

			IHostBuilder CreateDefaultBuilder() => Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
				.ConfigureAppConfiguration((context, configuration) => configuration
					.SetBasePath(
						IOManager.ResolvePath()));

			var setupWizardHostBuilder = CreateDefaultBuilder()
				.UseSetupApplication();

			IPostSetupServices postSetupServices;
			using (var setupHost = setupWizardHostBuilder.Build())
			{
				postSetupServices = setupHost.Services.GetRequiredService<IPostSetupServices>();
				await setupHost.RunAsync(cancellationToken).ConfigureAwait(false);
			}

			var hostBuilder = CreateDefaultBuilder()
				.ConfigureWebHost(webHostBuilder =>
					webHostBuilder
						.UseKestrel(kestrelOptions =>
						{
							var serverPortProvider = kestrelOptions.ApplicationServices.GetRequiredService<IServerPortProvider>();
							kestrelOptions.ListenAnyIP(
								serverPortProvider.HttpApiPort,
								listenOptions => listenOptions.Protocols = HttpProtocols.Http1AndHttp2);
						})
						.UseIIS()
						.UseIISIntegration()
						.UseApplication(postSetupServices)
						.SuppressStatusMessages(true)
						.UseShutdownTimeout(TimeSpan.FromMilliseconds(postSetupServices.GeneralConfiguration.RestartTimeout)));

			if (updatePath != null)
				hostBuilder.UseContentRoot(
					IOManager.ResolvePath(
						IOManager.GetDirectoryName(assemblyInformationProvider.Path)));

			return new Server(hostBuilder, updatePath);
		}
	}
}
