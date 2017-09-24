using Microsoft.Deployment.WindowsInstaller;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TGServiceInterface;

namespace TGInstallerWrapper
{
	public partial class Main : Form
	{
		const string InstallDir = "TG Station Server";  //keep this in sync with the msi installer
		bool installing = false;
		bool cancelled = false;
		bool pathIsDefault = true;
		public Main()
		{
			InitializeComponent();
			FormClosing += Main_FormClosing;
			PathTextBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + Path.DirectorySeparatorChar + InstallDir;
			if (Server.VerifyConnection() == null)
				try
				{
					VersionLabel.Text = Server.GetComponent<ITGSService>().Version();
				}
				catch
				{
					VersionLabel.Text = "< v3.0.85.0 (Missing ITGService.Version())";
				}
			TargetVersionLabel.Text += " v" + FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion;
		}

		private void Main_FormClosing(object sender, FormClosingEventArgs e)
		{
			e.Cancel = installing;  //not allowed, nununu
		}

		enum PKillType
		{
			Killed,
			Aborted,
			NoneFound,
		}

		PKillType PromptKillProcess(string name)
		{
			foreach (var P in Process.GetProcessesByName(name))
			{
				if (!P.HasExited && MessageBox.Show(String.Format("Found running {0}.exe! Shall I terminate it?", name), "Warning", MessageBoxButtons.YesNo) != DialogResult.Yes)
					return PKillType.Aborted;
				if (!P.HasExited)
				{
					P.Kill();
					P.WaitForExit();
				}
				P.Dispose();
				return PKillType.Killed;
			}
			return PKillType.NoneFound;
		}

		async void DoInstall()
		{
			string path = null;
			string logfile = null;
			try
			{
				while (true)
				{
					var res = PromptKillProcess("TGCommandLine");
					if (res == PKillType.Aborted)
						return;
					else if (res == PKillType.Killed)
						continue;
					res = PromptKillProcess("TGControlPanel");
					if (res == PKillType.Aborted)
						return;
					else if (res == PKillType.Killed)
						continue;
					break;
				}
				
				var args = new List<string>();
				if (!pathIsDefault)
					args.Add(String.Format("INSTALLFOLDER=\"{0}\"", PathTextBox.Text));
				if (StartShortcutsCheckbox.Checked)
					args.Add("INSTALLSHORTCUTSTART=1");
				if (DeskShortcutsCheckbox.Checked)
					args.Add("INSTALLSHORTCUTDESK=1");

				args.Add("REBOOT=R");

				SelectPathButton.Enabled = false;
				PathTextBox.Enabled = false;
				DeskShortcutsCheckbox.Enabled = false;
				StartShortcutsCheckbox.Enabled = false;
				InstallButton.Enabled = false;
				ShowLogCheckbox.Enabled = false;
				InstallButton.Text = "Installing...";

				path = Path.GetTempFileName();
				File.Delete(path);  //we want a dir not a file
				Directory.CreateDirectory(path);
				var msipath = path + Path.DirectorySeparatorChar + "TGServiceInstaller.msi";
				File.WriteAllBytes(msipath, Properties.Resources.TGServiceInstaller);
				File.WriteAllBytes(path + Path.DirectorySeparatorChar + "cab1.cab", Properties.Resources.cab1);

				ProgressBar.Style = ProgressBarStyle.Marquee;

				if (Server.VerifyConnection() == null)
					try
					{
						Server.GetComponent<ITGSService>().PrepareForUpdate();
                        Thread.Sleep(3000); //chat messages
					}
					catch
					{
						if (MessageBox.Show("ITGSService.PrepareForUpdate() threw an exception! Existing DreamDaemon instances will be terminated. Continue?", "Warning", MessageBoxButtons.YesNo) != DialogResult.Yes)
							return;
					}

				InstallCancelButton.Enabled = true;

				if (ShowLogCheckbox.Checked)
				{
					logfile = path + Path.DirectorySeparatorChar + "tgsinstall.log";
					Installer.EnableLog(InstallLogModes.Verbose | InstallLogModes.PropertyDump, logfile);
				}
				var cl = String.Join(" ", args);
				Installer.SetInternalUI(InstallUIOptions.Silent);
				Installer.SetExternalUI(OnUIUpdate, InstallLogModes.Progress);

				await Task.Factory.StartNew(() => Installer.InstallProduct(msipath, cl));

				if (cancelled)
				{
					MessageBox.Show("Operation cancelled!");
					return;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error: " + ex.ToString());
				return;
			}
			finally
			{
				InstallCancelButton.Enabled = false;
				ShowLogCheckbox.Enabled = true;
				SelectPathButton.Enabled = true;
				PathTextBox.Enabled = true;
				DeskShortcutsCheckbox.Enabled = true;
				StartShortcutsCheckbox.Enabled = true;
				ProgressBar.Style = ProgressBarStyle.Blocks;
				InstallButton.Enabled = true;
				InstallButton.Text = "Install";
				installing = false;
				cancelled = false;
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

		MessageResult OnUIUpdate(InstallMessage messageType, string message, MessageButtons buttons, MessageIcon icon, MessageDefaultButton defaultButton)
		{
			if (cancelled && installing)
			{
				installing = false;
				return MessageResult.Cancel;
			}
			return MessageResult.OK;
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

		private void CancelButton_Click(object sender, EventArgs e)
		{
			cancelled = true;
			InstallCancelButton.Enabled = false;
		}
	}
}
