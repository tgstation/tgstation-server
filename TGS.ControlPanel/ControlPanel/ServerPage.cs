using Octokit;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using TGS.Interface;
using TGS.Interface.Components;

namespace TGS.ControlPanel
{
	partial class ControlPanel
	{
		bool updatingFields = false;

		/// <summary>
		/// <see cref="GitHubClient"/> used for checking the merged state of <see cref="PullRequest"/>s
		/// </summary>
		GitHubClient ghclient;

		void InitServerPage()
		{
			projectNameText.LostFocus += ProjectNameText_LostFocus;
			projectNameText.KeyDown += ProjectNameText_KeyDown;
			ServerStartBGW.RunWorkerCompleted += ServerStartBGW_RunWorkerCompleted;
			ghclient = new GitHubClient(new ProductHeaderValue(Assembly.GetExecutingAssembly().GetName().Name));
			var config = Properties.Settings.Default;
			if (!String.IsNullOrWhiteSpace(config.GitHubAPIKey))
				ghclient.Credentials = new Credentials(Helpers.DecryptData(config.GitHubAPIKey, config.GitHubAPIKeyEntropy));
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

		private void CompileCancelButton_Click(object sender, EventArgs e)
		{
			var res = Instance.Compiler.Cancel();
			if (res != null)
				MessageBox.Show(res);
			LoadServerPage();
		}

		void LoadServerPage()
		{
			var RepoExists = Instance.Repository.Exists();
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
			ServerGStopButton.Visible = RepoExists;
			ServerStartButton.Visible = RepoExists;
			ServerGRestartButton.Visible = RepoExists;
			ServerRestartButton.Visible = RepoExists;
			PortLabel.Visible = RepoExists;
			ServerStopButton.Visible = RepoExists;
			TestMergeManagerButton.Visible = RepoExists;
			UpdateServerButton.Visible = RepoExists;
			RemoveAllTestMergesButton.Visible = RepoExists;
			WorldAnnounceField.Visible = RepoExists;
			WorldAnnounceButton.Visible = RepoExists;
			WorldAnnounceLabel.Visible = RepoExists;
			SyncCommitsCheckBox.Visible = RepoExists;

			if (updatingFields)
				return;

			var DM = Instance.Compiler;
			var DD = Instance.DreamDaemon;
			var Config = Instance.Config;
			var Repo = Instance.Repository;

			try
			{
				updatingFields = true;
				
				ServerPathLabel.Text = "Server Path: " + Instance.ServerDirectory();

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

				UpdateServerButton.Enabled = false;
				TestMergeManagerButton.Enabled = false;
				RemoveAllTestMergesButton.Enabled = false;
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
						UpdateServerButton.Enabled = true;
						TestMergeManagerButton.Enabled = true;
						RemoveAllTestMergesButton.Enabled = true;
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
				Instance.Compiler.SetProjectName(projectNameText.Text);
		}

		private void PortSelector_ValueChanged(object sender, EventArgs e)
		{
			if (!updatingFields)
				Instance.DreamDaemon.SetPort((ushort)PortSelector.Value);
		}

		private void ServerPageRefreshButton_Click(object sender, EventArgs e)
		{
			LoadServerPage();
		}

		private void InitializeButton_Click(object sender, EventArgs e)
		{
			if (!Instance.Compiler.Initialize())
				MessageBox.Show("Unable to start initialization!");
			LoadServerPage();
		}
		private void CompileButton_Click(object sender, EventArgs e)
		{
			if (!Instance.Compiler.Compile())
				MessageBox.Show("Unable to start compilation!");
			LoadServerPage();
		}

		private void AutostartCheckbox_CheckedChanged(object sender, System.EventArgs e)
		{
			if (!updatingFields)
				Instance.DreamDaemon.SetAutostart(AutostartCheckbox.Checked);
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
				e.Result = Instance.DreamDaemon.Start();
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
			var res = Instance.DreamDaemon.Stop();
			if (res != null)
				MessageBox.Show(res);
		}

		private void ServerRestartButton_Click(object sender, EventArgs e)
		{
			var DialogResult = MessageBox.Show("This will immediately restart the server. Continue?", "Confim", MessageBoxButtons.YesNo);
			if (DialogResult == DialogResult.No)
				return;
			var res = Instance.DreamDaemon.Restart();
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
			Instance.DreamDaemon.RequestStop();
			LoadServerPage();
		}

		private void ServerGRestartButton_Click(object sender, EventArgs e)
		{
			var DialogResult = MessageBox.Show("This will restart the server when the current round ends. Continue?", "Confim", MessageBoxButtons.YesNo);
			if (DialogResult == DialogResult.No)
				return;
			Instance.DreamDaemon.RequestRestart();
		}

		/// <summary>
		/// Launches the <see cref="TestMergeManager"/>
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The <see cref="EventArgs"/></param>
		void TestMergeManagerButton_Click(object sender, System.EventArgs e)
		{
			using (var TMM = new TestMergeManager(Instance, ghclient))
				TMM.ShowDialog();
			LoadServerPage();
		}

		/// <summary>
		/// Calls <see cref="UpdateServer"/>
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The <see cref="EventArgs"/></param>
		void UpdateServerButton_Click(object sender, EventArgs e)
		{
			UpdateServer();
		}

		/// <summary>
		/// Calls <see cref="ITGRepository.Update(bool)"/> with a <see langword="true"/> parameter, re-merging any current <see cref="PullRequest"/>s at their current commit, calls <see cref="ITGRepository.GenerateChangelog(out string)"/> and <see cref="ITGRepository.SynchronizePush"/>, and starts the <see cref="ITGCompiler.Compile(bool)"/> prompting the user with any errors that may occur. Merged <see cref="PullRequest"/>s are not remerged
		/// </summary>
		async void UpdateServer()
		{
			try
			{
				UseWaitCursor = true;
				Enabled = false;
				try
				{
					string res = null;
					var repo = Instance.Repository;
					var pulls = await Task.Run(() => repo.MergedPullRequests(out res));

					if (pulls == null)
					{
						MessageBox.Show(res);
						return;
					}

					List<Task<PullRequest>> pullsRequests = null;
					if (Program.GetRepositoryRemote(repo, out string remoteOwner, out string remoteName))
					{
						//find out which of the PRs have been merged
						pullsRequests = new List<Task<PullRequest>>();
						foreach (var I in pulls)
							pullsRequests.Add(ghclient.PullRequest.Get(remoteOwner, remoteName, I.Number));
					}

					res = await Task.Run(() => repo.Update(true));

					if (res != null)
					{
						MessageBox.Show(res, "Error updating repository");
						return;
					}

					await Task.Run(() => repo.GenerateChangelog(out res));

					if (res != null)
						MessageBox.Show(res, "Error generating changelog");

					res = await Task.Run(() => repo.SynchronizePush());

					if (res != null)
						MessageBox.Show(res, "Error synchronizing commits");

					if (pullsRequests != null)
						Task.WaitAll(pullsRequests.ToArray());

					foreach (var I in pullsRequests)
						if (I.Result.Merged)
							pulls.RemoveAll(x => x.Number == I.Result.Number);

					var mergeResults = await Task.Run(() => repo.MergePullRequests(pulls, true));
					var compileStartResult = await Task.Run(() => Instance.Compiler.Compile(true));

					//Show any errors
					for (var I = 0; I < mergeResults.Count(); ++I)
					{
						var err = mergeResults.ElementAt(I);
						if (err != null)
							MessageBox.Show(err, String.Format("Error re-merging PR #{0}", pulls[I].Number));
					}

					if (!compileStartResult)
						MessageBox.Show(res, "Error starting compile!");
				}
				finally
				{
					UseWaitCursor = false;
					Enabled = true;
				}
			}
			catch (ForbiddenException)
			{
				if (ghclient.Credentials.AuthenticationType == AuthenticationType.Anonymous)
				{
					if (Program.RateLimitPrompt(ghclient))
						UpdateServer();
					return;
				}
				else
					throw;
			}
			LoadServerPage();
		}

		/// <summary>
		/// Calls <see cref="ITGRepository.Reset(bool)"/> with a <see langword="true"/> parameter, calls <see cref="ITGRepository.GenerateChangelog(out string)"/>, and starts the <see cref="ITGCompiler.Compile(bool)"/> prompting the user with any errors that may occur.
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The <see cref="EventArgs"/></param>
		async void RemoveAllTestMergesButton_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show("Are you sure you want to remove all test merges?", "Confirm", MessageBoxButtons.YesNo) != DialogResult.Yes)
				return;
			try
			{
				UseWaitCursor = true;
				Enabled = false;
				try
				{
					var repo = Instance.Repository;

					var res = await Task.Run(() => repo.Reset(true));

					if (res != null)
					{
						MessageBox.Show(res, "Error resetting repository");
						return;
					}

					await Task.Run(() => repo.GenerateChangelog(out res));

					if (res != null)
						MessageBox.Show(res, "Error generating changelog");

					await Task.Run(() => Instance.Compiler.Compile(false));
					if (res != null)
						MessageBox.Show(res, "Error starting compile!");
				}
				finally
				{
					UseWaitCursor = false;
					Enabled = true;
				}
			}
			catch (ForbiddenException)
			{
				if (ghclient.Credentials.AuthenticationType == AuthenticationType.Anonymous)
				{
					if (Program.RateLimitPrompt(ghclient))
						UpdateServer();
					return;
				}
				else
					throw;
			}
			LoadServerPage();
		}

