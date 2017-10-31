using System;
using System.Windows.Forms;
using TGServiceInterface;

namespace TGControlPanel
{
	partial class Login : Form
	{
		/// <summary>
		/// Create a <see cref="Login"/> form
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
			using (var I = new Interface(IPTextBox.Text, (ushort)PortSelector.Value, UsernameTextBox.Text, PasswordTextBox.Text))
			{
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
				VerifyAndConnect(I);
			}
		}

		private void LocalLoginButton_Click(object sender, EventArgs e)
		{
			using (var I = new Interface())
			{
				Properties.Settings.Default.RemoteDefault = false;
				VerifyAndConnect(I);
			}
		}

		void VerifyAndConnect(Interface I)
		{
			var res = I.ConnectionStatus(out string error);
			if (!res.HasFlag(ConnectivityLevel.Connected))
			{
				MessageBox.Show("Unable to connect to service! Error: " + error);
				return;
			}
			if (!res.HasFlag(ConnectivityLevel.Authenticated))
			{
				MessageBox.Show("Authentication error: Username/password/windows identity is not authorized! Ensure you are a system administrator or in the correct Windows group on the service machine.");
				return;
			}
			Hide();
			using (var M = new ControlPanel(I))
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
