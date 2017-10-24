using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace TGServerService
{
	/// <summary>
	/// This tells the .msi there is a Windows <see cref="ServiceBase"/> in this <see cref="System.Reflection.Assembly"/> that needs installation
	/// </summary>
	[RunInstaller(true)]
	public partial class ProjectInstaller : Installer
	{
		/// <summary>
		/// Construct a <see cref="ProjectInstaller"/>
		/// </summary>
		public ProjectInstaller()
		{
			Installers.AddRange(new Installer[] {
				new ServiceProcessInstaller
				{
					Account = ServiceAccount.LocalSystem,
					Password = null,
					Username = null
				},
				new ServiceInstaller
				{
					Description = "/tg/station Server Service",
					DisplayName = "TG Station Server",
					ServiceName = "TG Station Server",
					StartType = ServiceStartMode.Automatic
				}
			});
		}
	}
}
