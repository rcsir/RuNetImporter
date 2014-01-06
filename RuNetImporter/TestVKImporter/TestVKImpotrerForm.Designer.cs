namespace TestVKImporter
{
    partial class TestVKImpotrerForm
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
            this.AuthButton = new System.Windows.Forms.Button();
            this.LoadUserInfoButton = new System.Windows.Forms.Button();
            this.GenerateGraphButton = new System.Windows.Forms.Button();
            this.userIdTextBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.userInfoTextBox = new System.Windows.Forms.TextBox();
            this.testVkDialogButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // AuthButton
            // 
            this.AuthButton.Location = new System.Drawing.Point(71, 53);
            this.AuthButton.Name = "AuthButton";
            this.AuthButton.Size = new System.Drawing.Size(224, 38);
            this.AuthButton.TabIndex = 0;
            this.AuthButton.Text = "Authenticate";
            this.AuthButton.UseVisualStyleBackColor = true;
            this.AuthButton.Click += new System.EventHandler(this.AuthButton_Click);
            // 
            // LoadUserInfoButton
            // 
            this.LoadUserInfoButton.Enabled = false;
            this.LoadUserInfoButton.Location = new System.Drawing.Point(71, 144);
            this.LoadUserInfoButton.Name = "LoadUserInfoButton";
            this.LoadUserInfoButton.Size = new System.Drawing.Size(224, 38);
            this.LoadUserInfoButton.TabIndex = 1;
            this.LoadUserInfoButton.Text = "Load User Info";
            this.LoadUserInfoButton.UseVisualStyleBackColor = true;
            this.LoadUserInfoButton.Click += new System.EventHandler(this.LoadUserInfoButton_Click);
            // 
            // GenerateGraphButton
            // 
            this.GenerateGraphButton.Enabled = false;
            this.GenerateGraphButton.Location = new System.Drawing.Point(71, 292);
            this.GenerateGraphButton.Name = "GenerateGraphButton";
            this.GenerateGraphButton.Size = new System.Drawing.Size(224, 38);
            this.GenerateGraphButton.TabIndex = 4;
            this.GenerateGraphButton.Text = "Generate Graph File";
            this.GenerateGraphButton.UseVisualStyleBackColor = true;
            this.GenerateGraphButton.Click += new System.EventHandler(this.GenerateGraphButton_Click);
            // 
            // userIdTextBox
            // 
            this.userIdTextBox.Enabled = false;
            this.userIdTextBox.Location = new System.Drawing.Point(105, 107);
            this.userIdTextBox.Name = "userIdTextBox";
            this.userIdTextBox.Size = new System.Drawing.Size(190, 20);
            this.userIdTextBox.TabIndex = 5;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(78, 110);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(21, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "ID:";
            // 
            // userInfoTextBox
            // 
            this.userInfoTextBox.Location = new System.Drawing.Point(71, 188);
            this.userInfoTextBox.Multiline = true;
            this.userInfoTextBox.Name = "userInfoTextBox";
            this.userInfoTextBox.ReadOnly = true;
            this.userInfoTextBox.Size = new System.Drawing.Size(224, 98);
            this.userInfoTextBox.TabIndex = 7;
            // 
            // testVkDialogButton
            // 
            this.testVkDialogButton.Location = new System.Drawing.Point(71, 347);
            this.testVkDialogButton.Name = "testVkDialogButton";
            this.testVkDialogButton.Size = new System.Drawing.Size(224, 38);
            this.testVkDialogButton.TabIndex = 8;
            this.testVkDialogButton.Text = "Test VKDialog";
            this.testVkDialogButton.UseVisualStyleBackColor = true;
            this.testVkDialogButton.Click += new System.EventHandler(this.testVkDialogButton_Click);
            // 
            // TestVKImpotrerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(365, 400);
            this.Controls.Add(this.testVkDialogButton);
            this.Controls.Add(this.userInfoTextBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.userIdTextBox);
            this.Controls.Add(this.GenerateGraphButton);
            this.Controls.Add(this.LoadUserInfoButton);
            this.Controls.Add(this.AuthButton);
            this.Name = "TestVKImpotrerForm";
            this.Text = "Test VK Impporter";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button AuthButton;
        private System.Windows.Forms.Button LoadUserInfoButton;
        private System.Windows.Forms.Button GenerateGraphButton;
        private System.Windows.Forms.TextBox userIdTextBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox userInfoTextBox;
        private System.Windows.Forms.Button testVkDialogButton;
    }
}

