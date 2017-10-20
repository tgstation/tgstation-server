using System.ComponentModel;
using System.Configuration.Install;

namespace TGServerService
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
