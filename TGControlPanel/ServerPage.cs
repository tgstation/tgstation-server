﻿﻿using System;
using System.ComponentModel;
using System.Windows.Forms;
using TGServiceInterface;

namespace TGControlPanel
{
	partial class Main
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
			if (!Server.AuthenticateAdmin())
			{
				ServerPathTextbox.Enabled = false;
				ServerPathTextbox.ReadOnly = true;
			}
			FullUpdateWorker.RunWorkerCompleted += FullUpdateWorker_RunWorkerCompleted;
			ServerPathTextbox.LostFocus += ServerPathTextbox_LostFocus;
			ServerPathTextbox.KeyDown += ServerPathTextbox_KeyDown;
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

		private void ServerPathTextbox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
				UpdateServerPath();
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
			var res = Server.GetComponent<ITGCompiler>().Cancel();
			if (res != null)
				MessageBox.Show(res);
			LoadServerPage();
		}

		private void ServerPathTextbox_LostFocus(object sender, EventArgs e)
		{
			UpdateServerPath();
		}

		void UpdateServerPath()
		{
			if (!Program.CheckAdminWithWarning())
			{
				ServerPathTextbox.Enabled = false;
				ServerPathTextbox.ReadOnly = true;
				return;
			}
			if (updatingFields || ServerPathTextbox.Text.Trim() == Server.GetComponent<ITGConfig>().ServerDirectory())
				return;
			var DialogResult = MessageBox.Show("This will move the entire server installation.", "Confim", MessageBoxButtons.YesNo);
			if (DialogResult != DialogResult.Yes)
				return;

			if (!Program.CheckAdminWithWarning())
			{
				ServerPathTextbox.Enabled = false;
				ServerPathTextbox.ReadOnly = true;
				return;
			}
			MessageBox.Show(Server.GetComponent<ITGAdministration>().MoveServer(ServerPathTextbox.Text) ?? "Success!");
		}

		void LoadServerPage()
		{
			var RepoExists = Server.GetComponent<ITGRepository>().Exists();
			compileButton.Visible = RepoExists;
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

			var DM = Server.GetComponent<ITGCompiler>();
			var DD = Server.GetComponent<ITGDreamDaemon>();
			var Config = Server.GetComponent<ITGConfig>();

			if (updatingFields)
				return;

			try
			{
				updatingFields = true;

				if (!ServerPathTextbox.Focused)
					ServerPathTextbox.Text = Config.ServerDirectory();

				SecuritySelector.SelectedIndex = (int)DD.SecurityLevel();

				if (!RepoExists)
					return;

				var DaeStat = DD.DaemonStatus();
				var Online = DaeStat == TGDreamDaemonStatus.Online;
				ServerStartButton.Enabled = !Online;
				ServerGStopButton.Enabled = Online;
				ServerGRestartButton.Enabled = Online;
				ServerStopButton.Enabled = Online;
				ServerRestartButton.Enabled = Online;

				switch (DaeStat)
				{
					case TGDreamDaemonStatus.HardRebooting:
						ServerStatusLabel.Text = "REBOOTING";
						break;
					case TGDreamDaemonStatus.Offline:
						ServerStatusLabel.Text = "OFFLINE";
						break;
					case TGDreamDaemonStatus.Online:
						ServerStatusLabel.Text = "ONLINE";
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
					case TGCompilerStatus.Compiling:
						CompilerStatusLabel.Text = "Compiling...";
						compileButton.Enabled = false;
						initializeButton.Enabled = false;
						CompileCancelButton.Enabled = true;
						break;
					case TGCompilerStatus.Initializing:
						CompilerStatusLabel.Text = "Initializing...";
						compileButton.Enabled = false;
						initializeButton.Enabled = false;
						CompileCancelButton.Enabled = false;
						break;
					case TGCompilerStatus.Initialized:
						CompilerStatusLabel.Text = "Idle";
						initializeButton.Enabled = true;
						compileButton.Enabled = true;
						CompileCancelButton.Enabled = false;
						break;
					case TGCompilerStatus.Uninitialized:
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
				Server.GetComponent<ITGCompiler>().SetProjectName(projectNameText.Text);
		}

		private void PortSelector_ValueChanged(object sender, EventArgs e)
		{
			if (!updatingFields)
				Server.GetComponent<ITGDreamDaemon>().SetPort((ushort)PortSelector.Value);
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
			if (!Server.GetComponent<ITGCompiler>().Initialize())
				MessageBox.Show("Unable to start initialization!");
			LoadServerPage();
		}
		private void CompileButton_Click(object sender, EventArgs e)
		{
			if (!Server.GetComponent<ITGCompiler>().Compile())
				MessageBox.Show("Unable to start compilation!");
			LoadServerPage();
		}

		private void AutostartCheckbox_CheckedChanged(object sender, System.EventArgs e)
		{
			if (!updatingFields)
				Server.GetComponent<ITGDreamDaemon>().SetAutostart(AutostartCheckbox.Checked);
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
				e.Result = Server.GetComponent<ITGDreamDaemon>().Start();
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
			var res = Server.GetComponent<ITGDreamDaemon>().Stop();
			if (res != null)
				MessageBox.Show(res);
		}

		private void ServerRestartButton_Click(object sender, EventArgs e)
		{
			var DialogResult = MessageBox.Show("This will immediately restart the server. Continue?", "Confim", MessageBoxButtons.YesNo);
			if (DialogResult == DialogResult.No)
				return;
			var res = Server.GetComponent<ITGDreamDaemon>().Restart();
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
			Server.GetComponent<ITGDreamDaemon>().RequestStop();
			LoadServerPage();
		}

		private void ServerGRestartButton_Click(object sender, EventArgs e)
		{
			var DialogResult = MessageBox.Show("This will restart the server when the current round ends. Continue?", "Confim", MessageBoxButtons.YesNo);
			if (DialogResult == DialogResult.No)
				return;
			Server.GetComponent<ITGDreamDaemon>().RequestRestart();
		}


		private void FullUpdateWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			try
			{
				var Repo = Server.GetComponent<ITGRepository>();
				var DM = Server.GetComponent<ITGCompiler>();
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
								updateError = Repo.PushChangelog();
							updateError = DM.Compile(true) ? updateError : "Compilation failed!";
						}
						break;
					case FullUpdateAction.UpdateHardTestmerge:
						updateError = Repo.Update(true);
						if (updateError == null)
						{
							Repo.GenerateChangelog(out updateError);
							if (updateError == null)
								updateError = Repo.PushChangelog();
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
								Repo.PushChangelog();   //not an error 99% of the time if this fails, just a dirty tree
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
				if (!Server.GetComponent<ITGDreamDaemon>().SetSecurityLevel((TGDreamDaemonSecurity)SecuritySelector.SelectedIndex))
					MessageBox.Show("Security change will be applied after next server reboot.");
		}

		private void WorldAnnounceButton_Click(object sender, EventArgs e)
		{
			var msg = WorldAnnounceField.Text;
			if (!String.IsNullOrWhiteSpace(msg))
			{
				var res = Server.GetComponent<ITGDreamDaemon>().WorldAnnounce(msg);
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
				Server.GetComponent<ITGDreamDaemon>().SetWebclient(WebclientCheckBox.Checked);
		}
	}
}
