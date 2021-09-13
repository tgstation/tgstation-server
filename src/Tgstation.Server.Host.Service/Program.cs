using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;

using Tgstation.Server.Host.Watchdog;

namespace Tgstation.Server.Host.Service
{
	/// <summary>
	/// Contains the entrypoint for the application.
	/// </summary>
	public static class Program
	{
		/// <summary>
		/// The <see cref="IWatchdogFactory"/> for the <see cref="Program"/>.
		/// </summary>
#pragma warning disable SA1401 // Fields should be private
		internal static IWatchdogFactory WatchdogFactory = new WatchdogFactory();
#pragma warning restore SA1401 // Fields should be private

		/// <summary>
		/// Entrypoint for the application.
		/// </summary>
		/// <param name="args">The application arguments.</param>
		/// <returns>A <see cref="Task"/> resulting in the <see cref="Program"/>'s exit code.</returns>
		public static async Task Main(string[] args)
		{
			if (!WindowsServiceHelpers.IsWindowsService())
			{
				await WatchdogHelpers.RunConsole(
					WatchdogFactory,
					loggerFactory =>
					{
						var logger = loggerFactory.CreateLogger(nameof(Program));
						var assemblyPath = Assembly.GetEntryAssembly()?.Location;
						if (assemblyPath == null)
							assemblyPath = "<Full Path to Tgstation.Server.Host.Service.exe>";

						logger.LogWarning(
							$"The server service is meant to be run via the Windows service manager. To install the server as a service in this location run `sc create tgstation-server binPath=\"{assemblyPath}\"` in an elevated command prompt. If it already exists, run `sc delete tgstation-server` first (after stopping it). This console will only let you configure tgstation-server.");
					},
					args,
					true)
					.ConfigureAwait(false);

				return;
			}

			using var host = Microsoft.Extensions.Hosting.Host
				.CreateDefaultBuilder(args)
				.ConfigureServices((hostContext, services) =>
				{
					services.AddSingleton(serviceProvider => WatchdogFactory.CreateWatchdog(serviceProvider.GetRequiredService<ILoggerFactory>()));
					services.AddHostedService<ServerService>()
						.Configure<EventLogSettings>(config =>
						{
							config.LogName = "tgstation-server Service";
							config.SourceName = "tgstation-server Service Source";
						});
				})
				.UseWindowsService()
				.Build();

			await host.RunAsync();
		}
	}
}
