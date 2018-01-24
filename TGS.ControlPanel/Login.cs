using System;
using System.Collections.Specialized;
using System.Windows.Forms;
using TGS.Interface;

namespace TGS.ControlPanel
{
	sealed partial class Login : Form
	{
		/// <summary>
		/// Currently selected saved <see cref="RemoteLoginInfo"/>
		/// </summary>
		RemoteLoginInfo currentLoginInfo;

		bool updatingFields;

		/// <summary>
		/// Construct a <see cref="Login"/>
		/// </summary> 
		public Login()
		{
			InitializeComponent();
			var Config = Properties.Settings.Default;
			AcceptButton = RemoteLoginButton;
			if(Config.RemoteDefault)
				RemoteLoginButton.TabIndex = 0; //make this the first thing selected when loading

			IPComboBox.SelectedIndexChanged += IPComboBox_SelectedIndexChanged;
			var loginInfo = Config.RemoteLoginInfo;
			if (loginInfo != null)
			{
				foreach(var I in loginInfo)
					IPComboBox.Items.Add(new RemoteLoginInfo(I));
				if (IPComboBox.Items.Count > 0)
					IPComboBox.SelectedIndex = 0;
			}

			IPComboBox.TextChanged += (a, b) => ClearFields(2);
			PortSelector.ValueChanged += (a, b) => ClearFields(3);
			UsernameTextBox.TextChanged += (a, b) => ClearFields(4);
			
		}

		void ClearFields(int start = 1)
		{
			if (updatingFields || currentLoginInfo == null)
				return;
			updatingFields = true;
			currentLoginInfo = null;
			switch (start) {
				case 1:
					IPComboBox.Text = "";
				case 2:
					PortSelector.Value = 38607;		
				case 3:
					UsernameTextBox.Text = "";
				case 4:
					if (currentLoginInfo.HasPassword)
						PasswordTextBox.Text = "";		
					break;
			}
			updatingFields = false;
		}

		void IPComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			RemoteLoginInfo newLoginInfo = IPComboBox.SelectedItem as RemoteLoginInfo;
			//new thing
			if (newLoginInfo == null)
			{
				ClearFields();
				currentLoginInfo = newLoginInfo;
				return;
			}
			updatingFields = true;
			currentLoginInfo = newLoginInfo;
			IPComboBox.Text = currentLoginInfo.IP;
			PortSelector.Value = currentLoginInfo.Port;
			UsernameTextBox.Text = currentLoginInfo.Username;
			SavePasswordCheckBox.Checked = currentLoginInfo.HasPassword;
			if (currentLoginInfo.HasPassword)
				PasswordTextBox.Text = "************";
			else
				PasswordTextBox.Text = "";
			updatingFields = false;
		}

		void RemoteLoginButton_Click(object sender, EventArgs e)
		{
			if (String.IsNullOrWhiteSpace(PasswordTextBox.Text) || String.IsNullOrWhiteSpace(UsernameTextBox.Text) || String.IsNullOrWhiteSpace(IPComboBox.Text) || PortSelector.Value == 0)
				return;

			RemoteLoginInfo loginInfo;
			if (currentLoginInfo == null)
			{
				loginInfo = new RemoteLoginInfo(IPComboBox.Text, (ushort)PortSelector.Value, UsernameTextBox.Text.Trim(), PasswordTextBox.Text);
			}
			else
			{
				loginInfo = (RemoteLoginInfo)IPComboBox.SelectedItem;
				if (!loginInfo.HasPassword)
					loginInfo.Password = PasswordTextBox.Text;
			}

			var I = new Client(loginInfo);
			var Config = Properties.Settings.Default;
			//This needs to be read here because V&C Closing us will corrupt the data
			var savePassword = SavePasswordCheckBox.Checked;
			if (VerifyAndConnect(I))
			{
				Config.RemoteDefault = true;

				if (!savePassword)
					loginInfo.Password = null;

				Config.RemoteLoginInfo = new StringCollection { loginInfo.ToJSON() };

				foreach (RemoteLoginInfo info in IPComboBox.Items)
					if (!info.Equals(loginInfo))
						Config.RemoteLoginInfo.Add(info.ToJSON());
			}
		}

		void LocalLoginButton_Click(object sender, EventArgs e)
		{
			Properties.Settings.Default.RemoteDefault = false;
			VerifyAndConnect(new Client());
		}

		/// <summary>
		/// Attempts a connection on a given <see cref="IClient"/>
		/// </summary>
		/// <param name="I">The <see cref="IClient"/> to attempt a connection on</param>
		/// <returns><see langword="true"/> if the connection was made and authenticated, <see langword="false"/> otherwise</returns>
		bool VerifyAndConnect(IClient I)
		{
			try
			{
				var res = I.ConnectionStatus(out string error);
				if (!res.HasFlag(ConnectivityLevel.Connected))
				{
					MessageBox.Show("Unable to connect to service! Error: " + error);
					return false;
				}
				if (!res.HasFlag(ConnectivityLevel.Authenticated))
				{
					MessageBox.Show("Authentication error: Username/password/windows identity is not authorized! Ensure you are a system administrator or in the correct Windows group on the service machine.");
					return false;
				}

				if (I.VersionMismatch(out error) && MessageBox.Show(error, "Warning", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
					return true;

				Close();
				Program.ServerInterface = I;
				return true;
			}
			catch
			{
				I.Dispose();
				throw;
			}
		}

		void SavePasswordCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			if (!updatingFields && !SavePasswordCheckBox.Checked && currentLoginInfo != null)
			{
				currentLoginInfo = null;
				PasswordTextBox.Text = "";
			}
		}

		/// <summary>
		/// Removes the selected item from <see cref="IPComboBox"/>
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The <see cref="EventArgs"/></param>
		void DeleteLoginButton_Click(object sender, EventArgs e)
		{
			//make sure we're trying to delete a real item
			if (IPComboBox.SelectedItem as RemoteLoginInfo == null)
				return;
			IPComboBox.Items.RemoveAt(IPComboBox.SelectedIndex);
		}
	}
}
