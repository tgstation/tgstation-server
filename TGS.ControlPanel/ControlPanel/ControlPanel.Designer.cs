namespace TGS.ControlPanel
{
	partial class ControlPanel
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
				Cleanup();
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ControlPanel));
			this.RepoBGW = new System.ComponentModel.BackgroundWorker();
			this.ServerStartBGW = new System.ComponentModel.BackgroundWorker();
			this.ChatPanel = new System.Windows.Forms.TabPage();
			this.label5 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.IRCModesComboBox = new System.Windows.Forms.ComboBox();
			this.label1 = new System.Windows.Forms.Label();
			this.AdminChannelsTextbox = new System.Windows.Forms.TextBox();
			this.WDChannelsTextbox = new System.Windows.Forms.TextBox();
			this.GameChannelsTextbox = new System.Windows.Forms.TextBox();
			this.DevChannelsTextbox = new System.Windows.Forms.TextBox();
			this.ChatNicknameText = new System.Windows.Forms.TextBox();
			this.ChatServerText = new System.Windows.Forms.TextBox();
			this.AuthField2 = new System.Windows.Forms.TextBox();
			this.AuthField1 = new System.Windows.Forms.TextBox();
			this.ChatAdminsTextBox = new System.Windows.Forms.TextBox();
			this.ChatRefreshButton = new System.Windows.Forms.Button();
			this.ChatNicknameTitle = new System.Windows.Forms.Label();
			this.ChatPortSelector = new System.Windows.Forms.NumericUpDown();
			this.ChatPortTitle = new System.Windows.Forms.Label();
			this.ChatServerTitle = new System.Windows.Forms.Label();
			this.ChatApplyButton = new System.Windows.Forms.Button();
			this.AuthField2Title = new System.Windows.Forms.Label();
			this.AuthField1Title = new System.Windows.Forms.Label();
			this.ChatReconnectButton = new System.Windows.Forms.Button();
			this.ChatStatusLabel = new System.Windows.Forms.Label();
			this.ChatStatusTitle = new System.Windows.Forms.Label();
			this.ChatEnabledCheckbox = new System.Windows.Forms.CheckBox();
			this.ChatProviderSelectorPanel = new System.Windows.Forms.Panel();
			this.DiscordProviderSwitch = new System.Windows.Forms.RadioButton();
			this.IRCProviderSwitch = new System.Windows.Forms.RadioButton();
			this.ChatProviderTitle = new System.Windows.Forms.Label();
			this.ChannelsTitle = new System.Windows.Forms.Label();
			this.ChatAdminsTitle = new System.Windows.Forms.Label();
			this.panel1 = new System.Windows.Forms.Panel();
			this.AdminModeSpecial = new System.Windows.Forms.RadioButton();
			this.AdminModeNormal = new System.Windows.Forms.RadioButton();
			this.ServerPanel = new System.Windows.Forms.TabPage();
			this.AutoUpdateMLabel = new System.Windows.Forms.Label();
			this.AutoUpdateInterval = new System.Windows.Forms.NumericUpDown();
			this.AutoUpdateCheckbox = new System.Windows.Forms.CheckBox();
			this.ServerPageRefreshButton = new System.Windows.Forms.Button();
			this.AutostartCheckbox = new System.Windows.Forms.CheckBox();
			this.WebclientCheckBox = new System.Windows.Forms.CheckBox();
			this.WorldAnnounceButton = new System.Windows.Forms.Button();
			this.WorldAnnounceField = new System.Windows.Forms.TextBox();
			this.projectNameText = new System.Windows.Forms.TextBox();
			this.WorldAnnounceLabel = new System.Windows.Forms.Label();
			this.SecuritySelector = new System.Windows.Forms.ComboBox();
			this.SecurityTitle = new System.Windows.Forms.Label();
			this.RemoveAllTestMergesButton = new System.Windows.Forms.Button();
			this.ServerPathLabel = new System.Windows.Forms.Label();
			this.CompileCancelButton = new System.Windows.Forms.Button();
			this.ProjectPathLabel = new System.Windows.Forms.Label();
			this.PortLabel = new System.Windows.Forms.Label();
			this.PortSelector = new System.Windows.Forms.NumericUpDown();
			this.TestMergeManagerButton = new System.Windows.Forms.Button();
			this.UpdateServerButton = new System.Windows.Forms.Button();
			this.ServerGRestartButton = new System.Windows.Forms.Button();
			this.ServerGStopButton = new System.Windows.Forms.CheckBox();
			this.ServerRestartButton = new System.Windows.Forms.Button();
			this.ServerStopButton = new System.Windows.Forms.Button();
			this.ServerStartButton = new System.Windows.Forms.Button();
			this.CompilerStatusLabel = new System.Windows.Forms.Label();
			this.CompilerLabel = new System.Windows.Forms.Label();
			this.compileButton = new System.Windows.Forms.Button();
			this.initializeButton = new System.Windows.Forms.Button();
			this.ServerStatusLabel = new System.Windows.Forms.Label();
			this.ServerStatusTitle = new System.Windows.Forms.Label();
			this.BYONDPanel = new System.Windows.Forms.TabPage();
			this.BYONDRefreshButton = new System.Windows.Forms.Button();
			this.LatestVersionLabel = new System.Windows.Forms.Label();
			this.LatestVersionTitle = new System.Windows.Forms.Label();
			this.StagedVersionLabel = new System.Windows.Forms.Label();
			this.StagedVersionTitle = new System.Windows.Forms.Label();
			this.StatusLabel = new System.Windows.Forms.Label();
			this.VersionLabel = new System.Windows.Forms.Label();
			this.VersionTitle = new System.Windows.Forms.Label();
			this.MinorVersionLabel = new System.Windows.Forms.Label();
			this.MajorVersionLabel = new System.Windows.Forms.Label();
			this.UpdateButton = new System.Windows.Forms.Button();
			this.MinorVersionNumeric = new System.Windows.Forms.NumericUpDown();
			this.MajorVersionNumeric = new System.Windows.Forms.NumericUpDown();
			this.RepoPanel = new System.Windows.Forms.TabPage();
			this.SyncCommitsCheckBox = new System.Windows.Forms.CheckBox();
			this.TGSJsonUpdate = new System.Windows.Forms.Button();
			this.RepoRefreshButton = new System.Windows.Forms.Button();
			this.BackupTagsList = new System.Windows.Forms.ListBox();
			this.ResetRemote = new System.Windows.Forms.Button();
			this.RecloneButton = new System.Windows.Forms.Button();
			this.RepoBranchTextBox = new System.Windows.Forms.TextBox();
			this.RepoRemoteTextBox = new System.Windows.Forms.TextBox();
			this.RepoGenChangelogButton = new System.Windows.Forms.Button();
			this.TestmergeSelector = new System.Windows.Forms.NumericUpDown();
			this.TestMergeListLabel = new System.Windows.Forms.ListBox();
			this.CurrentRevisionLabel = new System.Windows.Forms.Label();
			this.RepoApplyButton = new System.Windows.Forms.Button();
			this.HardReset = new System.Windows.Forms.Button();
			this.UpdateRepoButton = new System.Windows.Forms.Button();
			this.MergePRButton = new System.Windows.Forms.Button();
			this.IdentityLabel = new System.Windows.Forms.Label();
			this.TestMergeListTitle = new System.Windows.Forms.Label();
			this.RemoteNameTitle = new System.Windows.Forms.Label();
			this.BranchNameTitle = new System.Windows.Forms.Label();
			this.CurrentRevisionTitle = new System.Windows.Forms.Label();
			this.CloneRepositoryButton = new System.Windows.Forms.Button();
			this.RepoProgressBarLabel = new System.Windows.Forms.Label();
			this.RepoProgressBar = new System.Windows.Forms.ProgressBar();
			this.Panels = new System.Windows.Forms.TabControl();
			this.StaticPanel = new System.Windows.Forms.TabPage();
			this.RecreateStaticButton = new System.Windows.Forms.Button();
			this.StaticFileDownloadButton = new System.Windows.Forms.Button();
			this.StaticFilesRefreshButton = new System.Windows.Forms.Button();
			this.StaticFileUploadButton = new System.Windows.Forms.Button();
			this.StaticFileEditTextbox = new System.Windows.Forms.TextBox();
			this.StaticFileDeleteButton = new System.Windows.Forms.Button();
			this.StaticFileSaveButton = new System.Windows.Forms.Button();
			this.StaticFileCreateButton = new System.Windows.Forms.Button();
			this.label6 = new System.Windows.Forms.Label();
			this.StaticFileListBox = new System.Windows.Forms.ListBox();
			this.ChatPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.ChatPortSelector)).BeginInit();
			this.ChatProviderSelectorPanel.SuspendLayout();
			this.panel1.SuspendLayout();
			this.ServerPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.AutoUpdateInterval)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.PortSelector)).BeginInit();
			this.BYONDPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.MinorVersionNumeric)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.MajorVersionNumeric)).BeginInit();
			this.RepoPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.TestmergeSelector)).BeginInit();
			this.Panels.SuspendLayout();
			this.StaticPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// RepoBGW
			// 
			this.RepoBGW.WorkerReportsProgress = true;
			this.RepoBGW.WorkerSupportsCancellation = true;
			// 
			// ServerStartBGW
			// 
			this.ServerStartBGW.DoWork += new System.ComponentModel.DoWorkEventHandler(this.ServerStartBGW_DoWork);
			// 
			// ChatPanel
			// 
			this.ChatPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(133)))), ((int)(((byte)(143)))), ((int)(((byte)(160)))));
			this.ChatPanel.Controls.Add(this.label5);
			this.ChatPanel.Controls.Add(this.label4);
			this.ChatPanel.Controls.Add(this.label3);
			this.ChatPanel.Controls.Add(this.label2);
			this.ChatPanel.Controls.Add(this.IRCModesComboBox);
			this.ChatPanel.Controls.Add(this.label1);
			this.ChatPanel.Controls.Add(this.AdminChannelsTextbox);
			this.ChatPanel.Controls.Add(this.WDChannelsTextbox);
			this.ChatPanel.Controls.Add(this.GameChannelsTextbox);
			this.ChatPanel.Controls.Add(this.DevChannelsTextbox);
			this.ChatPanel.Controls.Add(this.ChatNicknameText);
			this.ChatPanel.Controls.Add(this.ChatServerText);
			this.ChatPanel.Controls.Add(this.AuthField2);
			this.ChatPanel.Controls.Add(this.AuthField1);
			this.ChatPanel.Controls.Add(this.ChatAdminsTextBox);
			this.ChatPanel.Controls.Add(this.ChatRefreshButton);
			this.ChatPanel.Controls.Add(this.ChatNicknameTitle);
			this.ChatPanel.Controls.Add(this.ChatPortSelector);
			this.ChatPanel.Controls.Add(this.ChatPortTitle);
			this.ChatPanel.Controls.Add(this.ChatServerTitle);
			this.ChatPanel.Controls.Add(this.ChatApplyButton);
			this.ChatPanel.Controls.Add(this.AuthField2Title);
			this.ChatPanel.Controls.Add(this.AuthField1Title);
			this.ChatPanel.Controls.Add(this.ChatReconnectButton);
			this.ChatPanel.Controls.Add(this.ChatStatusLabel);
			this.ChatPanel.Controls.Add(this.ChatStatusTitle);
			this.ChatPanel.Controls.Add(this.ChatEnabledCheckbox);
			this.ChatPanel.Controls.Add(this.ChatProviderSelectorPanel);
			this.ChatPanel.Controls.Add(this.ChatProviderTitle);
			this.ChatPanel.Controls.Add(this.ChannelsTitle);
			this.ChatPanel.Controls.Add(this.ChatAdminsTitle);
			this.ChatPanel.Controls.Add(this.panel1);
			this.ChatPanel.Location = new System.Drawing.Point(4, 22);
			this.ChatPanel.Name = "ChatPanel";
			this.ChatPanel.Padding = new System.Windows.Forms.Padding(3);
			this.ChatPanel.Size = new System.Drawing.Size(868, 366);
			this.ChatPanel.TabIndex = 4;
			this.ChatPanel.Text = "Chat";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label5.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.label5.Location = new System.Drawing.Point(686, 219);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(148, 18);
			this.label5.TabIndex = 43;
			this.label5.Text = "Game Messages:";
			this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.label4.Location = new System.Drawing.Point(516, 219);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(136, 18);
			this.label4.TabIndex = 42;
			this.label4.Text = "Developer Info:";
			this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.label3.Location = new System.Drawing.Point(686, 61);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(135, 18);
			this.label3.TabIndex = 41;
			this.label3.Text = "Administration:";
			this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.label2.Location = new System.Drawing.Point(516, 61);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(153, 18);
			this.label2.TabIndex = 40;
			this.label2.Text = "Server Watchdog:";
			this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// IRCModesComboBox
			// 
			this.IRCModesComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.IRCModesComboBox.FormattingEnabled = true;
			this.IRCModesComboBox.Items.AddRange(new object[] {
            "Voice - +",
            "Halfop - %",
            "Op - @",
            "Owner - ~"});
			this.IRCModesComboBox.Location = new System.Drawing.Point(369, 170);
			this.IRCModesComboBox.Name = "IRCModesComboBox";
			this.IRCModesComboBox.Size = new System.Drawing.Size(141, 21);
			this.IRCModesComboBox.TabIndex = 38;
			this.IRCModesComboBox.Visible = false;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.label1.Location = new System.Drawing.Point(30, 174);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(110, 18);
			this.label1.TabIndex = 39;
			this.label1.Text = "Admin Auth:";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// AdminChannelsTextbox
			// 
			this.AdminChannelsTextbox.Location = new System.Drawing.Point(689, 82);
			this.AdminChannelsTextbox.Multiline = true;
			this.AdminChannelsTextbox.Name = "AdminChannelsTextbox";
			this.AdminChannelsTextbox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.AdminChannelsTextbox.Size = new System.Drawing.Size(167, 120);
			this.AdminChannelsTextbox.TabIndex = 37;
			// 
			// WDChannelsTextbox
			// 
			this.WDChannelsTextbox.Location = new System.Drawing.Point(516, 82);
			this.WDChannelsTextbox.Multiline = true;
			this.WDChannelsTextbox.Name = "WDChannelsTextbox";
			this.WDChannelsTextbox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.WDChannelsTextbox.Size = new System.Drawing.Size(167, 120);
			this.WDChannelsTextbox.TabIndex = 36;
			// 
			// GameChannelsTextbox
			// 
			this.GameChannelsTextbox.Location = new System.Drawing.Point(689, 240);
			this.GameChannelsTextbox.Multiline = true;
			this.GameChannelsTextbox.Name = "GameChannelsTextbox";
			this.GameChannelsTextbox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.GameChannelsTextbox.Size = new System.Drawing.Size(167, 120);
			this.GameChannelsTextbox.TabIndex = 35;
			// 
			// DevChannelsTextbox
			// 
			this.DevChannelsTextbox.Location = new System.Drawing.Point(516, 240);
			this.DevChannelsTextbox.Multiline = true;
			this.DevChannelsTextbox.Name = "DevChannelsTextbox";
			this.DevChannelsTextbox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.DevChannelsTextbox.Size = new System.Drawing.Size(167, 120);
			this.DevChannelsTextbox.TabIndex = 34;
			// 
			// ChatNicknameText
			// 
			this.ChatNicknameText.Location = new System.Drawing.Point(174, 251);
			this.ChatNicknameText.Name = "ChatNicknameText";
			this.ChatNicknameText.Size = new System.Drawing.Size(151, 20);
			this.ChatNicknameText.TabIndex = 31;
			// 
			// ChatServerText
			// 
			this.ChatServerText.Location = new System.Drawing.Point(174, 199);
			this.ChatServerText.Name = "ChatServerText";
			this.ChatServerText.Size = new System.Drawing.Size(151, 20);
			this.ChatServerText.TabIndex = 27;
			// 
			// AuthField2
			// 
			this.AuthField2.Location = new System.Drawing.Point(174, 309);
			this.AuthField2.Name = "AuthField2";
			this.AuthField2.Size = new System.Drawing.Size(151, 20);
			this.AuthField2.TabIndex = 22;
			// 
			// AuthField1
			// 
			this.AuthField1.Location = new System.Drawing.Point(174, 283);
			this.AuthField1.Name = "AuthField1";
			this.AuthField1.Size = new System.Drawing.Size(151, 20);
			this.AuthField1.TabIndex = 21;
			// 
			// ChatAdminsTextBox
			// 
			this.ChatAdminsTextBox.Location = new System.Drawing.Point(369, 49);
			this.ChatAdminsTextBox.Multiline = true;
			this.ChatAdminsTextBox.Name = "ChatAdminsTextBox";
			this.ChatAdminsTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.ChatAdminsTextBox.Size = new System.Drawing.Size(141, 311);
			this.ChatAdminsTextBox.TabIndex = 10;
			// 
			// ChatRefreshButton
			// 
			this.ChatRefreshButton.Location = new System.Drawing.Point(174, 335);
			this.ChatRefreshButton.Name = "ChatRefreshButton";
			this.ChatRefreshButton.Size = new System.Drawing.Size(112, 25);
			this.ChatRefreshButton.TabIndex = 32;
			this.ChatRefreshButton.Text = "Refresh";
			this.ChatRefreshButton.UseVisualStyleBackColor = true;
			this.ChatRefreshButton.Click += new System.EventHandler(this.ChatRefreshButton_Click);
			// 
			// ChatNicknameTitle
			// 
			this.ChatNicknameTitle.AutoSize = true;
			this.ChatNicknameTitle.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.ChatNicknameTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.ChatNicknameTitle.Location = new System.Drawing.Point(30, 250);
			this.ChatNicknameTitle.Name = "ChatNicknameTitle";
			this.ChatNicknameTitle.Size = new System.Drawing.Size(94, 18);
			this.ChatNicknameTitle.TabIndex = 30;
			this.ChatNicknameTitle.Text = "Nickname:";
			this.ChatNicknameTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// ChatPortSelector
			// 
			this.ChatPortSelector.Location = new System.Drawing.Point(174, 225);
			this.ChatPortSelector.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
			this.ChatPortSelector.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.ChatPortSelector.Name = "ChatPortSelector";
			this.ChatPortSelector.Size = new System.Drawing.Size(151, 20);
			this.ChatPortSelector.TabIndex = 29;
			this.ChatPortSelector.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			// 
			// ChatPortTitle
			// 
			this.ChatPortTitle.AutoSize = true;
			this.ChatPortTitle.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.ChatPortTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.ChatPortTitle.Location = new System.Drawing.Point(30, 227);
			this.ChatPortTitle.Name = "ChatPortTitle";
			this.ChatPortTitle.Size = new System.Drawing.Size(48, 18);
			this.ChatPortTitle.TabIndex = 28;
			this.ChatPortTitle.Text = "Port:";
			this.ChatPortTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// ChatServerTitle
			// 
			this.ChatServerTitle.AutoSize = true;
			this.ChatServerTitle.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.ChatServerTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.ChatServerTitle.Location = new System.Drawing.Point(30, 201);
			this.ChatServerTitle.Name = "ChatServerTitle";
			this.ChatServerTitle.Size = new System.Drawing.Size(66, 18);
			this.ChatServerTitle.TabIndex = 26;
			this.ChatServerTitle.Text = "Server:";
			this.ChatServerTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// ChatApplyButton
			// 
			this.ChatApplyButton.Location = new System.Drawing.Point(56, 335);
			this.ChatApplyButton.Name = "ChatApplyButton";
			this.ChatApplyButton.Size = new System.Drawing.Size(112, 25);
			this.ChatApplyButton.TabIndex = 25;
			this.ChatApplyButton.Text = "Apply";
			this.ChatApplyButton.UseVisualStyleBackColor = true;
			this.ChatApplyButton.Click += new System.EventHandler(this.ChatApplyButton_Click);
			// 
			// AuthField2Title
			// 
			this.AuthField2Title.AutoSize = true;
			this.AuthField2Title.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.AuthField2Title.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.AuthField2Title.Location = new System.Drawing.Point(30, 311);
			this.AuthField2Title.Name = "AuthField2Title";
			this.AuthField2Title.Size = new System.Drawing.Size(45, 18);
			this.AuthField2Title.TabIndex = 24;
			this.AuthField2Title.Text = "AF2:";
			this.AuthField2Title.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// AuthField1Title
			// 
			this.AuthField1Title.AutoSize = true;
			this.AuthField1Title.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.AuthField1Title.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.AuthField1Title.Location = new System.Drawing.Point(30, 285);
			this.AuthField1Title.Name = "AuthField1Title";
			this.AuthField1Title.Size = new System.Drawing.Size(45, 18);
			this.AuthField1Title.TabIndex = 23;
			this.AuthField1Title.Text = "AF1:";
			this.AuthField1Title.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// ChatReconnectButton
			// 
			this.ChatReconnectButton.Enabled = false;
			this.ChatReconnectButton.Location = new System.Drawing.Point(174, 141);
			this.ChatReconnectButton.Name = "ChatReconnectButton";
			this.ChatReconnectButton.Size = new System.Drawing.Size(112, 25);
			this.ChatReconnectButton.TabIndex = 18;
			this.ChatReconnectButton.Text = "Reconnect";
			this.ChatReconnectButton.UseVisualStyleBackColor = true;
			this.ChatReconnectButton.Click += new System.EventHandler(this.ChatReconnectButton_Click);
			// 
			// ChatStatusLabel
			// 
			this.ChatStatusLabel.AutoSize = true;
			this.ChatStatusLabel.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.ChatStatusLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.ChatStatusLabel.Location = new System.Drawing.Point(171, 113);
			this.ChatStatusLabel.Name = "ChatStatusLabel";
			this.ChatStatusLabel.Size = new System.Drawing.Size(82, 18);
			this.ChatStatusLabel.TabIndex = 17;
			this.ChatStatusLabel.Text = "Unknown";
			this.ChatStatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// ChatStatusTitle
			// 
			this.ChatStatusTitle.AutoSize = true;
			this.ChatStatusTitle.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.ChatStatusTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.ChatStatusTitle.Location = new System.Drawing.Point(30, 113);
			this.ChatStatusTitle.Name = "ChatStatusTitle";
			this.ChatStatusTitle.Size = new System.Drawing.Size(112, 18);
			this.ChatStatusTitle.TabIndex = 16;
			this.ChatStatusTitle.Text = "Chat Status:";
			this.ChatStatusTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// ChatEnabledCheckbox
			// 
			this.ChatEnabledCheckbox.AutoSize = true;
			this.ChatEnabledCheckbox.Font = new System.Drawing.Font("Verdana", 12F);
			this.ChatEnabledCheckbox.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.ChatEnabledCheckbox.Location = new System.Drawing.Point(32, 142);
			this.ChatEnabledCheckbox.Name = "ChatEnabledCheckbox";
			this.ChatEnabledCheckbox.Size = new System.Drawing.Size(136, 22);
			this.ChatEnabledCheckbox.TabIndex = 15;
			this.ChatEnabledCheckbox.Text = "Chat Enabled";
			this.ChatEnabledCheckbox.UseVisualStyleBackColor = true;
			// 
			// ChatProviderSelectorPanel
			// 
			this.ChatProviderSelectorPanel.Controls.Add(this.DiscordProviderSwitch);
			this.ChatProviderSelectorPanel.Controls.Add(this.IRCProviderSwitch);
			this.ChatProviderSelectorPanel.Location = new System.Drawing.Point(23, 49);
			this.ChatProviderSelectorPanel.Name = "ChatProviderSelectorPanel";
			this.ChatProviderSelectorPanel.Size = new System.Drawing.Size(302, 50);
			this.ChatProviderSelectorPanel.TabIndex = 14;
			// 
			// DiscordProviderSwitch
			// 
			this.DiscordProviderSwitch.AutoSize = true;
			this.DiscordProviderSwitch.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.DiscordProviderSwitch.Location = new System.Drawing.Point(220, 17);
			this.DiscordProviderSwitch.Name = "DiscordProviderSwitch";
			this.DiscordProviderSwitch.Size = new System.Drawing.Size(61, 17);
			this.DiscordProviderSwitch.TabIndex = 13;
			this.DiscordProviderSwitch.TabStop = true;
			this.DiscordProviderSwitch.Text = "Discord";
			this.DiscordProviderSwitch.UseVisualStyleBackColor = true;
			this.DiscordProviderSwitch.CheckedChanged += new System.EventHandler(this.DiscordProviderSwitch_CheckedChanged);
			// 
			// IRCProviderSwitch
			// 
			this.IRCProviderSwitch.AutoSize = true;
			this.IRCProviderSwitch.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.IRCProviderSwitch.Location = new System.Drawing.Point(14, 17);
			this.IRCProviderSwitch.Name = "IRCProviderSwitch";
			this.IRCProviderSwitch.Size = new System.Drawing.Size(116, 17);
			this.IRCProviderSwitch.TabIndex = 12;
			this.IRCProviderSwitch.TabStop = true;
			this.IRCProviderSwitch.Text = "Internet Relay Chat";
			this.IRCProviderSwitch.UseVisualStyleBackColor = true;
			this.IRCProviderSwitch.CheckedChanged += new System.EventHandler(this.IRCProviderSwitch_CheckedChanged);
			// 
			// ChatProviderTitle
			// 
			this.ChatProviderTitle.AutoSize = true;
			this.ChatProviderTitle.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.ChatProviderTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.ChatProviderTitle.Location = new System.Drawing.Point(20, 15);
			this.ChatProviderTitle.Name = "ChatProviderTitle";
			this.ChatProviderTitle.Size = new System.Drawing.Size(144, 18);
			this.ChatProviderTitle.TabIndex = 13;
			this.ChatProviderTitle.Text = "Editing Provider:";
			this.ChatProviderTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// ChannelsTitle
			// 
			this.ChannelsTitle.AutoSize = true;
			this.ChannelsTitle.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.ChannelsTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.ChannelsTitle.Location = new System.Drawing.Point(565, 15);
			this.ChannelsTitle.Name = "ChannelsTitle";
			this.ChannelsTitle.Size = new System.Drawing.Size(234, 18);
			this.ChannelsTitle.TabIndex = 11;
			this.ChannelsTitle.Text = "Listen/Broadcast Channels:";
			this.ChannelsTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// ChatAdminsTitle
			// 
			this.ChatAdminsTitle.AutoSize = true;
			this.ChatAdminsTitle.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.ChatAdminsTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.ChatAdminsTitle.Location = new System.Drawing.Point(366, 15);
			this.ChatAdminsTitle.Name = "ChatAdminsTitle";
			this.ChatAdminsTitle.Size = new System.Drawing.Size(75, 18);
			this.ChatAdminsTitle.TabIndex = 9;
			this.ChatAdminsTitle.Text = "Admins:";
			this.ChatAdminsTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.AdminModeSpecial);
			this.panel1.Controls.Add(this.AdminModeNormal);
			this.panel1.Location = new System.Drawing.Point(174, 168);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(202, 25);
			this.panel1.TabIndex = 15;
			// 
			// AdminModeSpecial
			// 
			this.AdminModeSpecial.AutoSize = true;
			this.AdminModeSpecial.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.AdminModeSpecial.Location = new System.Drawing.Point(87, 8);
			this.AdminModeSpecial.Name = "AdminModeSpecial";
			this.AdminModeSpecial.Size = new System.Drawing.Size(94, 17);
			this.AdminModeSpecial.TabIndex = 13;
			this.AdminModeSpecial.TabStop = true;
			this.AdminModeSpecial.Text = "Channel Mode";
			this.AdminModeSpecial.UseVisualStyleBackColor = true;
			// 
			// AdminModeNormal
			// 
			this.AdminModeNormal.AutoSize = true;
			this.AdminModeNormal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.AdminModeNormal.Location = new System.Drawing.Point(3, 8);
			this.AdminModeNormal.Name = "AdminModeNormal";
			this.AdminModeNormal.Size = new System.Drawing.Size(78, 17);
			this.AdminModeNormal.TabIndex = 12;
			this.AdminModeNormal.TabStop = true;
			this.AdminModeNormal.Text = "Nicknames";
			this.AdminModeNormal.UseVisualStyleBackColor = true;
			this.AdminModeNormal.CheckedChanged += new System.EventHandler(this.AdminModeNormal_CheckedChanged);
			// 
			// ServerPanel
			// 
			this.ServerPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(133)))), ((int)(((byte)(143)))), ((int)(((byte)(160)))));
			this.ServerPanel.Controls.Add(this.AutoUpdateMLabel);
			this.ServerPanel.Controls.Add(this.AutoUpdateInterval);
			this.ServerPanel.Controls.Add(this.AutoUpdateCheckbox);
			this.ServerPanel.Controls.Add(this.ServerPageRefreshButton);
			this.ServerPanel.Controls.Add(this.AutostartCheckbox);
			this.ServerPanel.Controls.Add(this.WebclientCheckBox);
			this.ServerPanel.Controls.Add(this.WorldAnnounceButton);
			this.ServerPanel.Controls.Add(this.WorldAnnounceField);
			this.ServerPanel.Controls.Add(this.projectNameText);
			this.ServerPanel.Controls.Add(this.WorldAnnounceLabel);
			this.ServerPanel.Controls.Add(this.SecuritySelector);
			this.ServerPanel.Controls.Add(this.SecurityTitle);
			this.ServerPanel.Controls.Add(this.RemoveAllTestMergesButton);
			this.ServerPanel.Controls.Add(this.ServerPathLabel);
			this.ServerPanel.Controls.Add(this.CompileCancelButton);
			this.ServerPanel.Controls.Add(this.ProjectPathLabel);
			this.ServerPanel.Controls.Add(this.PortLabel);
			this.ServerPanel.Controls.Add(this.PortSelector);
			this.ServerPanel.Controls.Add(this.TestMergeManagerButton);
			this.ServerPanel.Controls.Add(this.UpdateServerButton);
			this.ServerPanel.Controls.Add(this.ServerGRestartButton);
			this.ServerPanel.Controls.Add(this.ServerGStopButton);
			this.ServerPanel.Controls.Add(this.ServerRestartButton);
			this.ServerPanel.Controls.Add(this.ServerStopButton);
			this.ServerPanel.Controls.Add(this.ServerStartButton);
			this.ServerPanel.Controls.Add(this.CompilerStatusLabel);
			this.ServerPanel.Controls.Add(this.CompilerLabel);
			this.ServerPanel.Controls.Add(this.compileButton);
			this.ServerPanel.Controls.Add(this.initializeButton);
			this.ServerPanel.Controls.Add(this.ServerStatusLabel);
			this.ServerPanel.Controls.Add(this.ServerStatusTitle);
			this.ServerPanel.Location = new System.Drawing.Point(4, 22);
			this.ServerPanel.Name = "ServerPanel";
			this.ServerPanel.Padding = new System.Windows.Forms.Padding(3);
			this.ServerPanel.Size = new System.Drawing.Size(868, 366);
			this.ServerPanel.TabIndex = 2;
			this.ServerPanel.Text = "Server";
			// 
			// AutoUpdateMLabel
			// 
			this.AutoUpdateMLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.AutoUpdateMLabel.AutoSize = true;
			this.AutoUpdateMLabel.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.AutoUpdateMLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.AutoUpdateMLabel.Location = new System.Drawing.Point(433, 136);
			this.AutoUpdateMLabel.Name = "AutoUpdateMLabel";
			this.AutoUpdateMLabel.Size = new System.Drawing.Size(21, 18);
			this.AutoUpdateMLabel.TabIndex = 47;
			this.AutoUpdateMLabel.Text = "M";
			this.AutoUpdateMLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.AutoUpdateMLabel.Visible = false;
			// 
			// AutoUpdateInterval
			// 
			this.AutoUpdateInterval.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.AutoUpdateInterval.Location = new System.Drawing.Point(365, 135);
			this.AutoUpdateInterval.Maximum = new decimal(new int[] {
            276447231,
            23283,
            0,
            0});
			this.AutoUpdateInterval.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.AutoUpdateInterval.Name = "AutoUpdateInterval";
			this.AutoUpdateInterval.Size = new System.Drawing.Size(62, 20);
			this.AutoUpdateInterval.TabIndex = 46;
			this.AutoUpdateInterval.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.AutoUpdateInterval.Visible = false;
			this.AutoUpdateInterval.ValueChanged += new System.EventHandler(this.AutoUpdateInterval_ValueChanged);
			// 
			// AutoUpdateCheckbox
			// 
			this.AutoUpdateCheckbox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.AutoUpdateCheckbox.AutoSize = true;
			this.AutoUpdateCheckbox.Font = new System.Drawing.Font("Verdana", 12F);
			this.AutoUpdateCheckbox.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.AutoUpdateCheckbox.Location = new System.Drawing.Point(228, 134);
			this.AutoUpdateCheckbox.Name = "AutoUpdateCheckbox";
			this.AutoUpdateCheckbox.Size = new System.Drawing.Size(131, 22);
			this.AutoUpdateCheckbox.TabIndex = 45;
			this.AutoUpdateCheckbox.Text = "Auto-Update";
			this.AutoUpdateCheckbox.UseVisualStyleBackColor = true;
			this.AutoUpdateCheckbox.Visible = false;
			this.AutoUpdateCheckbox.CheckedChanged += new System.EventHandler(this.AutoUpdateCheckbox_CheckedChanged);
			// 
			// ServerPageRefreshButton
			// 
			this.ServerPageRefreshButton.Anchor = System.Windows.Forms.AnchorStyles.Top;
			this.ServerPageRefreshButton.Location = new System.Drawing.Point(626, 13);
			this.ServerPageRefreshButton.Name = "ServerPageRefreshButton";
			this.ServerPageRefreshButton.Size = new System.Drawing.Size(118, 28);
			this.ServerPageRefreshButton.TabIndex = 43;
			this.ServerPageRefreshButton.Text = "Refresh";
			this.ServerPageRefreshButton.UseVisualStyleBackColor = true;
			this.ServerPageRefreshButton.Click += new System.EventHandler(this.ServerPageRefreshButton_Click);
			// 
			// AutostartCheckbox
			// 
			this.AutostartCheckbox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.AutostartCheckbox.AutoSize = true;
			this.AutostartCheckbox.Font = new System.Drawing.Font("Verdana", 12F);
			this.AutostartCheckbox.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.AutostartCheckbox.Location = new System.Drawing.Point(754, 16);
			this.AutostartCheckbox.Name = "AutostartCheckbox";
			this.AutostartCheckbox.Size = new System.Drawing.Size(104, 22);
			this.AutostartCheckbox.TabIndex = 15;
			this.AutostartCheckbox.Text = "Autostart";
			this.AutostartCheckbox.UseVisualStyleBackColor = true;
			this.AutostartCheckbox.CheckedChanged += new System.EventHandler(this.AutostartCheckbox_CheckedChanged);
			// 
			// WebclientCheckBox
			// 
			this.WebclientCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.WebclientCheckBox.AutoSize = true;
			this.WebclientCheckBox.Font = new System.Drawing.Font("Verdana", 12F);
			this.WebclientCheckBox.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.WebclientCheckBox.Location = new System.Drawing.Point(754, 57);
			this.WebclientCheckBox.Name = "WebclientCheckBox";
			this.WebclientCheckBox.Size = new System.Drawing.Size(108, 22);
			this.WebclientCheckBox.TabIndex = 42;
			this.WebclientCheckBox.Text = "Webclient";
			this.WebclientCheckBox.UseVisualStyleBackColor = true;
			this.WebclientCheckBox.CheckedChanged += new System.EventHandler(this.WebclientCheckBox_CheckedChanged);
			// 
			// WorldAnnounceButton
			// 
			this.WorldAnnounceButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.WorldAnnounceButton.Location = new System.Drawing.Point(785, 133);
			this.WorldAnnounceButton.Name = "WorldAnnounceButton";
			this.WorldAnnounceButton.Size = new System.Drawing.Size(77, 20);
			this.WorldAnnounceButton.TabIndex = 41;
			this.WorldAnnounceButton.Text = "Send";
			this.WorldAnnounceButton.UseVisualStyleBackColor = true;
			this.WorldAnnounceButton.Click += new System.EventHandler(this.WorldAnnounceButton_Click);
			// 
			// WorldAnnounceField
			// 
			this.WorldAnnounceField.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.WorldAnnounceField.Location = new System.Drawing.Point(566, 134);
			this.WorldAnnounceField.Name = "WorldAnnounceField";
			this.WorldAnnounceField.Size = new System.Drawing.Size(213, 20);
			this.WorldAnnounceField.TabIndex = 40;
			// 
			// projectNameText
			// 
			this.projectNameText.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.projectNameText.Location = new System.Drawing.Point(566, 163);
			this.projectNameText.Name = "projectNameText";
			this.projectNameText.Size = new System.Drawing.Size(296, 20);
			this.projectNameText.TabIndex = 29;
			// 
			// WorldAnnounceLabel
			// 
			this.WorldAnnounceLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.WorldAnnounceLabel.AutoSize = true;
			this.WorldAnnounceLabel.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.WorldAnnounceLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.WorldAnnounceLabel.Location = new System.Drawing.Point(466, 135);
			this.WorldAnnounceLabel.Name = "WorldAnnounceLabel";
			this.WorldAnnounceLabel.Size = new System.Drawing.Size(94, 18);
			this.WorldAnnounceLabel.TabIndex = 39;
			this.WorldAnnounceLabel.Text = "Announce:";
			this.WorldAnnounceLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// SecuritySelector
			// 
			this.SecuritySelector.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.SecuritySelector.FormattingEnabled = true;
			this.SecuritySelector.Items.AddRange(new object[] {
            "Trusted",
            "Safe",
            "Ultrasafe"});
			this.SecuritySelector.Location = new System.Drawing.Point(101, 134);
			this.SecuritySelector.Name = "SecuritySelector";
			this.SecuritySelector.Size = new System.Drawing.Size(121, 21);
			this.SecuritySelector.TabIndex = 38;
			this.SecuritySelector.SelectedIndexChanged += new System.EventHandler(this.SecuritySelector_SelectedIndexChanged);
			// 
			// SecurityTitle
			// 
			this.SecurityTitle.AutoSize = true;
			this.SecurityTitle.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.SecurityTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.SecurityTitle.Location = new System.Drawing.Point(15, 139);
			this.SecurityTitle.Name = "SecurityTitle";
			this.SecurityTitle.Size = new System.Drawing.Size(80, 18);
			this.SecurityTitle.TabIndex = 37;
			this.SecurityTitle.Text = "Security:";
			this.SecurityTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// RemoveAllTestMergesButton
			// 
			this.RemoveAllTestMergesButton.Anchor = System.Windows.Forms.AnchorStyles.Top;
			this.RemoveAllTestMergesButton.Location = new System.Drawing.Point(489, 94);
			this.RemoveAllTestMergesButton.Name = "RemoveAllTestMergesButton";
			this.RemoveAllTestMergesButton.Size = new System.Drawing.Size(142, 28);
			this.RemoveAllTestMergesButton.TabIndex = 36;
			this.RemoveAllTestMergesButton.Text = "Remove All Test Merges";
			this.RemoveAllTestMergesButton.UseVisualStyleBackColor = true;
			this.RemoveAllTestMergesButton.Click += new System.EventHandler(this.RemoveAllTestMergesButton_Click);
			// 
			// ServerPathLabel
			// 
			this.ServerPathLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.ServerPathLabel.AutoSize = true;
			this.ServerPathLabel.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.ServerPathLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.ServerPathLabel.Location = new System.Drawing.Point(15, 165);
			this.ServerPathLabel.Name = "ServerPathLabel";
			this.ServerPathLabel.Size = new System.Drawing.Size(109, 18);
			this.ServerPathLabel.TabIndex = 33;
			this.ServerPathLabel.Text = "Server Path:";
			this.ServerPathLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// CompileCancelButton
			// 
			this.CompileCancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.CompileCancelButton.Enabled = false;
			this.CompileCancelButton.Location = new System.Drawing.Point(382, 302);
			this.CompileCancelButton.Name = "CompileCancelButton";
			this.CompileCancelButton.Size = new System.Drawing.Size(69, 31);
			this.CompileCancelButton.TabIndex = 31;
			this.CompileCancelButton.Text = "Cancel";
			this.CompileCancelButton.UseVisualStyleBackColor = true;
			this.CompileCancelButton.Click += new System.EventHandler(this.CompileCancelButton_Click);
			// 
			// ProjectPathLabel
			// 
			this.ProjectPathLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.ProjectPathLabel.AutoSize = true;
			this.ProjectPathLabel.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.ProjectPathLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.ProjectPathLabel.Location = new System.Drawing.Point(445, 165);
			this.ProjectPathLabel.Name = "ProjectPathLabel";
			this.ProjectPathLabel.Size = new System.Drawing.Size(115, 18);
			this.ProjectPathLabel.TabIndex = 30;
			this.ProjectPathLabel.Text = "Project Path:";
			this.ProjectPathLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// PortLabel
			// 
			this.PortLabel.AutoSize = true;
			this.PortLabel.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.PortLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.PortLabel.Location = new System.Drawing.Point(4, 60);
			this.PortLabel.Name = "PortLabel";
			this.PortLabel.Size = new System.Drawing.Size(48, 18);
			this.PortLabel.TabIndex = 28;
			this.PortLabel.Text = "Port:";
			this.PortLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// PortSelector
			// 
			this.PortSelector.Location = new System.Drawing.Point(59, 60);
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
			this.PortSelector.Size = new System.Drawing.Size(60, 20);
			this.PortSelector.TabIndex = 27;
			this.PortSelector.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.PortSelector.ValueChanged += new System.EventHandler(this.PortSelector_ValueChanged);
			// 
			// TestMergeManagerButton
			// 
			this.TestMergeManagerButton.Anchor = System.Windows.Forms.AnchorStyles.Top;
			this.TestMergeManagerButton.Location = new System.Drawing.Point(343, 94);
			this.TestMergeManagerButton.Name = "TestMergeManagerButton";
			this.TestMergeManagerButton.Size = new System.Drawing.Size(142, 28);
			this.TestMergeManagerButton.TabIndex = 24;
			this.TestMergeManagerButton.Text = "Test Merge Manager";
			this.TestMergeManagerButton.UseVisualStyleBackColor = true;
			this.TestMergeManagerButton.Click += new System.EventHandler(this.TestMergeManagerButton_Click);
			// 
			// UpdateServerButton
			// 
			this.UpdateServerButton.Anchor = System.Windows.Forms.AnchorStyles.Top;
			this.UpdateServerButton.Location = new System.Drawing.Point(180, 94);
			this.UpdateServerButton.Name = "UpdateServerButton";
			this.UpdateServerButton.Size = new System.Drawing.Size(157, 28);
			this.UpdateServerButton.TabIndex = 21;
			this.UpdateServerButton.Text = "Update Server";
			this.UpdateServerButton.UseVisualStyleBackColor = true;
			this.UpdateServerButton.Click += new System.EventHandler(this.UpdateServerButton_Click);
			// 
			// ServerGRestartButton
			// 
			this.ServerGRestartButton.Anchor = System.Windows.Forms.AnchorStyles.Top;
			this.ServerGRestartButton.Location = new System.Drawing.Point(626, 54);
			this.ServerGRestartButton.Name = "ServerGRestartButton";
			this.ServerGRestartButton.Size = new System.Drawing.Size(118, 28);
			this.ServerGRestartButton.TabIndex = 20;
			this.ServerGRestartButton.Text = "Graceful Restart";
			this.ServerGRestartButton.UseVisualStyleBackColor = true;
			this.ServerGRestartButton.Click += new System.EventHandler(this.ServerGRestartButton_Click);
			// 
			// ServerGStopButton
			// 
			this.ServerGStopButton.Anchor = System.Windows.Forms.AnchorStyles.Top;
			this.ServerGStopButton.Appearance = System.Windows.Forms.Appearance.Button;
			this.ServerGStopButton.Location = new System.Drawing.Point(502, 54);
			this.ServerGStopButton.Name = "ServerGStopButton";
			this.ServerGStopButton.Size = new System.Drawing.Size(118, 28);
			this.ServerGStopButton.TabIndex = 19;
			this.ServerGStopButton.Text = "Graceful Stop";
			this.ServerGStopButton.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.ServerGStopButton.UseVisualStyleBackColor = true;
			this.ServerGStopButton.CheckedChanged += new System.EventHandler(this.ServerGStopButton_Checked);
			// 
			// ServerRestartButton
			// 
			this.ServerRestartButton.Anchor = System.Windows.Forms.AnchorStyles.Top;
			this.ServerRestartButton.Location = new System.Drawing.Point(378, 54);
			this.ServerRestartButton.Name = "ServerRestartButton";
			this.ServerRestartButton.Size = new System.Drawing.Size(118, 28);
			this.ServerRestartButton.TabIndex = 18;
			this.ServerRestartButton.Text = "Restart";
			this.ServerRestartButton.UseVisualStyleBackColor = true;
			this.ServerRestartButton.Click += new System.EventHandler(this.ServerRestartButton_Click);
			// 
			// ServerStopButton
			// 
			this.ServerStopButton.Anchor = System.Windows.Forms.AnchorStyles.Top;
			this.ServerStopButton.Location = new System.Drawing.Point(254, 54);
			this.ServerStopButton.Name = "ServerStopButton";
			this.ServerStopButton.Size = new System.Drawing.Size(118, 28);
			this.ServerStopButton.TabIndex = 17;
			this.ServerStopButton.Text = "Stop";
			this.ServerStopButton.UseVisualStyleBackColor = true;
			this.ServerStopButton.Click += new System.EventHandler(this.ServerStopButton_Click);
			// 
			// ServerStartButton
			// 
			this.ServerStartButton.Anchor = System.Windows.Forms.AnchorStyles.Top;
			this.ServerStartButton.Location = new System.Drawing.Point(130, 54);
			this.ServerStartButton.Name = "ServerStartButton";
			this.ServerStartButton.Size = new System.Drawing.Size(118, 28);
			this.ServerStartButton.TabIndex = 16;
			this.ServerStartButton.Text = "Start";
			this.ServerStartButton.UseVisualStyleBackColor = true;
			this.ServerStartButton.Click += new System.EventHandler(this.ServerStartButton_Click);
			// 
			// CompilerStatusLabel
			// 
			this.CompilerStatusLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.CompilerStatusLabel.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.CompilerStatusLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.CompilerStatusLabel.Location = new System.Drawing.Point(110, 271);
			this.CompilerStatusLabel.Name = "CompilerStatusLabel";
			this.CompilerStatusLabel.Size = new System.Drawing.Size(618, 28);
			this.CompilerStatusLabel.TabIndex = 14;
			this.CompilerStatusLabel.Text = "Idle";
			this.CompilerStatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// CompilerLabel
			// 
			this.CompilerLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.CompilerLabel.AutoSize = true;
			this.CompilerLabel.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.CompilerLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.CompilerLabel.Location = new System.Drawing.Point(379, 203);
			this.CompilerLabel.Name = "CompilerLabel";
			this.CompilerLabel.Size = new System.Drawing.Size(80, 18);
			this.CompilerLabel.TabIndex = 13;
			this.CompilerLabel.Text = "Compiler";
			this.CompilerLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// compileButton
			// 
			this.compileButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.compileButton.Enabled = false;
			this.compileButton.Location = new System.Drawing.Point(456, 240);
			this.compileButton.Name = "compileButton";
			this.compileButton.Size = new System.Drawing.Size(159, 28);
			this.compileButton.TabIndex = 12;
			this.compileButton.Text = "Copy from Repo and Compile";
			this.compileButton.UseVisualStyleBackColor = true;
			this.compileButton.Click += new System.EventHandler(this.CompileButton_Click);
			// 
			// initializeButton
			// 
			this.initializeButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.initializeButton.Enabled = false;
			this.initializeButton.Location = new System.Drawing.Point(219, 240);
			this.initializeButton.Name = "initializeButton";
			this.initializeButton.Size = new System.Drawing.Size(159, 28);
			this.initializeButton.TabIndex = 11;
			this.initializeButton.Text = "Initialize Game Folders";
			this.initializeButton.UseVisualStyleBackColor = true;
			this.initializeButton.Click += new System.EventHandler(this.InitializeButton_Click);
			// 
			// ServerStatusLabel
			// 
			this.ServerStatusLabel.AutoSize = true;
			this.ServerStatusLabel.Font = new System.Drawing.Font("Verdana", 10F);
			this.ServerStatusLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.ServerStatusLabel.Location = new System.Drawing.Point(136, 19);
			this.ServerStatusLabel.Name = "ServerStatusLabel";
			this.ServerStatusLabel.Size = new System.Drawing.Size(73, 17);
			this.ServerStatusLabel.TabIndex = 9;
			this.ServerStatusLabel.Text = "Unknown";
			this.ServerStatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// ServerStatusTitle
			// 
			this.ServerStatusTitle.AutoSize = true;
			this.ServerStatusTitle.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.ServerStatusTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.ServerStatusTitle.Location = new System.Drawing.Point(15, 17);
			this.ServerStatusTitle.Name = "ServerStatusTitle";
			this.ServerStatusTitle.Size = new System.Drawing.Size(125, 18);
			this.ServerStatusTitle.TabIndex = 8;
			this.ServerStatusTitle.Text = "Server Status:";
			this.ServerStatusTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// BYONDPanel
			// 
			this.BYONDPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(133)))), ((int)(((byte)(143)))), ((int)(((byte)(160)))));
			this.BYONDPanel.Controls.Add(this.BYONDRefreshButton);
			this.BYONDPanel.Controls.Add(this.LatestVersionLabel);
			this.BYONDPanel.Controls.Add(this.LatestVersionTitle);
			this.BYONDPanel.Controls.Add(this.StagedVersionLabel);
			this.BYONDPanel.Controls.Add(this.StagedVersionTitle);
			this.BYONDPanel.Controls.Add(this.StatusLabel);
			this.BYONDPanel.Controls.Add(this.VersionLabel);
			this.BYONDPanel.Controls.Add(this.VersionTitle);
			this.BYONDPanel.Controls.Add(this.MinorVersionLabel);
			this.BYONDPanel.Controls.Add(this.MajorVersionLabel);
			this.BYONDPanel.Controls.Add(this.UpdateButton);
			this.BYONDPanel.Controls.Add(this.MinorVersionNumeric);
			this.BYONDPanel.Controls.Add(this.MajorVersionNumeric);
			this.BYONDPanel.Location = new System.Drawing.Point(4, 22);
			this.BYONDPanel.Name = "BYONDPanel";
			this.BYONDPanel.Size = new System.Drawing.Size(868, 366);
			this.BYONDPanel.TabIndex = 1;
			this.BYONDPanel.Text = "BYOND";
			// 
			// BYONDRefreshButton
			// 
			this.BYONDRefreshButton.Location = new System.Drawing.Point(462, 252);
			this.BYONDRefreshButton.Name = "BYONDRefreshButton";
			this.BYONDRefreshButton.Size = new System.Drawing.Size(118, 28);
			this.BYONDRefreshButton.TabIndex = 14;
			this.BYONDRefreshButton.Text = "Refresh";
			this.BYONDRefreshButton.UseVisualStyleBackColor = true;
			this.BYONDRefreshButton.Click += new System.EventHandler(this.BYONDRefreshButton_Click);
			// 
			// LatestVersionLabel
			// 
			this.LatestVersionLabel.AutoSize = true;
			this.LatestVersionLabel.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.LatestVersionLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.LatestVersionLabel.Location = new System.Drawing.Point(425, 93);
			this.LatestVersionLabel.Name = "LatestVersionLabel";
			this.LatestVersionLabel.Size = new System.Drawing.Size(82, 18);
			this.LatestVersionLabel.TabIndex = 13;
			this.LatestVersionLabel.Text = "Unknown";
			this.LatestVersionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// LatestVersionTitle
			// 
			this.LatestVersionTitle.AutoSize = true;
			this.LatestVersionTitle.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.LatestVersionTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.LatestVersionTitle.Location = new System.Drawing.Point(286, 93);
			this.LatestVersionTitle.Name = "LatestVersionTitle";
			this.LatestVersionTitle.Size = new System.Drawing.Size(133, 18);
			this.LatestVersionTitle.TabIndex = 12;
			this.LatestVersionTitle.Text = "Latest Version:";
			this.LatestVersionTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// StagedVersionLabel
			// 
			this.StagedVersionLabel.AutoSize = true;
			this.StagedVersionLabel.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.StagedVersionLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.StagedVersionLabel.Location = new System.Drawing.Point(425, 131);
			this.StagedVersionLabel.Name = "StagedVersionLabel";
			this.StagedVersionLabel.Size = new System.Drawing.Size(82, 18);
			this.StagedVersionLabel.TabIndex = 11;
			this.StagedVersionLabel.Text = "Unknown";
			this.StagedVersionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.StagedVersionLabel.Visible = false;
			// 
			// StagedVersionTitle
			// 
			this.StagedVersionTitle.AutoSize = true;
			this.StagedVersionTitle.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.StagedVersionTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.StagedVersionTitle.Location = new System.Drawing.Point(282, 131);
			this.StagedVersionTitle.Name = "StagedVersionTitle";
			this.StagedVersionTitle.Size = new System.Drawing.Size(137, 18);
			this.StagedVersionTitle.TabIndex = 10;
			this.StagedVersionTitle.Text = "Staged version:";
			this.StagedVersionTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.StagedVersionTitle.Visible = false;
			// 
			// StatusLabel
			// 
			this.StatusLabel.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.StatusLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.StatusLabel.Location = new System.Drawing.Point(303, 320);
			this.StatusLabel.Name = "StatusLabel";
			this.StatusLabel.Size = new System.Drawing.Size(253, 37);
			this.StatusLabel.TabIndex = 9;
			this.StatusLabel.Text = "Idle";
			this.StatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// VersionLabel
			// 
			this.VersionLabel.AutoSize = true;
			this.VersionLabel.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.VersionLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.VersionLabel.Location = new System.Drawing.Point(425, 56);
			this.VersionLabel.Name = "VersionLabel";
			this.VersionLabel.Size = new System.Drawing.Size(82, 18);
			this.VersionLabel.TabIndex = 8;
			this.VersionLabel.Text = "Unknown";
			this.VersionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// VersionTitle
			// 
			this.VersionTitle.AutoSize = true;
			this.VersionTitle.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.VersionTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.VersionTitle.Location = new System.Drawing.Point(265, 56);
			this.VersionTitle.Name = "VersionTitle";
			this.VersionTitle.Size = new System.Drawing.Size(154, 18);
			this.VersionTitle.TabIndex = 7;
			this.VersionTitle.Text = "Installed Version:";
			this.VersionTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// MinorVersionLabel
			// 
			this.MinorVersionLabel.AutoSize = true;
			this.MinorVersionLabel.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.MinorVersionLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.MinorVersionLabel.Location = new System.Drawing.Point(521, 180);
			this.MinorVersionLabel.Name = "MinorVersionLabel";
			this.MinorVersionLabel.Size = new System.Drawing.Size(59, 18);
			this.MinorVersionLabel.TabIndex = 6;
			this.MinorVersionLabel.Text = "Minor:";
			this.MinorVersionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// MajorVersionLabel
			// 
			this.MajorVersionLabel.AutoSize = true;
			this.MajorVersionLabel.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.MajorVersionLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.MajorVersionLabel.Location = new System.Drawing.Point(276, 180);
			this.MajorVersionLabel.Name = "MajorVersionLabel";
			this.MajorVersionLabel.Size = new System.Drawing.Size(60, 18);
			this.MajorVersionLabel.TabIndex = 5;
			this.MajorVersionLabel.Text = "Major:";
			this.MajorVersionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// UpdateButton
			// 
			this.UpdateButton.Location = new System.Drawing.Point(268, 252);
			this.UpdateButton.Name = "UpdateButton";
			this.UpdateButton.Size = new System.Drawing.Size(118, 28);
			this.UpdateButton.TabIndex = 3;
			this.UpdateButton.Text = "Update";
			this.UpdateButton.UseVisualStyleBackColor = true;
			this.UpdateButton.Click += new System.EventHandler(this.UpdateButton_Click);
			// 
			// MinorVersionNumeric
			// 
			this.MinorVersionNumeric.Location = new System.Drawing.Point(490, 210);
			this.MinorVersionNumeric.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
			this.MinorVersionNumeric.Name = "MinorVersionNumeric";
			this.MinorVersionNumeric.Size = new System.Drawing.Size(120, 20);
			this.MinorVersionNumeric.TabIndex = 2;
			this.MinorVersionNumeric.Value = new decimal(new int[] {
            1381,
            0,
            0,
            0});
			// 
			// MajorVersionNumeric
			// 
			this.MajorVersionNumeric.Location = new System.Drawing.Point(245, 210);
			this.MajorVersionNumeric.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
			this.MajorVersionNumeric.Name = "MajorVersionNumeric";
			this.MajorVersionNumeric.Size = new System.Drawing.Size(120, 20);
			this.MajorVersionNumeric.TabIndex = 1;
			this.MajorVersionNumeric.Value = new decimal(new int[] {
            511,
            0,
            0,
            0});
			// 
			// RepoPanel
			// 
			this.RepoPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(133)))), ((int)(((byte)(143)))), ((int)(((byte)(160)))));
			this.RepoPanel.Controls.Add(this.SyncCommitsCheckBox);
			this.RepoPanel.Controls.Add(this.TGSJsonUpdate);
			this.RepoPanel.Controls.Add(this.RepoRefreshButton);
			this.RepoPanel.Controls.Add(this.BackupTagsList);
			this.RepoPanel.Controls.Add(this.ResetRemote);
			this.RepoPanel.Controls.Add(this.RecloneButton);
			this.RepoPanel.Controls.Add(this.RepoBranchTextBox);
			this.RepoPanel.Controls.Add(this.RepoRemoteTextBox);
			this.RepoPanel.Controls.Add(this.RepoGenChangelogButton);
			this.RepoPanel.Controls.Add(this.TestmergeSelector);
			this.RepoPanel.Controls.Add(this.TestMergeListLabel);
			this.RepoPanel.Controls.Add(this.CurrentRevisionLabel);
			this.RepoPanel.Controls.Add(this.RepoApplyButton);
			this.RepoPanel.Controls.Add(this.HardReset);
			this.RepoPanel.Controls.Add(this.UpdateRepoButton);
			this.RepoPanel.Controls.Add(this.MergePRButton);
			this.RepoPanel.Controls.Add(this.IdentityLabel);
			this.RepoPanel.Controls.Add(this.TestMergeListTitle);
			this.RepoPanel.Controls.Add(this.RemoteNameTitle);
			this.RepoPanel.Controls.Add(this.BranchNameTitle);
			this.RepoPanel.Controls.Add(this.CurrentRevisionTitle);
			this.RepoPanel.Controls.Add(this.CloneRepositoryButton);
			this.RepoPanel.Controls.Add(this.RepoProgressBarLabel);
			this.RepoPanel.Controls.Add(this.RepoProgressBar);
			this.RepoPanel.Location = new System.Drawing.Point(4, 22);
			this.RepoPanel.Name = "RepoPanel";
			this.RepoPanel.Padding = new System.Windows.Forms.Padding(3);
			this.RepoPanel.Size = new System.Drawing.Size(868, 366);
			this.RepoPanel.TabIndex = 0;
			this.RepoPanel.Text = "Repository";
			// 
			// SyncCommitsCheckBox
			// 
			this.SyncCommitsCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.SyncCommitsCheckBox.AutoSize = true;
			this.SyncCommitsCheckBox.Font = new System.Drawing.Font("Verdana", 12F);
			this.SyncCommitsCheckBox.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.SyncCommitsCheckBox.Location = new System.Drawing.Point(574, 16);
			this.SyncCommitsCheckBox.Name = "SyncCommitsCheckBox";
			this.SyncCommitsCheckBox.Size = new System.Drawing.Size(142, 22);
			this.SyncCommitsCheckBox.TabIndex = 46;
			this.SyncCommitsCheckBox.Text = "Sync Commits";
			this.SyncCommitsCheckBox.UseVisualStyleBackColor = true;
			this.SyncCommitsCheckBox.Visible = false;
			// 
			// TGSJsonUpdate
			// 
			this.TGSJsonUpdate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.TGSJsonUpdate.Location = new System.Drawing.Point(722, 212);
			this.TGSJsonUpdate.Name = "TGSJsonUpdate";
			this.TGSJsonUpdate.Size = new System.Drawing.Size(140, 29);
			this.TGSJsonUpdate.TabIndex = 36;
			this.TGSJsonUpdate.Text = "Update TGS3.json";
			this.TGSJsonUpdate.UseVisualStyleBackColor = true;
			this.TGSJsonUpdate.Visible = false;
			this.TGSJsonUpdate.Click += new System.EventHandler(this.TGSJsonUpdate_Click);
			// 
			// RepoRefreshButton
			// 
			this.RepoRefreshButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.RepoRefreshButton.Location = new System.Drawing.Point(722, 296);
			this.RepoRefreshButton.Name = "RepoRefreshButton";
			this.RepoRefreshButton.Size = new System.Drawing.Size(140, 29);
			this.RepoRefreshButton.TabIndex = 35;
			this.RepoRefreshButton.Text = "Refresh";
			this.RepoRefreshButton.UseVisualStyleBackColor = true;
			this.RepoRefreshButton.Visible = false;
			this.RepoRefreshButton.Click += new System.EventHandler(this.RepoRefreshButton_Click);
			// 
			// BackupTagsList
			// 
			this.BackupTagsList.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.BackupTagsList.Items.AddRange(new object[] {
            "None"});
			this.BackupTagsList.Location = new System.Drawing.Point(122, 142);
			this.BackupTagsList.Name = "BackupTagsList";
			this.BackupTagsList.ScrollAlwaysVisible = true;
			this.BackupTagsList.Size = new System.Drawing.Size(535, 95);
			this.BackupTagsList.TabIndex = 34;
			this.BackupTagsList.Visible = false;
			// 
			// ResetRemote
			// 
			this.ResetRemote.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.ResetRemote.Location = new System.Drawing.Point(722, 81);
			this.ResetRemote.Name = "ResetRemote";
			this.ResetRemote.Size = new System.Drawing.Size(140, 29);
			this.ResetRemote.TabIndex = 33;
			this.ResetRemote.Text = "Reset To Remote";
			this.ResetRemote.UseVisualStyleBackColor = true;
			this.ResetRemote.Visible = false;
			this.ResetRemote.Click += new System.EventHandler(this.ResetRemote_Click);
			// 
			// RecloneButton
			// 
			this.RecloneButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.RecloneButton.Location = new System.Drawing.Point(722, 247);
			this.RecloneButton.Name = "RecloneButton";
			this.RecloneButton.Size = new System.Drawing.Size(140, 29);
			this.RecloneButton.TabIndex = 32;
			this.RecloneButton.Text = "Reclone";
			this.RecloneButton.UseVisualStyleBackColor = true;
			this.RecloneButton.Visible = false;
			this.RecloneButton.Click += new System.EventHandler(this.RecloneButton_Click);
			// 
			// RepoBranchTextBox
			// 
			this.RepoBranchTextBox.Location = new System.Drawing.Point(122, 70);
			this.RepoBranchTextBox.Name = "RepoBranchTextBox";
			this.RepoBranchTextBox.Size = new System.Drawing.Size(535, 20);
			this.RepoBranchTextBox.TabIndex = 15;
			this.RepoBranchTextBox.Visible = false;
			// 
			// RepoRemoteTextBox
			// 
			this.RepoRemoteTextBox.Location = new System.Drawing.Point(122, 44);
			this.RepoRemoteTextBox.Name = "RepoRemoteTextBox";
			this.RepoRemoteTextBox.Size = new System.Drawing.Size(535, 20);
			this.RepoRemoteTextBox.TabIndex = 14;
			this.RepoRemoteTextBox.Visible = false;
			// 
			// RepoGenChangelogButton
			// 
			this.RepoGenChangelogButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.RepoGenChangelogButton.Location = new System.Drawing.Point(722, 177);
			this.RepoGenChangelogButton.Name = "RepoGenChangelogButton";
			this.RepoGenChangelogButton.Size = new System.Drawing.Size(140, 29);
			this.RepoGenChangelogButton.TabIndex = 27;
			this.RepoGenChangelogButton.Text = "Generate Changelog";
			this.RepoGenChangelogButton.UseVisualStyleBackColor = true;
			this.RepoGenChangelogButton.Visible = false;
			this.RepoGenChangelogButton.Click += new System.EventHandler(this.RepoGenChangelogButton_Click);
			// 
			// TestmergeSelector
			// 
			this.TestmergeSelector.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.TestmergeSelector.Location = new System.Drawing.Point(722, 151);
			this.TestmergeSelector.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
			this.TestmergeSelector.Name = "TestmergeSelector";
			this.TestmergeSelector.Size = new System.Drawing.Size(140, 20);
			this.TestmergeSelector.TabIndex = 22;
			this.TestmergeSelector.Visible = false;
			// 
			// TestMergeListLabel
			// 
			this.TestMergeListLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.TestMergeListLabel.HorizontalScrollbar = true;
			this.TestMergeListLabel.Items.AddRange(new object[] {
            "None"});
			this.TestMergeListLabel.Location = new System.Drawing.Point(122, 260);
			this.TestMergeListLabel.Name = "TestMergeListLabel";
			this.TestMergeListLabel.ScrollAlwaysVisible = true;
			this.TestMergeListLabel.Size = new System.Drawing.Size(535, 95);
			this.TestMergeListLabel.TabIndex = 21;
			this.TestMergeListLabel.Visible = false;
			// 
			// CurrentRevisionLabel
			// 
			this.CurrentRevisionLabel.AutoSize = true;
			this.CurrentRevisionLabel.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.CurrentRevisionLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.CurrentRevisionLabel.Location = new System.Drawing.Point(162, 14);
			this.CurrentRevisionLabel.Name = "CurrentRevisionLabel";
			this.CurrentRevisionLabel.Size = new System.Drawing.Size(82, 18);
			this.CurrentRevisionLabel.TabIndex = 20;
			this.CurrentRevisionLabel.Text = "Unknown";
			this.CurrentRevisionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.CurrentRevisionLabel.Visible = false;
			// 
			// RepoApplyButton
			// 
			this.RepoApplyButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.RepoApplyButton.Location = new System.Drawing.Point(722, 331);
			this.RepoApplyButton.Name = "RepoApplyButton";
			this.RepoApplyButton.Size = new System.Drawing.Size(140, 29);
			this.RepoApplyButton.TabIndex = 17;
			this.RepoApplyButton.Text = "Apply Changes";
			this.RepoApplyButton.UseVisualStyleBackColor = true;
			this.RepoApplyButton.Visible = false;
			this.RepoApplyButton.Click += new System.EventHandler(this.RepoApplyButton_Click);
			// 
			// HardReset
			// 
			this.HardReset.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.HardReset.Location = new System.Drawing.Point(722, 46);
			this.HardReset.Name = "HardReset";
			this.HardReset.Size = new System.Drawing.Size(140, 29);
			this.HardReset.TabIndex = 13;
			this.HardReset.Text = "Reset To Origin Branch";
			this.HardReset.UseVisualStyleBackColor = true;
			this.HardReset.Visible = false;
			this.HardReset.Click += new System.EventHandler(this.HardReset_Click);
			// 
			// UpdateRepoButton
			// 
			this.UpdateRepoButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.UpdateRepoButton.Location = new System.Drawing.Point(722, 11);
			this.UpdateRepoButton.Name = "UpdateRepoButton";
			this.UpdateRepoButton.Size = new System.Drawing.Size(140, 29);
			this.UpdateRepoButton.TabIndex = 12;
			this.UpdateRepoButton.Text = "Merge from Remote";
			this.UpdateRepoButton.UseVisualStyleBackColor = true;
			this.UpdateRepoButton.Visible = false;
			this.UpdateRepoButton.Click += new System.EventHandler(this.UpdateRepoButton_Click);
			// 
			// MergePRButton
			// 
			this.MergePRButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.MergePRButton.Location = new System.Drawing.Point(722, 116);
			this.MergePRButton.Name = "MergePRButton";
			this.MergePRButton.Size = new System.Drawing.Size(140, 29);
			this.MergePRButton.TabIndex = 11;
			this.MergePRButton.Text = "Merge Pull Request";
			this.MergePRButton.UseVisualStyleBackColor = true;
			this.MergePRButton.Visible = false;
			this.MergePRButton.Click += new System.EventHandler(this.TestMergeButton_Click);
			// 
			// IdentityLabel
			// 
			this.IdentityLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.IdentityLabel.AutoSize = true;
			this.IdentityLabel.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.IdentityLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.IdentityLabel.Location = new System.Drawing.Point(6, 142);
			this.IdentityLabel.Name = "IdentityLabel";
			this.IdentityLabel.Size = new System.Drawing.Size(116, 18);
			this.IdentityLabel.TabIndex = 8;
			this.IdentityLabel.Text = "Backup Tags:";
			this.IdentityLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.IdentityLabel.Visible = false;
			// 
			// TestMergeListTitle
			// 
			this.TestMergeListTitle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.TestMergeListTitle.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.TestMergeListTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.TestMergeListTitle.Location = new System.Drawing.Point(6, 260);
			this.TestMergeListTitle.Name = "TestMergeListTitle";
			this.TestMergeListTitle.Size = new System.Drawing.Size(110, 41);
			this.TestMergeListTitle.TabIndex = 6;
			this.TestMergeListTitle.Text = "Active Test Merges:";
			this.TestMergeListTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.TestMergeListTitle.Visible = false;
			// 
			// RemoteNameTitle
			// 
			this.RemoteNameTitle.AutoSize = true;
			this.RemoteNameTitle.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.RemoteNameTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.RemoteNameTitle.Location = new System.Drawing.Point(6, 46);
			this.RemoteNameTitle.Name = "RemoteNameTitle";
			this.RemoteNameTitle.Size = new System.Drawing.Size(78, 18);
			this.RemoteNameTitle.TabIndex = 5;
			this.RemoteNameTitle.Text = "Remote:";
			this.RemoteNameTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.RemoteNameTitle.Visible = false;
			// 
			// BranchNameTitle
			// 
			this.BranchNameTitle.AutoSize = true;
			this.BranchNameTitle.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.BranchNameTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.BranchNameTitle.Location = new System.Drawing.Point(6, 72);
			this.BranchNameTitle.Name = "BranchNameTitle";
			this.BranchNameTitle.Size = new System.Drawing.Size(70, 18);
			this.BranchNameTitle.TabIndex = 4;
			this.BranchNameTitle.Text = "Branch:";
			this.BranchNameTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.BranchNameTitle.Visible = false;
			// 
			// CurrentRevisionTitle
			// 
			this.CurrentRevisionTitle.AutoSize = true;
			this.CurrentRevisionTitle.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.CurrentRevisionTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.CurrentRevisionTitle.Location = new System.Drawing.Point(6, 14);
			this.CurrentRevisionTitle.Name = "CurrentRevisionTitle";
			this.CurrentRevisionTitle.Size = new System.Drawing.Size(150, 18);
			this.CurrentRevisionTitle.TabIndex = 3;
			this.CurrentRevisionTitle.Text = "Current Revision:";
			this.CurrentRevisionTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.CurrentRevisionTitle.Visible = false;
			// 
			// CloneRepositoryButton
			// 
			this.CloneRepositoryButton.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.CloneRepositoryButton.Location = new System.Drawing.Point(311, 191);
			this.CloneRepositoryButton.Name = "CloneRepositoryButton";
			this.CloneRepositoryButton.Size = new System.Drawing.Size(229, 34);
			this.CloneRepositoryButton.TabIndex = 2;
			this.CloneRepositoryButton.Text = "Clone Repository";
			this.CloneRepositoryButton.UseVisualStyleBackColor = true;
			this.CloneRepositoryButton.Visible = false;
			this.CloneRepositoryButton.Click += new System.EventHandler(this.CloneRepositoryButton_Click);
			// 
			// RepoProgressBarLabel
			// 
			this.RepoProgressBarLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.RepoProgressBarLabel.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.RepoProgressBarLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.RepoProgressBarLabel.Location = new System.Drawing.Point(184, 142);
			this.RepoProgressBarLabel.Name = "RepoProgressBarLabel";
			this.RepoProgressBarLabel.Size = new System.Drawing.Size(499, 46);
			this.RepoProgressBarLabel.TabIndex = 1;
			this.RepoProgressBarLabel.Text = "Searching for Repository...";
			this.RepoProgressBarLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// RepoProgressBar
			// 
			this.RepoProgressBar.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.RepoProgressBar.Location = new System.Drawing.Point(184, 191);
			this.RepoProgressBar.Name = "RepoProgressBar";
			this.RepoProgressBar.Size = new System.Drawing.Size(499, 23);
			this.RepoProgressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
			this.RepoProgressBar.TabIndex = 0;
			// 
			// Panels
			// 
			this.Panels.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.Panels.Controls.Add(this.RepoPanel);
			this.Panels.Controls.Add(this.BYONDPanel);
			this.Panels.Controls.Add(this.ServerPanel);
			this.Panels.Controls.Add(this.ChatPanel);
			this.Panels.Controls.Add(this.StaticPanel);
			this.Panels.Cursor = System.Windows.Forms.Cursors.Default;
			this.Panels.Location = new System.Drawing.Point(12, 12);
			this.Panels.Name = "Panels";
			this.Panels.SelectedIndex = 0;
			this.Panels.Size = new System.Drawing.Size(876, 392);
			this.Panels.TabIndex = 3;
			// 
			// StaticPanel
			// 
			this.StaticPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(133)))), ((int)(((byte)(143)))), ((int)(((byte)(160)))));
			this.StaticPanel.Controls.Add(this.RecreateStaticButton);
			this.StaticPanel.Controls.Add(this.StaticFileDownloadButton);
			this.StaticPanel.Controls.Add(this.StaticFilesRefreshButton);
			this.StaticPanel.Controls.Add(this.StaticFileUploadButton);
			this.StaticPanel.Controls.Add(this.StaticFileEditTextbox);
			this.StaticPanel.Controls.Add(this.StaticFileDeleteButton);
			this.StaticPanel.Controls.Add(this.StaticFileSaveButton);
			this.StaticPanel.Controls.Add(this.StaticFileCreateButton);
			this.StaticPanel.Controls.Add(this.label6);
			this.StaticPanel.Controls.Add(this.StaticFileListBox);
			this.StaticPanel.Location = new System.Drawing.Point(4, 22);
			this.StaticPanel.Name = "StaticPanel";
			this.StaticPanel.Padding = new System.Windows.Forms.Padding(3);
			this.StaticPanel.Size = new System.Drawing.Size(868, 366);
			this.StaticPanel.TabIndex = 5;
			this.StaticPanel.Text = "Static Files";
			// 
			// RecreateStaticButton
			// 
			this.RecreateStaticButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.RecreateStaticButton.Location = new System.Drawing.Point(471, 6);
			this.RecreateStaticButton.Name = "RecreateStaticButton";
			this.RecreateStaticButton.Size = new System.Drawing.Size(144, 22);
			this.RecreateStaticButton.TabIndex = 33;
			this.RecreateStaticButton.Text = "Recreate Static Directory";
			this.RecreateStaticButton.UseVisualStyleBackColor = true;
			this.RecreateStaticButton.Click += new System.EventHandler(this.RecreateStaticButton_Click);
			// 
			// StaticFileDownloadButton
			// 
			this.StaticFileDownloadButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.StaticFileDownloadButton.Location = new System.Drawing.Point(296, 6);
			this.StaticFileDownloadButton.Name = "StaticFileDownloadButton";
			this.StaticFileDownloadButton.Size = new System.Drawing.Size(71, 22);
			this.StaticFileDownloadButton.TabIndex = 32;
			this.StaticFileDownloadButton.Text = "Download";
			this.StaticFileDownloadButton.UseVisualStyleBackColor = true;
			this.StaticFileDownloadButton.Click += new System.EventHandler(this.StaticFileDownloadButton_Click);
			// 
			// StaticFilesRefreshButton
			// 
			this.StaticFilesRefreshButton.Location = new System.Drawing.Point(65, 6);
			this.StaticFilesRefreshButton.Name = "StaticFilesRefreshButton";
			this.StaticFilesRefreshButton.Size = new System.Drawing.Size(71, 22);
			this.StaticFilesRefreshButton.TabIndex = 31;
			this.StaticFilesRefreshButton.Text = "Refresh";
			this.StaticFilesRefreshButton.UseVisualStyleBackColor = true;
			this.StaticFilesRefreshButton.Click += new System.EventHandler(this.StaticFilesRefreshButton_Click);
			// 
			// StaticFileUploadButton
			// 
			this.StaticFileUploadButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.StaticFileUploadButton.Location = new System.Drawing.Point(219, 6);
			this.StaticFileUploadButton.Name = "StaticFileUploadButton";
			this.StaticFileUploadButton.Size = new System.Drawing.Size(71, 22);
			this.StaticFileUploadButton.TabIndex = 30;
			this.StaticFileUploadButton.Text = "Upload";
			this.StaticFileUploadButton.UseVisualStyleBackColor = true;
			this.StaticFileUploadButton.Click += new System.EventHandler(this.StaticFileUploadButton_Click);
			// 
			// StaticFileEditTextbox
			// 
			this.StaticFileEditTextbox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.StaticFileEditTextbox.Location = new System.Drawing.Point(219, 31);
			this.StaticFileEditTextbox.Multiline = true;
			this.StaticFileEditTextbox.Name = "StaticFileEditTextbox";
			this.StaticFileEditTextbox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.StaticFileEditTextbox.Size = new System.Drawing.Size(643, 329);
			this.StaticFileEditTextbox.TabIndex = 29;
			// 
			// StaticFileDeleteButton
			// 
			this.StaticFileDeleteButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.StaticFileDeleteButton.Location = new System.Drawing.Point(714, 6);
			this.StaticFileDeleteButton.Name = "StaticFileDeleteButton";
			this.StaticFileDeleteButton.Size = new System.Drawing.Size(71, 22);
			this.StaticFileDeleteButton.TabIndex = 28;
			this.StaticFileDeleteButton.Text = "Delete";
			this.StaticFileDeleteButton.UseVisualStyleBackColor = true;
			this.StaticFileDeleteButton.Click += new System.EventHandler(this.StaticFileDeleteButton_Click);
			// 
			// StaticFileSaveButton
			// 
			this.StaticFileSaveButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.StaticFileSaveButton.Location = new System.Drawing.Point(791, 6);
			this.StaticFileSaveButton.Name = "StaticFileSaveButton";
			this.StaticFileSaveButton.Size = new System.Drawing.Size(71, 22);
			this.StaticFileSaveButton.TabIndex = 27;
			this.StaticFileSaveButton.Text = "Save";
			this.StaticFileSaveButton.UseVisualStyleBackColor = true;
			this.StaticFileSaveButton.Click += new System.EventHandler(this.StaticFileSaveButton_Click);
			// 
			// StaticFileCreateButton
			// 
			this.StaticFileCreateButton.Location = new System.Drawing.Point(142, 6);
			this.StaticFileCreateButton.Name = "StaticFileCreateButton";
			this.StaticFileCreateButton.Size = new System.Drawing.Size(71, 22);
			this.StaticFileCreateButton.TabIndex = 26;
			this.StaticFileCreateButton.Text = "Add";
			this.StaticFileCreateButton.UseVisualStyleBackColor = true;
			this.StaticFileCreateButton.Click += new System.EventHandler(this.StaticFileCreateButton_Click);
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label6.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(242)))));
			this.label6.Location = new System.Drawing.Point(6, 10);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(53, 18);
			this.label6.TabIndex = 14;
			this.label6.Text = "Files:";
			this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// StaticFileListBox
			// 
			this.StaticFileListBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.StaticFileListBox.FormattingEnabled = true;
			this.StaticFileListBox.HorizontalScrollbar = true;
			this.StaticFileListBox.Location = new System.Drawing.Point(6, 31);
			this.StaticFileListBox.Name = "StaticFileListBox";
			this.StaticFileListBox.Size = new System.Drawing.Size(207, 329);
			this.StaticFileListBox.TabIndex = 0;
			this.StaticFileListBox.SelectedIndexChanged += new System.EventHandler(this.StaticFileListBox_SelectedIndexChanged);
			// 
			// ControlPanel
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(133)))), ((int)(((byte)(143)))), ((int)(((byte)(160)))));
			this.ClientSize = new System.Drawing.Size(900, 415);
			this.Controls.Add(this.Panels);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "ControlPanel";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "/tg/station 13 Server Control Panel";
			this.ChatPanel.ResumeLayout(false);
			this.ChatPanel.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.ChatPortSelector)).EndInit();
			this.ChatProviderSelectorPanel.ResumeLayout(false);
			this.ChatProviderSelectorPanel.PerformLayout();
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.ServerPanel.ResumeLayout(false);
			this.ServerPanel.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.AutoUpdateInterval)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.PortSelector)).EndInit();
			this.BYONDPanel.ResumeLayout(false);
			this.BYONDPanel.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.MinorVersionNumeric)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.MajorVersionNumeric)).EndInit();
			this.RepoPanel.ResumeLayout(false);
			this.RepoPanel.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.TestmergeSelector)).EndInit();
			this.Panels.ResumeLayout(false);
			this.StaticPanel.ResumeLayout(false);
			this.StaticPanel.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion
		private System.ComponentModel.BackgroundWorker RepoBGW;
		private System.ComponentModel.BackgroundWorker ServerStartBGW;
		private System.Windows.Forms.TabPage ChatPanel;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.ComboBox IRCModesComboBox;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox AdminChannelsTextbox;
		private System.Windows.Forms.TextBox WDChannelsTextbox;
		private System.Windows.Forms.TextBox GameChannelsTextbox;
		private System.Windows.Forms.TextBox DevChannelsTextbox;
		private System.Windows.Forms.TextBox ChatNicknameText;
		private System.Windows.Forms.TextBox ChatServerText;
		private System.Windows.Forms.TextBox AuthField2;
		private System.Windows.Forms.TextBox AuthField1;
		private System.Windows.Forms.TextBox ChatAdminsTextBox;
		private System.Windows.Forms.Button ChatRefreshButton;
		private System.Windows.Forms.Label ChatNicknameTitle;
		private System.Windows.Forms.NumericUpDown ChatPortSelector;
		private System.Windows.Forms.Label ChatPortTitle;
		private System.Windows.Forms.Label ChatServerTitle;
		private System.Windows.Forms.Button ChatApplyButton;
		private System.Windows.Forms.Label AuthField2Title;
		private System.Windows.Forms.Label AuthField1Title;
		private System.Windows.Forms.Button ChatReconnectButton;
		private System.Windows.Forms.Label ChatStatusLabel;
		private System.Windows.Forms.Label ChatStatusTitle;
		private System.Windows.Forms.CheckBox ChatEnabledCheckbox;
		private System.Windows.Forms.Panel ChatProviderSelectorPanel;
		private System.Windows.Forms.RadioButton DiscordProviderSwitch;
		private System.Windows.Forms.RadioButton IRCProviderSwitch;
		private System.Windows.Forms.Label ChatProviderTitle;
		private System.Windows.Forms.Label ChannelsTitle;
		private System.Windows.Forms.Label ChatAdminsTitle;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.RadioButton AdminModeSpecial;
		private System.Windows.Forms.RadioButton AdminModeNormal;
		private System.Windows.Forms.TabPage ServerPanel;
		private System.Windows.Forms.CheckBox AutostartCheckbox;
		private System.Windows.Forms.CheckBox WebclientCheckBox;
		private System.Windows.Forms.Button WorldAnnounceButton;
		private System.Windows.Forms.TextBox WorldAnnounceField;
		private System.Windows.Forms.Label WorldAnnounceLabel;
		private System.Windows.Forms.ComboBox SecuritySelector;
		private System.Windows.Forms.Label SecurityTitle;
		private System.Windows.Forms.Button RemoveAllTestMergesButton;
		private System.Windows.Forms.Label ServerPathLabel;
		private System.Windows.Forms.Button CompileCancelButton;
		private System.Windows.Forms.Label ProjectPathLabel;
		private System.Windows.Forms.Label PortLabel;
		private System.Windows.Forms.NumericUpDown PortSelector;
		private System.Windows.Forms.Button TestMergeManagerButton;
		private System.Windows.Forms.Button UpdateServerButton;
		private System.Windows.Forms.Button ServerGRestartButton;
		private System.Windows.Forms.CheckBox ServerGStopButton;
		private System.Windows.Forms.Button ServerRestartButton;
		private System.Windows.Forms.Button ServerStopButton;
		private System.Windows.Forms.Button ServerStartButton;
		private System.Windows.Forms.Label CompilerStatusLabel;
		private System.Windows.Forms.Label CompilerLabel;
		private System.Windows.Forms.Button compileButton;
		private System.Windows.Forms.Button initializeButton;
		private System.Windows.Forms.Label ServerStatusLabel;
		private System.Windows.Forms.Label ServerStatusTitle;
		private System.Windows.Forms.TabPage BYONDPanel;
		private System.Windows.Forms.Label LatestVersionLabel;
		private System.Windows.Forms.Label LatestVersionTitle;
		private System.Windows.Forms.Label StagedVersionLabel;
		private System.Windows.Forms.Label StagedVersionTitle;
		private System.Windows.Forms.Label StatusLabel;
		private System.Windows.Forms.Label VersionLabel;
		private System.Windows.Forms.Label VersionTitle;
		private System.Windows.Forms.Label MinorVersionLabel;
		private System.Windows.Forms.Label MajorVersionLabel;
		private System.Windows.Forms.Button UpdateButton;
		private System.Windows.Forms.NumericUpDown MinorVersionNumeric;
		private System.Windows.Forms.NumericUpDown MajorVersionNumeric;
		private System.Windows.Forms.TabPage RepoPanel;
		private System.Windows.Forms.Button RepoRefreshButton;
		private System.Windows.Forms.ListBox BackupTagsList;
		private System.Windows.Forms.Button ResetRemote;
		private System.Windows.Forms.Button RecloneButton;
		private System.Windows.Forms.TextBox RepoBranchTextBox;
		private System.Windows.Forms.TextBox RepoRemoteTextBox;
		private System.Windows.Forms.Button RepoGenChangelogButton;
		private System.Windows.Forms.NumericUpDown TestmergeSelector;
		private System.Windows.Forms.ListBox TestMergeListLabel;
		private System.Windows.Forms.Label CurrentRevisionLabel;
		private System.Windows.Forms.Button RepoApplyButton;
		private System.Windows.Forms.Button HardReset;
		private System.Windows.Forms.Button UpdateRepoButton;
		private System.Windows.Forms.Button MergePRButton;
		private System.Windows.Forms.Label IdentityLabel;
		private System.Windows.Forms.Label TestMergeListTitle;
		private System.Windows.Forms.Label RemoteNameTitle;
		private System.Windows.Forms.Label BranchNameTitle;
		private System.Windows.Forms.Label CurrentRevisionTitle;
		private System.Windows.Forms.Button CloneRepositoryButton;
		private System.Windows.Forms.Label RepoProgressBarLabel;
		private System.Windows.Forms.ProgressBar RepoProgressBar;
		private System.Windows.Forms.TabControl Panels;
		private System.Windows.Forms.TabPage StaticPanel;
		private System.Windows.Forms.Button StaticFileCreateButton;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.ListBox StaticFileListBox;
		private System.Windows.Forms.Button StaticFileUploadButton;
		private System.Windows.Forms.TextBox StaticFileEditTextbox;
		private System.Windows.Forms.Button StaticFileDeleteButton;
		private System.Windows.Forms.Button StaticFileSaveButton;
		private System.Windows.Forms.Button StaticFilesRefreshButton;
		private System.Windows.Forms.Button StaticFileDownloadButton;
		private System.Windows.Forms.Button TGSJsonUpdate;
		private System.Windows.Forms.Button BYONDRefreshButton;
		private System.Windows.Forms.Button ServerPageRefreshButton;
		private System.Windows.Forms.Button RecreateStaticButton;
		private System.Windows.Forms.NumericUpDown AutoUpdateInterval;
		private System.Windows.Forms.CheckBox AutoUpdateCheckbox;
		private System.Windows.Forms.Label AutoUpdateMLabel;
		private System.Windows.Forms.TextBox projectNameText;
		private System.Windows.Forms.CheckBox SyncCommitsCheckBox;
	}
}
