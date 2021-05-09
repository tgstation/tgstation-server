using System;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog.Events;

using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Setup
{
	/// <summary>
	/// DI root for configuring a <see cref="SetupWizard"/>.
	/// </summary>
	class SetupApplication
	{
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="SetupApplication"/>.
		/// </summary>
		protected static readonly IIOManager IOManager = new DefaultIOManager();

		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="SetupApplication"/>.
		/// </summary>
		protected static readonly IAssemblyInformationProvider AssemblyInformationProvider = new AssemblyInformationProvider();

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
		public void ConfigureServices(IServiceCollection services)
		{
			if (services == null)
				throw new ArgumentNullException(nameof(services));

			services.SetupLogging(config => config.MinimumLevel.Override("Microsoft", LogEventLevel.Warning));

			services.AddSingleton(IOManager);
			services.AddSingleton(AssemblyInformationProvider);

			services.AddSingleton<IConsole, IO.Console>();
			services.AddSingleton<IDatabaseConnectionFactory, DatabaseConnectionFactory>();
			services.AddSingleton<IPlatformIdentifier, PlatformIdentifier>();
			services.AddSingleton<IAsyncDelayer, AsyncDelayer>();

			services.UseStandardConfig<GeneralConfiguration>(Configuration);
			services.UseStandardConfig<DatabaseConfiguration>(Configuration);
			services.UseStandardConfig<SecurityConfiguration>(Configuration);
			services.UseStandardConfig<FileLoggingConfiguration>(Configuration);

			ConfigureHostedService(services);
		}

		/// <summary>
		/// Configures the <see cref="IHostedService"/>.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
		protected virtual void ConfigureHostedService(IServiceCollection services)
		{
			services.AddSingleton(typeof(IPostSetupServices<>), typeof(PostSetupServices<>));
			services.AddSingleton<IHostedService, SetupWizard>();
		}
	}
}
