using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using TGServiceInterface;
using TGServiceInterface.Components;

namespace TGControlPanel
{
	/// <summary>
	/// Form used for managing <see cref="ITGSService"/> <see cref="ITGInstance"/> manipulation functions
	/// </summary>
	sealed partial class InstanceSelector : CountedForm
	{
		/// <summary>
		/// The <see cref="IInterface"/> we build instance connections from
		/// </summary>
		readonly IInterface masterInterface;
		/// <summary>
		/// List of <see cref="InstanceMetadata"/> from <see cref="masterInterface"/>
		/// </summary>
		IList<InstanceMetadata> InstanceData;
		/// <summary>
		/// Used for modifying <see cref="EnabledCheckBox"/> without invoking its side effects
		/// </summary>
		bool UpdatingEnabledCheckbox = false;

		/// <summary>
		/// Construct an <see cref="InstanceSelector"/>
		/// </summary>
		/// <param name="I">An <see cref="IInterface"/> connected a the <see cref="ITGSService"/></param>
		public InstanceSelector(IInterface I)
		{
			InitializeComponent();
			InstanceListBox.MouseDoubleClick += InstanceListBox_MouseDoubleClick;
			masterInterface = I;
			RefreshInstances();
		}

		/// <summary>
		/// Connects to a <see cref="ITGInstance"/> if it is double clicked in <see cref="InstanceListBox"/>
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The <see cref="MouseEventArgs"/></param>
		void InstanceListBox_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			TryConnectToIndexInstance(InstanceListBox.IndexFromPoint(e.Location));
		}

		/// <summary>
		/// Returns the <see cref="InstanceMetadata"/> associated with <see cref="InstanceListBox"/>'s current selected index
		/// </summary>
		/// <returns>The <see cref="InstanceMetadata"/> associated with <see cref="InstanceListBox"/>'s current selected index if it exists, <see langword="null"/> otherwise</returns>
		InstanceMetadata GetSelectedInstanceMetadata()
		{
			var index = InstanceListBox.SelectedIndex;
			return index != ListBox.NoMatches ? InstanceData[index] : null;
		}

		/// <summary>
		/// Called by <see cref="Dispose(bool)"/>
		/// </summary>
		void Cleanup()
		{
			masterInterface.Dispose();
		}

		/// <summary>
		/// Loads the <see cref="InstanceListBox"/> using <see cref="ITGLanding.ListInstances"/>
		/// </summary>
		async void RefreshInstances()
		{
			InstanceListBox.Items.Clear();
			await WrapServerOp(() => {
				InstanceData = masterInterface.GetServiceComponent<ITGLanding>().ListInstances();
			});
			foreach(var I in InstanceData)
				InstanceListBox.Items.Add(String.Format("{0}: {1} - {2} - {3}", I.LoggingID, I.Name, I.Path, I.Enabled ? "ONLINE" : "OFFLINE"));
			var HasServerAdmin = masterInterface.ConnectionStatus().HasFlag(ConnectivityLevel.Administrator);
			CreateInstanceButton.Enabled = HasServerAdmin;
			ImportInstanceButton.Enabled = HasServerAdmin;
			RenameInstanceButton.Enabled = HasServerAdmin;
			DetachInstanceButton.Enabled = HasServerAdmin;
			EnabledCheckBox.Enabled = HasServerAdmin;
			if(InstanceData.Count > 0)
				InstanceListBox.SelectedIndex = 0;
		}

