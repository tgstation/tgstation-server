using Microsoft.Extensions.Hosting;
using System;
using Tgstation.Server.Host.Setup;

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extension methods for the <see cref="IHostBuilder"/> <see langword="class"/>.
	/// </summary>
	static class HostBuilderExtensions
	{
		/// <summary>
		/// Workaround for using the <see cref="SetupApplication"/> <see langword="class"/> for host startup.
		/// </summary>
		/// <param name="builder">The <see cref="IHostBuilder"/> to configure.</param>
		/// <returns>The configured <paramref name="builder"/>.</returns>
		public static IHostBuilder UseSetupApplication(this IHostBuilder builder)
		{
			if (builder == null)
				throw new ArgumentNullException(nameof(builder));

			return builder.ConfigureServices((context, services) =>
			{
				var setupApplication = new SetupApplication(context.Configuration);
				setupApplication.ConfigureServices(services);
			});
		}
	}
}
