using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using TGS.Interface;
using TGS.Interface.Components;

namespace TGS.ControlPanel
{
	partial class ControlPanel
	{
		IDictionary<int, string> IndexesToPaths = new Dictionary<int, string>();
		IList<string> EnumeratedPaths = new List<string>() { "" };
		bool enumerating = false;
		/// <summary>
		/// Whether or not the static page has been initialized yet
		/// </summary>
		bool initializedStaticPage = false;

		enum EnumResult
		{
			Enumerated,
			Unauthorized,
			NotEnumerated,
			Error,
		}

		void InitStaticPage()
		{
			if (initializedStaticPage)
				return;
			if(Instance.Administration == null)
				RecreateStaticButton.Visible = false;
			BuildFileList();
			initializedStaticPage = true;
		}

		void BuildFileList()
		{
			enumerating = true;
			IndexesToPaths.Clear();
			StaticFileListBox.Items.Clear();
			IndexesToPaths.Add(StaticFileListBox.Items.Add("/"), "/");
			if (EnumeratePath("", Instance.StaticFiles, 1) == EnumResult.Unauthorized)
			{
				StaticFileListBox.Items[0] += " (UNAUTHORIZED)";
				IndexesToPaths[0] = null;
			}
			StaticFileListBox.SelectedIndex = 0;
			enumerating = false;
		}

		EnumResult EnumeratePath(string path, ITGStatic config, int level)
		{
			if (!EnumeratedPaths.Contains(path))
				return EnumResult.NotEnumerated;
			var Enum = config.ListStaticDirectory(path, out string error, out bool unauthorized);
			if(Enum == null)
			{
				if (unauthorized)
					return EnumResult.Unauthorized;
				else
				{
					MessageBox.Show(String.Format("Could not enumerate static path \"{0}\" error: {1}", path, error));
					return EnumResult.Error;
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
					switch(EnumeratePath(fullpath, config, level + 1))
					{
						case EnumResult.Unauthorized:
							StaticFileListBox.Items[index] += " (UNAUTHORIZED)";
							IndexesToPaths[index] = null;
							break;
						case EnumResult.NotEnumerated:
							StaticFileListBox.Items[index] += " (...)";
							break;
						case EnumResult.Error:
							StaticFileListBox.Items[index] += " (ERROR)";
							IndexesToPaths[index] = null;
							break;
					}
					continue;
				}

				IndexesToPaths.Add(StaticFileListBox.Items.Add(DSNTimes(level) + I), Path.Combine(path, I));
			}
			return EnumResult.Enumerated;
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
			UpdateEditText();
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
				try
				{
					error = Instance.StaticFiles.WriteText(FileName, fileContents, out bool unauthorized);
				}
				catch (Exception ex)
				{
					error = "Failed to read file, most likely due to it being too large. The transfer limit is much higher on non-remote connections. " + ex.ToString();
				}
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
			string text, error;
			try
			{
				text = Instance.StaticFiles.ReadText(remotePath, false, out error, out bool unauthorized);
			}
			catch (Exception ex)
			{
				text = null;
				error = "Failed to read file, most likely due to it being too large. The transfer limit is much higher on non-remote connections. " + ex.ToString();
			}
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
			var res = Instance.StaticFiles.DeleteFile(IndexesToPaths[StaticFileListBox.SelectedIndex], out bool unauthorized);
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
			var config = Instance.StaticFiles;
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
			string res;
			bool unauthorized;
			try
			{
				res = Instance.StaticFiles.WriteText(IndexesToPaths[index], StaticFileEditTextbox.Text, out unauthorized);
			}
			catch (Exception ex)
			{
				unauthorized = false;
				res = "Failed to write file, most likely due to it being too large. The transfer limit is much higher on non-remote connections. " + ex.ToString();
			}
			if (res != null)
			{
				MessageBox.Show("Error: " + res);
				var title = (string)StaticFileListBox.Items[index];
				if (unauthorized && !title.Contains(" (UNAUTHORIZED)"))
					StaticFileListBox.Items[index] = title + " (UNAUTHORIZED)";
			}
			UpdateEditText();
		}

		private void StaticFileListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateEditText();
		}

		void UpdateEditText()
		{
			if (enumerating)
				return;
			var newIndex = StaticFileListBox.SelectedIndex;
			if (newIndex == -1)
				return;
			var path = IndexesToPaths[newIndex];
			var title = (string)StaticFileListBox.Items[newIndex];
			var authed_title = title.Replace(" (UNAUTHORIZED)", "").Replace(" (...)", "");
			if (authed_title[authed_title.Length - 1] == '/')
			{
				StaticFileEditTextbox.ReadOnly = true;
				StaticFileEditTextbox.Text = "Directory";
				if (path != null && path != "/")
				{
					EnumeratedPaths.Add(path);
					BuildFileList();
					enumerating = true;
					StaticFileListBox.SelectedIndex = newIndex;
					enumerating = false;
					return;
				}
			}
			else
			{
				string entry, error;
				bool unauthorized;
				try
				{
					entry = Instance.StaticFiles.ReadText(path, false, out error, out unauthorized);
				}
				catch(Exception e)
				{
					entry = null;
					unauthorized = false;
					error = "Failed to read file, most likely due to it being too large. The transfer limit is much higher on non-remote connections. " + e.ToString();
				}
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
					StaticFileEditTextbox.Text = entry.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", Environment.NewLine);
				}
			}
		}

		private void RecreateStaticButton_Click(object sender, EventArgs e)
		{
			if (!CheckAdminWithWarning())
			{
				RecreateStaticButton.Visible = false;
				return;
			}
			if (MessageBox.Show("This will rename the current static directory to a backup and recreate it. Continue?", "Confirm", MessageBoxButtons.YesNo) != DialogResult.Yes)
				return;
			if (!CheckAdminWithWarning())
			{
				RecreateStaticButton.Visible = false;
				return;
			}
			var res = Instance.Administration.RecreateStaticFolder();
			if (res != null)
				MessageBox.Show(res);
			BuildFileList();
		}
	}
}
