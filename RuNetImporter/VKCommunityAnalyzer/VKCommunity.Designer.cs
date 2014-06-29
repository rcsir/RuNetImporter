namespace VKCommunityAnalyzer
{
    partial class VKCommunity
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
            this.groupDescription = new System.Windows.Forms.TextBox();
            this.groupId2 = new System.Windows.Forms.TextBox();
            this.FindCommunityButton = new System.Windows.Forms.Button();
            this.WorkingFolderTextBox = new System.Windows.Forms.TextBox();
            this.WorkingFolderButton = new System.Windows.Forms.Button();
            this.userIdTextBox = new System.Windows.Forms.TextBox();
            this.AuthorizeButton = new System.Windows.Forms.Button();
            this.GroupsProgressBar = new System.Windows.Forms.ProgressBar();
            this.CancelJobBurron = new System.Windows.Forms.Button();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.groupsStatusStrip = new System.Windows.Forms.StatusStrip();
            this.stripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.backgroundMembersWorker = new System.ComponentModel.BackgroundWorker();
            this.DownloadGroupMembers = new System.Windows.Forms.Button();
            this.backgroundEgoNetWorker = new System.ComponentModel.BackgroundWorker();
            this.groupsStatusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupDescription
            // 
            this.groupDescription.Location = new System.Drawing.Point(138, 175);
            this.groupDescription.Multiline = true;
            this.groupDescription.Name = "groupDescription";
            this.groupDescription.ReadOnly = true;
            this.groupDescription.Size = new System.Drawing.Size(222, 73);
            this.groupDescription.TabIndex = 20;
            this.groupDescription.TabStop = false;
            // 
            // groupId2
            // 
            this.groupId2.Location = new System.Drawing.Point(12, 175);
            this.groupId2.Name = "groupId2";
            this.groupId2.ReadOnly = true;
            this.groupId2.Size = new System.Drawing.Size(120, 20);
            this.groupId2.TabIndex = 19;
            this.groupId2.TabStop = false;
            // 
            // FindCommunityButton
            // 
            this.FindCommunityButton.Location = new System.Drawing.Point(12, 139);
            this.FindCommunityButton.Name = "FindCommunityButton";
            this.FindCommunityButton.Size = new System.Drawing.Size(348, 30);
            this.FindCommunityButton.TabIndex = 18;
            this.FindCommunityButton.Text = "Find Community...";
            this.FindCommunityButton.UseVisualStyleBackColor = true;
            this.FindCommunityButton.Click += new System.EventHandler(this.FindCommunityButton_Click);
            // 
            // WorkingFolderTextBox
            // 
            this.WorkingFolderTextBox.Location = new System.Drawing.Point(12, 113);
            this.WorkingFolderTextBox.Name = "WorkingFolderTextBox";
            this.WorkingFolderTextBox.ReadOnly = true;
            this.WorkingFolderTextBox.Size = new System.Drawing.Size(348, 20);
            this.WorkingFolderTextBox.TabIndex = 17;
            // 
            // WorkingFolderButton
            // 
            this.WorkingFolderButton.Location = new System.Drawing.Point(12, 77);
            this.WorkingFolderButton.Name = "WorkingFolderButton";
            this.WorkingFolderButton.Size = new System.Drawing.Size(348, 30);
            this.WorkingFolderButton.TabIndex = 16;
            this.WorkingFolderButton.Text = "Working Folder...";
            this.WorkingFolderButton.UseVisualStyleBackColor = true;
            this.WorkingFolderButton.Click += new System.EventHandler(this.WorkingFolderButton_Click);
            // 
            // userIdTextBox
            // 
            this.userIdTextBox.Location = new System.Drawing.Point(12, 51);
            this.userIdTextBox.Name = "userIdTextBox";
            this.userIdTextBox.ReadOnly = true;
            this.userIdTextBox.Size = new System.Drawing.Size(348, 20);
            this.userIdTextBox.TabIndex = 15;
            // 
            // AuthorizeButton
            // 
            this.AuthorizeButton.Location = new System.Drawing.Point(12, 15);
            this.AuthorizeButton.Name = "AuthorizeButton";
            this.AuthorizeButton.Size = new System.Drawing.Size(348, 30);
            this.AuthorizeButton.TabIndex = 14;
            this.AuthorizeButton.Text = "Authorize...";
            this.AuthorizeButton.UseVisualStyleBackColor = true;
            this.AuthorizeButton.Click += new System.EventHandler(this.AuthorizeButton_Click);
            // 
            // GroupsProgressBar
            // 
            this.GroupsProgressBar.Location = new System.Drawing.Point(12, 406);
            this.GroupsProgressBar.Name = "GroupsProgressBar";
            this.GroupsProgressBar.Size = new System.Drawing.Size(351, 22);
            this.GroupsProgressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.GroupsProgressBar.TabIndex = 22;
            // 
            // CancelJobBurron
            // 
            this.CancelJobBurron.Enabled = false;
            this.CancelJobBurron.Location = new System.Drawing.Point(12, 370);
            this.CancelJobBurron.Name = "CancelJobBurron";
            this.CancelJobBurron.Size = new System.Drawing.Size(117, 30);
            this.CancelJobBurron.TabIndex = 21;
            this.CancelJobBurron.Text = "Cancel Operation";
            this.CancelJobBurron.UseVisualStyleBackColor = true;
            this.CancelJobBurron.Click += new System.EventHandler(this.CancelJobBurron_Click);
            // 
            // groupsStatusStrip
            // 
            this.groupsStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.stripStatusLabel});
            this.groupsStatusStrip.Location = new System.Drawing.Point(0, 438);
            this.groupsStatusStrip.Name = "groupsStatusStrip";
            this.groupsStatusStrip.Size = new System.Drawing.Size(371, 22);
            this.groupsStatusStrip.TabIndex = 23;
            this.groupsStatusStrip.Text = "statusStrip1";
            // 
            // stripStatusLabel
            // 
            this.stripStatusLabel.Name = "stripStatusLabel";
            this.stripStatusLabel.Size = new System.Drawing.Size(39, 17);
            this.stripStatusLabel.Text = "Status";
            // 
            // backgroundMembersWorker
            // 
            this.backgroundMembersWorker.WorkerReportsProgress = true;
            this.backgroundMembersWorker.WorkerSupportsCancellation = true;
            // 
            // DownloadGroupMembers
            // 
            this.DownloadGroupMembers.Location = new System.Drawing.Point(15, 263);
            this.DownloadGroupMembers.Name = "DownloadGroupMembers";
            this.DownloadGroupMembers.Size = new System.Drawing.Size(348, 30);
            this.DownloadGroupMembers.TabIndex = 24;
            this.DownloadGroupMembers.Text = "Download Members...";
            this.DownloadGroupMembers.UseVisualStyleBackColor = true;
            this.DownloadGroupMembers.Click += new System.EventHandler(this.DownloadGroupMembers_Click);
            // 
            // backgroundEgoNetWorker
            // 
            this.backgroundEgoNetWorker.WorkerReportsProgress = true;
            this.backgroundEgoNetWorker.WorkerSupportsCancellation = true;
            // 
            // VKCommunity
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(371, 460);
            this.Controls.Add(this.DownloadGroupMembers);
            this.Controls.Add(this.groupsStatusStrip);
            this.Controls.Add(this.GroupsProgressBar);
            this.Controls.Add(this.CancelJobBurron);
            this.Controls.Add(this.groupDescription);
            this.Controls.Add(this.groupId2);
            this.Controls.Add(this.FindCommunityButton);
            this.Controls.Add(this.WorkingFolderTextBox);
            this.Controls.Add(this.WorkingFolderButton);
            this.Controls.Add(this.userIdTextBox);
            this.Controls.Add(this.AuthorizeButton);
            this.Name = "VKCommunity";
            this.Text = "VK Community Analyzer";
            this.groupsStatusStrip.ResumeLayout(false);
            this.groupsStatusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox groupDescription;
        private System.Windows.Forms.TextBox groupId2;
        private System.Windows.Forms.Button FindCommunityButton;
        private System.Windows.Forms.TextBox WorkingFolderTextBox;
        private System.Windows.Forms.Button WorkingFolderButton;
        private System.Windows.Forms.TextBox userIdTextBox;
        private System.Windows.Forms.Button AuthorizeButton;
        private System.Windows.Forms.ProgressBar GroupsProgressBar;
        private System.Windows.Forms.Button CancelJobBurron;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.StatusStrip groupsStatusStrip;
        private System.Windows.Forms.ToolStripStatusLabel stripStatusLabel;
        private System.ComponentModel.BackgroundWorker backgroundMembersWorker;
        private System.Windows.Forms.Button DownloadGroupMembers;
        private System.ComponentModel.BackgroundWorker backgroundEgoNetWorker;
    }
}

