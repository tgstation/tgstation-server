using Microsoft.Deployment.WindowsInstaller;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TGS.Interface;
using TGS.Interface.Components;

namespace TGS.Installer.UI
{
	partial class Main : Form
	{
		const string DefaultInstallDir = "TG Station Server";  //keep this in sync with the msi installer

		string tempDir;
		bool installing = false;
		bool cancelled = false;
		bool pathIsDefault = true;

		IServerInterface Interface;

		/// <summary>
		/// Construct an installer form
		/// </summary>
		public Main()
		{
			InitializeComponent();
			SetupTempDir();
			CheckForExistingVersion();
			PathTextBox.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), DefaultInstallDir);
		}

		void SetupTempDir()
		{
			tempDir = Path.Combine(Path.GetTempPath(), "TGS3InstallerTempDir");
			try
			{
				if (File.Exists(tempDir))
					File.Delete(tempDir);
				else if (Directory.Exists(tempDir))
					Directory.Delete(tempDir, true);
			}
			catch { }
			if (File.Exists(tempDir) || Directory.Exists(tempDir))
			{
				tempDir = Path.GetTempFileName();
				File.Delete(tempDir);  //we want a dir not a file
			}
			try
			{
				Directory.CreateDirectory(tempDir);
			}
			catch
			{
				tempDir = null;
			}
		}

		void CleanTempDir() {
			if(tempDir != null)
				try
				{
					Directory.Delete(tempDir, true);
				}
				catch { }
		}

		void CheckForExistingVersion() {
			Interface = new ServerInterface();
			var verifiedConnection = Interface.ConnectionStatus().HasFlag(ConnectivityLevel.Administrator);
			try
			{
				VersionLabel.Text = Interface.GetServiceComponent<ITGSService>().Version();
				var isV0 = VersionLabel.Text.Contains("v3.0");
				if (isV0) //OH GOD!!!!
					MessageBox.Show("Upgrading from version 3.0 may trigger a bug that can delete /config and /data. IT IS STRONGLY RECCOMMENDED THAT YOU BACKUP THESE FOLDERS BEFORE UPDATING!", "Warning");
				if (isV0 || VersionLabel.Text.Contains("v3.1"))
					//Friendly reminger
					MessageBox.Show("Upgrading to service version 3.2 will break the 3.1 DMAPI. It is recommended you update your game to the 3.2 API before updating the servive to avoid having to trigger hard restarts.", "Note");
			}
			catch
			{
				if (verifiedConnection)
					VersionLabel.Text = "< v3.0.85.0 (Missing ITGService.Version())";
			}
		}

		bool ConfirmDangerousUpgrade()
		{
			return MessageBox.Show("Unable connect to service! Existing DreamDaemon instances will be terminated. Continue?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes;
		}

		bool TellServiceWereComingForThem()
		{
			var connectionVerified = Interface.ConnectionStatus().HasFlag(ConnectivityLevel.Administrator);
			try
			{
				Interface.GetServiceComponent<ITGSService>().PrepareForUpdate();
				Thread.Sleep(3000); //chat messages
				return true;
			}
			catch
			{
				return ConfirmDangerousUpgrade();
			}
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

		PKillType PromptKillProcesses(string name)
		{
			var processes = new List<Process>(Process.GetProcessesByName(name));
			processes.RemoveAll(x => x.HasExited);
			if(processes.Count > 0)
			{
				if (MessageBox.Show(String.Format("Found {1} running instance{2} of {0}.exe! Shall I terminate {3}?", name, processes.Count, processes.Count > 1 ? "s" : "", processes.Count > 1 ? "them" : "it"), "Warning", MessageBoxButtons.YesNo) != DialogResult.Yes)
					return PKillType.Aborted;
				foreach (var P in processes)
				{
					if (!P.HasExited)
					{
						P.Kill();
						P.WaitForExit();
					}
					P.Dispose();
				}
				return PKillType.Killed;
			}
			return PKillType.NoneFound;
		}

		async void DoInstall()
		{
			string logfile = null;
			try
			{
				while (true)
				{
					var res = PromptKillProcesses("TGS.CommandLine");
					if (res == PKillType.Aborted)
						return;
					else if (res == PKillType.Killed)
						continue;
					res = PromptKillProcesses("TGS.ControlPanel");
					if (res == PKillType.Aborted)
						return;
					else if (res == PKillType.Killed)
						continue;
					break;
				}
				
				if (!TellServiceWereComingForThem())
					return;

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

				var msipath = Path.Combine(tempDir, "TGS.Installer.msi");
				File.WriteAllBytes(msipath, Properties.Resources.TGSInstaller);
				File.WriteAllBytes(Path.Combine(tempDir, "cab1.cab"), Properties.Resources.cab1);

				ProgressBar.Style = ProgressBarStyle.Marquee;
				InstallCancelButton.Enabled = true;

				if (ShowLogCheckbox.Checked)
				{
					logfile = Path.Combine(tempDir, "tgsinstall.log");
					Microsoft.Deployment.WindowsInstaller.Installer.EnableLog(InstallLogModes.Verbose | InstallLogModes.PropertyDump, logfile);
				}
				var cl = String.Join(" ", args);
				Microsoft.Deployment.WindowsInstaller.Installer.SetInternalUI(InstallUIOptions.Silent);
				Microsoft.Deployment.WindowsInstaller.Installer.SetExternalUI(OnUIUpdate, InstallLogModes.Progress);

				await Task.Factory.StartNew(() => Microsoft.Deployment.WindowsInstaller.Installer.InstallProduct(msipath, cl));

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
			PathTextBox.Text = fbd.SelectedPath + Path.DirectorySeparatorChar + DefaultInstallDir;
		}

		private void CancelButton_Click(object sender, EventArgs e)
		{
			cancelled = true;
			InstallCancelButton.Enabled = false;
		}
	}
}