		/// <summary>
		/// Tries to start a <see cref="ControlPanel"/> for a given <see cref="InstanceListBox"/> <paramref name="index"/>
		/// </summary>
		/// <param name="index">The <see cref="ListBox.SelectedIndex"/> of <see cref="InstanceListBox"/> to connect to</param>
		async void TryConnectToIndexInstance(int index)
		{
			if (index == ListBox.NoMatches)
				return;
			var instanceName = InstanceData[index].Name;
			if (ControlPanel.InstancesInUse.TryGetValue(instanceName, out ControlPanel activeCP))
			{
				activeCP.BringToFront();
				return;
			}
			var InstanceAccessor = new Interface(masterInterface as Interface);
			try
			{
				ConnectivityLevel res = ConnectivityLevel.None;
				await WrapServerOp(() => { res = InstanceAccessor.ConnectToInstance(instanceName); });
				if (!res.HasFlag(ConnectivityLevel.Connected))
				{
					MessageBox.Show("Unable to connect to instance! Does it exist?");
					RefreshInstances();
				}
				else if (!res.HasFlag(ConnectivityLevel.Authenticated))
					MessageBox.Show("The current user is not authorized to access this instance!");
				else
					new ControlPanel(InstanceAccessor).Show();
			}
			catch
			{
				InstanceAccessor.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Prompts the user for parameters to <see cref="ITGInstanceManager.DetachInstance(string)"/>
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The <see cref="EventArgs"/></param>
		async void DetachInstanceButton_Click(object sender, EventArgs e)
		{
			var imd = GetSelectedInstanceMetadata();
			if (imd == null)
				return;
			if (MessageBox.Show(String.Format("This will dissociate the server instance at \"{0}\"! Are you sure?", imd.Path), "Instance Detach", MessageBoxButtons.YesNo) != DialogResult.Yes)
				return;
			string res = null;
			await WrapServerOp(() => res = masterInterface.GetServiceComponent<ITGInstanceManager>().DetachInstance(imd.Name));
			if (res != null)
				MessageBox.Show(res);
			RefreshInstances();
		}

		/// <summary>
		/// Calls <see cref="RefreshInstances"/>
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The <see cref="EventArgs"/></param>
		void RefreshButton_Click(object sender, EventArgs e)
		{
			RefreshInstances();
		}

		/// <summary>
		/// Prompts the user for parameters to <see cref="ITGInstanceManager.RenameInstance(string, string)"/>
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The <see cref="EventArgs"/></param>
		async void RenameInstanceButton_Click(object sender, EventArgs e)
		{
			var imd = GetSelectedInstanceMetadata();
			if (imd == null)
				return;
			var new_name = Program.TextPrompt("Instance Rename", "Enter a new name for the instance:");
			if (new_name == null)
				return;
			if (imd.Enabled && MessageBox.Show(String.Format("This will temporarily offline the server instance! Are you sure?", imd.Path), "Instance Restart", MessageBoxButtons.YesNo) != DialogResult.Yes)
				return;
			string res = null;
			await WrapServerOp(() => res = masterInterface.GetServiceComponent<ITGInstanceManager>().RenameInstance(imd.Name, new_name));
			if (res != null)
				MessageBox.Show(res);
			RefreshInstances();
		}

		/// <summary>
		/// Prompts the user for parameters to <see cref="ITGInstanceManager.ImportInstance(string)"/>
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The <see cref="EventArgs"/></param>
		async void ImportInstanceButton_Click(object sender, EventArgs e)
		{
			var instance_path = Program.TextPrompt("Instance Import", "Enter the full path to the instance:");
			if (instance_path == null)
				return;
			string res = null;
			await WrapServerOp(() => res = masterInterface.GetServiceComponent<ITGInstanceManager>().ImportInstance(instance_path));
			if (res != null)
				MessageBox.Show(res);
			RefreshInstances();
		}

		/// <summary>
		/// Prompts the user for parameters to <see cref="ITGInstanceManager.CreateInstance(string, string)"/>
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The <see cref="EventArgs"/></param>
		async void CreateInstanceButton_Click(object sender, EventArgs e)
		{
			var instance_name = Program.TextPrompt("Instance Creation", "Enter the name of the instance:");
			if (instance_name == null)
				return;
			var instance_path = Program.TextPrompt("Instance Creation", "Enter the full path to the instance:");
			if (instance_path == null)
				return;
			string res = null;
			await WrapServerOp(() => res = masterInterface.GetServiceComponent<ITGInstanceManager>().CreateInstance(instance_name, instance_path));
			if (res != null)
				MessageBox.Show(res);
			RefreshInstances();
		}

		/// <summary>
		/// Attempts to connect the user to an <see cref="ITGInstance"/> based on the <see cref="ListBox.SelectedIndex"/> of <see cref="InstanceListBox"/>
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The <see cref="EventArgs"/></param>
		void ConnectButton_Click(object sender, EventArgs e)
		{
			TryConnectToIndexInstance(InstanceListBox.SelectedIndex);
		}

		/// <summary>
		/// Prompts the user if they want to call <see cref="ITGInstanceManager.SetInstanceEnabled(string, bool)"/> to either online or offline an <see cref="ITGInstance"/> based on its current state
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The <see cref="EventArgs"/></param>
		async void EnabledCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			if (UpdatingEnabledCheckbox)
				return;
			var enabling = EnabledCheckBox.Checked;
			try
			{
				if (MessageBox.Show(String.Format("Are you sure you want to {0} this instance?", enabling ? "online" : "offline"), "Instance Status Change", MessageBoxButtons.YesNo) != DialogResult.Yes)
					return;
				string res = null;
				var index = InstanceListBox.SelectedIndex;
				await WrapServerOp(() => res = masterInterface.GetServiceComponent<ITGInstanceManager>().SetInstanceEnabled(InstanceData[index].Name, enabling));
				if (res != null)
					MessageBox.Show(res);
			}
			finally
			{
				RefreshInstances();
			}
		}

		/// <summary>
		/// Update <see cref="EnabledCheckBox"/> based on the selected <see cref="ITGInstance"/>'s <see cref="InstanceMetadata.Enabled"/> property
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The <see cref="EventArgs"/></param>
		void InstanceListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			var index = InstanceListBox.SelectedIndex;
			if (index != ListBox.NoMatches)
			{
				UpdatingEnabledCheckbox = true;
				EnabledCheckBox.Checked = InstanceData[index].Enabled;
				UpdatingEnabledCheckbox = false;
			}
		}
	}
}
