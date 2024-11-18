using System;
using System.Collections.Specialized;
using System.Configuration.Install;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

using McMaster.Extensions.CommandLineUtils;

using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Host.Common;
using Tgstation.Server.Host.Watchdog;

namespace Tgstation.Server.Host.Service
{
	/// <summary>
	/// Contains the entrypoint for the application.
	/// </summary>
	[SupportedOSPlatform("windows")]
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
		[Option(ShortName = "u", Description = "Uninstalls ANY installed tgstation-server service >=v4.0.0")]
		public bool Uninstall { get; }

		/// <summary>
		/// The --detach or -x option. Valid only with <see cref="Install"/>.
		/// </summary>
		[Option(ShortName = "x", Description = "If the service has to stop, detach any running DreamDaemon processes beforehand. Only supported on versions >=5.13.0")]
		public bool Detach { get; }

		/// <summary>
		/// The --restart or -r option.
		/// </summary>
		[Option(ShortName = "r", Description = "Stop and restart the tgstation-server service")]
		public bool Restart { get; }

		/// <summary>
		/// The --install or -i option.
		/// </summary>
		[Option(ShortName = "i", Description = "Installs this executable as the tgstation-server Windows service")]
		public bool Install { get; set; }

		/// <summary>
		/// The --force or -f option.
		/// </summary>
		[Option(ShortName = "f", Description = "Automatically agree to uninstall prompts")]
		public bool Force { get; set; }

		/// <summary>
		/// The --silent or -s option.
		/// </summary>
		[Option(ShortName = "s", Description = "Suppresses console output from the host watchdog")]
		public bool Silent { get; set; }

		/// <summary>
		/// The --configure or -c option.
		/// </summary>
		[Option(ShortName = "c", Description = "Runs the TGS setup wizard")]
		public bool Configure { get; set; }

		/// <summary>
		/// The --passthroughargs or -p option.
		/// </summary>
		[Option(ShortName = "p", Description = "Arguments passed to main host process")]
		public string? PassthroughArgs { get; set; }

		/// <summary>
		/// Entrypoint for the application.
		/// </summary>
		/// <param name="args">The application arguments.</param>
		/// <returns>A <see cref="Task"/> resulting in the <see cref="Program"/>'s exit code.</returns>
		static Task<int> Main(string[] args) => CommandLineApplication.ExecuteAsync<Program>(args);

		/// <summary>
		/// Command line handler, always runs.
		/// </summary>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		public async Task OnExecuteAsync()
		{
			var standardRun = !Install && !Uninstall && !Configure;
			if (standardRun)
				if (!Silent && !WindowsServiceHelpers.IsWindowsService())
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
				else
					using (var service = new ServerService(WatchdogFactory, GetPassthroughArgs(), LogLevel.Trace))
					{
						service.Run();
						return;
					}

			if (Configure)
				await RunConfigure(CancellationToken.None); // DCT: None available

			bool stopped = false;
			if (Uninstall)
			{
				foreach (ServiceController sc in ServiceController.GetServices())
				{
					bool match;
					using (sc)
					{
						match = sc.ServiceName == ServerService.Name;
						if (match)
							RestartService(sc);
					}

					if (match)
						InvokeSC(ServerService.Name);
				}

				stopped = true;
			}

			if (Install)
				stopped |= RunServiceInstall();

			if (Restart)
				foreach (ServiceController sc in ServiceController.GetServices())
					using (sc)
						if (sc.ServiceName == ServerService.Name)
						{
							if (!stopped)
								RestartService(sc);

							sc.Start();
						}
		}

		/// <summary>
		/// Runs sc.exe to either uninstall a given <paramref name="serviceToUninstall"/> or install the running <see cref="ServerService"/>.
		/// </summary>
		/// <param name="serviceToUninstall">The name of a service to uninstall.</param>
		void InvokeSC(string? serviceToUninstall)
		{
			using var installer = new ServiceInstaller();
			if (serviceToUninstall != null)
			{
				installer.Context = new InstallContext($"old-{serviceToUninstall}-uninstall.log", null);
				installer.ServiceName = serviceToUninstall;
				installer.Uninstall(null);
				return;
			}

			var fullPathToAssembly = Path.GetFullPath(
				Assembly.GetExecutingAssembly().Location);

			var assemblyDirectory = Path.GetDirectoryName(fullPathToAssembly);
			if (assemblyDirectory == null)
				throw new InvalidOperationException($"Failed to resolve directory name of {assemblyDirectory}");

			var assemblyNameWithoutExtension = Path.GetFileNameWithoutExtension(fullPathToAssembly);
			var exePath = Path.Combine(assemblyDirectory, $"{assemblyNameWithoutExtension}.exe");

			var programDataDirectory = Path.Combine(
				Environment.GetFolderPath(
					Environment.SpecialFolder.CommonApplicationData,
					Environment.SpecialFolderOption.DoNotVerify),
				Server.Common.Constants.CanonicalPackageName);

			using var processInstaller = new ServiceProcessInstaller();
			processInstaller.Account = ServiceAccount.LocalSystem;

			// Mimicing Tgstation.Server.Host.Service.Wix here, which is the source of truth for this data
			installer.Context = new InstallContext(
				Path.Combine(programDataDirectory, $"tgs-install-{Guid.NewGuid()}.log"),
				[
					$"assemblypath=\"{exePath}\"{(String.IsNullOrWhiteSpace(PassthroughArgs) ? String.Empty : $" -p=\"{PassthroughArgs}\"")}",
				]);
			installer.Description = $"{Server.Common.Constants.CanonicalPackageName} running as a Windows service.";
			installer.DisplayName = Server.Common.Constants.CanonicalPackageName;
			installer.StartType = ServiceStartMode.Automatic;
			installer.ServicesDependedOn = new string[] { "Tcpip", "Dhcp", "Dnscache" };
			installer.ServiceName = ServerService.Name;
			installer.Parent = processInstaller;

			var state = new ListDictionary();
			try
			{
				installer.Install(state);

				installer.Commit(state);
			}
			catch
			{
				installer.Rollback(state);
				throw;
			}
		}

		/// <summary>
		/// Attempt to install the TGS Service.
		/// </summary>
		/// <returns><see langword="true"/> if the service was stopped or detached as a result, <see langword="false"/> otherwise.</returns>
		bool RunServiceInstall()
		{
			// First check if the service already exists
			bool serviceStopped = false;
			if (Force || !WindowsServiceHelpers.IsWindowsService())
				foreach (ServiceController sc in ServiceController.GetServices())
					using (sc)
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
							InvokeSC(sc.ServiceName);
						}
					}

			InvokeSC(null);

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
				var serviceControllerCommand = PipeCommands.GetServiceCommandId(PipeCommands.CommandDetachingShutdown);
				serviceController.ExecuteCommand(serviceControllerCommand!.Value);
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
		async ValueTask RunConfigure(CancellationToken cancellationToken)
		{
			using var loggerFactory = LoggerFactory.Create(builder =>
			{
				if (!Silent)
					builder.AddConsole();
			});

			var watchdog = WatchdogFactory.CreateWatchdog(new NoopSignalChecker(), loggerFactory);
			await watchdog.RunAsync(true, GetPassthroughArgs(), cancellationToken);
		}

		/// <summary>
		/// Format <see cref="PassthroughArgs"/> into an <see cref="Array"/>.
		/// </summary>
		/// <returns><see cref="PassthroughArgs"/> formatted as a <see cref="string"/> <see cref="Array"/>.</returns>
		string[] GetPassthroughArgs() => PassthroughArgs?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
	}
}
