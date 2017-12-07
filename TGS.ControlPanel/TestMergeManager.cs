﻿using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using TGS.Interface;
using TGS.Interface.Components;

namespace TGS.ControlPanel
{
	sealed partial class TestMergeManager : ServerOpForm
	{
		/// <summary>
		/// Error message format used when <see cref="ITGRepository.MergedPullRequests(out string)"/> fails
		/// </summary>
		const string MergedPullsError = "Error retrieving currently merged pull requests: {0}";

		/// <summary>
		/// The <see cref="IServerInterface"/> connected to an <see cref="ITGInstance"/> to handle the pull requests for
		/// </summary>
		readonly IServerInterface currentInterface;

		/// <summary>
		/// The <see cref="GitHubClient"/> to use to read PR lists
		/// </summary>
		readonly GitHubClient client;

		/// <summary>
		/// The owner of the target <see cref="Repository"/>
		/// </summary>
		string repoOwner;
		/// <summary>
		/// The name of the target <see cref="Repository"/>
		/// </summary>
		string repoName;

		/// <summary>
		/// Construct a <see cref="TestMergeManager"/>
		/// </summary>
		/// <param name="interfaceToUse">The <see cref="IServerInterface"/> to use for managing the <see cref="ITGInstance"/></param>
		/// <param name="clientToUse">The <see cref="GitHubClient"/> to use for getting pull request information</param>
		public TestMergeManager(IServerInterface interfaceToUse, GitHubClient clientToUse)
		{
			InitializeComponent();
			DialogResult = DialogResult.Cancel;
			UpdateToRemoteRadioButton.Checked = true;
			currentInterface = interfaceToUse;
			client = clientToUse;
			Load += PullRequestManager_Load;
			PullRequestListBox.ItemCheck += PullRequestListBox_ItemCheck;
		}

		/// <summary>
		/// Called when an item in <see cref="PullRequestListBox"/> is checked or unchecked. Unchecks opposing PRs
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The <see cref="ItemCheckEventArgs"/></param>
		void PullRequestListBox_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			if (e.NewValue != CheckState.Checked)
				return;
			//let's uncheck the opposing PR# if this is one of the outdated/updated testmerge
			var item = (string)PullRequestListBox.Items[e.Index];
			var prefix = item.Split(' ')[0];

			for (var I = 0; I < PullRequestListBox.Items.Count; ++I)
			{
				var S = (string)PullRequestListBox.Items[I];
				if (S.Split(' ')[0] == prefix && S != item)
				{
					PullRequestListBox.SetItemChecked(I, false);
					break;
				}
			}
		}

		/// <summary>
		/// Populate <see cref="PullRequestListBox"/> with <see cref="PullRequestInfo"/> from github and check off ones that are currently merged. Prompts the user to login to GitHub if they hit the rate limit
		/// </summary>
		async void LoadPullRequests()
		{
			try
			{
				Enabled = false;
				UseWaitCursor = true;
				try
				{
					PullRequestListBox.Items.Clear();

					var repo = currentInterface.GetComponent<ITGRepository>();
					List<PullRequestInfo> pulls = null;
					
					//get started on this while we're processing here
					var pullsRequest = repo.MergedPullRequests();

					//Search for open PRs
					Enabled = false;
					UseWaitCursor = true;
					SearchIssuesResult result;
					try
					{
						result = await client.Search.SearchIssues(new SearchIssuesRequest
						{
							Repos = new RepositoryCollection { { repoOwner, repoName } },
							State = ItemState.Open,
							Type = IssueTypeQualifier.PullRequest
						});
					}
					finally
					{
						Enabled = true;
						UseWaitCursor = false;
					}

					//now we need to know what's merged
					pulls = await pullsRequest;
					if (pulls == null)
						MessageBox.Show(String.Format(MergedPullsError));

					//insert the open pull requests, checking already merged once
					foreach (var I in result.Items)
					{
						bool alreadyMerged = false;
						var pull = pulls.Where(x => x.Number == I.Number).FirstOrDefault();
						if (pull != null)
						{
							//we need the full info for this PR
							alreadyMerged = (await client.PullRequest.Get(repoOwner, repoName, I.Number)).Head.Sha == pull.Sha;
							if (alreadyMerged)
								pulls.Remove(pull);
						}
						InsertPullRequest(I, false, alreadyMerged);
					}

					//insert remaining merged pulls
					foreach (var I in pulls)
						InsertItem(String.Format("#{0} - {1} - OUTDATED: {2}", I.Number, I.Title, I.Sha), true, true);
				}
				finally
				{
					Enabled = true;
					UseWaitCursor = false;
				}
			}
			catch (ForbiddenException)
			{
				if (client.Credentials.AuthenticationType == AuthenticationType.Anonymous)  //assume request limit hit
				{
					if(Program.RateLimitPrompt(client))
						LoadPullRequests();
				}
				else
					throw;
			}
		}

