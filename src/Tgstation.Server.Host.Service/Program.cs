using System;
using System.Collections.Specialized;
using System.Configuration.Install;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.ServiceProcess;
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
						Arguments = String.Format(CultureInfo.InvariantCulture, "{0} {1}", Install ? "-i" : Uninstall ? "-u" : String.Empty, Configure ? "-c" : String.Empty),
						FileName = exe,
						WorkingDirectory = Environment.CurrentDirectory,
					};
					using (Process.Start(startInfo))
						return;
				}
			}

			using (var loggerFactory = new LoggerFactory())
			{
				if (Install)
				{
					if (Uninstall)
						return; // oh no, it's retarded...
					using (var processInstaller = new ServiceProcessInstaller())
					using (var installer = new ServiceInstaller())
					{
						processInstaller.Account = ServiceAccount.LocalSystem;

						installer.Context = new InstallContext("tgs-4-install.log", new string[] { String.Format(CultureInfo.InvariantCulture, "/assemblypath={0}", Assembly.GetEntryAssembly().Location) });
						installer.Description = "/tg/station 13 server v4 running as a windows service";
						installer.DisplayName = "/tg/station server 4";
						installer.DelayedAutoStart = true;
						installer.StartType = ServiceStartMode.Automatic;
						installer.ServicesDependedOn = new string[] { "Tcpip", "Dhcp", "Dnscache" };
						installer.ServiceName = ServerService.Name;
						installer.Parent = processInstaller;

						var state = new ListDictionary();
						installer.Install(state);
					}

					if (Configure)
					{
						Console.WriteLine("For this first run we'll launch the console runner so you may use the setup wizard.");
						Console.WriteLine("If it starts successfully, feel free to close it and then start the service from the Windows control panel.");
					}
				}
				else if (Uninstall)
					using (var installer = new ServiceInstaller())
					{
						installer.Context = new InstallContext("tgs-4-uninstall.log", null);
						installer.ServiceName = ServerService.Name;
						installer.Uninstall(null);
					}
				else if (!Configure)
					using (var service = new ServerService(WatchdogFactory, loggerFactory, Trace ? LogLevel.Trace : Debug ? LogLevel.Debug : LogLevel.Information))
						ServiceBase.Run(service);

				if (Configure)
				{
#pragma warning disable CS0618 // Type or member is obsolete
					loggerFactory.AddConsole();
#pragma warning restore CS0618 // Type or member is obsolete

					// DCT: None available
					await WatchdogFactory.CreateWatchdog(loggerFactory).RunAsync(true, Array.Empty<string>(), default).ConfigureAwait(false);
				}
			}
		}
	}
}
