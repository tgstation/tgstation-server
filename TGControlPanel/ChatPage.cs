using System;
using System.Collections.Generic;
using System.Windows.Forms;
using TGServiceInterface;

namespace TGControlPanel
{
	partial class Main
	{
		bool updatingChat = false;
		
		TGChatProvider ModifyingProvider
		{
			get { return (TGChatProvider)Properties.Settings.Default.LastChatProvider; }
			set { Properties.Settings.Default.LastChatProvider = (int)value; }
		}

		void LoadChatPage()
		{
			updatingChat = true;
			var Chat = Server.GetComponent<ITGChat>();
			var PI = Chat.ProviderInfos()[(int)ModifyingProvider];
			ChatAdminsTextBox.Visible = true;
			IRCModesComboBox.Visible = false;
			switch (ModifyingProvider)
			{
				case TGChatProvider.Discord:
					var DPI = new TGDiscordSetupInfo(PI);
					DiscordProviderSwitch.Select();
					AuthField1.Text = DPI.BotToken; //it's invisible so whatever
					AuthField1Title.Text = "Bot Token:";
					AuthField2.Visible = false;
					AuthField2Title.Visible = false;
					ChatServerText.Visible = false;
					ChatPortSelector.Visible = false;
					ChatServerTitle.Visible = false;
					ChatPortTitle.Visible = false;
					ChatNicknameText.Visible = false;
					ChatNicknameTitle.Visible = false;

					ChatAdminsTitle.Text = String.Format("Admin {0} IDs:", DPI.AdminsAreSpecial ? "Role" : "User");
					ChannelsTitle.Text = "Broadcast/Listening Channel IDs:";
					AdminModeNormal.Text = "User IDs";
					AdminModeSpecial.Text = "Role IDs";
					break;
				case TGChatProvider.IRC:
					var IRC = new TGIRCSetupInfo(PI);
					IRCProviderSwitch.Select();
					AuthField1.Text = IRC.AuthTarget;
					AuthField2.Text = IRC.AuthMessage;
					AuthField2.Visible = true;
					AuthField2Title.Visible = true;
					AuthField1Title.Text = "Auth Target:";
					AuthField2Title.Text = "Auth Message:";
					ChatServerText.Visible = true;
					ChatPortSelector.Visible = true;
					ChatServerTitle.Visible = true;
					ChatPortTitle.Visible = true;
					ChatServerText.Text = IRC.URL;
					ChatPortSelector.Value = IRC.Port;
					ChatNicknameText.Visible = true;
					ChatNicknameTitle.Visible = true;
					ChatNicknameText.Text = IRC.Nickname;
					ChatAdminsTitle.Text = String.Format("Admin {0}:", IRC.AdminsAreSpecial ? "Req Mode" : "Nicknames");
					ChannelsTitle.Text = "Broadcast/Listening Channels:";
					AdminModeNormal.Text = "Nicknames";
					AdminModeSpecial.Text = "Channel Mode";
					if (IRC.AdminsAreSpecial)
					{
						ChatAdminsTextBox.Visible = false;
						IRCModesComboBox.Visible = true;
						IRCModesComboBox.SelectedIndex = (int)IRC.AuthLevel;
					}
					break;
				default:
					Properties.Settings.Default.LastChatProvider = (int)TGChatProvider.IRC;
					LoadChatPage();
					return;
			}

			AdminModeNormal.Checked = !PI.AdminsAreSpecial;
			AdminModeSpecial.Checked = PI.AdminsAreSpecial;
			ChatEnabledCheckbox.Checked = PI.Enabled;
			if (!PI.Enabled)
				ChatStatusLabel.Text = "Disabled";
			else if (Chat.Connected(ModifyingProvider))
				ChatStatusLabel.Text = "Connected";
			else
				ChatStatusLabel.Text = "Disconnected";
			ChatReconnectButton.Enabled = PI.Enabled;

			AssignListToTextbox(PI.AdminList, ChatAdminsTextBox);
			AssignListToTextbox(PI.WatchdogChannels, WDChannelsTextbox);
			AssignListToTextbox(PI.AdminChannels, AdminChannelsTextbox);
			AssignListToTextbox(PI.DevChannels, DevChannelsTextbox);
			AssignListToTextbox(PI.GameChannels, GameChannelsTextbox);
			updatingChat = false;
		}