		private void SecuritySelector_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!updatingFields)
				if (!Instance.DreamDaemon.SetSecurityLevel((DreamDaemonSecurity)SecuritySelector.SelectedIndex))
					MessageBox.Show("Security change will be applied after next server reboot.");
		}

		private void WorldAnnounceButton_Click(object sender, EventArgs e)
		{
			var msg = WorldAnnounceField.Text;
			if (!String.IsNullOrWhiteSpace(msg))
			{
				var res = Instance.DreamDaemon.WorldAnnounce(msg);
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
				Instance.DreamDaemon.SetWebclient(WebclientCheckBox.Checked);
		}

		private void AutoUpdateInterval_ValueChanged(object sender, EventArgs e)
		{
			if (!updatingFields)
				Instance.Repository.SetAutoUpdateInterval((ulong)AutoUpdateInterval.Value);
		}

		private void AutoUpdateCheckbox_CheckedChanged(object sender, EventArgs e)
		{
			if (updatingFields)
				return;
			var on = AutoUpdateCheckbox.Visible && AutoUpdateCheckbox.Checked;
			AutoUpdateInterval.Visible = on;
			AutoUpdateMLabel.Visible = on;
			if (!on)
				Instance.Repository.SetAutoUpdateInterval(0);
			else
				Instance.Repository.SetAutoUpdateInterval((ulong)AutoUpdateInterval.Value);
		}

		private void createMinidump_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show("This will create a full memory minidump of the current DD process and save it in Diagnostics/Minidumps. Continue?", "Minidump", MessageBoxButtons.YesNo) != DialogResult.Yes)
				return;

			var result = Instance.DreamDaemon.CreateMinidump();

			if (result != null)
				MessageBox.Show(result, "Error");
			else
				MessageBox.Show("Minidump complete!", "Success");
		}
	}
}
