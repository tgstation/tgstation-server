﻿using System;
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
		int testmergePR;
		string updateError;
		bool updatingFields = false;

		string DDStatusString = null;
		void InitServerPage()
		{
			LoadServerPage();
			ServerTimer.Start();
			WorldStatusChecker.RunWorkerAsync();
			WorldStatusChecker.RunWorkerCompleted += WorldStatusChecker_RunWorkerCompleted;
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
			ServerTimer.Start();
		}

		private void WorldStatusChecker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (DDStatusString != "Topic recieve error!" || ServerStatusLabel.Text == "OFFLINE")
				ServerStatusLabel.Text = DDStatusString;
			WorldStatusTimer.Start();
		}

		private void CompileCancelButton_Click(object sender, EventArgs e)
		{
			var res = Service.GetComponent<ITGCompiler>(Program.Instance).Cancel();
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
			var Config = Service.GetComponent<ITGInstance>(Program.Instance);
			if (updatingFields || ServerPathTextbox.Text.Trim() == Config.InstanceDirectory())
				return;
			var DialogResult = MessageBox.Show("This will move the entire server installation.", "Confim", MessageBoxButtons.YesNo);
			if (DialogResult != DialogResult.Yes)
				return;
			MessageBox.Show(Config.MoveInstance(ServerPathTextbox.Text) ?? "Success!");
		}

		void LoadServerPage()
		{
			var RepoExists = Service.GetComponent<ITGRepository>(Program.Instance).Exists();
			compileButton.Visible = RepoExists;
			initializeButton.Visible = RepoExists;
			NudgePortSelector.Visible = RepoExists;
			AutostartCheckbox.Visible = RepoExists;
			PortSelector.Visible = RepoExists;
			projectNameText.Visible = RepoExists;
			compilerProgressBar.Visible = RepoExists;
			CompilerStatusLabel.Visible = RepoExists;
			CompileCancelButton.Visible = RepoExists;
			NudgePortLabel.Visible = RepoExists;
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

			var DM = Service.GetComponent<ITGCompiler>(Program.Instance);
			var DD = Service.GetComponent<ITGDreamDaemon>(Program.Instance);
			var Config = Service.GetComponent<ITGConfig>(Program.Instance);
			var Inst = Service.GetComponent<ITGInstance>(Program.Instance);

			if (updatingFields)
				return;

			updatingFields = true;

			if (!ServerPathTextbox.Focused)
				ServerPathTextbox.Text = Inst.InstanceDirectory();

			VisibilitySelector.SelectedIndex = (int)DD.VisibilityLevel();
			SecuritySelector.SelectedIndex = (int)DD.SecurityLevel();

			if (!RepoExists)
			{
				updatingFields = false;
				return;
			}

			var Online = DD.DaemonStatus() == TGDreamDaemonStatus.Online;
			ServerGStopButton.Enabled = Online;
			ServerGRestartButton.Enabled = Online;

			var ShuttingDown = DD.ShutdownInProgress();
			ServerGStopButton.Checked = ShuttingDown;
			ServerGStopButton.Enabled = !ShuttingDown;

			AutostartCheckbox.Checked = DD.Autostart();
			if (!PortSelector.Focused)
				PortSelector.Value = DD.Port();
			if (!projectNameText.Focused)
				projectNameText.Text = DM.ProjectName();

			var val = Config.InteropPort(out string error);
			if (error != null)
			{
				updatingFields = false;
				NudgePortSelector.Value = 4567;
				MessageBox.Show("Error (I will try and recover): " + error);
			}
			else
			{
				updatingFields = false;
				NudgePortSelector.Value = val;
			}

			switch (DM.GetStatus())
			{
				case TGCompilerStatus.Compiling:
					CompilerStatusLabel.Text = "Compiling...";
					compilerProgressBar.Style = ProgressBarStyle.Marquee;
					compileButton.Enabled = false;
					initializeButton.Enabled = false;
					CompileCancelButton.Enabled = true;
					break;
				case TGCompilerStatus.Initializing:
					CompilerStatusLabel.Text = "Initializing...";
					compilerProgressBar.Style = ProgressBarStyle.Marquee;
					compileButton.Enabled = false;
					initializeButton.Enabled = false;
					CompileCancelButton.Enabled = false;
					break;
				case TGCompilerStatus.Initialized:
					CompilerStatusLabel.Text = "Idle";
					compilerProgressBar.Style = ProgressBarStyle.Blocks;
					initializeButton.Enabled = true;
					compileButton.Enabled = true;
					CompileCancelButton.Enabled = false;
					break;
				case TGCompilerStatus.Uninitialized:
					CompilerStatusLabel.Text = "Uninitialized";
					compilerProgressBar.Style = ProgressBarStyle.Blocks;
					compileButton.Enabled = false;
					initializeButton.Enabled = true;
					CompileCancelButton.Enabled = false;
					break;
				default:
					CompilerStatusLabel.Text = "Unknown!";
					compilerProgressBar.Style = ProgressBarStyle.Blocks;
					initializeButton.Enabled = true;
					compileButton.Enabled = true;
					CompileCancelButton.Enabled = true;
					break;
			}
			error = DM.CompileError();
			if (error != null)
				MessageBox.Show("Error: " + error);
		}

		private void ServerTimer_Tick(object sender, System.EventArgs e)
		{
			try
			{
				LoadServerPage();
			}
			catch (Exception ex)
			{
				ServerTimer.Stop();
				Program.ServiceDisconnectException(ex);
			}
		}
		private void ProjectNameText_LostFocus(object sender, EventArgs e)
		{
			UpdateProjectName();
		}

		void UpdateProjectName()
		{
			if (!updatingFields)
				Service.GetComponent<ITGCompiler>(Program.Instance).SetProjectName(projectNameText.Text);
		}

		private void PortSelector_ValueChanged(object sender, EventArgs e)
		{
			if (!updatingFields)
				Service.GetComponent<ITGDreamDaemon>(Program.Instance).SetPort((ushort)PortSelector.Value);
		}

		private void RunServerUpdate(FullUpdateAction fua, int tm = 0)
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
			compilerProgressBar.Style = ProgressBarStyle.Marquee;
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
			ServerTimer.Stop();
			FullUpdateWorker.RunWorkerAsync();
		}

		private void InitializeButton_Click(object sender, EventArgs e)
		{
			if (!Service.GetComponent<ITGCompiler>(Program.Instance).Initialize())
				MessageBox.Show("Unable to start initialization!");
			LoadServerPage();
		}
		private void CompileButton_Click(object sender, EventArgs e)
		{
			if (!Service.GetComponent<ITGCompiler>(Program.Instance).Compile())
				MessageBox.Show("Unable to start compilation!");
			LoadServerPage();
		}
		//because of lol byond this can take some time...
		private void WorldStatusChecker_DoWork(object sender, DoWorkEventArgs e)
		{
			try
			{
				if (!Service.GetComponent<ITGRepository>(Program.Instance).Exists())
					DDStatusString = "NOT INSTALLED";
				else
					DDStatusString = Service.GetComponent<ITGDreamDaemon>(Program.Instance).StatusString(true);
			}
			catch
			{
				DDStatusString = "ERROR";
			}
		}

		private void WorldStatusTimer_Tick(object sender, System.EventArgs e)
		{
			WorldStatusTimer.Stop();
			WorldStatusChecker.RunWorkerAsync();
		}

		private void AutostartCheckbox_CheckedChanged(object sender, System.EventArgs e)
		{
			if (!updatingFields)
				Service.GetComponent<ITGDreamDaemon>(Program.Instance).SetAutostart(AutostartCheckbox.Checked);
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
				e.Result = Service.GetComponent<ITGDreamDaemon>(Program.Instance).Start();
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
			var res = Service.GetComponent<ITGDreamDaemon>(Program.Instance).Stop();
			if (res != null)
				MessageBox.Show(res);
		}

		private void ServerRestartButton_Click(object sender, EventArgs e)
		{
			var DialogResult = MessageBox.Show("This will immediately restart the server. Continue?", "Confim", MessageBoxButtons.YesNo);
			if (DialogResult == DialogResult.No)
				return;
			var res = Service.GetComponent<ITGDreamDaemon>(Program.Instance).Restart();
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
			Service.GetComponent<ITGDreamDaemon>(Program.Instance).RequestStop();
			LoadServerPage();
		}

		private void ServerGRestartButton_Click(object sender, EventArgs e)
		{
			var DialogResult = MessageBox.Show("This will restart the server when the current round ends. Continue?", "Confim", MessageBoxButtons.YesNo);
			if (DialogResult == DialogResult.No)
				return;
			Service.GetComponent<ITGDreamDaemon>(Program.Instance).RequestRestart();
		}
		private void FullUpdateWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			var Updater = Service.GetComponent<ITGInstance>(Program.Instance);
			switch (fuAction)
			{
				case FullUpdateAction.Testmerge:
					updateError = Updater.UpdateServer(TGRepoUpdateMethod.None, false, (ushort)testmergePR);
					break;
				case FullUpdateAction.UpdateHard:
					updateError = Updater.UpdateServer(TGRepoUpdateMethod.Hard, true);
					break;
				case FullUpdateAction.UpdateHardTestmerge:
					updateError = Updater.UpdateServer(TGRepoUpdateMethod.Hard, true, (ushort)testmergePR);
					break;
				case FullUpdateAction.UpdateMerge:
					updateError = Updater.UpdateServer(TGRepoUpdateMethod.Merge, true, (ushort)testmergePR);
					break;
				case FullUpdateAction.Reset:
					updateError = Updater.UpdateServer(TGRepoUpdateMethod.Reset, false, 0);
					break;
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
			RunServerUpdate(FullUpdateAction.UpdateHardTestmerge, (int)ServerTestmergeInput.Value);
		}

		private void UpdateMergeButton_Click(object sender, System.EventArgs e)
		{
			RunServerUpdate(FullUpdateAction.UpdateMerge);
		}
		private void TestmergeButton_Click(object sender, System.EventArgs e)
		{
			RunServerUpdate(FullUpdateAction.Testmerge, (int)ServerTestmergeInput.Value);
		}

		private void NudgePortSelector_ValueChanged(object sender, EventArgs e)
		{
			if (!updatingFields)
				Service.GetComponent<ITGConfig>(Program.Instance).SetInteropPort((ushort)NudgePortSelector.Value);
		}

		private void SecuritySelector_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!updatingFields)
				if (!Service.GetComponent<ITGDreamDaemon>(Program.Instance).SetSecurityLevel((TGDreamDaemonSecurity)SecuritySelector.SelectedIndex))
					MessageBox.Show("Security change will be applied after next server reboot.");
		}

		private void VisibilitySelector_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!updatingFields)
				if (!Service.GetComponent<ITGDreamDaemon>(Program.Instance).SetVisibility((TGDreamDaemonVisibility)VisibilitySelector.SelectedIndex))
					MessageBox.Show("Visibility change will be applied after next server reboot.");
		}
	}
}
