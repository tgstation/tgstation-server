namespace TGS.ControlPanel
{
	partial class Login
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Login));
			this.LocalLoginButton = new System.Windows.Forms.Button();
			this.CurrentRevisionTitle = new System.Windows.Forms.Label();
			this.IPTextBox = new System.Windows.Forms.TextBox();
			this.UsernameTextBox = new System.Windows.Forms.TextBox();
			this.PasswordTextBox = new System.Windows.Forms.TextBox();
			this.RemoteLoginButton = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.PortSelector = new System.Windows.Forms.NumericUpDown();
			this.SavePasswordCheckBox = new System.Windows.Forms.CheckBox();
			((System.ComponentModel.ISupportInitialize)(this.PortSelector)).BeginInit();
			this.SuspendLayout();
			// 
			// LocalLoginButton
			// 
			this.LocalLoginButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.LocalLoginButton.Location = new System.Drawing.Point(102, 12);
			this.LocalLoginButton.Name = "LocalLoginButton";
			this.LocalLoginButton.Size = new System.Drawing.Size(157, 25);
			this.LocalLoginButton.TabIndex = 13;
			this.LocalLoginButton.Text = "Connect to Local Service";
			this.LocalLoginButton.UseVisualStyleBackColor = true;
			this.LocalLoginButton.Click += new System.EventHandler(this.LocalLoginButton_Click);
			// 
			// CurrentRevisionTitle
			// 
			this.CurrentRevisionTitle.AutoSize = true;
			this.CurrentRevisionTitle.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.CurrentRevisionTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.CurrentRevisionTitle.Location = new System.Drawing.Point(116, 69);
			this.CurrentRevisionTitle.Name = "CurrentRevisionTitle";
			this.CurrentRevisionTitle.Size = new System.Drawing.Size(128, 18);
			this.CurrentRevisionTitle.TabIndex = 14;
			this.CurrentRevisionTitle.Text = "Remote Login:";
			this.CurrentRevisionTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// IPTextBox
			// 
			this.IPTextBox.Location = new System.Drawing.Point(129, 99);
			this.IPTextBox.Name = "IPTextBox";
			this.IPTextBox.Size = new System.Drawing.Size(146, 20);
			this.IPTextBox.TabIndex = 15;
			// 
			// UsernameTextBox
			// 
			this.UsernameTextBox.Location = new System.Drawing.Point(129, 132);
			this.UsernameTextBox.Name = "UsernameTextBox";
			this.UsernameTextBox.Size = new System.Drawing.Size(233, 20);
			this.UsernameTextBox.TabIndex = 16;
			// 
			// PasswordTextBox
			// 
			this.PasswordTextBox.Location = new System.Drawing.Point(129, 164);
			this.PasswordTextBox.Name = "PasswordTextBox";
			this.PasswordTextBox.Size = new System.Drawing.Size(233, 20);
			this.PasswordTextBox.TabIndex = 17;
			this.PasswordTextBox.UseSystemPasswordChar = true;
			// 
			// RemoteLoginButton
			// 
			this.RemoteLoginButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.RemoteLoginButton.Location = new System.Drawing.Point(102, 200);
			this.RemoteLoginButton.Name = "RemoteLoginButton";
			this.RemoteLoginButton.Size = new System.Drawing.Size(157, 25);
			this.RemoteLoginButton.TabIndex = 18;
			this.RemoteLoginButton.Text = "Connect to Remote Service";
			this.RemoteLoginButton.UseVisualStyleBackColor = true;
			this.RemoteLoginButton.Click += new System.EventHandler(this.RemoteLoginButton_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.label1.Location = new System.Drawing.Point(9, 99);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(120, 18);
			this.label1.TabIndex = 19;
			this.label1.Text = "Address/Port:";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.label2.Location = new System.Drawing.Point(9, 132);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(97, 18);
			this.label2.TabIndex = 20;
			this.label2.Text = "Username:";
			this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.label3.Location = new System.Drawing.Point(9, 164);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(92, 18);
			this.label3.TabIndex = 21;
			this.label3.Text = "Password:";
			this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.label4.Location = new System.Drawing.Point(-19, 40);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(468, 18);
			this.label4.TabIndex = 22;
			this.label4.Text = "______________________________________________";
			this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// PortSelector
			// 
			this.PortSelector.Location = new System.Drawing.Point(282, 99);
			this.PortSelector.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
			this.PortSelector.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.PortSelector.Name = "PortSelector";
			this.PortSelector.Size = new System.Drawing.Size(80, 20);
			this.PortSelector.TabIndex = 16;
			this.PortSelector.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			// 
			// SavePasswordCheckBox
			// 
			this.SavePasswordCheckBox.AutoSize = true;
			this.SavePasswordCheckBox.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.SavePasswordCheckBox.Location = new System.Drawing.Point(265, 205);
			this.SavePasswordCheckBox.Name = "SavePasswordCheckBox";
			this.SavePasswordCheckBox.Size = new System.Drawing.Size(100, 17);
			this.SavePasswordCheckBox.TabIndex = 23;
			this.SavePasswordCheckBox.Text = "Save Password";
			this.SavePasswordCheckBox.UseVisualStyleBackColor = true;
			this.SavePasswordCheckBox.CheckedChanged += new System.EventHandler(this.SavePasswordCheckBox_CheckedChanged);
			// 
			// Login
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(39)))), ((int)(((byte)(40)))), ((int)(((byte)(34)))));
			this.ClientSize = new System.Drawing.Size(374, 237);
			this.Controls.Add(this.SavePasswordCheckBox);
			this.Controls.Add(this.PortSelector);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.RemoteLoginButton);
			this.Controls.Add(this.PasswordTextBox);
			this.Controls.Add(this.UsernameTextBox);
			this.Controls.Add(this.IPTextBox);
			this.Controls.Add(this.CurrentRevisionTitle);
			this.Controls.Add(this.LocalLoginButton);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.Name = "Login";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Login";
			((System.ComponentModel.ISupportInitialize)(this.PortSelector)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button LocalLoginButton;
		private System.Windows.Forms.Label CurrentRevisionTitle;
		private System.Windows.Forms.TextBox IPTextBox;
		private System.Windows.Forms.TextBox UsernameTextBox;
		private System.Windows.Forms.TextBox PasswordTextBox;
		private System.Windows.Forms.Button RemoteLoginButton;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown PortSelector;
		private System.Windows.Forms.CheckBox SavePasswordCheckBox;
	}
}