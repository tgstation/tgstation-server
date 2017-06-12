using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace ServerService
{
	[RunInstaller(true)]
	public partial class ProjectInstaller : Installer
	{
		public ProjectInstaller()
		{
			InitializeComponent();
		}
	}
}
