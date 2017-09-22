using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using TGServiceInterface;

namespace TGControlPanel
{
	partial class Main
	{
		IDictionary<int, string> IndexesToPaths = new Dictionary<int, string>();
		void InitStaticPage()
		{
			BuildFileList();
		}

		void BuildFileList()
		{
			IndexesToPaths.Clear();
			StaticFileListBox.Items.Clear();
			IndexesToPaths.Add(StaticFileListBox.Items.Add("/"), "/");
			if (!EnumeratePath("", Server.GetComponent<ITGConfig>(), 1))
			{
				StaticFileListBox.Items[0] += " (UNAUTHORIZED)";
				IndexesToPaths[0] = null;
			}
			StaticFileListBox.SelectedIndex = 0;
		}

		bool EnumeratePath(string path, ITGConfig config, int level)
		{
			var Enum = config.ListStaticDirectory(path, out string error, out bool unauthorized);
			if(Enum == null)
			{
				if (unauthorized)
					return false;
				else
				{
					MessageBox.Show(String.Format("Could not enumerate static path \"{0}\" error: {1}", path, error));
					return true;
				}
			}
			foreach(var I in Enum)
			{
				if(I[0] == '/')
				{
					var dir = I.Remove(0, 1);
					var index =	StaticFileListBox.Items.Add(DSNTimes(level) + dir + '/');
					var fullpath = path + '/' + dir;
					IndexesToPaths.Add(index, fullpath);
					if (!EnumeratePath(fullpath, config, level + 1))
					{
						StaticFileListBox.Items[index] += " (UNAUTHORIZED)";
						IndexesToPaths[index] = null;
					}
					continue;
				}

				IndexesToPaths.Add(StaticFileListBox.Items.Add(DSNTimes(level) + I), Path.Combine(path, I));
			}
			return true;
		}

		string DSNTimes(int n)
		{
			var res = "";
			for (var I = 0; I < n; ++I)
				res += "  ";
			return res;
		}
		private void StaticFilesRefreshButton_Click(object sender, EventArgs e)
		{
			BuildFileList();
		}

		private void StaticFileUploadButton_Click(object sender, EventArgs e)
		{
			if (StaticFileEditTextbox.Text != "Directory")
			{
				MessageBox.Show("Please select a directory to upload the file to.");
				return;
			}
			var ofd = new OpenFileDialog()
			{
				CheckFileExists = true,
				CheckPathExists = true,
				DefaultExt = ".txt",
				Multiselect = false,
				Title = "Static File Upload",
				ValidateNames = true,
				Filter = "All files (*.*)|*.*",
				AddExtension = false,
				SupportMultiDottedExtensions = true,
			};
			if (ofd.ShowDialog() != DialogResult.OK)
				return;

			var fileToUpload = ofd.FileName;

			var FileName = Path.Combine(IndexesToPaths[StaticFileListBox.SelectedIndex], Path.GetFileName(fileToUpload));

			string fileContents = null;
			string error = null;
			try
			{
				fileContents = File.ReadAllText(fileToUpload);
			}
			catch (Exception ex)
			{
				error = ex.ToString();
			}
			if (error == null)
				error = Server.GetComponent<ITGConfig>().WriteText(FileName, fileContents, out bool unauthorized);
			if (error != null)
				MessageBox.Show("An error occurred: " + error);
			BuildFileList();
		}

		private void StaticFileDownloadButton_Click(object sender, EventArgs e)
		{
			if (StaticFileEditTextbox.ReadOnly)
			{
				MessageBox.Show("Cannot download this file!");
				return;
			}
			var remotePath = IndexesToPaths[StaticFileListBox.SelectedIndex];
			if (remotePath == null)
				return;
			var text = Server.GetComponent<ITGConfig>().ReadText(remotePath, false, out string error, out bool unauthorized);
			if (text != null)
			{
				var ofd = new SaveFileDialog()
				{
					CheckFileExists = false,
					CheckPathExists = true,
					DefaultExt = ".txt",
					Title = "Static File Download",
					ValidateNames = true,
					Filter = "All files (*.*)|*.*",
					AddExtension = false,
					CreatePrompt = false,
					OverwritePrompt = true,
					SupportMultiDottedExtensions = true,
				};
				if (ofd.ShowDialog() != DialogResult.OK)
					return;

				try
				{
					File.WriteAllText(ofd.FileName, text);
					return;
				}
				catch (Exception ex)
				{
					error = ex.ToString();
				}
			}
			MessageBox.Show("An error occurred: " + error);
		}

