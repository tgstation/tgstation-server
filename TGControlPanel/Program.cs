using System;
using System.Windows.Forms;
using TGServiceInterface;
using TGServiceInterface.Components;

namespace TGControlPanel
{
	static class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			try
			{
				if (Properties.Settings.Default.UpgradeRequired)
				{
					Properties.Settings.Default.Upgrade();
					Properties.Settings.Default.UpgradeRequired = false;
					Properties.Settings.Default.Save();
				}
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				ServerInterface.SetBadCertificateHandler(BadCertificateHandler);
				var login = new Login();
				login.Show();
				Application.Run();
			}
			catch (Exception e)
			{
				ServiceDisconnectException(e);
			}
			finally
			{
				Properties.Settings.Default.Save();
			}
		}
		static bool SSLErrorPromptResult = false;
		static bool BadCertificateHandler(string message)
		{
			if (!SSLErrorPromptResult)
			{
				var result = MessageBox.Show(message + " IT IS HIGHLY RECCOMENDED YOU DO NOT PROCEED! Continue?", "SSL Error", MessageBoxButtons.YesNo) == DialogResult.Yes;
				SSLErrorPromptResult = result;
				return result;
			}
			return true;
		}

		public static void ServiceDisconnectException(Exception e)
		{
			MessageBox.Show("An unhandled exception occurred. This usually means we lost connection to the service. Error" + e.ToString());
		}

		public static string TextPrompt(string caption, string text)
		{
			Form prompt = new Form()
			{
				Width = 500,
				Height = 150,
				FormBorderStyle = FormBorderStyle.FixedDialog,
				Text = caption,
				StartPosition = FormStartPosition.CenterScreen,
				MaximizeBox = false,
				MinimizeBox = false,
			};
			Label textLabel = new Label() { Left = 50, Top = 20, Text = text, AutoSize = true };
			TextBox textBox = new TextBox() { Left = 50, Top = 50, Width = 400 };
			Button confirmation = new Button() { Text = "Ok", Left = 350, Width = 100, Top = 70, DialogResult = DialogResult.OK };
			confirmation.Click += (sender, e) => { prompt.Close(); };
			prompt.Controls.Add(textBox);
			prompt.Controls.Add(confirmation);
			prompt.Controls.Add(textLabel);
			prompt.AcceptButton = confirmation;

			return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : null;
		}

		/// <summary>
		/// Prompts the user to open the <see cref="GitHubLoginPrompt"/> explaining the API rate limit
		/// </summary>
		/// <param name="client">The <see cref="Octokit.GitHubClient"/> to use</param>
		/// <returns><see langword="true"/> if the <see cref="GitHubLoginPrompt"/> ran and returned a <see cref="DialogResult.OK"/>, <see langword="false"/> otherwise</returns>
		public static bool RateLimitPrompt(Octokit.GitHubClient client)
		{
			if (MessageBox.Show("You seem to have hit the rate limit of 60 requests per hour of the GitHub API for anonymous requests. Would you like to enter credentials to bypass this?", "Rate limited", MessageBoxButtons.YesNo) != DialogResult.Yes)
				return false;
			using (var D = new GitHubLoginPrompt(client))
				return D.ShowDialog() == DialogResult.OK;
		}

		/// <summary>
		/// Gets the <paramref name="owner"/> and <paramref name="name"/> of a given <paramref name="repo"/>'s remote. Shows an error if the target remote isn't GitHub
		/// </summary>
		/// <param name="repo">The <see cref="ITGRepository"/> to get the remote of</param>
		/// <param name="owner">The owner of the remote <see cref="Octokit.Repository"/></param>
		/// <param name="name">The remote <see cref="Octokit.Repository.Name"/></param>
		/// <returns><see langword="true"/> if <paramref name="repo"/>'s remote was a valid GitHub <see cref="Octokit.Repository"/>, <see langword="false"/> otherwise and the user was prompted</returns>
		public static bool GetRepositoryRemote(ITGRepository repo, out string owner, out string name)
		{
			string remote = null;
			remote = repo.GetRemote(out string error);
			if (remote == null)
			{
				MessageBox.Show(String.Format("Error retrieving remote repository: {0}", error));
				owner = null;
				name = null;
				return false;
			}
			if (!remote.Contains("github.com"))
			{
				MessageBox.Show("Pull request support is only available for GitHub based repositories!", "Error");
				owner = null;
				name = null;
				return false;
			}

			//Assume standard gh format: [(git)|(https)]://github.com/owner/repo(.git)[0-1]
			var splits = remote.Split('/');
			name = splits[splits.Length - 1];
			owner = splits[splits.Length - 2].Split('.')[0];
			return true;
		}
	}
}
