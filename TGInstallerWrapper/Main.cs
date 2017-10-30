using Microsoft.Deployment.WindowsInstaller;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TGInstallerWrapper
{
	partial class Main : Form
	{
		const string DefaultInstallDir = "TG Station Server";  //keep this in sync with the msi installer

		//reflection shit, make sure it matches
		const string InterfaceDLL = "TGServiceInterface.dll";
		const string InterfaceNamespace = "TGServiceInterface";
		const string InterfaceComponentsNamespace = InterfaceNamespace + ".Components";
		const string InterfaceClass = InterfaceNamespace + ".Server";
		const string InterfaceServiceInterface = InterfaceComponentsNamespace + ".ITGSService";   //fuck this typo
		const string InterfaceClassVerifyConnection = "VerifyConnection";
		const string InterfaceClassGetComponent = "GetComponent";
		const string InterfaceServiceInterfaceVersion = "Version";
		const string InterfaceServiceInterfacePrepareForUpdate = "PrepareForUpdate";

		Assembly InterfaceAssembly;
		Type Server, ITGSService;
		MethodInfo VerifyConnection, GetComponentITGSService, Version, PrepareForUpdate;

		string tempDir;
		bool installing = false;
		bool cancelled = false;
		bool pathIsDefault = true;

		/// <summary>
		/// Construct an installer form
		/// </summary>
		public Main()
		{
			InitializeComponent();
			SetupTempDir();
			LoadInterfaceFromReflection();
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
					Directory.Delete(tempDir);
			}
			catch { }
			if (File.Exists(tempDir) || Directory.Exists(tempDir))
			{
				tempDir = Path.GetTempFileName();
				File.Delete(tempDir);  //we want a dir not a file
			}
			Directory.CreateDirectory(tempDir);
		}

		void LoadInterfaceFromReflection()
		{
			//so this is where we expect to find the interface dll
			try
			{
				var tmppath = Path.Combine(tempDir, InterfaceDLL);
				File.WriteAllBytes(tmppath, Properties.Resources.TGServiceInterface);
				InterfaceAssembly = Assembly.LoadFrom(tmppath);	//we can't link to it, or load the bytes directly because the thing will complain about mixing the DLLExport code and IL code
				Server = InterfaceAssembly.GetType(InterfaceClass);
				ITGSService = InterfaceAssembly.GetType(InterfaceServiceInterface);
				VerifyConnection = Server.GetMethod(InterfaceClassVerifyConnection);
				GetComponentITGSService = Server.GetMethod(InterfaceClassGetComponent).MakeGenericMethod(ITGSService);
				Version = ITGSService.GetMethod(InterfaceServiceInterfaceVersion);
				PrepareForUpdate = ITGSService.GetMethod(InterfaceServiceInterfacePrepareForUpdate);
			}
			catch (Exception e)
			{
				InterfaceAssembly = null;
				VersionLabel.Text = "Error: (Could not load interface dll)";
				MessageBox.Show(String.Format("An error occurred while loading {0} (This is an easily preventable bug, please report it)! Error: {1}", InterfaceDLL, e.ToString()));
			}
		}

		void CheckForExistingVersion() {
			if (InterfaceAssembly == null)
				return;
			var verifiedConnection = VerifyConnection.Invoke(null, null) == null;
			try
			{
				VersionLabel.Text = (string)Version.Invoke(GetComponentITGSService.Invoke(null, null), null);
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
			if (InterfaceAssembly == null)
				return ConfirmDangerousUpgrade();
			var connectionVerified = VerifyConnection.Invoke(null, null) == null;
			try
			{
				PrepareForUpdate.Invoke(GetComponentITGSService.Invoke(null, null), null);
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

				var msipath = Path.Combine(tempDir, "TGServiceInstaller.msi");
				File.WriteAllBytes(msipath, Properties.Resources.TGServiceInstaller);
				File.WriteAllBytes(Path.Combine(tempDir, "cab1.cab"), Properties.Resources.cab1);

				ProgressBar.Style = ProgressBarStyle.Marquee;
				InstallCancelButton.Enabled = true;

				if (ShowLogCheckbox.Checked)
				{
					logfile = Path.Combine(tempDir, "tgsinstall.log");
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
