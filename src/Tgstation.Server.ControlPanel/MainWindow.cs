using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Tgstation.Server.ControlPanel
{
	public partial class MainWindow : Form
	{
		readonly UserSettings userSettings;

		readonly Dictionary<TreeNode, TreeNodeMouseClickEventHandler> connectionsClickHandlers;

		public MainWindow()
		{
			InitializeComponent();

			instanceBrowser.NodeMouseClick += InstanceBrowser_NodeMouseClick;

			var rootNode = instanceBrowser.Nodes.Add("Server Connections");

			connectionsClickHandlers = new Dictionary<TreeNode, TreeNodeMouseClickEventHandler>
			{
				{ rootNode, InstanceBrowser_RootClick }
			};

			try
			{
				userSettings = JsonConvert.DeserializeObject<UserSettings>(Properties.Settings.Default.SettingsJson);
			}
			catch (JsonSerializationException)
			{
				userSettings = new UserSettings();
			}

			foreach(var I in userSettings.Connections)
			{
				if (I.Password == null)
					connectionsClickHandlers.Add(rootNode.Nodes.Add(I.Url + " (Sign In)"), InstanceBrowser_SignInClick);
				else
					connectionsClickHandlers.Add(rootNode.Nodes.Add(I.Url), InstanceBrowser_ConnectionClick);
			}
		}

		private void InstanceBrowser_ConnectionClick(object sender, TreeNodeMouseClickEventArgs e)
		{
			throw new NotImplementedException();
		}

		private void InstanceBrowser_SignInClick(object sender, TreeNodeMouseClickEventArgs e)
		{
			throw new NotImplementedException();
		}

		private void InstanceBrowser_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e) => connectionsClickHandlers[e.Node](sender, e);

		private void InstanceBrowser_RootClick(object sender, TreeNodeMouseClickEventArgs e)
		{
			if (e.Button != MouseButtons.Right)
				return;
			rootContextMenuStrip.Show(instanceBrowser, new Point(e.X, e.Y));
		}

		private void menuQuit_Click(object sender, EventArgs e) => Application.Exit();

		private void newConnectionToolStripMenuItem_Click(object sender, EventArgs e)
		{

		}
	}
}
