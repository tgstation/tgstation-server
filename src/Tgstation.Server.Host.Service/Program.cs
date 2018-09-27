using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Specialized;
using System.Configuration.Install;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.ServiceProcess;
using System.Windows.Forms;
using Tgstation.Server.Host.Watchdog;

namespace Tgstation.Server.Host.Service
{
	/// <summary>
	/// Contains the entrypoint for the application
	/// </summary>
	[ExcludeFromCodeCoverage]
	class Program
	{
		/// <summary>
		/// The --uninstall or -u option
		/// </summary>
		[Option(ShortName = "u")]
		public bool Uninstall { get; }

		/// <summary>
		/// The --install or -i option
		/// </summary>
		[Option(ShortName = "i")]
		public bool Install { get; set; }

		/// <summary>
		/// The --trace or -t option. Enables trace logs
		/// </summary>
		[Option(ShortName = "t")]
		public bool Trace { get; set; }

		/// <summary>
		/// The --debug or -d option. Enables debug logs
		/// </summary>
		[Option(ShortName = "d")]
		public bool Debug { get; set; }

		/// <summary>
		/// Check if the running user is a system administrator
		/// </summary>
		/// <returns><see langword="true"/> if the running user is a system administrator, <see langword="false"/> otherwise</returns>
		static bool IsAdministrator()
		{
			var user = WindowsIdentity.GetCurrent();
			var principal = new WindowsPrincipal(user);
			return principal.IsInRole(WindowsBuiltInRole.Administrator);
		}

		/// <summary>
		/// Command line handler, always runs
		/// </summary>
		public void OnExecute()
		{
			if (Environment.UserInteractive)
			{
				if (!IsAdministrator())
				{
					if (!(Install || Uninstall))
					{
						var result = MessageBox.Show("You are running the TGS windows service executable directly. It should only be run by the service control manager. Would you like to install the service in this location?", "TGS Service", MessageBoxButtons.YesNo);
						if (result == DialogResult.No)
							return;
						Install = true;
					}

					//try to restart as admin
					//its windows, first arg is .exe name guaranteed
					var exe = Environment.GetCommandLineArgs().First();
					var startInfo = new ProcessStartInfo
					{
						UseShellExecute = true,
						Verb = "runas",
						Arguments = Install ? "-i" : "-u",
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
					//oh no, it's retarded...
					return;
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
			}
			else if (Uninstall)
				using (var installer = new ServiceInstaller())
				{
					installer.Context = new InstallContext("tgs-4-uninstall.log", null);
					installer.ServiceName = ServerService.Name;
					installer.Uninstall(null);
				}
			else
				using (var loggerFactory = new LoggerFactory())
					ServiceBase.Run(new ServerService(new WatchdogFactory(), loggerFactory, Trace ? LogLevel.Trace : Debug ? LogLevel.Debug : LogLevel.Information));
		}

		/// <summary>
		/// Entrypoint for the application
		/// </summary>
		[STAThread]
		static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);
	}
}
