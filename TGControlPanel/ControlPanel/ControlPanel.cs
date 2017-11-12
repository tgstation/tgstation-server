using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using TGServiceInterface;
using TGServiceInterface.Components;

namespace TGControlPanel
{
	/// <summary>
	/// The main <see cref="ControlPanel"/> form
	/// </summary>
	sealed partial class ControlPanel : CountedForm
	{
		/// <summary>
		/// List of instances being used by open control panels
		/// </summary>
		public static IDictionary<string, ControlPanel> InstancesInUse { get; private set; } = new Dictionary<string, ControlPanel>();

		/// <summary>
		/// The <see cref="IInterface"/> instance for this <see cref="ControlPanel"/>
		/// </summary>
		readonly IInterface Interface;

		/// <summary>
		/// Constructs a <see cref="ControlPanel"/>
		/// </summary>
		/// <param name="I">The <see cref="IInterface"/> for the <see cref="ControlPanel"/></param>
		public ControlPanel(IInterface I)
		{
			InitializeComponent();
			FormClosed += ControlPanel_FormClosed;
			Interface = I;
			if (Interface.IsRemoteConnection)
			{
				var splits = Interface.GetServiceComponent<ITGLanding>().Version().Split(' ');
				Text = String.Format("TGS {0}: {1}:{2}", splits[splits.Length - 1], Interface.HTTPSURL, Interface.HTTPSPort);
			}
			Text += " Instance: " + I.InstanceName;
			if (Interface.VersionMismatch(out string error) && MessageBox.Show(error, "Warning", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
			{
				Close();
				return;
			}
			Panels.SelectedIndexChanged += Panels_SelectedIndexChanged;
			Panels.SelectedIndex += Math.Min(Properties.Settings.Default.LastPageIndex, Panels.TabCount - 1);
			InitRepoPage();
			InitBYONDPage();
			InitServerPage();
			InitStaticPage();
			UpdateSelectedPanel();
			InstancesInUse.Add(I.InstanceName, this);
		}

		/// <summary>
		/// Called when the <see cref="ControlPanel"/> is closed
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The <see cref="EventArgs"/></param>
		void ControlPanel_FormClosed(object sender, FormClosedEventArgs e)
		{
			InstancesInUse.Remove(Interface.InstanceName);
		}

		/// <summary>
		/// Called from <see cref="Dispose(bool)"/>
		/// </summary>
		void Cleanup()
		{
			InstancesInUse.Remove(Interface.InstanceName);
			Interface.Dispose();
		}

		private void Main_Resize(object sender, EventArgs e)
		{
			Panels.Location = new Point(10, 10);
			Panels.Width = ClientSize.Width - 20;
			Panels.Height = ClientSize.Height - 20;
		}

		/// <summary>
		/// Updates the content of <see cref="TabControl.SelectedTab"/> of <see cref="Panels"/>
		/// </summary>
		void UpdateSelectedPanel()
		{
			switch (Panels.SelectedIndex)
			{
				case 0: //repo
					PopulateRepoFields();
					break;
				case 1: //byond
					UpdateBYONDButtons();
					break;
				case 2: //scp
					LoadServerPage();
					break;
				case 3: //chat
					LoadChatPage();
					break;
			}
			Properties.Settings.Default.LastPageIndex = Panels.SelectedIndex;
		}

		void Panels_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateSelectedPanel();
		}

		bool CheckAdminWithWarning()
		{
			if (!Interface.ConnectToInstance().HasFlag(ConnectivityLevel.Administrator))
			{
				MessageBox.Show("Only system administrators may use this command!");
				return false;
			}
			return true;
		}
	}
}