		private void StaticFileDeleteButton_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show("Are you sure you want to delete " + ((string)StaticFileListBox.SelectedItem).Trim() + "?", "Confirm", MessageBoxButtons.YesNo) != DialogResult.Yes)
				return;
			var res = Server.GetComponent<ITGConfig>().DeleteFile(IndexesToPaths[StaticFileListBox.SelectedIndex], out bool unauthorized);
			if (res != null)
				MessageBox.Show(res);
			BuildFileList();
		}

		private void StaticFileCreateButton_Click(object sender, EventArgs e)
		{
			if(StaticFileEditTextbox.Text != "Directory")
			{
				MessageBox.Show("Please select a directory to create the file in.");
				return;
			}
			var FileName = Program.TextPrompt("Static File/Directory Creation", "Enter the name of the file/directory:");
			if (FileName == null)
				return;

			var resu = MessageBox.Show("Is this the name of a directory?", "Directory", MessageBoxButtons.YesNoCancel);
			if (resu == DialogResult.Cancel)
				return;
			var FullFileName = Path.Combine(IndexesToPaths[StaticFileListBox.SelectedIndex], FileName);
			if (resu == DialogResult.Yes)
				FullFileName = Path.Combine(FullFileName, "__TGS3_CP_DIRECTORY_CREATOR__");
			var config = Server.GetComponent<ITGConfig>();
			var res = config.WriteText(FullFileName, "", out bool unauthorized);
			if (res != null)
				MessageBox.Show(res);
			if (resu == DialogResult.Yes)
			{
				FullFileName = Path.Combine(FullFileName, "__TGS3_CP_DIRECTORY_CREATOR__");
				config.DeleteFile(FullFileName, out unauthorized);	//don't care about this
			}
			BuildFileList();
		}

		private void StaticFileSaveButton_Click(object sender, EventArgs e)
		{
			var index = StaticFileListBox.SelectedIndex;
			var res = Server.GetComponent<ITGConfig>().WriteText(IndexesToPaths[index], StaticFileEditTextbox.Text, out bool unauthorized);
			if (res != null)
			{
				MessageBox.Show("Error: " + res);
				var title = (string)StaticFileListBox.Items[index];
				if (unauthorized && !title.Contains(" (UNAUTHORIZED)"))
					StaticFileListBox.Items[index] = title + " (UNAUTHORIZED)";
			}
		}

		private void StaticFileListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateEditText();
		}

		void UpdateEditText()
		{
			var newIndex = StaticFileListBox.SelectedIndex;
			var path = IndexesToPaths[newIndex];
			var title = (string)StaticFileListBox.Items[newIndex];
			var authed_title = title.Replace(" (UNAUTHORIZED)", "");
			if (authed_title[authed_title.Length - 1] == '/')
			{
				StaticFileEditTextbox.ReadOnly = true;
				StaticFileEditTextbox.Text = "Directory";
			}
			else
			{
				var entry = Server.GetComponent<ITGConfig>().ReadText(path, false, out string error, out bool unauthorized);
				if (entry == null)
				{
					StaticFileEditTextbox.ReadOnly = true;
					StaticFileEditTextbox.Text = "ERROR: " + error;
					if (unauthorized && !title.Contains(" (UNAUTHORIZED)"))
						StaticFileListBox.Items[newIndex] = title + " (UNAUTHORIZED)";
				}
				else
				{
					StaticFileEditTextbox.ReadOnly = false;
					StaticFileEditTextbox.Text = entry;
				}
			}
		}
	}
}
