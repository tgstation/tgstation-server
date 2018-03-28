using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using TGS.Interface;

namespace TGS.ControlPanel
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
		/// The <see cref="IInstance"/> for this <see cref="ControlPanel"/>
		/// </summary>
		readonly IInstance Instance;

		/// <summary>
		/// Constructs a <see cref="ControlPanel"/>
		/// </summary>
		/// <param name="server">The <see cref="IServer"/> to use</param>
		/// <param name="instance">The <see cref="IInstance"/> to use</param>
		public ControlPanel(IServer server, IInstance instance)
		{
			InitializeComponent();

			FormClosed += ControlPanel_FormClosed;
			Panels.SelectedIndexChanged += Panels_SelectedIndexChanged;
			Panels.SelectedIndex += Math.Min(Properties.Settings.Default.LastPageIndex, Panels.TabCount - 1);

			Instance = instance;

			InstancesInUse.Add(Instance.Metadata.Name, this);
			
			Text = String.Format("TGS {0} Instance: {1}", server.Version, Instance.Metadata.Name);

			InitRepoPage();
			InitBYONDPage();
			InitServerPage();

			UpdateSelectedPanel();
		}

		/// <summary>
		/// Called when the <see cref="ControlPanel"/> is closed
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The <see cref="EventArgs"/></param>
		void ControlPanel_FormClosed(object sender, FormClosedEventArgs e)
		{
			InstancesInUse.Remove(Instance.Metadata.Name);
		}

		/// <summary>
		/// Called from <see cref="Dispose(bool)"/>
		/// </summary>
		void Cleanup()
		{
			InstancesInUse.Remove(Instance.Metadata.Name);
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
				case 4: //static
					InitStaticPage();
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
			if (Instance.Administration != null)
			{
				MessageBox.Show("Only system administrators may use this command!");
				return false;
			}
			return true;
		}
	}
}
