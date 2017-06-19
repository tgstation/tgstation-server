﻿using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using TGServiceInterface;

namespace TGControlPanel
{
	public partial class InstanceSelector : Form
	{
		public static int Execute()
		{
			using (var S = new InstanceSelector())
			{
				Application.Run(S);
				return S.SelectedInstance;
			}
		}

		int SelectedInstance = 0;

		public InstanceSelector()
		{
			InitializeComponent();
			InstanceListBox.MouseDoubleClick += InstanceListBox_MouseDoubleClick;
			DoRefresh();
		}

		private void InstanceListBox_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			if (InstanceListBox.IndexFromPoint(e.Location) != ListBox.NoMatches)
				DoSelect();
		}

		private void SelectInstanceButton_Click(object sender, EventArgs e)
		{
			DoSelect();
		}

		private void DeleteInstanceButton_Click(object sender, EventArgs e)
		{
			var test = GetSelectedInstanceID();
			var res = MessageBox.Show("WARNING: This will delete the ENTIRE server instance! Absolutely no backup operation will be performed! Continue?", "Confirm", MessageBoxButtons.YesNo);
			if (res != DialogResult.Yes)
				return;
			res = MessageBox.Show("Are you absolutely sure you're sure? There's no turning back after this!", "Verify", MessageBoxButtons.YesNo);
			if (res != DialogResult.Yes)
				return;

			Service.GetComponent<ITGInstance>(test).Delete();
			Thread.Sleep(1000);
			DoRefresh();
		}

		private void NewInstanceButton_Click(object sender, EventArgs e)
		{
			var Instname = InstanceNameBox.Text;
			if (String.IsNullOrWhiteSpace(Instname)) {
				MessageBox.Show("Please enter a name for the instance");
				return;
			}
			var instpath = Instname;
			foreach (char c in Path.GetInvalidFileNameChars())
				instpath = instpath.Replace(c, '_');

			var ofd = new FolderBrowserDialog()
			{
				Description = "Select the parent directory for the instance",
				ShowNewFolderButton = true
			};
			if (ofd.ShowDialog() != DialogResult.OK)
				return;

			var res = Service.Get().CreateInstance(Instname, ofd.SelectedPath + Path.DirectorySeparatorChar + instpath);
			if (res != null)
				MessageBox.Show(res);
			else
				DoRefresh();
			InstanceNameBox.Text = null;
		}

		private void RefreshButton_Click(object sender, EventArgs e)
		{
			DoRefresh();
		}

		void DoRefresh()
		{
			InstanceListBox.Items.Clear();
			foreach (var I in Service.Get().ListInstances())
				InstanceListBox.Items.Add(String.Format("{0}.\t{1}", I.Key, I.Value));
		}

		int GetSelectedInstanceID()
		{
			return Convert.ToInt32(((string)InstanceListBox.SelectedItem ?? "0").Split('.')[0]);
		}
		
		void DoSelect()
		{
			var test = GetSelectedInstanceID();
			if (test == 0)
				return;
			if (!Service.Get().ListInstances().ContainsKey(test))
			{
				MessageBox.Show("Selected instance no longer exists!");
				DoRefresh();
			}
			else
			{
				SelectedInstance = test;
				Close();
			}
		}
	}
}
