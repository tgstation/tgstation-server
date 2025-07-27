using System;
using System.IO.Abstractions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Setup;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extension methods for the <see cref="IWebHostBuilder"/> class.
	/// </summary>
	static class WebHostBuilderExtensions
	{
		/// <summary>
		/// Workaround for using the <see cref="Application"/> class for server startup.
		/// </summary>
		/// <param name="builder">The <see cref="IWebHostBuilder"/> to configure.</param>
		/// <param name="assemblyInformationProvider">The <see cref="IAssemblyInformationProvider"/> to use.</param>
		/// <param name="ioManager">The <see cref="IIOManager"/> to use.</param>
		/// <param name="postSetupServices">The <see cref="IPostSetupServices"/> to use.</param>
		/// <param name="fileSystem">The <see cref="IFileSystem"/> to use.</param>
		/// <returns>The configured <paramref name="builder"/>.</returns>
		public static IWebHostBuilder UseApplication(
			this IWebHostBuilder builder,
			IAssemblyInformationProvider assemblyInformationProvider,
			IIOManager ioManager,
			IPostSetupServices postSetupServices,
			IFileSystem fileSystem)
		{
			ArgumentNullException.ThrowIfNull(builder);
			ArgumentNullException.ThrowIfNull(assemblyInformationProvider);
			ArgumentNullException.ThrowIfNull(ioManager);
			ArgumentNullException.ThrowIfNull(postSetupServices);
			ArgumentNullException.ThrowIfNull(fileSystem);

			return builder.ConfigureServices(
				(context, services) =>
				{
					var application = new Application(context.Configuration, context.HostingEnvironment);
					application.ConfigureServices(services, assemblyInformationProvider, ioManager, postSetupServices, fileSystem);
					services.AddSingleton(application);
				})
				.Configure(ConfigureApplication);
		}

		/// <summary>
		/// Configures a given <paramref name="applicationBuilder"/>.
		/// </summary>
		/// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to configure.</param>
		static void ConfigureApplication(IApplicationBuilder applicationBuilder)
			=> applicationBuilder
				.ApplicationServices
				.GetRequiredService<Application>()
				.Configure(
					applicationBuilder, // TODO: find a way to call this func and auto-resolve the services
					applicationBuilder.ApplicationServices.GetRequiredService<IServerControl>(),
					applicationBuilder.ApplicationServices.GetRequiredService<ITokenFactory>(),
					applicationBuilder.ApplicationServices.GetRequiredService<IAssemblyInformationProvider>(),
					applicationBuilder.ApplicationServices.GetRequiredService<IOptions<ControlPanelConfiguration>>(),
					applicationBuilder.ApplicationServices.GetRequiredService<IOptions<GeneralConfiguration>>(),
					applicationBuilder.ApplicationServices.GetRequiredService<IOptions<DatabaseConfiguration>>(),
					applicationBuilder.ApplicationServices.GetRequiredService<IOptions<SecurityConfiguration>>(),
					applicationBuilder.ApplicationServices.GetRequiredService<IOptions<SwarmConfiguration>>(),
					applicationBuilder.ApplicationServices.GetRequiredService<IOptions<InternalConfiguration>>(),
					applicationBuilder.ApplicationServices.GetRequiredService<IOptions<SessionConfiguration>>(),
					applicationBuilder.ApplicationServices.GetRequiredService<ILogger<Application>>());
	}
}
