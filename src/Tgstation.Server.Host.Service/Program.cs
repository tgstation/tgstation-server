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
			if (Environment.UserInteractive)
			{
				if (!Install && !Uninstall && !Configure)
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
					var exe = Environment.GetCommandLineArgs().First();
					var startInfo = new ProcessStartInfo
					{
						UseShellExecute = true,
						Verb = "runas",
						Arguments = $"{(Install ? "-i" : Uninstall ? "-u" : String.Empty)} {(Configure ? "-c" : String.Empty)} {(Force ? "-f" : String.Empty)}",
						FileName = exe,
						WorkingDirectory = Environment.CurrentDirectory,
					};
					using (Process.Start(startInfo))
						return;
				}
			}

			if (Install)
			{
				if (Uninstall)
					return; // oh no, it's retarded...

				RunServiceInstall();

				if (Configure && !Silent)
					Console.WriteLine("For this first run we'll launch the console runner so you may use the setup wizard.");
			}
			else if (Uninstall)
				using (var installer = new ServiceInstaller())
				{
					installer.Context = new InstallContext("tgs-uninstall.log", null);
					if (Silent)
						installer.Context.Parameters["LogToConsole"] = false.ToString();

					installer.ServiceName = ServerService.Name;
					installer.Uninstall(null);
				}
			else if (!Configure)
			{
				using var service = new ServerService(WatchdogFactory, Trace ? LogLevel.Trace : Debug ? LogLevel.Debug : LogLevel.Information);
				ServiceBase.Run(service);
			}

			if (Configure)
				await RunConfigure(CancellationToken.None); // DCT: None available
		}

		/// <summary>
		/// Attempt to install the TGS Service.
		/// </summary>
		void RunServiceInstall()
		{
			// First check if the service already exists
			if (Force || Environment.UserInteractive)
				foreach (ServiceController sc in ServiceController.GetServices())
					if (sc.ServiceName == "tgstation-server" || sc.ServiceName == "tgstation-server-4")
					{
						DialogResult result = !Force
							? MessageBox.Show($"You already have another TGS service installed ({sc.ServiceName}). Would you like to uninstall it now? Pressing \"No\" will cancel this install.", "TGS Service", MessageBoxButtons.YesNo)
							: DialogResult.Yes;
						if (result != DialogResult.Yes)
							return; // is this needed after exit?

						// Stop it first to give it some cleanup time
						if (sc.Status == ServiceControllerStatus.Running)
						{
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
					}

			using var processInstaller = new ServiceProcessInstaller();
			using var installer = new ServiceInstaller();
			processInstaller.Account = ServiceAccount.LocalSystem;

			installer.Context = new InstallContext("tgs-install.log", new string[] { String.Format(CultureInfo.InvariantCulture, "/assemblypath={0}", Assembly.GetEntryAssembly().Location) });
			if (Silent)
				installer.Context.Parameters["LogToConsole"] = false.ToString();
			installer.Description = "tgstation-server running as a windows service";
			installer.DisplayName = "tgstation-server";
			installer.StartType = ServiceStartMode.Automatic;
			installer.ServicesDependedOn = new string[] { "Tcpip", "Dhcp", "Dnscache" };
			installer.ServiceName = ServerService.Name;
			installer.Parent = processInstaller;

			var state = new ListDictionary();
			installer.Install(state);
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
			await WatchdogFactory.CreateWatchdog(loggerFactory)
				.RunAsync(true, Array.Empty<string>(), cancellationToken);
		}
	}
}
