using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using TGServiceInterface;
using TGServiceInterface.Components;

namespace TGControlPanel
{
	sealed partial class TestMergeManager : ServerOpForm
	{
		/// <summary>
		/// The <see cref="IInterface"/> connected to an <see cref="ITGInstance"/> to handle the pull requests for
		/// </summary>
		readonly IInterface currentInterface;

		/// <summary>
		/// Construct a <see cref="TestMergeManager"/>
		/// </summary>
		public TestMergeManager(IInterface interfaceToUse)
		{
			InitializeComponent();
			Load += PullRequestManager_Load;
			DialogResult = DialogResult.Cancel;
			UpdateToRemoteRadioButton.Checked = true;
			currentInterface = interfaceToUse;
		}

		/// <summary>
		/// Populate <see cref="PullRequestListBox"/> with <see cref="PullRequestInfo"/> from github and check off ones that are currently merged
		/// </summary>
		async void LoadPullRequests()
		{
			Enabled = false;
			UseWaitCursor = true;
			try
			{
				PullRequestListBox.Items.Clear();

				var repo = currentInterface.GetComponent<ITGRepository>();
				string remote = null, error = null, error2 = null;
				IList<PullRequestInfo> pulls = null;

				var remoteRequest = WrapServerOp(() => remote = repo.GetRemote(out error));

				//get started on this while we're processing here
				var pullsRequest = Task.Factory.StartNew(() => pulls = repo.MergedPullRequests(out error2));

				await remoteRequest;

				if (remote == null)
				{
					MessageBox.Show(String.Format("Error retrieving remote repository: {0}", error));
					return;
				}
				if (!remote.Contains("github.com"))
				{
					MessageBox.Show("Pull request support is only available for github based repositories!", "Error");
					return;
				}

				//Assume standard gh format: [(git)|(https)]://github.com/owner/repo(.git)[0-1]
				var splits = remote.Split('/');
				var repoName = splits[splits.Length - 1];
				var repoOwner = splits[splits.Length - 2];

				//Search for open PRs
				var client = new GitHubClient(new ProductHeaderValue(Assembly.GetExecutingAssembly().GetName().Name));
				Enabled = false;
				UseWaitCursor = true;
				SearchIssuesResult result;
				try
				{
					result = await client.Search.SearchIssues(new SearchIssuesRequest
					{
						Repos = new RepositoryCollection
				{
					{ repoOwner, repoName }
				},
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
				await WrapServerOp(() => pullsRequest.Wait());
				if (pulls == null)
					MessageBox.Show(String.Format("Error retrieving currently merged pull requests: {0}", error2));

				foreach (var I in result.Items)
					PullRequestListBox.Items.Add(String.Format("#{0} - {1}", I.Number, I.Title), pulls.Any(x => x.Number == I.Number));
			}
			finally
			{
				Enabled = true;
				UseWaitCursor = false;
			}
		}

		/// <summary>
		/// Called when the <see cref="TestMergeManager"/> is loaded
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The <see cref="EventArgs"/></param>
		void PullRequestManager_Load(object sender, EventArgs e)
		{
			LoadPullRequests();
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
			try
			{
				//so first collect a list of pulls that are checked
				var pulls = new List<int>();
				foreach (var I in PullRequestListBox.CheckedItems)
					pulls.Add(Convert.ToInt32(((string)I).Split(' ')[0].Substring(1)));

				var repo = currentInterface.GetComponent<ITGRepository>();

				string error = null;
				//Do standard repo updates
				if (UpdateToRemoteRadioButton.Checked)
					await WrapServerOp(() => error = repo.Update(true));
				else if (UpdateToOriginRadioButton.Checked)
					await WrapServerOp(() => error = repo.Reset(true));

				if (error != null)
				{
					MessageBox.Show(String.Format("Error updating repository: {0}", error));
					return;
				}

				await WrapServerOp(() => repo.GenerateChangelog(out error));
				if (error != null)
					MessageBox.Show(String.Format("Error generating changelog: {0}", error));

				if (UpdateToRemoteRadioButton.Checked)
				{
					await WrapServerOp(() => error = repo.SynchronizePush());
					if (error != null)
						MessageBox.Show(String.Format("Error sychronizing repo: {0}", error));
				}

				//Merge the PRs, collect errors
				IList<string> errors = null;
				await WrapServerOp(() =>
				{
					errors = new List<string>();
					foreach (var I in pulls)
					{
						var res = repo.MergePullRequest(I);
						if (res != null)
							errors.Add(String.Format("Error merging PR #{0}: {1}", I, res));
					}
				});

				//Show any errors
				foreach (var I in errors)
					MessageBox.Show(I);
				if (errors.Count != 0)
					return;

				//Start the compile
				if (!currentInterface.GetComponent<ITGCompiler>().Compile(pulls.Count == 1))
					MessageBox.Show("Could not start compilation!");
				else
					MessageBox.Show("Test merges updated and compilation started!");
			}
			finally
			{
				Enabled = true;
				UseWaitCursor = false;
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
	}
}
