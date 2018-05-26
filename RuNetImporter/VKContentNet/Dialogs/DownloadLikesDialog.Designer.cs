namespace rcsir.net.vk.content.Dialogs
{
    partial class DownloadLikesDialog
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
            this.OKButton = new System.Windows.Forms.Button();
            this.CancelSearchButton = new System.Windows.Forms.Button();
            this.isGroupcheckBox = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupIdNumeric = new System.Windows.Forms.NumericUpDown();
            this.postsTextBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.groupIdNumeric)).BeginInit();
            this.SuspendLayout();
            // 
            // OKButton
            // 
            this.OKButton.Cursor = System.Windows.Forms.Cursors.Default;
            this.OKButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.OKButton.Location = new System.Drawing.Point(331, 286);
            this.OKButton.Name = "OKButton";
            this.OKButton.Size = new System.Drawing.Size(96, 31);
            this.OKButton.TabIndex = 0;
            this.OKButton.Text = "OK";
            this.OKButton.UseVisualStyleBackColor = true;
            this.OKButton.Click += new System.EventHandler(this.OKButton_Click);
            // 
            // CancelSearchButton
            // 
            this.CancelSearchButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelSearchButton.Location = new System.Drawing.Point(225, 286);
            this.CancelSearchButton.Name = "CancelSearchButton";
            this.CancelSearchButton.Size = new System.Drawing.Size(100, 31);
            this.CancelSearchButton.TabIndex = 1;
            this.CancelSearchButton.Text = "Cancel";
            this.CancelSearchButton.UseVisualStyleBackColor = true;
            // 
            // isGroupcheckBox
            // 
            this.isGroupcheckBox.AutoCheck = false;
            this.isGroupcheckBox.AutoSize = true;
            this.isGroupcheckBox.Checked = true;
            this.isGroupcheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.isGroupcheckBox.Enabled = false;
            this.isGroupcheckBox.Location = new System.Drawing.Point(132, 62);
            this.isGroupcheckBox.Margin = new System.Windows.Forms.Padding(4);
            this.isGroupcheckBox.Name = "isGroupcheckBox";
            this.isGroupcheckBox.Size = new System.Drawing.Size(84, 21);
            this.isGroupcheckBox.TabIndex = 9;
            this.isGroupcheckBox.TabStop = false;
            this.isGroupcheckBox.Text = "is Group";
            this.isGroupcheckBox.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(17, 32);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(103, 17);
            this.label1.TabIndex = 8;
            this.label1.Text = "Group/User ID:";
            // 
            // groupIdNumeric
            // 
            this.groupIdNumeric.Enabled = false;
            this.groupIdNumeric.Location = new System.Drawing.Point(132, 30);
            this.groupIdNumeric.Margin = new System.Windows.Forms.Padding(4);
            this.groupIdNumeric.Maximum = new decimal(new int[] {
            1000000000,
            0,
            0,
            0});
            this.groupIdNumeric.Name = "groupIdNumeric";
            this.groupIdNumeric.ReadOnly = true;
            this.groupIdNumeric.Size = new System.Drawing.Size(292, 22);
            this.groupIdNumeric.TabIndex = 7;
            this.groupIdNumeric.TabStop = false;
            // 
            // postsTextBox
            // 
            this.postsTextBox.Location = new System.Drawing.Point(132, 116);
            this.postsTextBox.Multiline = true;
            this.postsTextBox.Name = "postsTextBox";
            this.postsTextBox.Size = new System.Drawing.Size(292, 121);
            this.postsTextBox.TabIndex = 10;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 116);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(74, 17);
            this.label2.TabIndex = 11;
            this.label2.Text = "Post ID(s):";
            // 
            // DownloadLikesDialog
            // 
            this.AcceptButton = this.OKButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(439, 341);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.postsTextBox);
            this.Controls.Add(this.isGroupcheckBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.groupIdNumeric);
            this.Controls.Add(this.CancelSearchButton);
            this.Controls.Add(this.OKButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "DownloadLikesDialog";
            this.Text = "Download Likes Network";
            this.Load += new System.EventHandler(this.DownloadLikesDialog_Load);
            ((System.ComponentModel.ISupportInitialize)(this.groupIdNumeric)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button OKButton;
        private System.Windows.Forms.Button CancelSearchButton;
        private System.Windows.Forms.CheckBox isGroupcheckBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown groupIdNumeric;
        private System.Windows.Forms.TextBox postsTextBox;
        private System.Windows.Forms.Label label2;

    }
}