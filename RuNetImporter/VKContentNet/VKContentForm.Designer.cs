namespace VKContentNet
{
    partial class VKContentForm
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
            this.FindGroupsButton = new System.Windows.Forms.Button();
            this.WorkingFolderTextBox = new System.Windows.Forms.TextBox();
            this.WorkingFolderButton = new System.Windows.Forms.Button();
            this.userIdTextBox = new System.Windows.Forms.TextBox();
            this.AuthorizeButton = new System.Windows.Forms.Button();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.GroupsProgressBar = new System.Windows.Forms.ProgressBar();
            this.Authorize = new System.Windows.Forms.Button();
            this.groupsStatusStrip = new System.Windows.Forms.StatusStrip();
            this.groupsStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.DownloadGroupPosts = new System.Windows.Forms.Button();
            this.backgroundGroupsWorker = new System.ComponentModel.BackgroundWorker();
            this.groupsStatusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupDescription
            // 
            this.groupDescription.Location = new System.Drawing.Point(267, 326);
            this.groupDescription.Margin = new System.Windows.Forms.Padding(6);
            this.groupDescription.Multiline = true;
            this.groupDescription.Name = "groupDescription";
            this.groupDescription.ReadOnly = true;
            this.groupDescription.Size = new System.Drawing.Size(440, 137);
            this.groupDescription.TabIndex = 20;
            this.groupDescription.TabStop = false;
            // 
            // groupId2
            // 
            this.groupId2.Location = new System.Drawing.Point(15, 326);
            this.groupId2.Margin = new System.Windows.Forms.Padding(6);
            this.groupId2.Name = "groupId2";
            this.groupId2.ReadOnly = true;
            this.groupId2.Size = new System.Drawing.Size(236, 31);
            this.groupId2.TabIndex = 19;
            this.groupId2.TabStop = false;
            // 
            // FindGroupsButton
            // 
            this.FindGroupsButton.Location = new System.Drawing.Point(15, 257);
            this.FindGroupsButton.Margin = new System.Windows.Forms.Padding(6);
            this.FindGroupsButton.Name = "FindGroupsButton";
            this.FindGroupsButton.Size = new System.Drawing.Size(696, 58);
            this.FindGroupsButton.TabIndex = 18;
            this.FindGroupsButton.Text = "Find Groups...";
            this.FindGroupsButton.UseVisualStyleBackColor = true;
            this.FindGroupsButton.Click += new System.EventHandler(this.FindGroupsButton_Click);
            // 
            // WorkingFolderTextBox
            // 
            this.WorkingFolderTextBox.Location = new System.Drawing.Point(15, 207);
            this.WorkingFolderTextBox.Margin = new System.Windows.Forms.Padding(6);
            this.WorkingFolderTextBox.Name = "WorkingFolderTextBox";
            this.WorkingFolderTextBox.ReadOnly = true;
            this.WorkingFolderTextBox.Size = new System.Drawing.Size(692, 31);
            this.WorkingFolderTextBox.TabIndex = 17;
            // 
            // WorkingFolderButton
            // 
            this.WorkingFolderButton.Location = new System.Drawing.Point(15, 137);
            this.WorkingFolderButton.Margin = new System.Windows.Forms.Padding(6);
            this.WorkingFolderButton.Name = "WorkingFolderButton";
            this.WorkingFolderButton.Size = new System.Drawing.Size(696, 58);
            this.WorkingFolderButton.TabIndex = 16;
            this.WorkingFolderButton.Text = "Working Folder...";
            this.WorkingFolderButton.UseVisualStyleBackColor = true;
            this.WorkingFolderButton.Click += new System.EventHandler(this.WorkingFolderButton_Click);
            // 
            // userIdTextBox
            // 
            this.userIdTextBox.Location = new System.Drawing.Point(15, 87);
            this.userIdTextBox.Margin = new System.Windows.Forms.Padding(6);
            this.userIdTextBox.Name = "userIdTextBox";
            this.userIdTextBox.ReadOnly = true;
            this.userIdTextBox.Size = new System.Drawing.Size(692, 31);
            this.userIdTextBox.TabIndex = 15;
            // 
            // AuthorizeButton
            // 
            this.AuthorizeButton.Location = new System.Drawing.Point(15, 18);
            this.AuthorizeButton.Margin = new System.Windows.Forms.Padding(6);
            this.AuthorizeButton.Name = "AuthorizeButton";
            this.AuthorizeButton.Size = new System.Drawing.Size(696, 58);
            this.AuthorizeButton.TabIndex = 14;
            this.AuthorizeButton.Text = "Authorize...";
            this.AuthorizeButton.UseVisualStyleBackColor = true;
            this.AuthorizeButton.Click += new System.EventHandler(this.AuthorizeButton_Click);
            // 
            // GroupsProgressBar
            // 
            this.GroupsProgressBar.Location = new System.Drawing.Point(15, 707);
            this.GroupsProgressBar.Margin = new System.Windows.Forms.Padding(6);
            this.GroupsProgressBar.Name = "GroupsProgressBar";
            this.GroupsProgressBar.Size = new System.Drawing.Size(702, 42);
            this.GroupsProgressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.GroupsProgressBar.TabIndex = 23;
            // 
            // Authorize
            // 
            this.Authorize.Enabled = false;
            this.Authorize.Location = new System.Drawing.Point(15, 638);
            this.Authorize.Margin = new System.Windows.Forms.Padding(6);
            this.Authorize.Name = "Authorize";
            this.Authorize.Size = new System.Drawing.Size(234, 58);
            this.Authorize.TabIndex = 22;
            this.Authorize.Text = "Cancel Operation";
            this.Authorize.UseVisualStyleBackColor = true;
            this.Authorize.Click += new System.EventHandler(this.CancelJobButton_Click);
            // 
            // groupsStatusStrip
            // 
            this.groupsStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.groupsStripStatusLabel});
            this.groupsStatusStrip.Location = new System.Drawing.Point(0, 787);
            this.groupsStatusStrip.Name = "groupsStatusStrip";
            this.groupsStatusStrip.Padding = new System.Windows.Forms.Padding(2, 0, 28, 0);
            this.groupsStatusStrip.Size = new System.Drawing.Size(730, 37);
            this.groupsStatusStrip.TabIndex = 21;
            this.groupsStatusStrip.Text = "statusStrip1";
            // 
            // groupsStripStatusLabel
            // 
            this.groupsStripStatusLabel.Name = "groupsStripStatusLabel";
            this.groupsStripStatusLabel.Size = new System.Drawing.Size(80, 32);
            this.groupsStripStatusLabel.Text = "Status";
            // 
            // DownloadGroupPosts
            // 
            this.DownloadGroupPosts.Location = new System.Drawing.Point(15, 484);
            this.DownloadGroupPosts.Margin = new System.Windows.Forms.Padding(6);
            this.DownloadGroupPosts.Name = "DownloadGroupPosts";
            this.DownloadGroupPosts.Size = new System.Drawing.Size(696, 58);
            this.DownloadGroupPosts.TabIndex = 24;
            this.DownloadGroupPosts.Text = "Download Posts and Comments...";
            this.DownloadGroupPosts.UseVisualStyleBackColor = true;
            this.DownloadGroupPosts.Click += new System.EventHandler(this.DownloadGroupPosts_Click);
            // 
            // backgroundGroupsWorker
            // 
            this.backgroundGroupsWorker.WorkerReportsProgress = true;
            this.backgroundGroupsWorker.WorkerSupportsCancellation = true;
            // 
            // VKContentForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(730, 824);
            this.Controls.Add(this.DownloadGroupPosts);
            this.Controls.Add(this.GroupsProgressBar);
            this.Controls.Add(this.Authorize);
            this.Controls.Add(this.groupsStatusStrip);
            this.Controls.Add(this.groupDescription);
            this.Controls.Add(this.groupId2);
            this.Controls.Add(this.FindGroupsButton);
            this.Controls.Add(this.WorkingFolderTextBox);
            this.Controls.Add(this.WorkingFolderButton);
            this.Controls.Add(this.userIdTextBox);
            this.Controls.Add(this.AuthorizeButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "VKContentForm";
            this.Text = "VK Content";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.VKContentForm_FormClosing);
            this.Load += new System.EventHandler(this.VKContentForm_Load);
            this.groupsStatusStrip.ResumeLayout(false);
            this.groupsStatusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox groupDescription;
        private System.Windows.Forms.TextBox groupId2;
        private System.Windows.Forms.Button FindGroupsButton;
        private System.Windows.Forms.TextBox WorkingFolderTextBox;
        private System.Windows.Forms.Button WorkingFolderButton;
        private System.Windows.Forms.TextBox userIdTextBox;
        private System.Windows.Forms.Button AuthorizeButton;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.ProgressBar GroupsProgressBar;
        private System.Windows.Forms.Button Authorize;
        private System.Windows.Forms.StatusStrip groupsStatusStrip;
        private System.Windows.Forms.ToolStripStatusLabel groupsStripStatusLabel;
        private System.Windows.Forms.Button DownloadGroupPosts;
        private System.ComponentModel.BackgroundWorker backgroundGroupsWorker;
    }
}