		static void AssignListToTextbox(IList<string> a, TextBox b)
		{
			b.Text = "";
			foreach (var I in a)
				b.Text += I + Environment.NewLine;
		}

		private void ChatRefreshButton_Click(object sender, EventArgs e)
		{
			LoadChatPage();
		}

		private void ChatReconnectButton_Click(object sender, EventArgs e)
		{
			Server.GetComponent<ITGChat>().Reconnect(ModifyingProvider);
			LoadChatPage();
		}

		static string[] SplitByLine(TextBox t)
		{
			var channels = t.Text.Split('\n');

			var finalChannels = new List<string>();
			foreach (var I in channels)
			{
				var trimmed = I.Trim();
				if(trimmed != "")
					finalChannels.Add(trimmed);
			}
			return finalChannels.ToArray();
		}

		private void DiscordProviderSwitch_CheckedChanged(object sender, EventArgs e)
		{
			if (!updatingChat && DiscordProviderSwitch.Checked)
			{
				ModifyingProvider = TGChatProvider.Discord;
				LoadChatPage();
			}
		}

		private void IRCProviderSwitch_CheckedChanged(object sender, EventArgs e)
		{
			if (!updatingChat && IRCProviderSwitch.Checked)
			{
				ModifyingProvider = TGChatProvider.IRC;
				LoadChatPage();
			}
		}
		void SetAdminsAreSpecial(bool value)
		{
			var Chat = Server.GetComponent<ITGChat>();
			var PI = Chat.ProviderInfos()[(int)ModifyingProvider];
			PI.AdminsAreSpecial = value;
			var res = Chat.SetProviderInfo(PI);
			if (res != null)
				MessageBox.Show(res);
			LoadChatPage();
		}

		private void AdminModeNormal_CheckedChanged(object sender, EventArgs e)
		{
			if (!updatingChat && AdminModeNormal.Checked)
				SetAdminsAreSpecial(false);
		}

		private void AdminModeSpecial_CheckedChanged(object sender, EventArgs e)
		{
			if (!updatingChat && AdminModeSpecial.Checked)
				SetAdminsAreSpecial(true);
		}

		private void ChatApplyButton_Click(object sender, EventArgs e)
		{
			string res = null;
			TGChatSetupInfo wip = null;
			switch (ModifyingProvider)
			{
				case TGChatProvider.Discord:
					wip = new TGDiscordSetupInfo()
					{
						BotToken = AuthField1.Text
					};
					break;
				case TGChatProvider.IRC:
					wip = new TGIRCSetupInfo()
					{
						AuthMessage = AuthField2.Text,
						AuthTarget = AuthField1.Text,
						Nickname = ChatNicknameText.Text,
						URL = ChatServerText.Text,
						Port = (ushort)ChatPortSelector.Value,
						AuthLevel = (IRCMode)IRCModesComboBox.SelectedIndex,
					};
					break;
				default:
					res = "You really shouldn't be able to read this.";
					break;
			}

			if (res == null)
			{
				wip.AdminChannels = new List<string>(AdminChannelsTextbox.Text.Split('\n'));
				wip.WatchdogChannels = new List<string>(WDChannelsTextbox.Text.Split('\n'));
				wip.DevChannels = new List<string>(DevChannelsTextbox.Text.Split('\n'));
				wip.GameChannels = new List<string>(GameChannelsTextbox.Text.Split('\n'));
				wip.AdminList = new List<string>(ChatAdminsTextBox.Text.Split('\n'));
				wip.Enabled = ChatEnabledCheckbox.Checked;

				res = Server.GetComponent<ITGChat>().SetProviderInfo(wip);
			}
			if (res != null)
				MessageBox.Show(res);
			LoadChatPage();
		}
	}
}