		/// <summary>
		/// Format an entry for <paramref name="issue"/> and insert it into <see cref="PullRequestListBox"/>
		/// </summary>
		/// <param name="issue">The <see cref="Issue"/> to format, must contain a <see cref="PullRequest"/></param>
		/// <param name="prioritize">If this or <paramref name="isChecked"/> is <see langword="true"/>, <paramref name="issue"/> will be inserted at the top of <see cref="PullRequestListBox"/> as opposed to the bottom</param>
		/// <param name="isChecked">If the item should be checked</param>
		void InsertPullRequest(Issue issue, bool prioritize, bool isChecked)
		{
			bool needsTesting = false;
			foreach (var J in issue.Labels)
				if (J.Name.ToLower().Contains("test"))
				{
					needsTesting = true;
					break;
				}
			var itemString = String.Format("#{0} - {1}{2}", issue.Number, issue.Title, issue.PullRequest != null && issue.PullRequest.Merged ? " - MERGED ON REMOTE" : needsTesting ? " - TESTING REQUESTED" : "");
			InsertItem(itemString, prioritize || needsTesting, isChecked);
		}

		/// <summary>
		/// Insert an <paramref name="itemString"/> into <see cref="PullRequestListBox"/>
		/// </summary>
		/// <param name="itemString">The <see cref="string"/> to insert</param>
		/// <param name="prioritize">If this or <paramref name="isChecked"/> is <see langword="true"/>, <paramref name="itemString"/> will be inserted at the top of <see cref="PullRequestListBox"/> as opposed to the bottom</param>
		/// <param name="isChecked">If the item should be checked</param>
		void InsertItem(string itemString, bool prioritize, bool isChecked)
		{
			prioritize = prioritize || isChecked;
			if (prioritize)
			{
				PullRequestListBox.Items.Insert(0, itemString);
				if (isChecked)
					PullRequestListBox.SetItemChecked(0, true);
			}
			else
				PullRequestListBox.Items.Add(itemString, isChecked);
		}

		/// <summary>
		/// Called when the <see cref="TestMergeManager"/> is loaded. Sets <see cref="repoOwner"/> and <see cref="repoName"/>
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The <see cref="EventArgs"/></param>
		void PullRequestManager_Load(object sender, EventArgs e)
		{
			Enabled = false;
			var repo = currentInterface.GetComponent<ITGRepository>();
			if(!Program.GetRepositoryRemote(repo, out repoOwner, out repoName))
			{
				Close();
				return;
			}
			LoadPullRequests();
		}

		/// <summary>
		/// Calls <see cref="ITGRepository.GenerateChangelog(out string)"/> and shows the user an error prompt if it fails
		/// </summary>
		/// <param name="repo">The <see cref="ITGRepository"/> to call <see cref="ITGRepository.GenerateChangelog(out string)"/> on</param>
		async void GenerateChangelog(ITGRepository repo)
		{
			string error = null;
			await WrapServerOp(() => repo.GenerateChangelog(out error));
			if (error != null)
				MessageBox.Show(String.Format("Error generating changelog: {0}", error));
		}

