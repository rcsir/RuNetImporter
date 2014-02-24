namespace VKFinder
{
    partial class VKFinderForm
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
            this.CancelButton = new System.Windows.Forms.Button();
            this.FindProgressBar = new System.Windows.Forms.ProgressBar();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // AuthorizeButton
            // 
            this.AuthorizeButton.Location = new System.Drawing.Point(12, 12);
            this.AuthorizeButton.Name = "AuthorizeButton";
            this.AuthorizeButton.Size = new System.Drawing.Size(348, 30);
            this.AuthorizeButton.TabIndex = 0;
            this.AuthorizeButton.Text = "Authorize...";
            this.AuthorizeButton.UseVisualStyleBackColor = true;
            this.AuthorizeButton.Click += new System.EventHandler(this.AuthorizeButton_Click);
            // 
            // userIdTextBox
            // 
            this.userIdTextBox.Location = new System.Drawing.Point(12, 48);
            this.userIdTextBox.Name = "userIdTextBox";
            this.userIdTextBox.ReadOnly = true;
            this.userIdTextBox.Size = new System.Drawing.Size(348, 20);
            this.userIdTextBox.TabIndex = 1;
            this.userIdTextBox.TextChanged += new System.EventHandler(this.userIdTextBox_TextChanged);
            // 
            // FindUsersButton
            // 
            this.FindUsersButton.Enabled = false;
            this.FindUsersButton.Location = new System.Drawing.Point(12, 145);
            this.FindUsersButton.Name = "FindUsersButton";
            this.FindUsersButton.Size = new System.Drawing.Size(168, 30);
            this.FindUsersButton.TabIndex = 2;
            this.FindUsersButton.Text = "Find Users...";
            this.FindUsersButton.UseVisualStyleBackColor = true;
            this.FindUsersButton.Click += new System.EventHandler(this.FindUsersButton_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(12, 83);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(348, 30);
            this.button1.TabIndex = 3;
            this.button1.Text = "Working Folder...";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // WorkingFolderTextBox
            // 
            this.WorkingFolderTextBox.Location = new System.Drawing.Point(12, 119);
            this.WorkingFolderTextBox.Name = "WorkingFolderTextBox";
            this.WorkingFolderTextBox.ReadOnly = true;
            this.WorkingFolderTextBox.Size = new System.Drawing.Size(348, 20);
            this.WorkingFolderTextBox.TabIndex = 4;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 238);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(371, 22);
            this.statusStrip1.TabIndex = 5;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(118, 17);
            this.toolStripStatusLabel1.Text = "toolStripStatusLabel1";
            // 
            // backgroundFinderWorker
            // 
            this.backgroundFinderWorker.WorkerReportsProgress = true;
            this.backgroundFinderWorker.WorkerSupportsCancellation = true;
            // 
            // CancelButton
            // 
            this.CancelButton.Enabled = false;
            this.CancelButton.Location = new System.Drawing.Point(199, 145);
            this.CancelButton.Name = "CancelButton";
            this.CancelButton.Size = new System.Drawing.Size(161, 30);
            this.CancelButton.TabIndex = 6;
            this.CancelButton.Text = "Cancel";
            this.CancelButton.UseVisualStyleBackColor = true;
            this.CancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // FindProgressBar
            // 
            this.FindProgressBar.Location = new System.Drawing.Point(12, 197);
            this.FindProgressBar.Name = "FindProgressBar";
            this.FindProgressBar.Size = new System.Drawing.Size(348, 25);
            this.FindProgressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.FindProgressBar.TabIndex = 7;
            // 
            // VKFinderForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(371, 260);
            this.Controls.Add(this.FindProgressBar);
            this.Controls.Add(this.CancelButton);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.WorkingFolderTextBox);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.FindUsersButton);
            this.Controls.Add(this.userIdTextBox);
            this.Controls.Add(this.AuthorizeButton);
            this.Name = "VKFinderForm";
            this.Text = "VKFinder";
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
        private System.Windows.Forms.Button CancelButton;
        private System.Windows.Forms.ProgressBar FindProgressBar;
    }
}

