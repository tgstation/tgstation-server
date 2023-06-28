using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

using McMaster.Extensions.CommandLineUtils;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Host.Common;
using Tgstation.Server.Host.Watchdog;

namespace Tgstation.Server.Host.Service
{
	/// <summary>
	/// Contains the entrypoint for the application.
	/// </summary>
	sealed class Program
	{
		/// <summary>
		/// The <see cref="IWatchdogFactory"/> for the <see cref="Program"/>.
		/// </summary>
#pragma warning disable SA1401 // Fields should be private
		internal static IWatchdogFactory WatchdogFactory = new WatchdogFactory();
#pragma warning restore SA1401 // Fields should be private

		/// <summary>
		/// The --uninstall or -u option.
		/// </summary>
		[Option(ShortName = "u")]
		public bool Uninstall { get; }

		/// <summary>
		/// The --detach or -x option. Valid only with <see cref="Install"/>.
		/// </summary>
		[Option(ShortName = "x")]
		public bool Detach { get; }

		/// <summary>
		/// The --restart or -r option.
		/// </summary>
		[Option(ShortName = "r")]
		public bool Restart { get; }

		/// <summary>
		/// The --install or -i option.
		/// </summary>
		[Option(ShortName = "i")]
		public bool Install { get; set; }

		/// <summary>
		/// The --force or -f option.
		/// </summary>
		[Option(ShortName = "f")]
		public bool Force { get; set; }

		/// <summary>
		/// The --silent or -s option.
		/// </summary>
		[Option(ShortName = "s")]
		public bool Silent { get; set; }

		/// <summary>
		/// The --configure or -c option.
		/// </summary>
		[Option(ShortName = "c")]
		public bool Configure { get; set; }

		/// <summary>
		/// The --trace or -t option. Enables trace logs.
		/// </summary>
		[Option(ShortName = "t")]
		public bool Trace { get; set; }

		/// <summary>
		/// The --debug or -d option. Enables debug logs.
		/// </summary>
		[Option(ShortName = "d")]
		public bool Debug { get; set; }

		/// <summary>
		/// Entrypoint for the application.
		/// </summary>
		/// <param name="args">The application arguments.</param>
		/// <returns>A <see cref="Task"/> resulting in the <see cref="Program"/>'s exit code.</returns>
		static Task<int> Main(string[] args) => CommandLineApplication.ExecuteAsync<Program>(args);

		/// <summary>
		/// Runs sc.exe to either uninstall a given <paramref name="serviceToUninstall"/> or install the running <see cref="ServerService"/>.
		/// </summary>
		/// <param name="serviceToUninstall">The name of a service to uninstall.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		static async ValueTask InvokeSC(string serviceToUninstall)
		{
			using var process = new Process();
			process.StartInfo.FileName = "C:/Windows/System32/sc.exe";

			var fullPathToAssembly = Path.GetFullPath(
				Assembly.GetExecutingAssembly().Location);

			var assemblyDirectory = Path.GetDirectoryName(fullPathToAssembly);
			var assemblyNameWithoutExtension = Path.GetFileNameWithoutExtension(fullPathToAssembly);
			var exePath = Path.Combine(assemblyDirectory, $"{assemblyNameWithoutExtension}.exe");

			process.StartInfo.Arguments = serviceToUninstall == null
				? $"create tgstation-server binPath=\"{exePath}\" start=auto depend=Tcpip/Dhcp/Dnscache"
				: $"delete {serviceToUninstall}";

			process.Start();

			await process.WaitForExitAsync();
		}

		/// <summary>
		/// Command line handler, always runs.
		/// </summary>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		public async Task OnExecuteAsync()
		{
			var standardRun = !Install && !Uninstall && !Configure;

			if (Environment.UserInteractive && standardRun)
			{
				var result = NativeMethods.MessageBox(
					default,
					"You are running the TGS windows service executable directly. It should only be run by the service control manager. Would you like to install and configure the service in this location?",
					"TGS Service",
					NativeMethods.MessageBoxButtons.YesNo);

				if (result != NativeMethods.DialogResult.Yes)
					return;

				Install = true;
				Configure = true;
			}

			if (Configure)
				await RunConfigure(CancellationToken.None); // DCT: None available

			bool stopped = false;
			if (Uninstall)
			{
				foreach (ServiceController sc in ServiceController.GetServices())
					if (sc.ServiceName == ServerService.Name)
					{
						RestartService(sc);
						await InvokeSC(ServerService.Name);
						break;
					}

				stopped = true;
			}

			if (Install)
				stopped |= await RunServiceInstall();

			if (standardRun)
			{
				using var service = new ServerService(WatchdogFactory, Trace ? LogLevel.Trace : Debug ? LogLevel.Debug : LogLevel.Information);
				ServiceBase.Run(service);
			}

			if (Restart)
				foreach (ServiceController sc in ServiceController.GetServices())
					if (sc.ServiceName == ServerService.Name)
					{
						if (!stopped)
							RestartService(sc);

						sc.Start();
						break;
					}
		}

		/// <summary>
		/// Attempt to install the TGS Service.
		/// </summary>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in <see langword="true"/> if the service was stopped or detached as a result, <see langword="false"/> otherwise.</returns>
		async ValueTask<bool> RunServiceInstall()
		{
			// First check if the service already exists
			bool serviceStopped = false;
			if (Force || Environment.UserInteractive)
				foreach (ServiceController sc in ServiceController.GetServices())
				{
					var serviceName = sc.ServiceName;
					if (serviceName == ServerService.Name || serviceName == "tgstation-server-4")
					{
						NativeMethods.DialogResult result = !Force
							? NativeMethods.MessageBox(
								default,
								$"You already have another TGS service installed ({sc.ServiceName}). Would you like to uninstall it now? Pressing \"No\" will cancel this install.",
								"TGS Service",
								NativeMethods.MessageBoxButtons.YesNo)
							: NativeMethods.DialogResult.Yes;
						if (result != NativeMethods.DialogResult.Yes)
							return false; // is this needed after exit?

						// Stop it first to give it some cleanup time
						RestartService(sc);

						// And remove it
						await InvokeSC(sc.ServiceName);
					}
				}

			await InvokeSC(null);

			return serviceStopped;
		}

		/// <summary>
		/// Restarts a service using a given <paramref name="serviceController"/>.
		/// </summary>
		/// <param name="serviceController">The <see cref="ServiceController"/> for the service to restart.</param>
		void RestartService(ServiceController serviceController)
		{
			if (serviceController.Status != ServiceControllerStatus.Running)
				return;

			var stop = !Detach;
			if (!stop)
			{
				serviceController.ExecuteCommand(
					ServerService.GetCommand(
						PipeCommands.CommandDetachingShutdown)
					.Value);
				serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
				if (serviceController.Status != ServiceControllerStatus.Stopped)
					stop = true;
			}

			if (stop)
			{
				serviceController.Stop();
				serviceController.WaitForStatus(ServiceControllerStatus.Stopped);
			}
		}

		/// <summary>
		/// Runs the host application with the setup wizard.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task RunConfigure(CancellationToken cancellationToken)
		{
			using var loggerFactory = LoggerFactory.Create(builder =>
			{
				if (!Silent)
					builder.AddConsole();
			});

			var watchdog = WatchdogFactory.CreateWatchdog(new NoopSignalChecker(), loggerFactory);
			await watchdog.RunAsync(true, Array.Empty<string>(), cancellationToken);
		}
	}
}
