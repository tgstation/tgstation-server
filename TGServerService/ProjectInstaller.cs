using System.ComponentModel;
using System.Configuration.Install;

namespace ServerService
{
	[RunInstaller(true)]
	partial class ProjectInstaller : Installer
	{
		public ProjectInstaller()
		{
			InitializeComponent();
		}
	}
}
