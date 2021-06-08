using System;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Tgstation.Server.Host.Components;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Setup;

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
		/// <param name="postSetupServices">The <see cref="IPostSetupServices"/> to use.</param>
		/// <returns>The configured <paramref name="builder"/>.</returns>
		public static IWebHostBuilder UseApplication(this IWebHostBuilder builder, IPostSetupServices postSetupServices)
		{
			if (builder == null)
				throw new ArgumentNullException(nameof(builder));
			if (postSetupServices == null)
				throw new ArgumentNullException(nameof(postSetupServices));

			return builder.ConfigureServices((context, services) =>
				{
					var application = new Application(context.Configuration, context.HostingEnvironment);
					application.ConfigureServices(services, postSetupServices);
					services.AddSingleton(application);
				})
				.Configure(applicationBuilder => applicationBuilder
					.ApplicationServices
					.GetRequiredService<Application>()
					.Configure(
						applicationBuilder,
						applicationBuilder.ApplicationServices.GetRequiredService<IServerControl>(),
						applicationBuilder.ApplicationServices.GetRequiredService<ITokenFactory>(),
						applicationBuilder.ApplicationServices.GetRequiredService<IInstanceManager>(),
						applicationBuilder.ApplicationServices.GetRequiredService<IServerAddressProvider>(),
						applicationBuilder.ApplicationServices.GetRequiredService<IOptions<ControlPanelConfiguration>>(),
						applicationBuilder.ApplicationServices.GetRequiredService<IOptions<GeneralConfiguration>>(),
						applicationBuilder.ApplicationServices.GetRequiredService<ILogger<Application>>()));
		}
	}
}
