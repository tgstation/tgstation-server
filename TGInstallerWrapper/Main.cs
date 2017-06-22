using Microsoft.Deployment.WindowsInstaller;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using TGServiceInterface;

namespace TGInstallerWrapper
{
	public partial class Main : Form
	{
		const string InstallDir = "TG Station Server";  //keep this in sync with the msi installer
		bool installing = false;
		bool pathIsDefault = true;
		public Main()
		{
			InitializeComponent();
			FormClosing += Main_FormClosing;
			PathTextBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + Path.DirectorySeparatorChar + InstallDir;
		}

		private void Main_FormClosing(object sender, FormClosingEventArgs e)
		{
			e.Cancel = installing;	//not allowed, nununu
		}

		async void DoInstall()
		{
			string path = null;
			string logfile = null;
			try
			{
				var args = new List<string>();
				if (!pathIsDefault)
					args.Add(String.Format("INSTALLFOLDER=\"{0}\"", PathTextBox.Text));
				if (StartShortcutsCheckbox.Checked)
					args.Add("INSTALLSHORTCUTSTART=1");
				if (DeskShortcutsCheckbox.Checked)
					args.Add("INSTALLSHORTCUTDESK=1");

				SelectPathButton.Enabled = false;
				PathTextBox.Enabled = false;
				DeskShortcutsCheckbox.Enabled = false;
				StartShortcutsCheckbox.Enabled = false;
				InstallButton.Enabled = false;
				ShowLogCheckbox.Enabled = false;
				InstallButton.Text = "Installing...";
				UseWaitCursor = true;

				path = Path.GetTempFileName();
				File.Delete(path);  //we want a dir not a file
				Directory.CreateDirectory(path);
				var msipath = path + Path.DirectorySeparatorChar + "TGServiceInstaller.msi";
				File.WriteAllBytes(msipath, Properties.Resources.TGServiceInstaller);
				File.WriteAllBytes(path + Path.DirectorySeparatorChar + "cab1.cab", Properties.Resources.cab1);

				var res = Server.VerifyConnection();
				if (res != null)
				{
					if (MessageBox.Show("Could not connect to installed service. If the service has to be stopped, existing DreamDaemon instances will be terminated. Continue?", "Warning", MessageBoxButtons.YesNo) != DialogResult.Yes)
						return;
				}
				else
					Server.GetComponent<ITGSService>().PrepareForUpdate();

				ProgressBar.Style = ProgressBarStyle.Marquee;

				if (ShowLogCheckbox.Checked)
				{
					logfile = path + Path.DirectorySeparatorChar + "tgsinstall.log";
					Installer.EnableLog(InstallLogModes.Verbose | InstallLogModes.PropertyDump, logfile);
				}
				var cl = args.Count > 0 ? String.Join(" ", args) : "";

				Installer.SetInternalUI(InstallUIOptions.Silent);
				installing = true;
				await Task.Factory.StartNew(() => Installer.InstallProduct(msipath, cl));
				installing = false;
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error: " + ex.ToString());
				return;
			}
			finally
			{
				ShowLogCheckbox.Enabled = true;
				SelectPathButton.Enabled = true;
				PathTextBox.Enabled = true;
				DeskShortcutsCheckbox.Enabled = true;
				StartShortcutsCheckbox.Enabled = true;
				ProgressBar.Style = ProgressBarStyle.Blocks;
				InstallButton.Enabled = true;
				InstallButton.Text = "Install";
				UseWaitCursor = false;
				installing = false;
				if (ShowLogCheckbox.Checked && logfile != null)
					try
					{
						Process.Start(logfile).WaitForInputIdle();
					}
					catch { }
				if (path != null)
					try
					{
						Directory.Delete(path, true);
					}
					catch { }
			}
			MessageBox.Show("Success!");
			Application.Exit();
		}

		private void InstallButton_Click(object sender, EventArgs e)
		{
			DoInstall();
		}

		private void SelectPathButton_Click(object sender, EventArgs e)
		{
			var fbd = new FolderBrowserDialog()
			{
				Description = "Select where you would like to install the service executables. This isn't where an actual server instance is created.",
				ShowNewFolderButton = true
			};
			if (fbd.ShowDialog() != DialogResult.OK)
				return;
			pathIsDefault = false;
			PathTextBox.Text = fbd.SelectedPath + Path.DirectorySeparatorChar + InstallDir;
		}
	}
}
