using System;
using System.Windows.Forms;
using TGServiceInterface;

namespace TGControlPanel
{
	public partial class Login : Form
	{
		public Login()
		{
			InitializeComponent();
			IPTextBox.Text = Properties.Settings.Default.RemoteIP;
			var Config = Properties.Settings.Default;
			UsernameTextBox.Text = Config.RemoteUsername;
			PortSelector.Value = Config.RemotePort;
			AcceptButton = RemoteLoginButton;
		}

		private void RemoteLoginButton_Click(object sender, EventArgs e)
		{
			IPTextBox.Text = IPTextBox.Text.Trim();
			UsernameTextBox.Text = UsernameTextBox.Text.Trim();
			Server.SetRemoteLoginInformation(IPTextBox.Text, (ushort)PortSelector.Value, UsernameTextBox.Text, PasswordTextBox.Text);
			Properties.Settings.Default.RemoteIP = IPTextBox.Text;
			Properties.Settings.Default.RemoteUsername = UsernameTextBox.Text;
			VerifyAndConnect();
		}

		private void LocalLoginButton_Click(object sender, EventArgs e)
		{
            Server.MakeLocalConnection();
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
			new Main().ShowDialog();
			Close();
		}
	}
}
