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
		/// Populate <see cref="PullRequestListBox"/> with <see cref="PullRequestInfo"/> from github and check off ones that are currently merged. Prompts the user to login to GitHub if they hit the rate limit
		/// </summary>
		/// <param name="client">The <see cref="GitHubClient"/> to use. If <see langword="null"/>, one will be created</param>
		async void LoadPullRequests(GitHubClient client)
		{
			if(client == null)
				client = new GitHubClient(new ProductHeaderValue(Assembly.GetExecutingAssembly().GetName().Name));
			var config = Properties.Settings.Default;
			if (!String.IsNullOrWhiteSpace(config.GitHubAPIKey))
				client.Credentials = new Credentials(Helpers.DecryptData(config.GitHubAPIKey, config.GitHubAPIKeyEntropy));
			try
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
					await WrapServerOp(() => pullsRequest.Wait());
					if (pulls == null)
						MessageBox.Show(String.Format("Error retrieving currently merged pull requests: {0}", error2));

					foreach (var I in result.Items)
					{
						bool needsTesting = false;
						foreach(var J in I.Labels)
							if (J.Name.ToLower().Contains("test"))
							{
								needsTesting = true;
								break;
							}
						var itemString = String.Format("#{0} - {1}{2}", I.Number, I.Title, needsTesting ? " - TESTING REQUESTED" : "");
						var isChecked = pulls.Any(x => x.Number == I.Number);
						if (needsTesting || isChecked)
						{
							PullRequestListBox.Items.Insert(0, itemString);
							if(isChecked)
								PullRequestListBox.SetItemChecked(0, true);
						}
						else
							PullRequestListBox.Items.Add(itemString, isChecked);
					}
				}
				finally
				{
					Enabled = true;
					UseWaitCursor = false;
				}
			}
			catch (ForbiddenException)
			{
				if (client.Credentials.AuthenticationType == AuthenticationType.Anonymous)	//assume request limit hit
				{
					if (MessageBox.Show("You seem to have hit the rate limit of 60 requests per hour of the GitHub API for anonymous requests. Would you like to enter credentials to bypass this?", "Rate limited", MessageBoxButtons.YesNo) != DialogResult.Yes)
						return;
					using (var D = new GitHubLoginPrompt(client))
						if (D.ShowDialog() == DialogResult.OK)
							LoadPullRequests(client);
				}
				else
					throw;
			}
		}

		/// <summary>
		/// Called when the <see cref="TestMergeManager"/> is loaded
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The <see cref="EventArgs"/></param>
		void PullRequestManager_Load(object sender, EventArgs e)
		{
			LoadPullRequests(null);
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

				if (UpdateToRemoteRadioButton.Checked)
				{
					GenerateChangelog(repo);
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

				if (pulls.Count > 0)
					//regen the changelog
					GenerateChangelog(repo);

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
			LoadPullRequests(null);
		}
	}
}
