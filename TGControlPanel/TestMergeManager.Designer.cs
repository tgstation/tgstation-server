namespace TGControlPanel
{
	partial class TestMergeManager
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TestMergeManager));
			this.PullRequestListBox = new System.Windows.Forms.CheckedListBox();
			this.ApplyButton = new System.Windows.Forms.Button();
			this.UpdateToRemoteRadioButton = new System.Windows.Forms.RadioButton();
			this.UpdateToOriginRadioButton = new System.Windows.Forms.RadioButton();
			this.NoUpdateRadioButton = new System.Windows.Forms.RadioButton();
			this.RefreshButton = new System.Windows.Forms.Button();
			this.UpdateActionLabel = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// PullRequestListBox
			// 
			this.PullRequestListBox.FormattingEnabled = true;
			this.PullRequestListBox.Location = new System.Drawing.Point(13, 13);
			this.PullRequestListBox.Name = "PullRequestListBox";
			this.PullRequestListBox.Size = new System.Drawing.Size(588, 199);
			this.PullRequestListBox.TabIndex = 0;
			// 
			// ApplyButton
			// 
			this.ApplyButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.ApplyButton.Location = new System.Drawing.Point(515, 226);
			this.ApplyButton.Name = "ApplyButton";
			this.ApplyButton.Size = new System.Drawing.Size(86, 23);
			this.ApplyButton.TabIndex = 2;
			this.ApplyButton.Text = "Apply";
			this.ApplyButton.UseVisualStyleBackColor = true;
			this.ApplyButton.Click += new System.EventHandler(this.ApplyButton_Click);
			// 
			// UpdateToRemoteRadioButton
			// 
			this.UpdateToRemoteRadioButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.UpdateToRemoteRadioButton.AutoSize = true;
			this.UpdateToRemoteRadioButton.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.UpdateToRemoteRadioButton.Location = new System.Drawing.Point(105, 229);
			this.UpdateToRemoteRadioButton.Name = "UpdateToRemoteRadioButton";
			this.UpdateToRemoteRadioButton.Size = new System.Drawing.Size(116, 17);
			this.UpdateToRemoteRadioButton.TabIndex = 3;
			this.UpdateToRemoteRadioButton.TabStop = true;
			this.UpdateToRemoteRadioButton.Text = "Update To Remote";
			this.UpdateToRemoteRadioButton.UseVisualStyleBackColor = true;
			// 
			// UpdateToOriginRadioButton
			// 
			this.UpdateToOriginRadioButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.UpdateToOriginRadioButton.AutoSize = true;
			this.UpdateToOriginRadioButton.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.UpdateToOriginRadioButton.Location = new System.Drawing.Point(227, 229);
			this.UpdateToOriginRadioButton.Name = "UpdateToOriginRadioButton";
			this.UpdateToOriginRadioButton.Size = new System.Drawing.Size(106, 17);
			this.UpdateToOriginRadioButton.TabIndex = 4;
			this.UpdateToOriginRadioButton.TabStop = true;
			this.UpdateToOriginRadioButton.Text = "Update To Origin";
			this.UpdateToOriginRadioButton.UseVisualStyleBackColor = true;
			// 
			// NoUpdateRadioButton
			// 
			this.NoUpdateRadioButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.NoUpdateRadioButton.AutoSize = true;
			this.NoUpdateRadioButton.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.NoUpdateRadioButton.Location = new System.Drawing.Point(339, 229);
			this.NoUpdateRadioButton.Name = "NoUpdateRadioButton";
			this.NoUpdateRadioButton.Size = new System.Drawing.Size(77, 17);
			this.NoUpdateRadioButton.TabIndex = 5;
			this.NoUpdateRadioButton.TabStop = true;
			this.NoUpdateRadioButton.Text = "No Update";
			this.NoUpdateRadioButton.UseVisualStyleBackColor = true;
			// 
			// RefreshButton
			// 
			this.RefreshButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.RefreshButton.Location = new System.Drawing.Point(422, 226);
			this.RefreshButton.Name = "RefreshButton";
			this.RefreshButton.Size = new System.Drawing.Size(86, 23);
			this.RefreshButton.TabIndex = 6;
			this.RefreshButton.Text = "Refresh";
			this.RefreshButton.UseVisualStyleBackColor = true;
			this.RefreshButton.Click += new System.EventHandler(this.RefreshButton_Click);
			// 
			// UpdateActionLabel
			// 
			this.UpdateActionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.UpdateActionLabel.AutoSize = true;
			this.UpdateActionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.UpdateActionLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.UpdateActionLabel.Location = new System.Drawing.Point(12, 229);
			this.UpdateActionLabel.Name = "UpdateActionLabel";
			this.UpdateActionLabel.Size = new System.Drawing.Size(96, 16);
			this.UpdateActionLabel.TabIndex = 7;
			this.UpdateActionLabel.Text = "Update Action:";
			// 
			// TestMergeManager
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(40)))), ((int)(((byte)(34)))));
			this.ClientSize = new System.Drawing.Size(613, 261);
			this.Controls.Add(this.RefreshButton);
			this.Controls.Add(this.NoUpdateRadioButton);
			this.Controls.Add(this.UpdateToOriginRadioButton);
			this.Controls.Add(this.UpdateToRemoteRadioButton);
			this.Controls.Add(this.ApplyButton);
			this.Controls.Add(this.PullRequestListBox);
			this.Controls.Add(this.UpdateActionLabel);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.Name = "TestMergeManager";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Test Merge Manager";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckedListBox PullRequestListBox;
		private System.Windows.Forms.Button ApplyButton;
		private System.Windows.Forms.RadioButton UpdateToRemoteRadioButton;
		private System.Windows.Forms.RadioButton UpdateToOriginRadioButton;
		private System.Windows.Forms.RadioButton NoUpdateRadioButton;
		private System.Windows.Forms.Button RefreshButton;
		private System.Windows.Forms.Label UpdateActionLabel;
	}
}