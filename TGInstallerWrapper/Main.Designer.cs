namespace TGInstallerWrapper
{
	partial class Main
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
			this.ProgressBar = new System.Windows.Forms.ProgressBar();
			this.DeskShortcutsCheckbox = new System.Windows.Forms.CheckBox();
			this.StartShortcutsCheckbox = new System.Windows.Forms.CheckBox();
			this.InstallingLabel = new System.Windows.Forms.Label();
			this.PathTextBox = new System.Windows.Forms.TextBox();
			this.SelectPathButton = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.VersionLabel = new System.Windows.Forms.Label();
			this.InstallButton = new System.Windows.Forms.Button();
			this.ShowLogCheckbox = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// ProgressBar
			// 
			this.ProgressBar.Location = new System.Drawing.Point(11, 131);
			this.ProgressBar.Name = "ProgressBar";
			this.ProgressBar.Size = new System.Drawing.Size(513, 24);
			this.ProgressBar.TabIndex = 0;
			// 
			// DeskShortcutsCheckbox
			// 
			this.DeskShortcutsCheckbox.AutoSize = true;
			this.DeskShortcutsCheckbox.Checked = true;
			this.DeskShortcutsCheckbox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.DeskShortcutsCheckbox.Font = new System.Drawing.Font("Verdana", 12F);
			this.DeskShortcutsCheckbox.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.DeskShortcutsCheckbox.Location = new System.Drawing.Point(12, 61);
			this.DeskShortcutsCheckbox.Name = "DeskShortcutsCheckbox";
			this.DeskShortcutsCheckbox.Size = new System.Drawing.Size(214, 22);
			this.DeskShortcutsCheckbox.TabIndex = 2;
			this.DeskShortcutsCheckbox.Text = "Add Desktop Shortcuts";
			this.DeskShortcutsCheckbox.UseVisualStyleBackColor = true;
			// 
			// StartShortcutsCheckbox
			// 
			this.StartShortcutsCheckbox.AutoSize = true;
			this.StartShortcutsCheckbox.Checked = true;
			this.StartShortcutsCheckbox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.StartShortcutsCheckbox.Font = new System.Drawing.Font("Verdana", 12F);
			this.StartShortcutsCheckbox.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.StartShortcutsCheckbox.Location = new System.Drawing.Point(289, 61);
			this.StartShortcutsCheckbox.Name = "StartShortcutsCheckbox";
			this.StartShortcutsCheckbox.Size = new System.Drawing.Size(236, 22);
			this.StartShortcutsCheckbox.TabIndex = 3;
			this.StartShortcutsCheckbox.Text = "Add Start Menu Shortcuts";
			this.StartShortcutsCheckbox.UseVisualStyleBackColor = true;
			// 
			// InstallingLabel
			// 
			this.InstallingLabel.AutoSize = true;
			this.InstallingLabel.Font = new System.Drawing.Font("Verdana", 12F);
			this.InstallingLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.InstallingLabel.Location = new System.Drawing.Point(8, 37);
			this.InstallingLabel.Name = "InstallingLabel";
			this.InstallingLabel.Size = new System.Drawing.Size(52, 18);
			this.InstallingLabel.TabIndex = 1;
			this.InstallingLabel.Text = "Path:";
			// 
			// PathTextBox
			// 
			this.PathTextBox.Location = new System.Drawing.Point(66, 35);
			this.PathTextBox.Name = "PathTextBox";
			this.PathTextBox.ReadOnly = true;
			this.PathTextBox.Size = new System.Drawing.Size(413, 20);
			this.PathTextBox.TabIndex = 4;
			// 
			// SelectPathButton
			// 
			this.SelectPathButton.Location = new System.Drawing.Point(485, 35);
			this.SelectPathButton.Name = "SelectPathButton";
			this.SelectPathButton.Size = new System.Drawing.Size(35, 20);
			this.SelectPathButton.TabIndex = 6;
			this.SelectPathButton.Text = "...";
			this.SelectPathButton.UseVisualStyleBackColor = true;
			this.SelectPathButton.Click += new System.EventHandler(this.SelectPathButton_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Verdana", 12F);
			this.label1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.label1.Location = new System.Drawing.Point(8, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(155, 18);
			this.label1.TabIndex = 7;
			this.label1.Text = "Detected Version:";
			// 
			// VersionLabel
			// 
			this.VersionLabel.AutoSize = true;
			this.VersionLabel.Font = new System.Drawing.Font("Verdana", 12F);
			this.VersionLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.VersionLabel.Location = new System.Drawing.Point(169, 9);
			this.VersionLabel.Name = "VersionLabel";
			this.VersionLabel.Size = new System.Drawing.Size(228, 18);
			this.VersionLabel.TabIndex = 8;
			this.VersionLabel.Text = "None (No Service Running)";
			// 
			// InstallButton
			// 
			this.InstallButton.Location = new System.Drawing.Point(140, 89);
			this.InstallButton.Name = "InstallButton";
			this.InstallButton.Size = new System.Drawing.Size(127, 23);
			this.InstallButton.TabIndex = 9;
			this.InstallButton.Text = "Install";
			this.InstallButton.UseVisualStyleBackColor = true;
			this.InstallButton.Click += new System.EventHandler(this.InstallButton_Click);
			// 
			// ShowLogCheckbox
			// 
			this.ShowLogCheckbox.AutoSize = true;
			this.ShowLogCheckbox.Font = new System.Drawing.Font("Verdana", 12F);
			this.ShowLogCheckbox.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.ShowLogCheckbox.Location = new System.Drawing.Point(289, 90);
			this.ShowLogCheckbox.Name = "ShowLogCheckbox";
			this.ShowLogCheckbox.Size = new System.Drawing.Size(105, 22);
			this.ShowLogCheckbox.TabIndex = 10;
			this.ShowLogCheckbox.Text = "Show Log";
			this.ShowLogCheckbox.UseVisualStyleBackColor = true;
			// 
			// Main
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(40)))), ((int)(((byte)(34)))));
			this.ClientSize = new System.Drawing.Size(537, 167);
			this.Controls.Add(this.ShowLogCheckbox);
			this.Controls.Add(this.InstallButton);
			this.Controls.Add(this.VersionLabel);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.SelectPathButton);
			this.Controls.Add(this.PathTextBox);
			this.Controls.Add(this.StartShortcutsCheckbox);
			this.Controls.Add(this.DeskShortcutsCheckbox);
			this.Controls.Add(this.InstallingLabel);
			this.Controls.Add(this.ProgressBar);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "Main";
			this.Text = "/tg/station Server Installer";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ProgressBar ProgressBar;
		private System.Windows.Forms.CheckBox DeskShortcutsCheckbox;
		private System.Windows.Forms.CheckBox StartShortcutsCheckbox;
		private System.Windows.Forms.Label InstallingLabel;
		private System.Windows.Forms.TextBox PathTextBox;
		private System.Windows.Forms.Button SelectPathButton;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label VersionLabel;
		private System.Windows.Forms.Button InstallButton;
		private System.Windows.Forms.CheckBox ShowLogCheckbox;
	}
}

