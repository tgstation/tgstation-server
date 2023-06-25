using System;
using System.Collections.Specialized;
using System.Configuration.Install;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

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
		/// The --resume or -r option.
		/// </summary>
		[Option(ShortName = "r")]
		public bool Resume { get; }

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
		/// Check if the running user is a system administrator.
		/// </summary>
		/// <returns><see langword="true"/> if the running user is a system administrator, <see langword="false"/> otherwise.</returns>
		static bool IsAdministrator()
		{
			var user = WindowsIdentity.GetCurrent();
			var principal = new WindowsPrincipal(user);
			return principal.IsInRole(WindowsBuiltInRole.Administrator);
		}

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

			if (Environment.UserInteractive)
			{
				if (standardRun)
				{
					var result = MessageBox.Show("You are running the TGS windows service executable directly. It should only be run by the service control manager. Would you like to install and configure the service in this location?", "TGS Service", MessageBoxButtons.YesNo);
					if (result != DialogResult.Yes)
						return;
					Install = true;
					Configure = true;
				}

				if (!IsAdministrator())
				{
					// try to restart as admin
					// its windows, first arg is .exe name guaranteed
					var args = Environment.GetCommandLineArgs();
					var exe = args.First();
					var startInfo = new ProcessStartInfo
					{
						UseShellExecute = true,
						Verb = "runas",
						Arguments = String.Join(" ", args.Skip(1)),
						FileName = exe,
						WorkingDirectory = Environment.CurrentDirectory,
					};
					using (Process.Start(startInfo))
						return;
				}
			}

			if (Configure)
				await RunConfigure(CancellationToken.None); // DCT: None available

			if (Uninstall)
				using (var installer = new ServiceInstaller())
				{
					installer.Context = new InstallContext("tgs-uninstall.log", null);
					if (Silent)
						installer.Context.Parameters["LogToConsole"] = false.ToString();

					installer.ServiceName = ServerService.Name;
					installer.Uninstall(null);
				}

			if (Install)
			{
				RunServiceInstall();
			}

			if (standardRun)
			{
				using var service = new ServerService(WatchdogFactory, Trace ? LogLevel.Trace : Debug ? LogLevel.Debug : LogLevel.Information);
				ServiceBase.Run(service);
			}

			if (Resume)
				foreach (ServiceController sc in ServiceController.GetServices())
					if (sc.ServiceName == ServerService.Name)
					{
						sc.Start();
						break;
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
			if (Force || Environment.UserInteractive)
				foreach (ServiceController sc in ServiceController.GetServices())
					if (sc.ServiceName == ServerService.Name || sc.ServiceName == "tgstation-server-4")
					{
						DialogResult result = !Force
							? MessageBox.Show($"You already have another TGS service installed ({sc.ServiceName}). Would you like to uninstall it now? Pressing \"No\" will cancel this install.", "TGS Service", MessageBoxButtons.YesNo)
							: DialogResult.Yes;
						if (result != DialogResult.Yes)
							return false; // is this needed after exit?

						// Stop it first to give it some cleanup time
						if (sc.Status == ServiceControllerStatus.Running)
						{
							if (Detach)
								sc.ExecuteCommand(
									ServerService.GetCommand(
										PipeCommands.CommandDetachingShutdown)
									.Value);
							else
								sc.Stop();

							sc.WaitForStatus(ServiceControllerStatus.Stopped);
						}

						// And remove it
						using var serviceInstaller = new ServiceInstaller();
						serviceInstaller.Context = new InstallContext($"old-{sc.ServiceName}-uninstall.log", null);

						if (Silent)
							serviceInstaller.Context.Parameters["LogToConsole"] = false.ToString();

						serviceInstaller.ServiceName = sc.ServiceName;
						serviceInstaller.Uninstall(null);

						serviceStopped = true;
					}

			using var processInstaller = new ServiceProcessInstaller();
			using var installer = new ServiceInstaller();
			processInstaller.Account = ServiceAccount.LocalSystem;

			installer.Context = new InstallContext("tgs-install.log", new string[] { String.Format(CultureInfo.InvariantCulture, "/assemblypath={0}", Assembly.GetEntryAssembly().Location) });
			if (Silent)
				installer.Context.Parameters["LogToConsole"] = false.ToString();
			installer.Description = "tgstation-server running as a windows service";
			installer.DisplayName = ServerService.Name;
			installer.StartType = ServiceStartMode.Automatic;
			installer.ServicesDependedOn = new string[] { "Tcpip", "Dhcp", "Dnscache" };
			installer.ServiceName = ServerService.Name;
			installer.Parent = processInstaller;

			var state = new ListDictionary();
			installer.Install(state);

			return serviceStopped;
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
