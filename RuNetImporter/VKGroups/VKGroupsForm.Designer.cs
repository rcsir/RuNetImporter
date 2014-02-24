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
            // VKGroupsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(375, 356);
            this.Controls.Add(this.userIdTextBox);
            this.Controls.Add(this.AuthorizeButton);
            this.Name = "VKGroupsForm";
            this.Text = "VKGroups";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox userIdTextBox;
        private System.Windows.Forms.Button AuthorizeButton;
    }
}

