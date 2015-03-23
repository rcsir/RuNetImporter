namespace VKGroups
{
    partial class VKGroupsForm
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
            this.userIdTextBox = new System.Windows.Forms.TextBox();
            this.AuthorizeButton = new System.Windows.Forms.Button();
            this.backgroundGroupsWorker = new System.ComponentModel.BackgroundWorker();
            this.groupsStatusStrip = new System.Windows.Forms.StatusStrip();
            this.groupsStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.WorkingFolderButton = new System.Windows.Forms.Button();
            this.WorkingFolderTextBox = new System.Windows.Forms.TextBox();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.FindGroupsButton = new System.Windows.Forms.Button();
            this.DownloadGroupPosts = new System.Windows.Forms.Button();
            this.CancelJobBurron = new System.Windows.Forms.Button();
            this.GroupsProgressBar = new System.Windows.Forms.ProgressBar();
            this.DownloadGroupMembers = new System.Windows.Forms.Button();
            this.backgroundMembersWorker = new System.ComponentModel.BackgroundWorker();
            this.groupId2 = new System.Windows.Forms.TextBox();
            this.groupDescription = new System.Windows.Forms.TextBox();
            this.DownloadMembersNetwork = new System.Windows.Forms.Button();
            this.backgroundNetworkWorker = new System.ComponentModel.BackgroundWorker();
            this.backgroundEgoNetWorker = new System.ComponentModel.BackgroundWorker();
            this.DownloadEgoNets = new System.Windows.Forms.Button();
            this.groupsStatusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // userIdTextBox
            // 
            this.userIdTextBox.Location = new System.Drawing.Point(24, 92);
            this.userIdTextBox.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.userIdTextBox.Name = "userIdTextBox";
            this.userIdTextBox.ReadOnly = true;
            this.userIdTextBox.Size = new System.Drawing.Size(692, 31);
            this.userIdTextBox.TabIndex = 3;
            this.userIdTextBox.TextChanged += new System.EventHandler(this.userIdTextBox_TextChanged);
            // 
            // AuthorizeButton
            // 
            this.AuthorizeButton.Location = new System.Drawing.Point(24, 23);
            this.AuthorizeButton.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.AuthorizeButton.Name = "AuthorizeButton";
            this.AuthorizeButton.Size = new System.Drawing.Size(696, 58);
            this.AuthorizeButton.TabIndex = 2;
            this.AuthorizeButton.Text = "Authorize...";
            this.AuthorizeButton.UseVisualStyleBackColor = true;
            this.AuthorizeButton.Click += new System.EventHandler(this.AuthorizeButton_Click);
            // 
            // backgroundGroupsWorker
            // 
            this.backgroundGroupsWorker.WorkerReportsProgress = true;
            this.backgroundGroupsWorker.WorkerSupportsCancellation = true;
            // 
            // groupsStatusStrip
            // 
            this.groupsStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.groupsStripStatusLabel});
            this.groupsStatusStrip.Location = new System.Drawing.Point(0, 975);
            this.groupsStatusStrip.Name = "groupsStatusStrip";
            this.groupsStatusStrip.Padding = new System.Windows.Forms.Padding(2, 0, 28, 0);
            this.groupsStatusStrip.Size = new System.Drawing.Size(750, 37);
            this.groupsStatusStrip.TabIndex = 4;
            this.groupsStatusStrip.Text = "statusStrip1";
            // 
            // groupsStripStatusLabel
            // 
            this.groupsStripStatusLabel.Name = "groupsStripStatusLabel";
            this.groupsStripStatusLabel.Size = new System.Drawing.Size(80, 32);
            this.groupsStripStatusLabel.Text = "Status";
            // 
            // WorkingFolderButton
            // 
            this.WorkingFolderButton.Location = new System.Drawing.Point(24, 142);
            this.WorkingFolderButton.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.WorkingFolderButton.Name = "WorkingFolderButton";
            this.WorkingFolderButton.Size = new System.Drawing.Size(696, 58);
            this.WorkingFolderButton.TabIndex = 5;
            this.WorkingFolderButton.Text = "Working Folder...";
            this.WorkingFolderButton.UseVisualStyleBackColor = true;
            this.WorkingFolderButton.Click += new System.EventHandler(this.WorkingFolderButton_Click);
            // 
            // WorkingFolderTextBox
            // 
            this.WorkingFolderTextBox.Location = new System.Drawing.Point(24, 212);
            this.WorkingFolderTextBox.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.WorkingFolderTextBox.Name = "WorkingFolderTextBox";
            this.WorkingFolderTextBox.ReadOnly = true;
            this.WorkingFolderTextBox.Size = new System.Drawing.Size(692, 31);
            this.WorkingFolderTextBox.TabIndex = 6;
            this.WorkingFolderTextBox.TextChanged += new System.EventHandler(this.WorkingFolderTextBox_TextChanged);
            // 
            // FindGroupsButton
            // 
            this.FindGroupsButton.Location = new System.Drawing.Point(24, 262);
            this.FindGroupsButton.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.FindGroupsButton.Name = "FindGroupsButton";
            this.FindGroupsButton.Size = new System.Drawing.Size(696, 58);
            this.FindGroupsButton.TabIndex = 7;
            this.FindGroupsButton.Text = "Find Groups...";
            this.FindGroupsButton.UseVisualStyleBackColor = true;
            this.FindGroupsButton.Click += new System.EventHandler(this.FindGroupsButton_Click);
            // 
            // DownloadGroupPosts
            // 
            this.DownloadGroupPosts.Location = new System.Drawing.Point(24, 552);
            this.DownloadGroupPosts.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.DownloadGroupPosts.Name = "DownloadGroupPosts";
            this.DownloadGroupPosts.Size = new System.Drawing.Size(696, 58);
            this.DownloadGroupPosts.TabIndex = 8;
            this.DownloadGroupPosts.Text = "Download Posts and Comments...";
            this.DownloadGroupPosts.UseVisualStyleBackColor = true;
            this.DownloadGroupPosts.Click += new System.EventHandler(this.DownloadGroupPosts_Click);
            // 
            // CancelJobBurron
            // 
            this.CancelJobBurron.Enabled = false;
            this.CancelJobBurron.Location = new System.Drawing.Point(24, 837);
            this.CancelJobBurron.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.CancelJobBurron.Name = "CancelJobBurron";
            this.CancelJobBurron.Size = new System.Drawing.Size(234, 58);
            this.CancelJobBurron.TabIndex = 9;
            this.CancelJobBurron.Text = "Cancel Operation";
            this.CancelJobBurron.UseVisualStyleBackColor = true;
            this.CancelJobBurron.Click += new System.EventHandler(this.CancelJobButton_Click);
            // 
            // GroupsProgressBar
            // 
            this.GroupsProgressBar.Location = new System.Drawing.Point(24, 906);
            this.GroupsProgressBar.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.GroupsProgressBar.Name = "GroupsProgressBar";
            this.GroupsProgressBar.Size = new System.Drawing.Size(702, 42);
            this.GroupsProgressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.GroupsProgressBar.TabIndex = 10;
            // 
            // DownloadGroupMembers
            // 
            this.DownloadGroupMembers.Location = new System.Drawing.Point(24, 483);
            this.DownloadGroupMembers.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.DownloadGroupMembers.Name = "DownloadGroupMembers";
            this.DownloadGroupMembers.Size = new System.Drawing.Size(696, 58);
            this.DownloadGroupMembers.TabIndex = 11;
            this.DownloadGroupMembers.Text = "Download Members...";
            this.DownloadGroupMembers.UseVisualStyleBackColor = true;
            this.DownloadGroupMembers.Click += new System.EventHandler(this.DownloadGroupMembers_Click);
            // 
            // backgroundMembersWorker
            // 
            this.backgroundMembersWorker.WorkerReportsProgress = true;
            this.backgroundMembersWorker.WorkerSupportsCancellation = true;
            // 
            // groupId2
            // 
            this.groupId2.Location = new System.Drawing.Point(24, 331);
            this.groupId2.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.groupId2.Name = "groupId2";
            this.groupId2.ReadOnly = true;
            this.groupId2.Size = new System.Drawing.Size(236, 31);
            this.groupId2.TabIndex = 12;
            this.groupId2.TabStop = false;
            this.groupId2.TextChanged += new System.EventHandler(this.groupId2_TextChanged);
            // 
            // groupDescription
            // 
            this.groupDescription.Location = new System.Drawing.Point(276, 331);
            this.groupDescription.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.groupDescription.Multiline = true;
            this.groupDescription.Name = "groupDescription";
            this.groupDescription.ReadOnly = true;
            this.groupDescription.Size = new System.Drawing.Size(440, 137);
            this.groupDescription.TabIndex = 13;
            this.groupDescription.TabStop = false;
            this.groupDescription.TextChanged += new System.EventHandler(this.groupDescription_TextChanged);
            // 
            // DownloadMembersNetwork
            // 
            this.DownloadMembersNetwork.Location = new System.Drawing.Point(24, 621);
            this.DownloadMembersNetwork.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.DownloadMembersNetwork.Name = "DownloadMembersNetwork";
            this.DownloadMembersNetwork.Size = new System.Drawing.Size(696, 58);
            this.DownloadMembersNetwork.TabIndex = 14;
            this.DownloadMembersNetwork.Text = "Download Groups Network...";
            this.DownloadMembersNetwork.UseVisualStyleBackColor = true;
            this.DownloadMembersNetwork.Click += new System.EventHandler(this.DownloadMembersNetwork_Click);
            // 
            // backgroundNetworkWorker
            // 
            this.backgroundNetworkWorker.WorkerReportsProgress = true;
            this.backgroundNetworkWorker.WorkerSupportsCancellation = true;
            // 
            // backgroundEgoNetWorker
            // 
            this.backgroundEgoNetWorker.WorkerReportsProgress = true;
            this.backgroundEgoNetWorker.WorkerSupportsCancellation = true;
            // 
            // DownloadEgoNets
            // 
            this.DownloadEgoNets.Location = new System.Drawing.Point(24, 760);
            this.DownloadEgoNets.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.DownloadEgoNets.Name = "DownloadEgoNets";
            this.DownloadEgoNets.Size = new System.Drawing.Size(696, 58);
            this.DownloadEgoNets.TabIndex = 16;
            this.DownloadEgoNets.Text = "Download Members Ego Nets...";
            this.DownloadEgoNets.UseVisualStyleBackColor = true;
            this.DownloadEgoNets.Click += new System.EventHandler(this.DownloadEgoNets_Click);
            // 
            // VKGroupsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(750, 1012);
            this.Controls.Add(this.DownloadEgoNets);
            this.Controls.Add(this.DownloadMembersNetwork);
            this.Controls.Add(this.groupDescription);
            this.Controls.Add(this.groupId2);
            this.Controls.Add(this.DownloadGroupMembers);
            this.Controls.Add(this.GroupsProgressBar);
            this.Controls.Add(this.CancelJobBurron);
            this.Controls.Add(this.DownloadGroupPosts);
            this.Controls.Add(this.FindGroupsButton);
            this.Controls.Add(this.WorkingFolderTextBox);
            this.Controls.Add(this.WorkingFolderButton);
            this.Controls.Add(this.groupsStatusStrip);
            this.Controls.Add(this.userIdTextBox);
            this.Controls.Add(this.AuthorizeButton);
            this.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.Name = "VKGroupsForm";
            this.Text = "VKGroups";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.VKGroupsForm_FormClosing);
            this.Load += new System.EventHandler(this.VKGroupsForm_Load);
            this.groupsStatusStrip.ResumeLayout(false);
            this.groupsStatusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox userIdTextBox;
        private System.Windows.Forms.Button AuthorizeButton;
        private System.ComponentModel.BackgroundWorker backgroundGroupsWorker;
        private System.Windows.Forms.StatusStrip groupsStatusStrip;
        private System.Windows.Forms.ToolStripStatusLabel groupsStripStatusLabel;
        private System.Windows.Forms.Button WorkingFolderButton;
        private System.Windows.Forms.TextBox WorkingFolderTextBox;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.Button FindGroupsButton;
        private System.Windows.Forms.Button DownloadGroupPosts;
        private System.Windows.Forms.Button CancelJobBurron;
        private System.Windows.Forms.ProgressBar GroupsProgressBar;
        private System.Windows.Forms.Button DownloadGroupMembers;
        private System.ComponentModel.BackgroundWorker backgroundMembersWorker;
        private System.Windows.Forms.TextBox groupId2;
        private System.Windows.Forms.TextBox groupDescription;
        private System.Windows.Forms.Button DownloadMembersNetwork;
        private System.ComponentModel.BackgroundWorker backgroundNetworkWorker;
        private System.ComponentModel.BackgroundWorker backgroundEgoNetWorker;
        private System.Windows.Forms.Button DownloadEgoNets;
    }
}

