using Microsoft.Deployment.WindowsInstaller;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TGS.Interface;
using TGS.Interface.Components;
using TGS.Server;

namespace TGS.Installer.UI
{
	partial class Main : Form
	{
		const string DefaultInstallDir = "TG Station Server";  //keep this in sync with the msi installer

		string tempDir;
		bool installing = false;
		bool cancelled = false;
		bool pathIsDefault = true;
		/// <summary>
		/// If we should attempt to make a <see cref="ServerConfig"/> for the new install
		/// </summary>
		bool attemptNetSettingsMigration = false;

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
				var realVersion = Interface.ServerVersion;
				var isV0 = realVersion < new Version(3, 1, 0, 0);
				if (isV0) //OH GOD!!!!
					MessageBox.Show("Upgrading from version 3.0 may trigger a bug that can delete /config and /data. IT IS STRONGLY RECCOMMENDED THAT YOU BACKUP THESE FOLDERS BEFORE UPDATING!", "Warning");
				var isUnderV2 = isV0 || realVersion < new Version(3, 2, 0, 0);
				if (isUnderV2)
					//Friendly reminger
					MessageBox.Show("Upgrading to service version 3.2 will break the 3.1 DMAPI. It is recommended you update your game to the 3.2 API before updating the servive to avoid having to trigger hard restarts.", "Note");
				attemptNetSettingsMigration = isUnderV2;
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
		static string TextPrompt(string caption, string text)
		{
			Form prompt = new Form()
			{
				Width = 500,
				Height = 150,
				FormBorderStyle = FormBorderStyle.FixedDialog,
				Text = caption,
				StartPosition = FormStartPosition.CenterScreen,
				MaximizeBox = false,
				MinimizeBox = false,
			};
			Label textLabel = new Label() { Left = 50, Top = 20, Text = text, AutoSize = true };
			TextBox textBox = new TextBox() { Left = 50, Top = 50, Width = 400 };
			Button confirmation = new Button() { Text = "Ok", Left = 350, Width = 100, Top = 70, DialogResult = DialogResult.OK };
			confirmation.Click += (sender, e) => { prompt.Close(); };
			prompt.Controls.Add(textBox);
			prompt.Controls.Add(confirmation);
			prompt.Controls.Add(textLabel);
			prompt.AcceptButton = confirmation;

			return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : null;
		}

		bool HandleNoConfigMigration(ServerConfig sc)
		{
			var path = @"C:\TGSTATION-SERVER-3";
			if (Directory.Exists(path)) {
				sc.InstancePaths.Add(path);
				new InstanceConfig(path).Save();
				return MessageBox.Show(String.Format("Unable to migrate settings, default config created at {0}. You will need to reconfigure your server instance. Continue with installation?", path), "Migration Error", MessageBoxButtons.YesNo) == DialogResult.Yes;
			}
			return MessageBox.Show("All config migrations have failed. You will need to fully recreate your server. Continue with installation?", "Migration Failure", MessageBoxButtons.YesNo) == DialogResult.Yes;
		}

