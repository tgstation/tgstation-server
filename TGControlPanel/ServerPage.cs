﻿﻿using System;
using System.ComponentModel;
using System.Windows.Forms;
using TGServiceInterface;
using TGServiceInterface.Components;

namespace TGControlPanel
{
	partial class ControlPanel
	{
		enum FullUpdateAction
		{
			UpdateHard,
			UpdateMerge,
			UpdateHardTestmerge,
			Reset,
			Testmerge,
		}

		FullUpdateAction fuAction;
		ushort testmergePR;
		string updateError;
		bool updatingFields = false;

		void InitServerPage()
		{
			LoadServerPage();
			FullUpdateWorker.RunWorkerCompleted += FullUpdateWorker_RunWorkerCompleted;
			projectNameText.LostFocus += ProjectNameText_LostFocus;
			projectNameText.KeyDown += ProjectNameText_KeyDown;
			ServerStartBGW.RunWorkerCompleted += ServerStartBGW_RunWorkerCompleted;
		}

		private void ServerStartBGW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Result != null)
				MessageBox.Show((string)e.Result);
			LoadServerPage();
		}

		private void ProjectNameText_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
				UpdateProjectName();
		}

		private void FullUpdateWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (updateError != null)
				MessageBox.Show(updateError);
			UpdateHardButton.Enabled = true;
			UpdateMergeButton.Enabled = true;
			TestmergeButton.Enabled = true;
			UpdateTestmergeButton.Enabled = true;
			LoadServerPage();
		}

		private void CompileCancelButton_Click(object sender, EventArgs e)
		{
			var res = Interface.GetComponent<ITGCompiler>().Cancel();
			if (res != null)
				MessageBox.Show(res);
			LoadServerPage();
		}

		void LoadServerPage()
		{
			var RepoExists = Interface.GetComponent<ITGRepository>().Exists();
			compileButton.Visible = RepoExists;
			AutoUpdateCheckbox.Visible = RepoExists;
			initializeButton.Visible = RepoExists;
			AutostartCheckbox.Visible = RepoExists;
			WebclientCheckBox.Visible = RepoExists;
			PortSelector.Visible = RepoExists;
			projectNameText.Visible = RepoExists;
			CompilerStatusLabel.Visible = RepoExists;
			CompileCancelButton.Visible = RepoExists;
			CompilerLabel.Visible = RepoExists;
			ProjectPathLabel.Visible = RepoExists;
			ServerPRLabel.Visible = RepoExists;
			ServerGStopButton.Visible = RepoExists;
			ServerStartButton.Visible = RepoExists;
			ServerGRestartButton.Visible = RepoExists;
			ServerRestartButton.Visible = RepoExists;
			PortLabel.Visible = RepoExists;
			ServerStopButton.Visible = RepoExists;
			TestmergeButton.Visible = RepoExists;
			ServerTestmergeInput.Visible = RepoExists;
			UpdateHardButton.Visible = RepoExists;
			UpdateMergeButton.Visible = RepoExists;
			UpdateTestmergeButton.Visible = RepoExists;
			ResetTestmerge.Visible = RepoExists;
			WorldAnnounceField.Visible = RepoExists;
			WorldAnnounceButton.Visible = RepoExists;
			WorldAnnounceLabel.Visible = RepoExists;

			if (updatingFields)
				return;

			var DM = Interface.GetComponent<ITGCompiler>();
			var DD = Interface.GetComponent<ITGDreamDaemon>();
			var Config = Interface.GetComponent<ITGConfig>();
			var Repo = Interface.GetComponent<ITGRepository>();

			try
			{
				updatingFields = true;
				
				ServerPathLabel.Text = "Server Path: " + Config.ServerDirectory();

				SecuritySelector.SelectedIndex = (int)DD.SecurityLevel();

				if (!RepoExists)
					return;

				var interval = Repo.AutoUpdateInterval();
				var interval_not_zero = interval != 0;
				AutoUpdateCheckbox.Checked = interval_not_zero;
				AutoUpdateInterval.Visible = interval_not_zero;
				AutoUpdateMLabel.Visible = interval_not_zero;
				if (interval_not_zero)
					AutoUpdateInterval.Value = interval;

				var DaeStat = DD.DaemonStatus();
				var Online = DaeStat == DreamDaemonStatus.Online;
				ServerStartButton.Enabled = !Online;
				ServerGStopButton.Enabled = Online;
				ServerGRestartButton.Enabled = Online;
				ServerStopButton.Enabled = Online;
				ServerRestartButton.Enabled = Online;

				switch (DaeStat)
				{
					case DreamDaemonStatus.HardRebooting:
						ServerStatusLabel.Text = "REBOOTING";
						break;
					case DreamDaemonStatus.Offline:
						ServerStatusLabel.Text = "OFFLINE";
						break;
					case DreamDaemonStatus.Online:
						ServerStatusLabel.Text = "ONLINE";
						var pc = DD.PlayerCount();
						if (pc != -1)
							ServerStatusLabel.Text += " (" + pc + " players)";
						break;
				}
				
				ServerGStopButton.Checked = DD.ShutdownInProgress();

				AutostartCheckbox.Checked = DD.Autostart();
				WebclientCheckBox.Checked = DD.Webclient();
				if (!PortSelector.Focused)
					PortSelector.Value = DD.Port();
				if (!projectNameText.Focused)
					projectNameText.Text = DM.ProjectName();

				switch (DM.GetStatus())
				{
					case CompilerStatus.Compiling:
						CompilerStatusLabel.Text = "Compiling...";
						compileButton.Enabled = false;
						initializeButton.Enabled = false;
						CompileCancelButton.Enabled = true;
						break;
					case CompilerStatus.Initializing:
						CompilerStatusLabel.Text = "Initializing...";
						compileButton.Enabled = false;
						initializeButton.Enabled = false;
						CompileCancelButton.Enabled = false;
						break;
					case CompilerStatus.Initialized:
						CompilerStatusLabel.Text = "Idle";
						initializeButton.Enabled = true;
						compileButton.Enabled = true;
						CompileCancelButton.Enabled = false;
						break;
					case CompilerStatus.Uninitialized:
						CompilerStatusLabel.Text = "Uninitialized";
						compileButton.Enabled = false;
						initializeButton.Enabled = true;
						CompileCancelButton.Enabled = false;
						break;
					default:
						CompilerStatusLabel.Text = "Unknown!";
						initializeButton.Enabled = true;
						compileButton.Enabled = true;
						CompileCancelButton.Enabled = true;
						break;
				}
				var error = DM.CompileError();
				if (error != null)
					MessageBox.Show("Error: " + error);
			}
			finally
			{
				updatingFields = false;
			}
		}
		
		private void ProjectNameText_LostFocus(object sender, EventArgs e)
		{
			UpdateProjectName();
		}

		void UpdateProjectName()
		{
			if (!updatingFields)
				Interface.GetComponent<ITGCompiler>().SetProjectName(projectNameText.Text);
		}

		private void PortSelector_ValueChanged(object sender, EventArgs e)
		{
			if (!updatingFields)
				Interface.GetComponent<ITGDreamDaemon>().SetPort((ushort)PortSelector.Value);
		}

		private void RunServerUpdate(FullUpdateAction fua, ushort tm = 0)
		{
			if (FullUpdateWorker.IsBusy)
				return;
			testmergePR = tm;
			fuAction = fua;
			initializeButton.Enabled = false;
			compileButton.Enabled = false;
			UpdateHardButton.Enabled = false;
			UpdateMergeButton.Enabled = false;
			TestmergeButton.Enabled = false;
			UpdateTestmergeButton.Enabled = false;
			switch (fuAction)
			{
				case FullUpdateAction.Testmerge:
					CompilerStatusLabel.Text = String.Format("Testmerging pull request #{0}...", testmergePR);
					break;
				case FullUpdateAction.UpdateHard:
					CompilerStatusLabel.Text = String.Format("Updating Server (RESET)...");
					break;
				case FullUpdateAction.UpdateMerge:
					CompilerStatusLabel.Text = String.Format("Updating Server (MERGE)...");
					break;
				case FullUpdateAction.UpdateHardTestmerge:
					CompilerStatusLabel.Text = String.Format("Updating and testmerging pull request #{0}...", testmergePR);
					break;
			}
			FullUpdateWorker.RunWorkerAsync();
		}

		private void ServerPageRefreshButton_Click(object sender, EventArgs e)
		{
			LoadServerPage();
		}

		private void InitializeButton_Click(object sender, EventArgs e)
		{
			if (!Interface.GetComponent<ITGCompiler>().Initialize())
				MessageBox.Show("Unable to start initialization!");
			LoadServerPage();
		}
		private void CompileButton_Click(object sender, EventArgs e)
		{
			if (!Interface.GetComponent<ITGCompiler>().Compile())
				MessageBox.Show("Unable to start compilation!");
			LoadServerPage();
		}

		private void AutostartCheckbox_CheckedChanged(object sender, System.EventArgs e)
		{
			if (!updatingFields)
				Interface.GetComponent<ITGDreamDaemon>().SetAutostart(AutostartCheckbox.Checked);
		}
		private void ServerStartButton_Click(object sender, System.EventArgs e)
		{
			if (!ServerStartBGW.IsBusy)
				ServerStartBGW.RunWorkerAsync();
		}

		private void ServerStartBGW_DoWork(object sender, DoWorkEventArgs e)
		{
			try
			{
				e.Result = Interface.GetComponent<ITGDreamDaemon>().Start();
			}
			catch (Exception ex)
			{
				e.Result = ex.ToString();
			}
		}

		private void ServerStopButton_Click(object sender, EventArgs e)
		{
			var DialogResult = MessageBox.Show("This will immediately shut down the server. Continue?", "Confim", MessageBoxButtons.YesNo);
			if (DialogResult == DialogResult.No)
				return;
			var res = Interface.GetComponent<ITGDreamDaemon>().Stop();
			if (res != null)
				MessageBox.Show(res);
		}

		private void ServerRestartButton_Click(object sender, EventArgs e)
		{
			var DialogResult = MessageBox.Show("This will immediately restart the server. Continue?", "Confim", MessageBoxButtons.YesNo);
			if (DialogResult == DialogResult.No)
				return;
			var res = Interface.GetComponent<ITGDreamDaemon>().Restart();
			if (res != null)
				MessageBox.Show(res);
		}

		private void ServerGStopButton_Checked(object sender, EventArgs e)
		{
			if (updatingFields)
				return;
			var DialogResult = MessageBox.Show("This will shut down the server when the current round ends. Continue?", "Confim", MessageBoxButtons.YesNo);
			if (DialogResult == DialogResult.No)
				return;
			Interface.GetComponent<ITGDreamDaemon>().RequestStop();
			LoadServerPage();
		}

		private void ServerGRestartButton_Click(object sender, EventArgs e)
		{
			var DialogResult = MessageBox.Show("This will restart the server when the current round ends. Continue?", "Confim", MessageBoxButtons.YesNo);
			if (DialogResult == DialogResult.No)
				return;
			Interface.GetComponent<ITGDreamDaemon>().RequestRestart();
		}


		private void FullUpdateWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			try
			{
				var Repo = Interface.GetComponent<ITGRepository>();
				var DM = Interface.GetComponent<ITGCompiler>();
				switch (fuAction)
				{
					case FullUpdateAction.Testmerge:
						updateError = Repo.MergePullRequest(testmergePR);
						if (updateError == null)
						{
							Repo.GenerateChangelog(out updateError);
							updateError = DM.Compile(true) ? updateError : "Compilation failed!";
						}
						break;
					case FullUpdateAction.UpdateHard:
						updateError = Repo.Update(true);
						if (updateError == null)
						{
							Repo.GenerateChangelog(out updateError);
							if (updateError == null)
								updateError = Repo.SynchronizePush();
							updateError = DM.Compile(true) ? updateError : "Compilation failed!";
						}
						break;
					case FullUpdateAction.UpdateHardTestmerge:
						updateError = Repo.Update(true);
						if (updateError == null)
						{
							Repo.GenerateChangelog(out updateError);
							if (updateError == null)
								updateError = Repo.SynchronizePush();
							updateError = Repo.MergePullRequest(testmergePR);
							if (updateError == null)
							{
								Repo.GenerateChangelog(out updateError);
								updateError = DM.Compile(true) ? updateError : "Compilation failed!";
							}
						}
						break;
					case FullUpdateAction.UpdateMerge:
						updateError = Repo.Update(false);
						if (updateError == null)
						{
							Repo.GenerateChangelog(out updateError);
							if (updateError == null)
								Repo.SynchronizePush();   //not an error 99% of the time if this fails, just a dirty tree
							updateError = DM.Compile(true) ? updateError : "Compilation failed!";
						}
						break;
					case FullUpdateAction.Reset:
						updateError = Repo.Reset(true);
						if (updateError == null)
						{
							Repo.GenerateChangelog(out updateError);
							updateError = DM.Compile(true) ? updateError : "Compilation failed!";
						}
						break;
				}
			}
			catch (Exception ex)
			{
				Program.ServiceDisconnectException(ex);
			}
		}
		private void ResetTestmerge_Click(object sender, EventArgs e)
		{
			RunServerUpdate(FullUpdateAction.Reset);
		}

		private void UpdateHardButton_Click(object sender, System.EventArgs e)
		{
			RunServerUpdate(FullUpdateAction.UpdateHard);
		}

		private void UpdateTestmergeButton_Click(object sender, System.EventArgs e)
		{
			RunServerUpdate(FullUpdateAction.UpdateHardTestmerge, (ushort)ServerTestmergeInput.Value);
		}

		private void UpdateMergeButton_Click(object sender, System.EventArgs e)
		{
			RunServerUpdate(FullUpdateAction.UpdateMerge);
		}
		private void TestmergeButton_Click(object sender, System.EventArgs e)
		{
			RunServerUpdate(FullUpdateAction.Testmerge, (ushort)ServerTestmergeInput.Value);
		}

		private void SecuritySelector_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!updatingFields)
				if (!Interface.GetComponent<ITGDreamDaemon>().SetSecurityLevel((DreamDaemonSecurity)SecuritySelector.SelectedIndex))
					MessageBox.Show("Security change will be applied after next server reboot.");
		}

		private void WorldAnnounceButton_Click(object sender, EventArgs e)
		{
			var msg = WorldAnnounceField.Text;
			if (!String.IsNullOrWhiteSpace(msg))
			{
				var res = Interface.GetComponent<ITGDreamDaemon>().WorldAnnounce(msg);
				if (res != null)
				{
					MessageBox.Show(res);
					return;
				}
			}
			WorldAnnounceField.Text = "";
		}

		private void WebclientCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			if (!updatingFields)
				Interface.GetComponent<ITGDreamDaemon>().SetWebclient(WebclientCheckBox.Checked);
		}

		private void AutoUpdateInterval_ValueChanged(object sender, EventArgs e)
		{
			if (!updatingFields)
				Interface.GetComponent<ITGRepository>().SetAutoUpdateInterval((ulong)AutoUpdateInterval.Value);
		}

		private void AutoUpdateCheckbox_CheckedChanged(object sender, EventArgs e)
		{
			if (updatingFields)
				return;
			var on = AutoUpdateCheckbox.Visible && AutoUpdateCheckbox.Checked;
			AutoUpdateInterval.Visible = on;
			AutoUpdateMLabel.Visible = on;
			if (!on)
				Interface.GetComponent<ITGRepository>().SetAutoUpdateInterval(0);
			else
				Interface.GetComponent<ITGRepository>().SetAutoUpdateInterval((ulong)AutoUpdateInterval.Value);
		}
	}
}
