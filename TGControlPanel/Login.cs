using System;
using System.Windows.Forms;
using TGServiceInterface;

namespace TGControlPanel
{
	public partial class Login : Form
	{
		/// <summary>
		/// Create a <see cref="Login"/>
		/// </summary>
		public Login()
		{
			InitializeComponent();
			var Config = Properties.Settings.Default;
			IPTextBox.Text = Config.RemoteIP;
			UsernameTextBox.Text = Config.RemoteUsername;
			PortSelector.Value = Config.RemotePort;
			AcceptButton = RemoteLoginButton;
			if(Config.RemoteDefault)
				RemoteLoginButton.TabIndex = 0; //make this the first thing selected when loading
			var decrypted = Helpers.DecryptData(Config.RemotePassword, Config.RemoteEntropy);
			if (decrypted != null)
			{
				PasswordTextBox.Text = decrypted;
				SavePasswordCheckBox.Checked = true;
			}
		}

		private void RemoteLoginButton_Click(object sender, EventArgs e)
		{
			IPTextBox.Text = IPTextBox.Text.Trim();
			UsernameTextBox.Text = UsernameTextBox.Text.Trim();
			Server.SetRemoteLoginInformation(IPTextBox.Text, (ushort)PortSelector.Value, UsernameTextBox.Text, PasswordTextBox.Text);
			var Config = Properties.Settings.Default;
			Config.RemoteIP = IPTextBox.Text;
			Config.RemoteUsername = UsernameTextBox.Text;
			if (SavePasswordCheckBox.Checked)
			{
				Config.RemotePassword = Helpers.EncryptData(PasswordTextBox.Text, out string entrop);
				Config.RemoteEntropy = entrop;
			}
			else
			{
				Config.RemotePassword = null;
				Config.RemoteEntropy = null;
			}
			Config.RemoteDefault = true;
			VerifyAndConnect();
		}

		private void LocalLoginButton_Click(object sender, EventArgs e)
		{
            Server.MakeLocalConnection();
			Properties.Settings.Default.RemoteDefault = false;
			VerifyAndConnect();
		}

		void VerifyAndConnect()
		{
			var res = Server.VerifyConnection();
			if (res != null)
			{
				MessageBox.Show("Unable to connect to service! Error: " + res);
				return;
			}
			if (!Server.Authenticate())
			{
				MessageBox.Show("Authentication error: Username/password/windows identity is not authorized! Ensure you are a system administrator or in the correct Windows group on the service machine.");
				return;
			}
			Hide();
			using (var M = new Main())
				M.ShowDialog();
			Close();
		}

		private void SavePasswordCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			if (!SavePasswordCheckBox.Checked)
			{
				var Config = Properties.Settings.Default;
				Config.RemotePassword = null;
				Config.RemoteEntropy = null;
			}
		}
	}
}
