namespace TGS.ControlPanel
{
	partial class GitHubLoginPrompt
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
			this.PasswordLabel = new System.Windows.Forms.Label();
			this.UsernameLabel = new System.Windows.Forms.Label();
			this.PasswordTextBox = new System.Windows.Forms.TextBox();
			this.UsernameTextBox = new System.Windows.Forms.TextBox();
			this.DividerLabel = new System.Windows.Forms.Label();
			this.APIKeyLabel = new System.Windows.Forms.Label();
			this.APIKeyTextBox = new System.Windows.Forms.TextBox();
			this.OrLabel = new System.Windows.Forms.Label();
			this.LoginButton = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// PasswordLabel
			// 
			this.PasswordLabel.AutoSize = true;
			this.PasswordLabel.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.PasswordLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.PasswordLabel.Location = new System.Drawing.Point(12, 41);
			this.PasswordLabel.Name = "PasswordLabel";
			this.PasswordLabel.Size = new System.Drawing.Size(92, 18);
			this.PasswordLabel.TabIndex = 25;
			this.PasswordLabel.Text = "Password:";
			this.PasswordLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// UsernameLabel
			// 
			this.UsernameLabel.AutoSize = true;
			this.UsernameLabel.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.UsernameLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.UsernameLabel.Location = new System.Drawing.Point(12, 9);
			this.UsernameLabel.Name = "UsernameLabel";
			this.UsernameLabel.Size = new System.Drawing.Size(97, 18);
			this.UsernameLabel.TabIndex = 24;
			this.UsernameLabel.Text = "Username:";
			this.UsernameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// PasswordTextBox
			// 
			this.PasswordTextBox.Location = new System.Drawing.Point(132, 41);
			this.PasswordTextBox.Name = "PasswordTextBox";
			this.PasswordTextBox.Size = new System.Drawing.Size(233, 20);
			this.PasswordTextBox.TabIndex = 23;
			this.PasswordTextBox.UseSystemPasswordChar = true;
			// 
			// UsernameTextBox
			// 
			this.UsernameTextBox.Location = new System.Drawing.Point(132, 9);
			this.UsernameTextBox.Name = "UsernameTextBox";
			this.UsernameTextBox.Size = new System.Drawing.Size(233, 20);
			this.UsernameTextBox.TabIndex = 22;
			// 
			// DividerLabel
			// 
			this.DividerLabel.AutoSize = true;
			this.DividerLabel.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.DividerLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.DividerLabel.Location = new System.Drawing.Point(-24, 59);
			this.DividerLabel.Name = "DividerLabel";
			this.DividerLabel.Size = new System.Drawing.Size(468, 18);
			this.DividerLabel.TabIndex = 26;
			this.DividerLabel.Text = "______________________________________________";
			this.DividerLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// APIKeyLabel
			// 
			this.APIKeyLabel.AutoSize = true;
			this.APIKeyLabel.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.APIKeyLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.APIKeyLabel.Location = new System.Drawing.Point(12, 95);
			this.APIKeyLabel.Name = "APIKeyLabel";
			this.APIKeyLabel.Size = new System.Drawing.Size(78, 18);
			this.APIKeyLabel.TabIndex = 28;
			this.APIKeyLabel.Text = "API Key:";
			this.APIKeyLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// APIKeyTextBox
			// 
			this.APIKeyTextBox.Location = new System.Drawing.Point(132, 95);
			this.APIKeyTextBox.Name = "APIKeyTextBox";
			this.APIKeyTextBox.Size = new System.Drawing.Size(233, 20);
			this.APIKeyTextBox.TabIndex = 27;
			this.APIKeyTextBox.UseSystemPasswordChar = true;
			// 
			// OrLabel
			// 
			this.OrLabel.AutoSize = true;
			this.OrLabel.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.OrLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.OrLabel.Location = new System.Drawing.Point(157, 64);
			this.OrLabel.Name = "OrLabel";
			this.OrLabel.Size = new System.Drawing.Size(32, 18);
			this.OrLabel.TabIndex = 29;
			this.OrLabel.Text = "OR";
			this.OrLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// LoginButton
			// 
			this.LoginButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.LoginButton.Location = new System.Drawing.Point(145, 133);
			this.LoginButton.Name = "LoginButton";
			this.LoginButton.Size = new System.Drawing.Size(88, 25);
			this.LoginButton.TabIndex = 30;
			this.LoginButton.Text = InitalLoginButtonText;
			this.LoginButton.UseVisualStyleBackColor = true;
			this.LoginButton.Click += new System.EventHandler(this.LoginButton_Click);
			// 
			// GitHubLoginPrompt
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(40)))), ((int)(((byte)(34)))));
			this.ClientSize = new System.Drawing.Size(376, 170);
			this.Controls.Add(this.LoginButton);
			this.Controls.Add(this.OrLabel);
			this.Controls.Add(this.APIKeyLabel);
			this.Controls.Add(this.APIKeyTextBox);
			this.Controls.Add(this.DividerLabel);
			this.Controls.Add(this.PasswordLabel);
			this.Controls.Add(this.UsernameLabel);
			this.Controls.Add(this.PasswordTextBox);
			this.Controls.Add(this.UsernameTextBox);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "GitHubLoginPrompt";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Login To GitHub";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label PasswordLabel;
		private System.Windows.Forms.Label UsernameLabel;
		private System.Windows.Forms.TextBox PasswordTextBox;
		private System.Windows.Forms.TextBox UsernameTextBox;
		private System.Windows.Forms.Label DividerLabel;
		private System.Windows.Forms.Label APIKeyLabel;
		private System.Windows.Forms.TextBox APIKeyTextBox;
		private System.Windows.Forms.Label OrLabel;
		private System.Windows.Forms.Button LoginButton;
	}
}