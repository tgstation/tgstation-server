using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using TGServiceInterface;
using TGServiceInterface.Components;

namespace TGControlPanel
{
	/// <summary>
	/// Form used for managing <see cref="TGServiceInterface.Components.ITGSService"/> <see cref="TGServiceInterface.Components.ITGInstance"/> manipulation functions
	/// </summary>
	partial class InstanceSelector : CountedForm
	{
		/// <summary>
		/// The <see cref="IInterface"/> we build instance connections from
		/// </summary>
		readonly IInterface masterInterface;
		/// <summary>
		/// List of <see cref="InstanceMetadata"/> from <see cref="masterInterface"/>
		/// </summary>
		IList<InstanceMetadata> InstanceData;
		public InstanceSelector(IInterface I)
		{
			InitializeComponent();
			InstanceListBox.MouseDoubleClick += InstanceListBox_MouseDoubleClick;
			masterInterface = I;
			RefreshInstances();
		}

		/// <summary>
		/// Used to wrap <see cref="Interface"/> calls in a non-blocking fashion
		/// </summary>
		/// <param name="action">The <see cref="Interface"/> operation to wrap</param>
		Task WrapServerOp(Action action)
		{
			Enabled = false;
			UseWaitCursor = true;
			try
			{
				return Task.Factory.StartNew(action);
			}
			finally
			{
				Enabled = true;
				UseWaitCursor = false;
			}
		}

		/// <summary>
		/// Connects to a <see cref="TGServiceInterface.Components.ITGInstance"/> if it is double clicked in <see cref="InstanceListBox"/>
		/// </summary>
		/// <param name="sender">The sender of the event</param>
		/// <param name="e">The <see cref="MouseEventArgs"/></param>
		void InstanceListBox_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			var index = InstanceListBox.IndexFromPoint(e.Location);
			if (index != ListBox.NoMatches)
				TryConnectToInstance(InstanceData[index].Name);
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
		}

		/// <summary>
		/// Tries to start a <see cref="ControlPanel"/> for a given <paramref name="instanceName"/>
		/// </summary>
		/// <param name="instanceName">The name of the <see cref="ITGInstance"/> to connect to</param>
		async void TryConnectToInstance(string instanceName)
		{
			if(ControlPanel.InstancesInUse.TryGetValue(instanceName, out ControlPanel activeCP))
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
			await WrapServerOp(() => { res = masterInterface.GetServiceComponent<ITGInstanceManager>().DetachInstance(imd.Name); });
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
			await WrapServerOp(() => { res = masterInterface.GetServiceComponent<ITGInstanceManager>().RenameInstance(imd.Name, new_name); });
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
			await WrapServerOp(() => { res = masterInterface.GetServiceComponent<ITGInstanceManager>().ImportInstance(instance_path); });
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
			await WrapServerOp(() => { res = masterInterface.GetServiceComponent<ITGInstanceManager>().CreateInstance(instance_name, instance_path); });
			if (res != null)
				MessageBox.Show(res);
			RefreshInstances();
		}
	}
}
