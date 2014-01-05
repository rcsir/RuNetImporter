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
            this.components = new System.ComponentModel.Container();
            this.AuthButton = new System.Windows.Forms.Button();
            this.GeByAreFriendsButton = new System.Windows.Forms.Button();
            this.LoadFriendsButton = new System.Windows.Forms.Button();
            this.GetMutualButton = new System.Windows.Forms.Button();
            this.GenerateGraphAreButton = new System.Windows.Forms.Button();
            this.GenerateGraphMutualButton = new System.Windows.Forms.Button();
            this.userInfoTextBox = new System.Windows.Forms.TextBox();
            this.areLabel = new System.Windows.Forms.Label();
            this.areTimeTextBox = new System.Windows.Forms.TextBox();
            this.mutualLabel = new System.Windows.Forms.Label();
            this.mutualTimeTextBox = new System.Windows.Forms.TextBox();
            this.mutualGroupBox = new System.Windows.Forms.GroupBox();
            this.areGroupBox = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.userIdTextBox = new System.Windows.Forms.TextBox();
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.timer = new System.Windows.Forms.Timer(this.components);
            this.TestAllButton = new System.Windows.Forms.Button();
            this.mutualGroupBox.SuspendLayout();
            this.areGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // AuthButton
            // 
            this.AuthButton.Location = new System.Drawing.Point(142, 21);
            this.AuthButton.Name = "AuthButton";
            this.AuthButton.Size = new System.Drawing.Size(153, 38);
            this.AuthButton.TabIndex = 0;
            this.AuthButton.Text = "Authenticate";
            this.AuthButton.UseVisualStyleBackColor = true;
            this.AuthButton.Click += new System.EventHandler(this.AuthButton_Click);
            // 
            // GeByAreFriendsButton
            // 
            this.GeByAreFriendsButton.Enabled = false;
            this.GeByAreFriendsButton.Location = new System.Drawing.Point(11, 20);
            this.GeByAreFriendsButton.Name = "GeByAreFriendsButton";
            this.GeByAreFriendsButton.Size = new System.Drawing.Size(224, 38);
            this.GeByAreFriendsButton.TabIndex = 1;
            this.GeByAreFriendsButton.Text = "Get Graph by areFriends";
            this.GeByAreFriendsButton.UseVisualStyleBackColor = true;
            this.GeByAreFriendsButton.Click += new System.EventHandler(this.GeByAreFriendsButton_Click);
            // 
            // LoadFriendsButton
            // 
            this.LoadFriendsButton.Enabled = false;
            this.LoadFriendsButton.Location = new System.Drawing.Point(142, 101);
            this.LoadFriendsButton.Name = "LoadFriendsButton";
            this.LoadFriendsButton.Size = new System.Drawing.Size(153, 38);
            this.LoadFriendsButton.TabIndex = 2;
            this.LoadFriendsButton.Text = "Load Friends";
            this.LoadFriendsButton.UseVisualStyleBackColor = true;
            this.LoadFriendsButton.Click += new System.EventHandler(this.LoadFriendsButton_Click);
            // 
            // GetMutualButton
            // 
            this.GetMutualButton.Enabled = false;
            this.GetMutualButton.Location = new System.Drawing.Point(9, 19);
            this.GetMutualButton.Name = "GetMutualButton";
            this.GetMutualButton.Size = new System.Drawing.Size(224, 38);
            this.GetMutualButton.TabIndex = 3;
            this.GetMutualButton.Text = "Get Graph by MutualFriends";
            this.GetMutualButton.UseVisualStyleBackColor = true;
            this.GetMutualButton.Click += new System.EventHandler(this.GetMutualButton_Click);
            // 
            // GenerateGraphAreButton
            // 
            this.GenerateGraphAreButton.Enabled = false;
            this.GenerateGraphAreButton.Location = new System.Drawing.Point(11, 110);
            this.GenerateGraphAreButton.Name = "GenerateGraphAreButton";
            this.GenerateGraphAreButton.Size = new System.Drawing.Size(224, 38);
            this.GenerateGraphAreButton.TabIndex = 4;
            this.GenerateGraphAreButton.Text = "Generate Graph File by areFriends";
            this.GenerateGraphAreButton.UseVisualStyleBackColor = true;
            this.GenerateGraphAreButton.Click += new System.EventHandler(this.GenerateGraphAreButton_Click);
            // 
            // GenerateGraphMutualButton
            // 
            this.GenerateGraphMutualButton.Enabled = false;
            this.GenerateGraphMutualButton.Location = new System.Drawing.Point(9, 109);
            this.GenerateGraphMutualButton.Name = "GenerateGraphMutualButton";
            this.GenerateGraphMutualButton.Size = new System.Drawing.Size(224, 38);
            this.GenerateGraphMutualButton.TabIndex = 5;
            this.GenerateGraphMutualButton.Text = "Generate Graph File by MutualFriends";
            this.GenerateGraphMutualButton.UseVisualStyleBackColor = true;
            this.GenerateGraphMutualButton.Click += new System.EventHandler(this.GenerateGraphMutualButton_Click);
            // 
            // userInfoTextBox
            // 
            this.userInfoTextBox.Location = new System.Drawing.Point(324, 21);
            this.userInfoTextBox.Multiline = true;
            this.userInfoTextBox.Name = "userInfoTextBox";
            this.userInfoTextBox.ReadOnly = true;
            this.userInfoTextBox.Size = new System.Drawing.Size(243, 118);
            this.userInfoTextBox.TabIndex = 8;
            // 
            // areLabel
            // 
            this.areLabel.AutoSize = true;
            this.areLabel.Location = new System.Drawing.Point(7, 66);
            this.areLabel.Name = "areLabel";
            this.areLabel.Size = new System.Drawing.Size(33, 13);
            this.areLabel.TabIndex = 10;
            this.areLabel.Text = "Time:";
            // 
            // areTimeTextBox
            // 
            this.areTimeTextBox.Enabled = false;
            this.areTimeTextBox.Location = new System.Drawing.Point(42, 62);
            this.areTimeTextBox.Name = "areTimeTextBox";
            this.areTimeTextBox.Size = new System.Drawing.Size(190, 20);
            this.areTimeTextBox.TabIndex = 9;
            // 
            // mutualLabel
            // 
            this.mutualLabel.AutoSize = true;
            this.mutualLabel.Location = new System.Drawing.Point(6, 64);
            this.mutualLabel.Name = "mutualLabel";
            this.mutualLabel.Size = new System.Drawing.Size(33, 13);
            this.mutualLabel.TabIndex = 12;
            this.mutualLabel.Text = "Time:";
            // 
            // mutualTimeTextBox
            // 
            this.mutualTimeTextBox.Enabled = false;
            this.mutualTimeTextBox.Location = new System.Drawing.Point(41, 61);
            this.mutualTimeTextBox.Name = "mutualTimeTextBox";
            this.mutualTimeTextBox.Size = new System.Drawing.Size(190, 20);
            this.mutualTimeTextBox.TabIndex = 11;
            // 
            // mutualGroupBox
            // 
            this.mutualGroupBox.Controls.Add(this.mutualLabel);
            this.mutualGroupBox.Controls.Add(this.mutualTimeTextBox);
            this.mutualGroupBox.Controls.Add(this.GenerateGraphMutualButton);
            this.mutualGroupBox.Controls.Add(this.GetMutualButton);
            this.mutualGroupBox.Location = new System.Drawing.Point(323, 153);
            this.mutualGroupBox.Name = "mutualGroupBox";
            this.mutualGroupBox.Size = new System.Drawing.Size(243, 161);
            this.mutualGroupBox.TabIndex = 13;
            this.mutualGroupBox.TabStop = false;
            this.mutualGroupBox.Text = "Mutual Friends";
            // 
            // areGroupBox
            // 
            this.areGroupBox.Controls.Add(this.areLabel);
            this.areGroupBox.Controls.Add(this.areTimeTextBox);
            this.areGroupBox.Controls.Add(this.GenerateGraphAreButton);
            this.areGroupBox.Controls.Add(this.GeByAreFriendsButton);
            this.areGroupBox.Location = new System.Drawing.Point(60, 153);
            this.areGroupBox.Name = "areGroupBox";
            this.areGroupBox.Size = new System.Drawing.Size(243, 161);
            this.areGroupBox.TabIndex = 14;
            this.areGroupBox.TabStop = false;
            this.areGroupBox.Text = "Are Friends";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(139, 67);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(21, 13);
            this.label1.TabIndex = 16;
            this.label1.Text = "ID:";
            // 
            // userIdTextBox
            // 
            this.userIdTextBox.Enabled = false;
            this.userIdTextBox.Location = new System.Drawing.Point(158, 64);
            this.userIdTextBox.Name = "userIdTextBox";
            this.userIdTextBox.Size = new System.Drawing.Size(137, 20);
            this.userIdTextBox.TabIndex = 15;
            // 
            // pictureBox
            // 
            this.pictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBox.ImageLocation = "http://usd1.mycdn.me/res/stub_128x96.gif";
            this.pictureBox.Location = new System.Drawing.Point(4, 2);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size(130, 98);
            this.pictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox.TabIndex = 17;
            this.pictureBox.TabStop = false;
            // 
            // TestAllButton
            // 
            this.TestAllButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.TestAllButton.Location = new System.Drawing.Point(196, 344);
            this.TestAllButton.Name = "TestAllButton";
            this.TestAllButton.Size = new System.Drawing.Size(224, 38);
            this.TestAllButton.TabIndex = 18;
            this.TestAllButton.Text = "Test All";
            this.TestAllButton.UseVisualStyleBackColor = true;
            this.TestAllButton.Click += new System.EventHandler(this.TestAllButton_Click);
            // 
            // TestOKImpotrerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(608, 394);
            this.Controls.Add(this.TestAllButton);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.userIdTextBox);
            this.Controls.Add(this.areGroupBox);
            this.Controls.Add(this.mutualGroupBox);
            this.Controls.Add(this.userInfoTextBox);
            this.Controls.Add(this.LoadFriendsButton);
            this.Controls.Add(this.AuthButton);
            this.Controls.Add(this.pictureBox);
            this.Name = "TestOKImpotrerForm";
            this.Text = "Test OK Impporter";
            this.mutualGroupBox.ResumeLayout(false);
            this.mutualGroupBox.PerformLayout();
            this.areGroupBox.ResumeLayout(false);
            this.areGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button AuthButton;
        private System.Windows.Forms.Button GeByAreFriendsButton;
        private System.Windows.Forms.Button LoadFriendsButton;
        private System.Windows.Forms.Button GetMutualButton;
        private System.Windows.Forms.Button GenerateGraphAreButton;
        private System.Windows.Forms.Button GenerateGraphMutualButton;
        private System.Windows.Forms.TextBox userInfoTextBox;
        private System.Windows.Forms.Label areLabel;
        private System.Windows.Forms.TextBox areTimeTextBox;
        private System.Windows.Forms.Label mutualLabel;
        private System.Windows.Forms.TextBox mutualTimeTextBox;
        private System.Windows.Forms.GroupBox mutualGroupBox;
        private System.Windows.Forms.GroupBox areGroupBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox userIdTextBox;
        private System.Windows.Forms.PictureBox pictureBox;
        private System.Windows.Forms.Timer timer;
        private System.Windows.Forms.Button TestAllButton;
    }
}

