using System;

using Microsoft.Extensions.Hosting;

using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Setup;
using Tgstation.Server.Host.System;

#nullable disable

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extension methods for the <see cref="IHostBuilder"/> class.
	/// </summary>
	static class HostBuilderExtensions
	{
		/// <summary>
		/// Workaround for using the <see cref="SetupApplication"/> class for host startup.
		/// </summary>
		/// <param name="builder">The <see cref="IHostBuilder"/> to configure.</param>
		/// <param name="assemblyInformationProvider">The <see cref="IAssemblyInformationProvider"/> to use.</param>
		/// <param name="ioManager">The <see cref="IIOManager"/> to use.</param>
		/// <returns>The configured <paramref name="builder"/>.</returns>
		public static IHostBuilder UseSetupApplication(
			this IHostBuilder builder,
			IAssemblyInformationProvider assemblyInformationProvider,
			IIOManager ioManager)
		{
			ArgumentNullException.ThrowIfNull(builder);

			return builder.ConfigureServices((context, services) =>
			{
				var setupApplication = new SetupApplication(context.Configuration);
				setupApplication.ConfigureServices(services, assemblyInformationProvider, ioManager);
			});
		}
	}
}
