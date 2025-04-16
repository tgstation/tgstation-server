using System;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog.Events;

using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.System;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Setup
{
	/// <summary>
	/// DI root for configuring a <see cref="SetupWizard"/>.
	/// </summary>
	public class SetupApplication
	{
		/// <summary>
		/// The <see cref="IConfiguration"/> for the <see cref="SetupApplication"/>.
		/// </summary>
		protected IConfiguration Configuration { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SetupApplication"/> class.
		/// </summary>
		/// <param name="configuration">The value of <see cref="Configuration"/>.</param>
		public SetupApplication(IConfiguration configuration)
		{
			Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
		}

		/// <summary>
		/// Configure dependency injected services.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
		/// <param name="assemblyInformationProvider">The <see cref="IAssemblyInformationProvider"/> needed for configuration.</param>
		/// <param name="ioManager">The <see cref="IIOManager"/> needed for configuration.</param>
		public void ConfigureServices(IServiceCollection services, IAssemblyInformationProvider assemblyInformationProvider, IIOManager ioManager)
		{
			ArgumentNullException.ThrowIfNull(services);
			ArgumentNullException.ThrowIfNull(assemblyInformationProvider);
			ArgumentNullException.ThrowIfNull(ioManager);

			services.SetupLogging(config => config.MinimumLevel.Override("Microsoft", LogEventLevel.Warning));

			services.AddSingleton(ioManager);
			services.AddSingleton(assemblyInformationProvider);

			services.AddSingleton<IConsole, IO.Console>();
			services.AddSingleton<IDatabaseConnectionFactory, DatabaseConnectionFactory>();
			services.AddSingleton<IPlatformIdentifier, PlatformIdentifier>();
			services.AddSingleton<IAsyncDelayer, AsyncDelayer>();

			// these configs are what's injected into PostSetupServices
			services.UseStandardConfig<GeneralConfiguration>(Configuration);
			services.UseStandardConfig<DatabaseConfiguration>(Configuration);
			services.UseStandardConfig<SecurityConfiguration>(Configuration);
			services.UseStandardConfig<FileLoggingConfiguration>(Configuration);
			services.UseStandardConfig<ElasticsearchConfiguration>(Configuration);
			services.UseStandardConfig<InternalConfiguration>(Configuration);
			services.UseStandardConfig<SwarmConfiguration>(Configuration);
			services.UseStandardConfig<SessionConfiguration>(Configuration);

			ConfigureHostedService(services);
		}

		/// <summary>
		/// Configures the <see cref="IHostedService"/>.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
		protected virtual void ConfigureHostedService(IServiceCollection services)
		{
			services.AddSingleton<IPostSetupServices, PostSetupServices>();
			services.AddSingleton<IHostedService, SetupWizard>();
		}
	}
}
