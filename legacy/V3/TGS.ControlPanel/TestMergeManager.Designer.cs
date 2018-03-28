namespace TGS.ControlPanel
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
			this.ApplyingPullRequestsLabel = new System.Windows.Forms.Label();
			this.ApplyingPullRequestsProgressBar = new System.Windows.Forms.ProgressBar();
			this.AddPRButton = new System.Windows.Forms.Button();
			this.AddPRNumericUpDown = new System.Windows.Forms.NumericUpDown();
			((System.ComponentModel.ISupportInitialize)(this.AddPRNumericUpDown)).BeginInit();
			this.SuspendLayout();
			// 
			// PullRequestListBox
			// 
			this.PullRequestListBox.CheckOnClick = true;
			this.PullRequestListBox.FormattingEnabled = true;
			this.PullRequestListBox.Location = new System.Drawing.Point(13, 13);
			this.PullRequestListBox.Name = "PullRequestListBox";
			this.PullRequestListBox.Size = new System.Drawing.Size(588, 199);
			this.PullRequestListBox.TabIndex = 0;
			this.PullRequestListBox.ThreeDCheckBoxes = true;
			this.PullRequestListBox.UseWaitCursor = true;
			// 
			// ApplyButton
			// 
			this.ApplyButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.ApplyButton.Location = new System.Drawing.Point(544, 226);
			this.ApplyButton.Name = "ApplyButton";
			this.ApplyButton.Size = new System.Drawing.Size(57, 23);
			this.ApplyButton.TabIndex = 2;
			this.ApplyButton.Text = "Apply";
			this.ApplyButton.UseVisualStyleBackColor = true;
			this.ApplyButton.UseWaitCursor = true;
			this.ApplyButton.Click += new System.EventHandler(this.ApplyButton_Click);
			// 
			// UpdateToRemoteRadioButton
			// 
			this.UpdateToRemoteRadioButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.UpdateToRemoteRadioButton.AutoSize = true;
			this.UpdateToRemoteRadioButton.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.UpdateToRemoteRadioButton.Location = new System.Drawing.Point(13, 229);
			this.UpdateToRemoteRadioButton.Name = "UpdateToRemoteRadioButton";
			this.UpdateToRemoteRadioButton.Size = new System.Drawing.Size(116, 17);
			this.UpdateToRemoteRadioButton.TabIndex = 3;
			this.UpdateToRemoteRadioButton.TabStop = true;
			this.UpdateToRemoteRadioButton.Text = "Update To Remote";
			this.UpdateToRemoteRadioButton.UseVisualStyleBackColor = true;
			this.UpdateToRemoteRadioButton.UseWaitCursor = true;
			// 
			// UpdateToOriginRadioButton
			// 
			this.UpdateToOriginRadioButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.UpdateToOriginRadioButton.AutoSize = true;
			this.UpdateToOriginRadioButton.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.UpdateToOriginRadioButton.Location = new System.Drawing.Point(135, 229);
			this.UpdateToOriginRadioButton.Name = "UpdateToOriginRadioButton";
			this.UpdateToOriginRadioButton.Size = new System.Drawing.Size(106, 17);
			this.UpdateToOriginRadioButton.TabIndex = 4;
			this.UpdateToOriginRadioButton.TabStop = true;
			this.UpdateToOriginRadioButton.Text = "Update To Origin";
			this.UpdateToOriginRadioButton.UseVisualStyleBackColor = true;
			this.UpdateToOriginRadioButton.UseWaitCursor = true;
			// 
			// NoUpdateRadioButton
			// 
			this.NoUpdateRadioButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.NoUpdateRadioButton.AutoSize = true;
			this.NoUpdateRadioButton.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.NoUpdateRadioButton.Location = new System.Drawing.Point(247, 229);
			this.NoUpdateRadioButton.Name = "NoUpdateRadioButton";
			this.NoUpdateRadioButton.Size = new System.Drawing.Size(77, 17);
			this.NoUpdateRadioButton.TabIndex = 5;
			this.NoUpdateRadioButton.TabStop = true;
			this.NoUpdateRadioButton.Text = "No Update";
			this.NoUpdateRadioButton.UseVisualStyleBackColor = true;
			this.NoUpdateRadioButton.UseWaitCursor = true;
			// 
			// RefreshButton
			// 
			this.RefreshButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.RefreshButton.Location = new System.Drawing.Point(481, 226);
			this.RefreshButton.Name = "RefreshButton";
			this.RefreshButton.Size = new System.Drawing.Size(57, 23);
			this.RefreshButton.TabIndex = 6;
			this.RefreshButton.Text = "Refresh";
			this.RefreshButton.UseVisualStyleBackColor = true;
			this.RefreshButton.UseWaitCursor = true;
			this.RefreshButton.Click += new System.EventHandler(this.RefreshButton_Click);
			// 
			// ApplyingPullRequestsLabel
			// 
			this.ApplyingPullRequestsLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.ApplyingPullRequestsLabel.AutoSize = true;
			this.ApplyingPullRequestsLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.ApplyingPullRequestsLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.ApplyingPullRequestsLabel.Location = new System.Drawing.Point(224, 92);
			this.ApplyingPullRequestsLabel.Name = "ApplyingPullRequestsLabel";
			this.ApplyingPullRequestsLabel.Size = new System.Drawing.Size(156, 16);
			this.ApplyingPullRequestsLabel.TabIndex = 8;
			this.ApplyingPullRequestsLabel.Text = "Applying Pull Requests...";
			this.ApplyingPullRequestsLabel.UseWaitCursor = true;
			this.ApplyingPullRequestsLabel.Visible = false;
			// 
			// ApplyingPullRequestsProgressBar
			// 
			this.ApplyingPullRequestsProgressBar.Location = new System.Drawing.Point(48, 112);
			this.ApplyingPullRequestsProgressBar.Name = "ApplyingPullRequestsProgressBar";
			this.ApplyingPullRequestsProgressBar.Size = new System.Drawing.Size(513, 23);
			this.ApplyingPullRequestsProgressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
			this.ApplyingPullRequestsProgressBar.TabIndex = 9;
			this.ApplyingPullRequestsProgressBar.UseWaitCursor = true;
			this.ApplyingPullRequestsProgressBar.Visible = false;
			// 
			// AddPRButton
			// 
			this.AddPRButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.AddPRButton.Location = new System.Drawing.Point(418, 226);
			this.AddPRButton.Name = "AddPRButton";
			this.AddPRButton.Size = new System.Drawing.Size(57, 23);
			this.AddPRButton.TabIndex = 10;
			this.AddPRButton.Text = "Add PR";
			this.AddPRButton.UseVisualStyleBackColor = true;
			this.AddPRButton.UseWaitCursor = true;
			this.AddPRButton.Click += new System.EventHandler(this.AddPRButton_Click);
			// 
			// AddPRNumericUpDown
			// 
			this.AddPRNumericUpDown.Location = new System.Drawing.Point(331, 229);
			this.AddPRNumericUpDown.Maximum = new decimal(new int[] {
            10000000,
            0,
            0,
            0});
			this.AddPRNumericUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.AddPRNumericUpDown.Name = "AddPRNumericUpDown";
			this.AddPRNumericUpDown.Size = new System.Drawing.Size(81, 20);
			this.AddPRNumericUpDown.TabIndex = 11;
			this.AddPRNumericUpDown.UseWaitCursor = true;
			this.AddPRNumericUpDown.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			// 
			// TestMergeManager
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(40)))), ((int)(((byte)(34)))));
			this.ClientSize = new System.Drawing.Size(613, 261);
			this.Controls.Add(this.AddPRNumericUpDown);
			this.Controls.Add(this.AddPRButton);
			this.Controls.Add(this.ApplyingPullRequestsProgressBar);
			this.Controls.Add(this.ApplyingPullRequestsLabel);
			this.Controls.Add(this.RefreshButton);
			this.Controls.Add(this.NoUpdateRadioButton);
			this.Controls.Add(this.UpdateToOriginRadioButton);
			this.Controls.Add(this.UpdateToRemoteRadioButton);
			this.Controls.Add(this.ApplyButton);
			this.Controls.Add(this.PullRequestListBox);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.Name = "TestMergeManager";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Test Merge Manager";
			this.UseWaitCursor = true;
			((System.ComponentModel.ISupportInitialize)(this.AddPRNumericUpDown)).EndInit();
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
		private System.Windows.Forms.Label ApplyingPullRequestsLabel;
		private System.Windows.Forms.ProgressBar ApplyingPullRequestsProgressBar;
		private System.Windows.Forms.Button AddPRButton;
		private System.Windows.Forms.NumericUpDown AddPRNumericUpDown;
	}
}