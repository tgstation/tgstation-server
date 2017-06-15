namespace TGControlPanel
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
			this.InstanceListBox = new System.Windows.Forms.ListBox();
			this.NewInstanceButton = new System.Windows.Forms.Button();
			this.DeleteInstanceButton = new System.Windows.Forms.Button();
			this.SelectInstanceButton = new System.Windows.Forms.Button();
			this.RefreshButton = new System.Windows.Forms.Button();
			this.InstanceNameBox = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// InstanceListBox
			// 
			this.InstanceListBox.FormattingEnabled = true;
			this.InstanceListBox.Location = new System.Drawing.Point(12, 12);
			this.InstanceListBox.Name = "InstanceListBox";
			this.InstanceListBox.ScrollAlwaysVisible = true;
			this.InstanceListBox.Size = new System.Drawing.Size(318, 238);
			this.InstanceListBox.TabIndex = 0;
			// 
			// NewInstanceButton
			// 
			this.NewInstanceButton.Location = new System.Drawing.Point(255, 256);
			this.NewInstanceButton.Name = "NewInstanceButton";
			this.NewInstanceButton.Size = new System.Drawing.Size(75, 20);
			this.NewInstanceButton.TabIndex = 1;
			this.NewInstanceButton.Text = "New";
			this.NewInstanceButton.UseVisualStyleBackColor = true;
			this.NewInstanceButton.Click += new System.EventHandler(this.NewInstanceButton_Click);
			// 
			// DeleteInstanceButton
			// 
			this.DeleteInstanceButton.Location = new System.Drawing.Point(93, 283);
			this.DeleteInstanceButton.Name = "DeleteInstanceButton";
			this.DeleteInstanceButton.Size = new System.Drawing.Size(75, 23);
			this.DeleteInstanceButton.TabIndex = 2;
			this.DeleteInstanceButton.Text = "Delete";
			this.DeleteInstanceButton.UseVisualStyleBackColor = true;
			this.DeleteInstanceButton.Click += new System.EventHandler(this.DeleteInstanceButton_Click);
			// 
			// SelectInstanceButton
			// 
			this.SelectInstanceButton.Location = new System.Drawing.Point(255, 283);
			this.SelectInstanceButton.Name = "SelectInstanceButton";
			this.SelectInstanceButton.Size = new System.Drawing.Size(75, 23);
			this.SelectInstanceButton.TabIndex = 3;
			this.SelectInstanceButton.Text = "OK";
			this.SelectInstanceButton.UseVisualStyleBackColor = true;
			this.SelectInstanceButton.Click += new System.EventHandler(this.SelectInstanceButton_Click);
			// 
			// RefreshButton
			// 
			this.RefreshButton.Location = new System.Drawing.Point(12, 283);
			this.RefreshButton.Name = "RefreshButton";
			this.RefreshButton.Size = new System.Drawing.Size(75, 23);
			this.RefreshButton.TabIndex = 4;
			this.RefreshButton.Text = "Refresh";
			this.RefreshButton.UseVisualStyleBackColor = true;
			this.RefreshButton.Click += new System.EventHandler(this.RefreshButton_Click);
			// 
			// InstanceNameBox
			// 
			this.InstanceNameBox.Location = new System.Drawing.Point(12, 256);
			this.InstanceNameBox.Name = "InstanceNameBox";
			this.InstanceNameBox.Size = new System.Drawing.Size(237, 20);
			this.InstanceNameBox.TabIndex = 5;
			// 
			// InstanceSelector
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(40)))), ((int)(((byte)(34)))));
			this.ClientSize = new System.Drawing.Size(341, 316);
			this.Controls.Add(this.InstanceNameBox);
			this.Controls.Add(this.RefreshButton);
			this.Controls.Add(this.SelectInstanceButton);
			this.Controls.Add(this.DeleteInstanceButton);
			this.Controls.Add(this.NewInstanceButton);
			this.Controls.Add(this.InstanceListBox);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
			this.MaximizeBox = false;
			this.Name = "InstanceSelector";
			this.Text = "Select Instance to Administrate";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ListBox InstanceListBox;
		private System.Windows.Forms.Button NewInstanceButton;
		private System.Windows.Forms.Button DeleteInstanceButton;
		private System.Windows.Forms.Button SelectInstanceButton;
		private System.Windows.Forms.Button RefreshButton;
		private System.Windows.Forms.TextBox InstanceNameBox;
	}
}