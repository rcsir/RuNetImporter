namespace rcsir.net.vk.content.Dialogs
{
    partial class FindGroupsDialog
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
            this.label1 = new System.Windows.Forms.Label();
            this.groupIdNumeric = new System.Windows.Forms.NumericUpDown();
            this.isGroupcheckBox = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.groupIdNumeric)).BeginInit();
            this.SuspendLayout();
            // 
            // CancelSearchButton
            // 
            this.CancelSearchButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelSearchButton.Location = new System.Drawing.Point(269, 302);
            this.CancelSearchButton.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.CancelSearchButton.Name = "CancelSearchButton";
            this.CancelSearchButton.Size = new System.Drawing.Size(100, 28);
            this.CancelSearchButton.TabIndex = 3;
            this.CancelSearchButton.Text = "Cancel";
            this.CancelSearchButton.UseVisualStyleBackColor = true;
            // 
            // OKButton
            // 
            this.OKButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.OKButton.Location = new System.Drawing.Point(377, 302);
            this.OKButton.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.OKButton.Name = "OKButton";
            this.OKButton.Size = new System.Drawing.Size(100, 28);
            this.OKButton.TabIndex = 2;
            this.OKButton.Text = "OK";
            this.OKButton.UseVisualStyleBackColor = true;
            this.OKButton.Click += new System.EventHandler(this.OKButton_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(57, 46);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(103, 17);
            this.label1.TabIndex = 7;
            this.label1.Text = "Group/User ID:";
            // 
            // groupIdNumeric
            // 
            this.groupIdNumeric.Location = new System.Drawing.Point(172, 43);
            this.groupIdNumeric.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupIdNumeric.Maximum = new decimal(new int[] {
            1000000000,
            0,
            0,
            0});
            this.groupIdNumeric.Name = "groupIdNumeric";
            this.groupIdNumeric.Size = new System.Drawing.Size(292, 22);
            this.groupIdNumeric.TabIndex = 6;
            // 
            // isGroupcheckBox
            // 
            this.isGroupcheckBox.AutoSize = true;
            this.isGroupcheckBox.Checked = true;
            this.isGroupcheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.isGroupcheckBox.Location = new System.Drawing.Point(172, 87);
            this.isGroupcheckBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.isGroupcheckBox.Name = "isGroupcheckBox";
            this.isGroupcheckBox.Size = new System.Drawing.Size(84, 21);
            this.isGroupcheckBox.TabIndex = 8;
            this.isGroupcheckBox.Text = "is Group";
            this.isGroupcheckBox.UseVisualStyleBackColor = true;
            // 
            // FindGroupsDialog
            // 
            this.AcceptButton = this.OKButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelSearchButton;
            this.ClientSize = new System.Drawing.Size(493, 345);
            this.Controls.Add(this.isGroupcheckBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.groupIdNumeric);
            this.Controls.Add(this.CancelSearchButton);
            this.Controls.Add(this.OKButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "FindGroupsDialog";
            this.Text = "Find a Community by ID";
            ((System.ComponentModel.ISupportInitialize)(this.groupIdNumeric)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button CancelSearchButton;
        private System.Windows.Forms.Button OKButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown groupIdNumeric;
        private System.Windows.Forms.CheckBox isGroupcheckBox;
    }
}