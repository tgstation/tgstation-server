using McMaster.Extensions.CommandLineUtils;
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
		public bool Install { get; }

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
			if ((Install || Uninstall) && !IsAdministrator())
			{
				//try to restart as admin
				var argList = Environment.GetCommandLineArgs().ToList();
				//its windows, first arg is .exe name guaranteed
				var exe = argList.First();
				argList.RemoveAt(0);
				var startInfo = new ProcessStartInfo
				{
					UseShellExecute = true,
					Verb = "runas",
					Arguments = String.Join(" ", argList),
					FileName = exe,
					WorkingDirectory = Environment.CurrentDirectory,
				};
				using (Process.Start(startInfo))
					return;
			}

			if (Install)
			{
				if (Uninstall)
					//oh no, it's retarded...
					return;
				using (var processInstaller = new ServiceProcessInstaller())
				using (var installer = new ServiceInstaller())
				{
					processInstaller.Account = ServiceAccount.NetworkService;

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
				ServiceBase.Run(new ServerService(new WatchdogFactory()));
		}

		/// <summary>
		/// Entrypoint for the application
		/// </summary>
		static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);
	}
}
