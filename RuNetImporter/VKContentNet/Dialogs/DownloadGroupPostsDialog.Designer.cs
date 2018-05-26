namespace rcsir.net.vk.content.Dialogs
{
    partial class DownloadGroupPostsDialog
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
            this.CancelSearchButton = new System.Windows.Forms.Button();
            this.OKButton = new System.Windows.Forms.Button();
            this.groupIdNumeric = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.isGroupcheckBox = new System.Windows.Forms.CheckBox();
            this.dateTimeFromPicker = new System.Windows.Forms.DateTimePicker();
            this.dateTimeToPicker = new System.Windows.Forms.DateTimePicker();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.groupWall = new System.Windows.Forms.CheckBox();
            this.groupTopics = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.groupIdNumeric)).BeginInit();
            this.SuspendLayout();
            // 
            // CancelSearchButton
            // 
            this.CancelSearchButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelSearchButton.Location = new System.Drawing.Point(209, 314);
            this.CancelSearchButton.Margin = new System.Windows.Forms.Padding(4);
            this.CancelSearchButton.Name = "CancelSearchButton";
            this.CancelSearchButton.Size = new System.Drawing.Size(100, 28);
            this.CancelSearchButton.TabIndex = 3;
            this.CancelSearchButton.Text = "Cancel";
            this.CancelSearchButton.UseVisualStyleBackColor = true;
            // 
            // OKButton
            // 
            this.OKButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.OKButton.Location = new System.Drawing.Point(317, 314);
            this.OKButton.Margin = new System.Windows.Forms.Padding(4);
            this.OKButton.Name = "OKButton";
            this.OKButton.Size = new System.Drawing.Size(100, 28);
            this.OKButton.TabIndex = 2;
            this.OKButton.Text = "OK";
            this.OKButton.UseVisualStyleBackColor = true;
            this.OKButton.Click += new System.EventHandler(this.OKButton_Click);
            // 
            // groupIdNumeric
            // 
            this.groupIdNumeric.Enabled = false;
            this.groupIdNumeric.Location = new System.Drawing.Point(131, 42);
            this.groupIdNumeric.Margin = new System.Windows.Forms.Padding(4);
            this.groupIdNumeric.Maximum = new decimal(new int[] {
            1000000000,
            0,
            0,
            0});
            this.groupIdNumeric.Name = "groupIdNumeric";
            this.groupIdNumeric.ReadOnly = true;
            this.groupIdNumeric.Size = new System.Drawing.Size(292, 22);
            this.groupIdNumeric.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 44);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(103, 17);
            this.label1.TabIndex = 5;
            this.label1.Text = "Group/User ID:";
            // 
            // isGroupcheckBox
            // 
            this.isGroupcheckBox.AutoCheck = false;
            this.isGroupcheckBox.AutoSize = true;
            this.isGroupcheckBox.Checked = true;
            this.isGroupcheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.isGroupcheckBox.Enabled = false;
            this.isGroupcheckBox.Location = new System.Drawing.Point(131, 74);
            this.isGroupcheckBox.Margin = new System.Windows.Forms.Padding(4);
            this.isGroupcheckBox.Name = "isGroupcheckBox";
            this.isGroupcheckBox.Size = new System.Drawing.Size(84, 21);
            this.isGroupcheckBox.TabIndex = 6;
            this.isGroupcheckBox.Text = "is Group";
            this.isGroupcheckBox.UseVisualStyleBackColor = true;
            // 
            // dateTimeFromPicker
            // 
            this.dateTimeFromPicker.Location = new System.Drawing.Point(131, 130);
            this.dateTimeFromPicker.Margin = new System.Windows.Forms.Padding(4);
            this.dateTimeFromPicker.Name = "dateTimeFromPicker";
            this.dateTimeFromPicker.Size = new System.Drawing.Size(291, 22);
            this.dateTimeFromPicker.TabIndex = 7;
            this.dateTimeFromPicker.Value = new System.DateTime(2014, 11, 11, 0, 0, 0, 0);
            // 
            // dateTimeToPicker
            // 
            this.dateTimeToPicker.Location = new System.Drawing.Point(131, 174);
            this.dateTimeToPicker.Margin = new System.Windows.Forms.Padding(4);
            this.dateTimeToPicker.Name = "dateTimeToPicker";
            this.dateTimeToPicker.Size = new System.Drawing.Size(291, 22);
            this.dateTimeToPicker.TabIndex = 8;
            this.dateTimeToPicker.Value = new System.DateTime(2014, 11, 11, 18, 28, 14, 0);
            this.dateTimeToPicker.ValueChanged += new System.EventHandler(this.dateTimeToPicker_ValueChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(16, 138);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(44, 17);
            this.label2.TabIndex = 9;
            this.label2.Text = "From:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(16, 181);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(29, 17);
            this.label3.TabIndex = 10;
            this.label3.Text = "To:";
            // 
            // groupWall
            // 
            this.groupWall.AutoSize = true;
            this.groupWall.Checked = true;
            this.groupWall.CheckState = System.Windows.Forms.CheckState.Checked;
            this.groupWall.Location = new System.Drawing.Point(131, 217);
            this.groupWall.Name = "groupWall";
            this.groupWall.Size = new System.Drawing.Size(238, 21);
            this.groupWall.TabIndex = 12;
            this.groupWall.Text = "Group Wall Posts and Comments";
            this.groupWall.UseVisualStyleBackColor = true;
            // 
            // groupTopics
            // 
            this.groupTopics.AutoSize = true;
            this.groupTopics.Checked = true;
            this.groupTopics.CheckState = System.Windows.Forms.CheckState.Checked;
            this.groupTopics.Location = new System.Drawing.Point(131, 252);
            this.groupTopics.Name = "groupTopics";
            this.groupTopics.Size = new System.Drawing.Size(261, 21);
            this.groupTopics.TabIndex = 13;
            this.groupTopics.Text = "Group Discussion Boards Comments";
            this.groupTopics.UseVisualStyleBackColor = true;
            // 
            // DownloadGroupPostsDialog
            // 
            this.AcceptButton = this.OKButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelSearchButton;
            this.ClientSize = new System.Drawing.Size(439, 361);
            this.Controls.Add(this.groupTopics);
            this.Controls.Add(this.groupWall);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.dateTimeToPicker);
            this.Controls.Add(this.dateTimeFromPicker);
            this.Controls.Add(this.isGroupcheckBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.groupIdNumeric);
            this.Controls.Add(this.CancelSearchButton);
            this.Controls.Add(this.OKButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "DownloadGroupPostsDialog";
            this.Text = "Download Posts and Comments";
            this.Load += new System.EventHandler(this.DownloadGroupPostsDialog_Load);
            ((System.ComponentModel.ISupportInitialize)(this.groupIdNumeric)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button CancelSearchButton;
        private System.Windows.Forms.Button OKButton;
        private System.Windows.Forms.NumericUpDown groupIdNumeric;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox isGroupcheckBox;
        private System.Windows.Forms.DateTimePicker dateTimeFromPicker;
        private System.Windows.Forms.DateTimePicker dateTimeToPicker;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox groupWall;
        private System.Windows.Forms.CheckBox groupTopics;
    }
}