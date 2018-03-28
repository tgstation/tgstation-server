namespace TGS.ControlPanel
{
	partial class InstanceSelector
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InstanceSelector));
			this.InstanceListBox = new System.Windows.Forms.ListBox();
			this.CreateInstanceButton = new System.Windows.Forms.Button();
			this.ImportInstanceButton = new System.Windows.Forms.Button();
			this.RenameInstanceButton = new System.Windows.Forms.Button();
			this.DetachInstanceButton = new System.Windows.Forms.Button();
			this.RefreshButton = new System.Windows.Forms.Button();
			this.ConnectButton = new System.Windows.Forms.Button();
			this.EnabledCheckBox = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// InstanceListBox
			// 
			this.InstanceListBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.InstanceListBox.FormattingEnabled = true;
			this.InstanceListBox.Location = new System.Drawing.Point(13, 13);
			this.InstanceListBox.Name = "InstanceListBox";
			this.InstanceListBox.Size = new System.Drawing.Size(339, 238);
			this.InstanceListBox.TabIndex = 0;
			this.InstanceListBox.SelectedIndexChanged += new System.EventHandler(this.InstanceListBox_SelectedIndexChanged);
			// 
			// CreateInstanceButton
			// 
			this.CreateInstanceButton.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.CreateInstanceButton.Location = new System.Drawing.Point(358, 15);
			this.CreateInstanceButton.Name = "CreateInstanceButton";
			this.CreateInstanceButton.Size = new System.Drawing.Size(148, 25);
			this.CreateInstanceButton.TabIndex = 14;
			this.CreateInstanceButton.Text = "Create Instance";
			this.CreateInstanceButton.UseVisualStyleBackColor = true;
			this.CreateInstanceButton.Click += new System.EventHandler(this.CreateInstanceButton_Click);
			// 
			// ImportInstanceButton
			// 
			this.ImportInstanceButton.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.ImportInstanceButton.Location = new System.Drawing.Point(358, 45);
			this.ImportInstanceButton.Name = "ImportInstanceButton";
			this.ImportInstanceButton.Size = new System.Drawing.Size(148, 25);
			this.ImportInstanceButton.TabIndex = 15;
			this.ImportInstanceButton.Text = "Import Instance";
			this.ImportInstanceButton.UseVisualStyleBackColor = true;
			this.ImportInstanceButton.Click += new System.EventHandler(this.ImportInstanceButton_Click);
			// 
			// RenameInstanceButton
			// 
			this.RenameInstanceButton.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.RenameInstanceButton.Location = new System.Drawing.Point(358, 76);
			this.RenameInstanceButton.Name = "RenameInstanceButton";
			this.RenameInstanceButton.Size = new System.Drawing.Size(148, 25);
			this.RenameInstanceButton.TabIndex = 16;
			this.RenameInstanceButton.Text = "Rename Instance";
			this.RenameInstanceButton.UseVisualStyleBackColor = true;
			this.RenameInstanceButton.Click += new System.EventHandler(this.RenameInstanceButton_Click);
			// 
			// DetachInstanceButton
			// 
			this.DetachInstanceButton.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.DetachInstanceButton.Location = new System.Drawing.Point(358, 107);
			this.DetachInstanceButton.Name = "DetachInstanceButton";
			this.DetachInstanceButton.Size = new System.Drawing.Size(148, 25);
			this.DetachInstanceButton.TabIndex = 17;
			this.DetachInstanceButton.Text = "Detach Instance";
			this.DetachInstanceButton.UseVisualStyleBackColor = true;
			this.DetachInstanceButton.Click += new System.EventHandler(this.DetachInstanceButton_Click);
			// 
			// RefreshButton
			// 
			this.RefreshButton.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.RefreshButton.Location = new System.Drawing.Point(358, 195);
			this.RefreshButton.Name = "RefreshButton";
			this.RefreshButton.Size = new System.Drawing.Size(148, 25);
			this.RefreshButton.TabIndex = 19;
			this.RefreshButton.Text = "Refresh";
			this.RefreshButton.UseVisualStyleBackColor = true;
			this.RefreshButton.Click += new System.EventHandler(this.RefreshButton_Click);
			// 
			// ConnectButton
			// 
			this.ConnectButton.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.ConnectButton.Location = new System.Drawing.Point(358, 226);
			this.ConnectButton.Name = "ConnectButton";
			this.ConnectButton.Size = new System.Drawing.Size(148, 25);
			this.ConnectButton.TabIndex = 20;
			this.ConnectButton.Text = "Connect";
			this.ConnectButton.UseVisualStyleBackColor = true;
			this.ConnectButton.Click += new System.EventHandler(this.ConnectButton_Click);
			// 
			// EnabledCheckBox
			// 
			this.EnabledCheckBox.AutoSize = true;
			this.EnabledCheckBox.ForeColor = System.Drawing.Color.White;
			this.EnabledCheckBox.Location = new System.Drawing.Point(396, 149);
			this.EnabledCheckBox.Name = "EnabledCheckBox";
			this.EnabledCheckBox.Size = new System.Drawing.Size(65, 17);
			this.EnabledCheckBox.TabIndex = 21;
			this.EnabledCheckBox.Text = "Enabled";
			this.EnabledCheckBox.UseVisualStyleBackColor = true;
			this.EnabledCheckBox.CheckedChanged += new System.EventHandler(this.EnabledCheckBox_CheckedChanged);
			// 
			// InstanceSelector
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(40)))), ((int)(((byte)(34)))));
			this.ClientSize = new System.Drawing.Size(518, 261);
			this.Controls.Add(this.EnabledCheckBox);
			this.Controls.Add(this.ConnectButton);
			this.Controls.Add(this.RefreshButton);
			this.Controls.Add(this.DetachInstanceButton);
			this.Controls.Add(this.RenameInstanceButton);
			this.Controls.Add(this.ImportInstanceButton);
			this.Controls.Add(this.CreateInstanceButton);
			this.Controls.Add(this.InstanceListBox);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "InstanceSelector";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Server Instances";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ListBox InstanceListBox;
		private System.Windows.Forms.Button CreateInstanceButton;
		private System.Windows.Forms.Button ImportInstanceButton;
		private System.Windows.Forms.Button RenameInstanceButton;
		private System.Windows.Forms.Button DetachInstanceButton;
		private System.Windows.Forms.Button RefreshButton;
		private System.Windows.Forms.Button ConnectButton;
		private System.Windows.Forms.CheckBox EnabledCheckBox;
	}
}