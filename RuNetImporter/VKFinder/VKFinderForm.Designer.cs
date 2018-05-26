namespace rcsir.net.vk.finder
{
    partial class VkFinderForm
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
            this.AuthorizeButton = new System.Windows.Forms.Button();
            this.userIdTextBox = new System.Windows.Forms.TextBox();
            this.FindUsersButton = new System.Windows.Forms.Button();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.button1 = new System.Windows.Forms.Button();
            this.WorkingFolderTextBox = new System.Windows.Forms.TextBox();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.backgroundFinderWorker = new System.ComponentModel.BackgroundWorker();
            this.CancelFindButton = new System.Windows.Forms.Button();
            this.FindProgressBar = new System.Windows.Forms.ProgressBar();
            this.backgroundLoaderWorker = new System.ComponentModel.BackgroundWorker();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // AuthorizeButton
            // 
            this.AuthorizeButton.Location = new System.Drawing.Point(16, 15);
            this.AuthorizeButton.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.AuthorizeButton.Name = "AuthorizeButton";
            this.AuthorizeButton.Size = new System.Drawing.Size(464, 37);
            this.AuthorizeButton.TabIndex = 0;
            this.AuthorizeButton.Text = "Authorize...";
            this.AuthorizeButton.UseVisualStyleBackColor = true;
            this.AuthorizeButton.Click += new System.EventHandler(this.AuthorizeButton_Click);
            // 
            // userIdTextBox
            // 
            this.userIdTextBox.Location = new System.Drawing.Point(16, 59);
            this.userIdTextBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.userIdTextBox.Name = "userIdTextBox";
            this.userIdTextBox.ReadOnly = true;
            this.userIdTextBox.Size = new System.Drawing.Size(463, 22);
            this.userIdTextBox.TabIndex = 1;
            // 
            // FindUsersButton
            // 
            this.FindUsersButton.Enabled = false;
            this.FindUsersButton.Location = new System.Drawing.Point(16, 178);
            this.FindUsersButton.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.FindUsersButton.Name = "FindUsersButton";
            this.FindUsersButton.Size = new System.Drawing.Size(224, 37);
            this.FindUsersButton.TabIndex = 2;
            this.FindUsersButton.Text = "Find Users...";
            this.FindUsersButton.UseVisualStyleBackColor = true;
            this.FindUsersButton.Click += new System.EventHandler(this.FindUsersButton_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(16, 102);
            this.button1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(464, 37);
            this.button1.TabIndex = 3;
            this.button1.Text = "Working Folder...";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // WorkingFolderTextBox
            // 
            this.WorkingFolderTextBox.Location = new System.Drawing.Point(16, 146);
            this.WorkingFolderTextBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.WorkingFolderTextBox.Name = "WorkingFolderTextBox";
            this.WorkingFolderTextBox.ReadOnly = true;
            this.WorkingFolderTextBox.Size = new System.Drawing.Size(463, 22);
            this.WorkingFolderTextBox.TabIndex = 4;
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 295);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Padding = new System.Windows.Forms.Padding(1, 0, 19, 0);
            this.statusStrip1.Size = new System.Drawing.Size(495, 25);
            this.statusStrip1.TabIndex = 5;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(151, 20);
            this.toolStripStatusLabel1.Text = "toolStripStatusLabel1";
            // 
            // backgroundFinderWorker
            // 
            this.backgroundFinderWorker.WorkerReportsProgress = true;
            this.backgroundFinderWorker.WorkerSupportsCancellation = true;
            // 
            // CancelFindButton
            // 
            this.CancelFindButton.Enabled = false;
            this.CancelFindButton.Location = new System.Drawing.Point(265, 178);
            this.CancelFindButton.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.CancelFindButton.Name = "CancelFindButton";
            this.CancelFindButton.Size = new System.Drawing.Size(215, 37);
            this.CancelFindButton.TabIndex = 6;
            this.CancelFindButton.Text = "Cancel";
            this.CancelFindButton.UseVisualStyleBackColor = true;
            this.CancelFindButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // FindProgressBar
            // 
            this.FindProgressBar.Location = new System.Drawing.Point(16, 242);
            this.FindProgressBar.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.FindProgressBar.Name = "FindProgressBar";
            this.FindProgressBar.Size = new System.Drawing.Size(464, 31);
            this.FindProgressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.FindProgressBar.TabIndex = 7;
            // 
            // VKFinderForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(495, 320);
            this.Controls.Add(this.FindProgressBar);
            this.Controls.Add(this.CancelFindButton);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.WorkingFolderTextBox);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.FindUsersButton);
            this.Controls.Add(this.userIdTextBox);
            this.Controls.Add(this.AuthorizeButton);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "VkFinderForm";
            this.Text = "VKFinder";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.VKFinderForm_FormClosing);
            this.Load += new System.EventHandler(this.VKFinderForm_Load);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button AuthorizeButton;
        private System.Windows.Forms.TextBox userIdTextBox;
        private System.Windows.Forms.Button FindUsersButton;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox WorkingFolderTextBox;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.ComponentModel.BackgroundWorker backgroundFinderWorker;
        private System.Windows.Forms.Button CancelFindButton;
        private System.Windows.Forms.ProgressBar FindProgressBar;
        private System.ComponentModel.BackgroundWorker backgroundLoaderWorker;
    }
}

