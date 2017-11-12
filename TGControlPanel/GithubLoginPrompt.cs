using Octokit;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using TGServiceInterface;

namespace TGControlPanel
{
	/// <summary>
	/// Used for recieving a GitHub API key for use in <see cref="Credentials"/>
	/// </summary>
	sealed partial class GitHubLoginPrompt : Form
	{
		/// <summary>
		/// The text to appear on <see cref="LoginButton"/>
		/// </summary>
		const string InitalLoginButtonText = "Login";

		/// <summary>
		/// The <see cref="GitHubClient"/> to use for OAuth requests
		/// </summary>
		readonly GitHubClient client;

		/// <summary>
		/// Construct a <see cref="GitHubLoginPrompt"/>
		/// </summary>
		/// <param name="c">The <see cref="GitHubClient"/> to use for requests</param>
		public GitHubLoginPrompt(GitHubClient c)
		{
			InitializeComponent();
			AcceptButton = LoginButton;
			DialogResult = DialogResult.Cancel;
			client = c;
		}

		/// <summary>
		/// Calls <see cref="GetAPIKey"/>. On success, encrypts it's return value, saves it in <see cref="Properties.Settings.GitHubAPIKey"/> and <see cref="Properties.Settings.GitHubAPIKeyEntropy"/>, and closes the <see cref="GitHubLoginPrompt"/>. On failure, shows a <see cref="MessageBox"/> and exits
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The <see cref="EventArgs"/></param>
		async void LoginButton_Click(object sender, EventArgs e)
		{
			LoginButton.Text = "Logging in...";
			Enabled = false;
			UseWaitCursor = true;
			var APIKey = await GetAPIKey();
			if(APIKey == null)
			{
				MessageBox.Show("Authentication failure!");
				Enabled = true;
				UseWaitCursor = false;
				LoginButton.Text = "Login";
				return;
			}
			if (String.IsNullOrWhiteSpace(APIKeyTextBox.Text))
				//They used username authentication, let them know we made a token
				MessageBox.Show("A personal access token has been created on your account for use with the Control Panel");

			//Encrypt it and let's be on our way
			var Config = Properties.Settings.Default;
			Config.GitHubAPIKey = Helpers.EncryptData(APIKey, out string entropy);
			Config.GitHubAPIKeyEntropy = entropy;
			DialogResult = DialogResult.OK;
			Close();
		}

		/// <summary>
		/// Retrieves an API key from GitHub based on login information or from the <see cref="APIKeyTextBox"/> and verifies it
		/// </summary>
		/// <returns>The GitHub API key on success, <see langword="null"/> on failure</returns>
		async Task<string> GetAPIKey()
		{
			string APIKey;
			if (String.IsNullOrWhiteSpace(APIKeyTextBox.Text))
			{
				try
				{
					client.Credentials = new Credentials(UsernameTextBox.Text, PasswordTextBox.Text);
					var token = await client.Authorization.Create(new NewAuthorization { Note = "TGControlPanel token to bypass rate limiting" });
					APIKey = token.Token;
				}
				catch (AuthorizationException)
				{
					return null;
				}
			}
			else
				APIKey = APIKeyTextBox.Text;

			//validate it by pinging a random repository
			client.Credentials = new Credentials(APIKey);
			try
			{
				await client.Repository.Get("Dextraspace", "Test");
			}
			catch (AuthorizationException)
			{
				return null;
			}
			return APIKey;
		}
	}
}
