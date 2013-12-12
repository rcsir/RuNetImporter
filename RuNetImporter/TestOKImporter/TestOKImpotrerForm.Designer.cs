namespace TestOKImporter
{
    partial class TestOKImpotrerForm
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
            this.LoadFriendsButton = new System.Windows.Forms.Button();
            this.GetMutualButton = new System.Windows.Forms.Button();
            this.GenerateGraphButton = new System.Windows.Forms.Button();
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
            this.LoadUserInfoButton.Location = new System.Drawing.Point(71, 113);
            this.LoadUserInfoButton.Name = "LoadUserInfoButton";
            this.LoadUserInfoButton.Size = new System.Drawing.Size(224, 38);
            this.LoadUserInfoButton.TabIndex = 1;
            this.LoadUserInfoButton.Text = "Load User Info";
            this.LoadUserInfoButton.UseVisualStyleBackColor = true;
            this.LoadUserInfoButton.Click += new System.EventHandler(this.LoadUserInfoButton_Click);
            // 
            // LoadFriendsButton
            // 
            this.LoadFriendsButton.Location = new System.Drawing.Point(71, 174);
            this.LoadFriendsButton.Name = "LoadFriendsButton";
            this.LoadFriendsButton.Size = new System.Drawing.Size(224, 38);
            this.LoadFriendsButton.TabIndex = 2;
            this.LoadFriendsButton.Text = "Load Friends";
            this.LoadFriendsButton.UseVisualStyleBackColor = true;
            this.LoadFriendsButton.Click += new System.EventHandler(this.LoadFriendsButton_Click);
            // 
            // GetMutualButton
            // 
            this.GetMutualButton.Location = new System.Drawing.Point(71, 235);
            this.GetMutualButton.Name = "GetMutualButton";
            this.GetMutualButton.Size = new System.Drawing.Size(224, 38);
            this.GetMutualButton.TabIndex = 3;
            this.GetMutualButton.Text = "Get Mutual";
            this.GetMutualButton.UseVisualStyleBackColor = true;
            this.GetMutualButton.Click += new System.EventHandler(this.GetMutualButton_Click);
            // 
            // GenerateGraphButton
            // 
            this.GenerateGraphButton.Location = new System.Drawing.Point(71, 292);
            this.GenerateGraphButton.Name = "GenerateGraphButton";
            this.GenerateGraphButton.Size = new System.Drawing.Size(224, 38);
            this.GenerateGraphButton.TabIndex = 4;
            this.GenerateGraphButton.Text = "Generate Graph File";
            this.GenerateGraphButton.UseVisualStyleBackColor = true;
            this.GenerateGraphButton.Click += new System.EventHandler(this.GenerateGraphButton_Click);
            // 
            // TestVKImpotrerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(365, 400);
            this.Controls.Add(this.GenerateGraphButton);
            this.Controls.Add(this.GetMutualButton);
            this.Controls.Add(this.LoadFriendsButton);
            this.Controls.Add(this.LoadUserInfoButton);
            this.Controls.Add(this.AuthButton);
            this.Name = "TestOKImpotrerForm";
            this.Text = "Test OK Impporter";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button AuthButton;
        private System.Windows.Forms.Button LoadUserInfoButton;
        private System.Windows.Forms.Button LoadFriendsButton;
        private System.Windows.Forms.Button GetMutualButton;
        private System.Windows.Forms.Button GenerateGraphButton;
    }
}