		/// <summary>
		/// Called when the <see cref="ApplyButton"/> is clicked. Calls <see cref="ITGRepository.Update(bool)"/> if necessary, merge pull requests, and call <see cref="ITGCompiler.Compile(bool)"/>. Closes the <see cref="TestMergeManager"/> if appropriate
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The <see cref="EventArgs"/></param>
		async void ApplyButton_Click(object sender, EventArgs e)
		{
			Enabled = false;
			UseWaitCursor = true;
			ApplyingPullRequestsLabel.Visible = true;
			ApplyingPullRequestsProgressBar.Visible = true;
			PullRequestListBox.Visible = false;
			try
			{
				//so first collect a list of pulls that are checked
				var pulls = new List<PullRequestInfo>();
				foreach (var I in PullRequestListBox.CheckedItems)
				{
					var S = (string)I;
					string mergedSha = null;
					var splits = S.Split(' ');
					if(S.Contains(" - OUTDATED: "))
						mergedSha = splits[splits.Length - 1];
					var key = Convert.ToInt32((splits[0].Substring(1)));
					try
					{
						pulls.Add(new PullRequestInfo(key, mergedSha));
					}
					catch
					{
						MessageBox.Show(String.Format("Checked both keep and update option for #{0}", key), "Error");
						return;
					}
				}

				var repo = currentInterface.GetComponent<ITGRepository>();

				string error = null;
				//Do standard repo updates
				if (UpdateToRemoteRadioButton.Checked)
					error = await repo.Update(true);
				else if (UpdateToOriginRadioButton.Checked)
					await WrapServerOp(() => error = repo.Reset(true));

				if (error != null)
				{
					MessageBox.Show(String.Format("Error updating repository: {0}", error));
					return;
				}

				if (UpdateToRemoteRadioButton.Checked)
				{
					GenerateChangelog(repo);
					error = await Task.Run(() => repo.SynchronizePush());
				}

				//Merge the PRs, collect errors
				var errors = await repo.MergePullRequests(pulls, false);

				if (errors != null)
				{
					//Show any errors
					for (var I = 0; I < errors.Count(); ++I)
					{
						var err = errors.ElementAt(I);
						if (err != null)
							MessageBox.Show(err, String.Format("Error merging PR #{0}", pulls[I].Number));
					}
					return;
				}

				if (pulls.Count > 0)
					//regen the changelog
					GenerateChangelog(repo);

				//Start the compile
				var compileStarted = await Task.Run(() => currentInterface.GetComponent<ITGCompiler>().Compile(pulls.Count == 1));
				
				if (error != null)
					MessageBox.Show(String.Format("Error sychronizing repo: {0}", error));

				if (!compileStarted)
					MessageBox.Show("Could not start compilation!");
				else
					MessageBox.Show("Test merges updated and compilation started!");
			}
			finally
			{
				Enabled = true;
				UseWaitCursor = false;
				ApplyingPullRequestsLabel.Visible = false;
				ApplyingPullRequestsProgressBar.Visible = false;
				PullRequestListBox.Visible = true;
			}
		}

		/// <summary>
		/// Called when the <see cref="RefreshButton"/> is clicked
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The <see cref="EventArgs"/></param>
		void RefreshButton_Click(object sender, EventArgs e)
		{
			LoadPullRequests();
		}

		/// <summary>
		/// Called when the <see cref="AddPRButton"/> is clicked. Adds the PR with the number in <see cref="AddPRNumericUpDown"/> to <see cref="PullRequestListBox"/> or shows an error prompt if it doesn't/already exists
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The <see cref="EventArgs"/></param>
		async void AddPRButton_Click(object sender, EventArgs e)
		{

			Enabled = false;
			UseWaitCursor = true;
			try
			{
				IList<PullRequestInfo> pulls;
				var mergedPullsTask = currentInterface.GetComponent<ITGRepository>().MergedPullRequests();

				int PRNumber;
				try
				{
					PRNumber = Convert.ToInt32(AddPRNumericUpDown.Value);
				}
				catch
				{
					MessageBox.Show("Invalid PR number!");
					return;
				}
				var found = false;
				var asString = PRNumber.ToString();
				foreach (var I in PullRequestListBox.Items)
					if (((string)I).Split(' ')[0].Substring(1) == asString)
					{
						found = true;
						break;
					}
				if (found)
				{
					MessageBox.Show("That PR is already in the list!");
					return;
				}

				pulls = await mergedPullsTask;
				if (pulls == null)
					MessageBox.Show(String.Format(MergedPullsError));
				//get the PR in question
				var PR = await client.Issue.Get(repoName, repoOwner, PRNumber);
				if(PR == null ||PR.PullRequest == null)
				{
					MessageBox.Show("That doesn't seem to be a valid PR!");
					return;
				}
				InsertPullRequest(PR, true, pulls == null || pulls.Any(x => x.Number == PRNumber));
			}
			finally
			{
				UseWaitCursor = false;
				Enabled = true;
			}
		}
	}
}
