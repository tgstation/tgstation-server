using System;
using System.Windows.Forms;
using TGS.Interface;
using TGS.Interface.Components;

namespace TGS.ControlPanel
{
	partial class ControlPanel
	{
		string lastReadError = null;
		void InitBYONDPage()
		{
			var BYOND = Interface.GetComponent<ITGByond>();
			var CV = BYOND.GetVersion(ByondVersion.Installed).Result;
			if (CV == null)
				CV = BYOND.GetVersion(ByondVersion.Staged).Result;
			if (CV != null)
			{
				var splits = CV.Split('.');
				if(splits.Length == 2)
				{
					try
					{
						var Major = Convert.ToInt32(splits[0]);
						var Minor = Convert.ToInt32(splits[1]);
						MajorVersionNumeric.Value = Major;
						MinorVersionNumeric.Value = Minor;
					}
					catch { }
				}
			}

			var latestVer = BYOND.GetVersion(ByondVersion.Latest).Result;
			LatestVersionLabel.Text = latestVer;

			try
			{
				var splits = latestVer.Split('.');
				var maj = Convert.ToInt32(splits[0]);
				MinorVersionNumeric.Value = Convert.ToInt32(splits[1]);
				MajorVersionNumeric.Value = maj;
			}
			catch { }
		}
		private void UpdateButton_Click(object sender, EventArgs e)
		{
			UpdateBYONDButtons();
			if (!Interface.GetComponent<ITGByond>().UpdateToVersion((int)MajorVersionNumeric.Value, (int)MinorVersionNumeric.Value).Result)
				MessageBox.Show("Unable to begin update, there is another operation in progress.");
		}
		
		private void BYONDRefreshButton_Click(object sender, EventArgs e)
		{
			UpdateBYONDButtons();
		}
		void UpdateBYONDButtons()
		{
			var BYOND = Interface.GetComponent<ITGByond>();

			VersionLabel.Text = BYOND.GetVersion(ByondVersion.Installed).Result ?? "Not Installed";

			StagedVersionTitle.Visible = false;
			StagedVersionLabel.Visible = false;
			switch (BYOND.CurrentStatus().Result)
			{
				case ByondStatus.Idle:
				case ByondStatus.Starting:
					StatusLabel.Text = "Idle";
					UpdateButton.Enabled = true;
					break;
				case ByondStatus.Downloading:
					StatusLabel.Text = "Downloading...";
					UpdateButton.Enabled = false;
					break;
				case ByondStatus.Staging:
					StatusLabel.Text = "Staging...";
					UpdateButton.Enabled = false;
					break;
				case ByondStatus.Staged:
					StagedVersionTitle.Visible = true;
					StagedVersionLabel.Visible = true;
					StagedVersionLabel.Text = BYOND.GetVersion(ByondVersion.Staged).Result ?? "Unknown";
					StatusLabel.Text = "Staged and waiting for BYOND to shutdown...";
					UpdateButton.Enabled = true;
					break;
				case ByondStatus.Updating:
					StatusLabel.Text = "Applying update...";
					UpdateButton.Enabled = false;
					break;
			}
			var error = Interface.GetComponent<ITGByond>().GetError().Result;
			if (error != lastReadError)
			{
				lastReadError = error;
				if (error != null)
					MessageBox.Show("An error occurred: " + lastReadError);
			}
		}
	}
}
