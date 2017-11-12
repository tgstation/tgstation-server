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
		/// Error message format used when <see cref="ITGRepository.MergedPullRequests(out string)"/> fails
		/// </summary>
		const string MergedPullsError = "Error retrieving currently merged pull requests: {0}";

		/// <summary>
		/// The <see cref="IInterface"/> connected to an <see cref="ITGInstance"/> to handle the pull requests for
		/// </summary>
		readonly IInterface currentInterface;

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
		public TestMergeManager(IInterface interfaceToUse)
		{
			InitializeComponent();
			DialogResult = DialogResult.Cancel;
			UpdateToRemoteRadioButton.Checked = true;
			currentInterface = interfaceToUse;
			client = new GitHubClient(new ProductHeaderValue(Assembly.GetExecutingAssembly().GetName().Name));
			Load += PullRequestManager_Load;
		}

		/// <summary>
		/// Populate <see cref="PullRequestListBox"/> with <see cref="PullRequestInfo"/> from github and check off ones that are currently merged. Prompts the user to login to GitHub if they hit the rate limit
		/// </summary>
		async void LoadPullRequests()
		{
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
					string error = null;
					IList<PullRequestInfo> pulls = null;
					
					//get started on this while we're processing here
					var pullsRequest = Task.Factory.StartNew(() => pulls = repo.MergedPullRequests(out error));

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
					await pullsRequest;
					if (pulls == null)
						MessageBox.Show(String.Format(MergedPullsError, error));

					foreach (var I in result.Items)
						InsertPullRequest(I, pulls.Any(x => x.Number == I.Number));
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
							LoadPullRequests();
				}
				else
					throw;
			}
		}

		void InsertPullRequest(Issue I, bool isChecked)
		{
			bool needsTesting = false;
			foreach (var J in I.Labels)
				if (J.Name.ToLower().Contains("test"))
				{
					needsTesting = true;
					break;
				}
			var itemString = String.Format("#{0} - {1}{2}", I.Number, I.Title, needsTesting ? " - TESTING REQUESTED" : "");
			if (needsTesting || isChecked)
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
		async void PullRequestManager_Load(object sender, EventArgs e)
		{
			var repo = currentInterface.GetComponent<ITGRepository>();
			string remote = null, error = null;
			await WrapServerOp(() => remote = repo.GetRemote(out error));
			if (remote == null)
			{
				MessageBox.Show(String.Format("Error retrieving remote repository: {0}", error));
				Close();
				return;
			}
			if (!remote.Contains("github.com"))
			{
				MessageBox.Show("Pull request support is only available for github based repositories!", "Error");
				Close();
				return;
			}

			//Assume standard gh format: [(git)|(https)]://github.com/owner/repo(.git)[0-1]
			var splits = remote.Split('/');
			repoName = splits[splits.Length - 1];
			repoOwner = splits[splits.Length - 2];

			Enabled = true;
			UseWaitCursor = true;

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
			LoadPullRequests();
		}

		bool PullRequestsStringsListContainsPRNumber(CheckedListBox.ObjectCollection items, int PRNumber)
		{
			return false;
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
				IList<PullRequestInfo> pulls = null;
				string error = null;
				var mergedPullsTask = WrapServerOp(() => pulls = currentInterface.GetComponent<ITGRepository>().MergedPullRequests(out error));

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

				await mergedPullsTask;
				if (pulls == null)
					MessageBox.Show(String.Format(MergedPullsError, error));
				//get the PR in question
				InsertPullRequest(await client.Issue.Get(repoName, repoOwner, PRNumber), pulls == null || pulls.Any(x => x.Number == PRNumber));
			}
			finally
			{
				UseWaitCursor = false;
				Enabled = true;
			}
		}
	}
}
