namespace Tgstation.Server.ControlPanel
{
	partial class MainWindow
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow));
			this.instanceListSplit = new System.Windows.Forms.SplitContainer();
			this.instanceBrowser = new System.Windows.Forms.TreeView();
			this.consoleSplit = new System.Windows.Forms.SplitContainer();
			this.mainMenu1 = new System.Windows.Forms.MainMenu(this.components);
			this.menuItem1 = new System.Windows.Forms.MenuItem();
			this.menuExport = new System.Windows.Forms.MenuItem();
			this.menuImport = new System.Windows.Forms.MenuItem();
			this.menuQuit = new System.Windows.Forms.MenuItem();
			this.rootContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.newConnectionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.serverContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.refreshToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			((System.ComponentModel.ISupportInitialize)(this.instanceListSplit)).BeginInit();
			this.instanceListSplit.Panel1.SuspendLayout();
			this.instanceListSplit.Panel2.SuspendLayout();
			this.instanceListSplit.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.consoleSplit)).BeginInit();
			this.consoleSplit.SuspendLayout();
			this.rootContextMenuStrip.SuspendLayout();
			this.serverContextMenuStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// instanceListSplit
			// 
			this.instanceListSplit.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.instanceListSplit.Dock = System.Windows.Forms.DockStyle.Fill;
			this.instanceListSplit.Location = new System.Drawing.Point(0, 0);
			this.instanceListSplit.Name = "instanceListSplit";
			// 
			// instanceListSplit.Panel1
			// 
			this.instanceListSplit.Panel1.Controls.Add(this.instanceBrowser);
			// 
			// instanceListSplit.Panel2
			// 
			this.instanceListSplit.Panel2.Controls.Add(this.consoleSplit);
			this.instanceListSplit.Size = new System.Drawing.Size(1428, 747);
			this.instanceListSplit.SplitterDistance = 322;
			this.instanceListSplit.TabIndex = 0;
			// 
			// instanceBrowser
			// 
			this.instanceBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
			this.instanceBrowser.Location = new System.Drawing.Point(0, 0);
			this.instanceBrowser.Name = "instanceBrowser";
			this.instanceBrowser.Size = new System.Drawing.Size(318, 743);
			this.instanceBrowser.TabIndex = 0;
			// 
			// consoleSplit
			// 
			this.consoleSplit.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.consoleSplit.Dock = System.Windows.Forms.DockStyle.Fill;
			this.consoleSplit.Location = new System.Drawing.Point(0, 0);
			this.consoleSplit.Name = "consoleSplit";
			this.consoleSplit.Orientation = System.Windows.Forms.Orientation.Horizontal;
			this.consoleSplit.Size = new System.Drawing.Size(1102, 747);
			this.consoleSplit.SplitterDistance = 603;
			this.consoleSplit.TabIndex = 0;
			// 
			// mainMenu1
			// 
			this.mainMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItem1});
			// 
			// menuItem1
			// 
			this.menuItem1.Index = 0;
			this.menuItem1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuExport,
            this.menuImport,
            this.menuQuit});
			this.menuItem1.Text = "File";
			// 
			// menuExport
			// 
			this.menuExport.Index = 0;
			this.menuExport.Text = "Export Settings";
			// 
			// menuImport
			// 
			this.menuImport.Index = 1;
			this.menuImport.Text = "Import Settings";
			// 
			// menuQuit
			// 
			this.menuQuit.Index = 2;
			this.menuQuit.Text = "Quit";
			this.menuQuit.Click += new System.EventHandler(this.menuQuit_Click);
			// 
			// rootContextMenuStrip
			// 
			this.rootContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newConnectionToolStripMenuItem});
			this.rootContextMenuStrip.Name = "rootContextMenuStrip";
			this.rootContextMenuStrip.Size = new System.Drawing.Size(164, 26);
			// 
			// newConnectionToolStripMenuItem
			// 
			this.newConnectionToolStripMenuItem.Name = "newConnectionToolStripMenuItem";
			this.newConnectionToolStripMenuItem.Size = new System.Drawing.Size(163, 22);
			this.newConnectionToolStripMenuItem.Text = "New Connection";
			this.newConnectionToolStripMenuItem.Click += new System.EventHandler(this.newConnectionToolStripMenuItem_Click);
			// 
			// serverContextMenuStrip
			// 
			this.serverContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.refreshToolStripMenuItem,
            this.deleteToolStripMenuItem});
			this.serverContextMenuStrip.Name = "serverContextMenuStrip";
			this.serverContextMenuStrip.Size = new System.Drawing.Size(114, 48);
			// 
			// refreshToolStripMenuItem
			// 
			this.refreshToolStripMenuItem.Name = "refreshToolStripMenuItem";
			this.refreshToolStripMenuItem.Size = new System.Drawing.Size(113, 22);
			this.refreshToolStripMenuItem.Text = "Refresh";
			// 
			// deleteToolStripMenuItem
			// 
			this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
			this.deleteToolStripMenuItem.Size = new System.Drawing.Size(113, 22);
			this.deleteToolStripMenuItem.Text = "Delete";
			// 
			// MainWindow
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1428, 747);
			this.Controls.Add(this.instanceListSplit);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Menu = this.mainMenu1;
			this.Name = "MainWindow";
			this.Text = "Tgstation Server Control Panel";
			this.instanceListSplit.Panel1.ResumeLayout(false);
			this.instanceListSplit.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.instanceListSplit)).EndInit();
			this.instanceListSplit.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.consoleSplit)).EndInit();
			this.consoleSplit.ResumeLayout(false);
			this.rootContextMenuStrip.ResumeLayout(false);
			this.serverContextMenuStrip.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.SplitContainer instanceListSplit;
		private System.Windows.Forms.MainMenu mainMenu1;
		private System.Windows.Forms.MenuItem menuItem1;
		private System.Windows.Forms.MenuItem menuExport;
		private System.Windows.Forms.MenuItem menuQuit;
		private System.Windows.Forms.MenuItem menuImport;
		private System.Windows.Forms.TreeView instanceBrowser;
		private System.Windows.Forms.SplitContainer consoleSplit;
		private System.Windows.Forms.ContextMenuStrip rootContextMenuStrip;
		private System.Windows.Forms.ToolStripMenuItem newConnectionToolStripMenuItem;
		private System.Windows.Forms.ContextMenuStrip serverContextMenuStrip;
		private System.Windows.Forms.ToolStripMenuItem refreshToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
	}
}