		/// <summary>
		/// Migrate to the new <see cref="ServerConfig"/> since the <see cref="Server.Server"/> won't know about it until it's upgraded
		/// </summary>
		bool AttemptMigrationOfNetSettings()
		{
			if (!attemptNetSettingsMigration)
				return true;
			var sc = new ServerConfig();
			try
			{
				sc.PythonPath = Interface.GetServiceComponent<ITGSService>().PythonPath();
			}
			catch { }
			try
			{
				sc.RemoteAccessPort = Interface.GetServiceComponent<ITGSService>().RemoteAccessPort();
			}
			catch { }
			try
			{
				foreach (var I in Interface.GetServiceComponent<ITGLanding>().ListInstances())
					sc.InstancePaths.Add(I.Path);
			}
			catch { }

			try
			{
				if (sc.InstancePaths.Count > 0) //nice suprise, this is uneeded
					return true;

				//stopping the service here is MANDATORY to get the correct reattach values
				using (var controller = new ServiceController("TG Station Server"))
				{
					controller.Stop();
					controller.WaitForStatus(ServiceControllerStatus.Stopped);
					//we need to find the old user.config
					//check wow64 first
					var path = @"C:\Windows\SysWOW64\config\systemprofile\AppData\Local\TGServerService";
					if (!Directory.Exists(path))
					{
						//ok... check the System32 path
						path = @"C:\Windows\System32\config\systemprofile\AppData\Local\TGServerService";
						if (!Directory.Exists(path))
						{
							//well, i'm out of ideas, just use the default location
							var res = HandleNoConfigMigration(sc);
							if(!res)
							{
								controller.Start();
								return false;
							}
						}
					}

					//now who knows wtf windows calls the damn folder
					//take our best guess based on last modified time
					DirectoryInfo lastModified = null;
					foreach (var D in new DirectoryInfo(path).GetDirectories())
						if (lastModified == null || D.LastWriteTime > lastModified.LastWriteTime)
							lastModified = D;

					if (lastModified == null)
					{
						//well, i'm out of ideas, just use the default location
						var res = HandleNoConfigMigration(sc);
						if (!res)
						{
							controller.Start();
							return false;
						}
					}

					var next = lastModified;
					lastModified = null;
					foreach (var D in next.GetDirectories())
						if (lastModified == null || D.LastWriteTime > lastModified.LastWriteTime)
							lastModified = D;

					if (lastModified == null)
					{
						//well, i'm out of ideas, just use the default location
						var res = HandleNoConfigMigration(sc);
						if (!res)
						{
							controller.Start();
							return false;
						}
					}

					path = Path.Combine(tempDir, "user.config");
					var netConfigPath = Path.Combine(lastModified.FullName, "user.config");
					File.Copy(netConfigPath, path, true);
					new InstanceConfig(tempDir).Save();

					var instanceConfigPath = Path.Combine(tempDir, "Instance.json");

					if (MessageBox.Show(String.Format("The 3.2 settings migration is a manual process, please open \"{0}\" with your favorite text editor and copy the values under TGServerService.Properties.Settings to the relevent fields at \"{1}\". If something in the original config appears wrong to you, correct it in the new config, but do not modify the \"Version\", \"Enabled\", or \"Name\" fields at all. See field mappings here: https://github.com/tgstation/tgstation-server/blob/a372b22fd3367dd60ee0cbebd9210f4b072c952d/TGServerService/DeprecatedInstanceConfig.cs#L23-L39", path, instanceConfigPath), "Manual Migration Required", MessageBoxButtons.OKCancel) != DialogResult.OK)
					{
						controller.Start();
						return false;
					}

					var name = TextPrompt("Set Instance Directory", String.Format("Please enter the ServerDirectory entry from the original config here.{0}Use backslashes and uppercase letters. Leave this blank if it is not present.", Environment.NewLine));
					if (name == null)
					{
						controller.Start();
						return false;
					}

					if (String.IsNullOrWhiteSpace(name))
						name = @"C:\TGSTATION-SERVER-3";

					sc.InstancePaths.Add(name);

					if (MessageBox.Show(String.Format("Please confirm you have finished copying settings from \"{0}\" to \"{1}\"!", path, instanceConfigPath), "Last Confirmation", MessageBoxButtons.OKCancel) != DialogResult.OK)
					{
						controller.Start();
						return false;
					}
					//validate it for good measure
					try
					{
						InstanceConfig.Load(tempDir);
					}
					catch (Exception e)
					{
						MessageBox.Show(String.Format("JSON Validation Error: {0}", e.Message));
						controller.Start();
						return false;
					}
					File.Copy(instanceConfigPath, Path.Combine(name, "Instance.json"), true);
					return true;
				}
			}
			finally
			{
				Directory.CreateDirectory(Server.Server.MigrationConfigDirectory);
				sc.Save(Server.Server.MigrationConfigDirectory);
			}
		}

		async void DoInstall()
		{
			string logfile = null;
			try
			{
				while (true)
				{
					var res = PromptKillProcesses("TGCommandLine");
					if (res == PKillType.Aborted)
						return;
					else if (res == PKillType.Killed)
						continue;
					res = PromptKillProcesses("TGControlPanel");
					if (res == PKillType.Aborted)
						return;
					else if (res == PKillType.Killed)
						continue;
					break;
				}

				if (!TellServiceWereComingForThem())
					return;

				if (!AttemptMigrationOfNetSettings())
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

				// TODO: Uncomment this when OnUIUpdate is more robust
				// Microsoft.Deployment.WindowsInstaller.Installer.SetInternalUI(InstallUIOptions.Silent);
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
