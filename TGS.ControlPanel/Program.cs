using System;
using System.Windows.Forms;
using TGS.Interface;
using TGS.Interface.Components;

namespace TGS.ControlPanel
{
	static class Program
	{
		public static IClient ServerInterface;
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
				Interface.Client.SetBadCertificateHandler(BadCertificateHandler);
				Application.Run(new Login());
				if(ServerInterface != null)
				{
					new InstanceSelector(ServerInterface.Server).Show();
					Application.Run();
				}
			}
			catch (Exception e)
			{
				if (ServerInterface != null)
					ServerInterface.Dispose();
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
	}
}
