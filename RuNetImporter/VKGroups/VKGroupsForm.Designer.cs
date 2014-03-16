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
            this.button1 = new System.Windows.Forms.Button();
            this.WorkingFolderTextBox = new System.Windows.Forms.TextBox();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.FindGroupsButton = new System.Windows.Forms.Button();
            this.DownloadGroupPosts = new System.Windows.Forms.Button();
            this.CancelJobBurron = new System.Windows.Forms.Button();
            this.GroupsProgressBar = new System.Windows.Forms.ProgressBar();
            this.groupsStatusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // userIdTextBox
            // 
            this.userIdTextBox.Location = new System.Drawing.Point(12, 48);
            this.userIdTextBox.Name = "userIdTextBox";
            this.userIdTextBox.ReadOnly = true;
            this.userIdTextBox.Size = new System.Drawing.Size(348, 20);
            this.userIdTextBox.TabIndex = 3;
            // 
            // AuthorizeButton
            // 
            this.AuthorizeButton.Location = new System.Drawing.Point(12, 12);
            this.AuthorizeButton.Name = "AuthorizeButton";
            this.AuthorizeButton.Size = new System.Drawing.Size(348, 30);
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
            this.groupsStatusStrip.Location = new System.Drawing.Point(0, 334);
            this.groupsStatusStrip.Name = "groupsStatusStrip";
            this.groupsStatusStrip.Size = new System.Drawing.Size(375, 22);
            this.groupsStatusStrip.TabIndex = 4;
            this.groupsStatusStrip.Text = "statusStrip1";
            // 
            // groupsStripStatusLabel
            // 
            this.groupsStripStatusLabel.Name = "groupsStripStatusLabel";
            this.groupsStripStatusLabel.Size = new System.Drawing.Size(39, 17);
            this.groupsStripStatusLabel.Text = "Status";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(12, 74);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(348, 30);
            this.button1.TabIndex = 5;
            this.button1.Text = "Working Folder...";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // WorkingFolderTextBox
            // 
            this.WorkingFolderTextBox.Location = new System.Drawing.Point(12, 110);
            this.WorkingFolderTextBox.Name = "WorkingFolderTextBox";
            this.WorkingFolderTextBox.ReadOnly = true;
            this.WorkingFolderTextBox.Size = new System.Drawing.Size(348, 20);
            this.WorkingFolderTextBox.TabIndex = 6;
            // 
            // FindGroupsButton
            // 
            this.FindGroupsButton.Location = new System.Drawing.Point(12, 136);
            this.FindGroupsButton.Name = "FindGroupsButton";
            this.FindGroupsButton.Size = new System.Drawing.Size(348, 30);
            this.FindGroupsButton.TabIndex = 7;
            this.FindGroupsButton.Text = "Find Groups...";
            this.FindGroupsButton.UseVisualStyleBackColor = true;
            this.FindGroupsButton.Click += new System.EventHandler(this.FindGroupsButton_Click);
            // 
            // DownloadGroupPosts
            // 
            this.DownloadGroupPosts.Location = new System.Drawing.Point(12, 183);
            this.DownloadGroupPosts.Name = "DownloadGroupPosts";
            this.DownloadGroupPosts.Size = new System.Drawing.Size(348, 30);
            this.DownloadGroupPosts.TabIndex = 8;
            this.DownloadGroupPosts.Text = "Download Group Posts...";
            this.DownloadGroupPosts.UseVisualStyleBackColor = true;
            this.DownloadGroupPosts.Click += new System.EventHandler(this.DownloadGroupPosts_Click);
            // 
            // CancelJobBurron
            // 
            this.CancelJobBurron.Enabled = false;
            this.CancelJobBurron.Location = new System.Drawing.Point(12, 263);
            this.CancelJobBurron.Name = "CancelJobBurron";
            this.CancelJobBurron.Size = new System.Drawing.Size(348, 30);
            this.CancelJobBurron.TabIndex = 9;
            this.CancelJobBurron.Text = "Cancel";
            this.CancelJobBurron.UseVisualStyleBackColor = true;
            this.CancelJobBurron.Click += new System.EventHandler(this.CancelJobBurron_Click);
            // 
            // GroupsProgressBar
            // 
            this.GroupsProgressBar.Location = new System.Drawing.Point(12, 302);
            this.GroupsProgressBar.Name = "GroupsProgressBar";
            this.GroupsProgressBar.Size = new System.Drawing.Size(348, 22);
            this.GroupsProgressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.GroupsProgressBar.TabIndex = 10;
            // 
            // VKGroupsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(375, 356);
            this.Controls.Add(this.GroupsProgressBar);
            this.Controls.Add(this.CancelJobBurron);
            this.Controls.Add(this.DownloadGroupPosts);
            this.Controls.Add(this.FindGroupsButton);
            this.Controls.Add(this.WorkingFolderTextBox);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.groupsStatusStrip);
            this.Controls.Add(this.userIdTextBox);
            this.Controls.Add(this.AuthorizeButton);
            this.Name = "VKGroupsForm";
            this.Text = "VKGroups";
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
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox WorkingFolderTextBox;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.Button FindGroupsButton;
        private System.Windows.Forms.Button DownloadGroupPosts;
        private System.Windows.Forms.Button CancelJobBurron;
        private System.Windows.Forms.ProgressBar GroupsProgressBar;
    }
}

