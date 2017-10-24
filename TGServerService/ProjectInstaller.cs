using System.ComponentModel;
using System.Configuration.Install;

namespace TGServerService
{
	/// <summary>
	/// This tells the .msi there is a Windows <see cref="System.ServiceProcess.ServiceBase"/> in this <see cref="System.Reflection.Assembly"/> that needs installation
	/// </summary>
	[RunInstaller(true)]
	public partial class ProjectInstaller : Installer
	{
		/// <summary>
		/// Construct a <see cref="ProjectInstaller"/>
		/// </summary>
		public ProjectInstaller()
		{
			InitializeComponent();
		}
	}
}
