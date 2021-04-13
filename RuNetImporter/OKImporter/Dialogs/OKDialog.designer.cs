﻿using System.Drawing;

namespace rcsir.net.ok.importer.Dialogs
{
    partial class OKDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OKDialog));
            this.btnCancel = new System.Windows.Forms.Button();
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.grpAttributes = new System.Windows.Forms.GroupBox();
            this.chkSelectAll = new System.Windows.Forms.CheckBox();
            this.dgAttributes = new System.Windows.Forms.DataGridView();
            this.attributeColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.includeColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.attributeValueColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.grpOptions = new System.Windows.Forms.GroupBox();
            this.chkIncludeMe = new System.Windows.Forms.CheckBox();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.slStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblMainText = new System.Windows.Forms.Label();
            this.btnLogin = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.ttRateLimit = new System.Windows.Forms.ToolTip(this.components);
            this.ttLogoutLogin = new System.Windows.Forms.ToolTip(this.components);
            this.ttCallPerSecond = new System.Windows.Forms.ToolTip(this.components);
            this.grpAttributes.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgAttributes)).BeginInit();
            this.grpOptions.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(517, 443);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(105, 41);
            this.btnCancel.TabIndex = 0;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.button1_Click);
            // 
            // linkLabel1
            // 
            this.linkLabel1.AutoSize = true;
            this.linkLabel1.Location = new System.Drawing.Point(207, 41);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(142, 13);
            this.linkLabel1.TabIndex = 12;
            this.linkLabel1.TabStop = true;
            this.linkLabel1.Text = "Click here to logout from OK.";
            this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
            // 
            // grpAttributes
            // 
            this.grpAttributes.Controls.Add(this.chkSelectAll);
            this.grpAttributes.Controls.Add(this.dgAttributes);
            this.grpAttributes.Location = new System.Drawing.Point(12, 78);
            this.grpAttributes.Name = "grpAttributes";
            this.grpAttributes.Size = new System.Drawing.Size(610, 277);
            this.grpAttributes.TabIndex = 11;
            this.grpAttributes.TabStop = false;
            this.grpAttributes.Text = "Attributes";
            // 
            // chkSelectAll
            // 
            this.chkSelectAll.Location = new System.Drawing.Point(215, 19);
            this.chkSelectAll.Name = "chkSelectAll";
            this.chkSelectAll.Size = new System.Drawing.Size(14, 15);
            this.chkSelectAll.TabIndex = 1;
            this.chkSelectAll.UseVisualStyleBackColor = true;
            this.chkSelectAll.CheckedChanged += new System.EventHandler(this.chkSelectAll_CheckedChanged);
            // 
            // dgAttributes
            // 
            this.dgAttributes.AllowUserToAddRows = false;
            this.dgAttributes.AllowUserToDeleteRows = false;
            this.dgAttributes.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgAttributes.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.attributeColumn,
            this.includeColumn,
            this.attributeValueColumn});
            this.dgAttributes.Location = new System.Drawing.Point(6, 19);
            this.dgAttributes.Name = "dgAttributes";
            this.dgAttributes.RowHeadersVisible = false;
            this.dgAttributes.RowTemplate.Height = 24;
            this.dgAttributes.Size = new System.Drawing.Size(598, 246);
            this.dgAttributes.TabIndex = 0;
            // 
            // attributeColumn
            // 
            this.attributeColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.attributeColumn.HeaderText = "Attribute";
            this.attributeColumn.Name = "attributeColumn";
            this.attributeColumn.ReadOnly = true;
            // 
            // includeColumn
            // 
            this.includeColumn.HeaderText = "Include";
            this.includeColumn.Name = "includeColumn";
            this.includeColumn.Width = 61;
            // 
            // attributeValueColumn
            // 
            this.attributeValueColumn.HeaderText = "Attribute";
            this.attributeValueColumn.Name = "attributeValueColumn";
            this.attributeValueColumn.Visible = false;
            // 
            // grpOptions
            // 
            this.grpOptions.Controls.Add(this.chkIncludeMe);
            this.grpOptions.Location = new System.Drawing.Point(18, 371);
            this.grpOptions.Name = "grpOptions";
            this.grpOptions.Size = new System.Drawing.Size(604, 51);
            this.grpOptions.TabIndex = 10;
            this.grpOptions.TabStop = false;
            this.grpOptions.Text = "Options";
            // 
            // chkIncludeMe
            // 
            this.chkIncludeMe.AutoSize = true;
            this.chkIncludeMe.Location = new System.Drawing.Point(6, 19);
            this.chkIncludeMe.Name = "chkIncludeMe";
            this.chkIncludeMe.Size = new System.Drawing.Size(79, 17);
            this.chkIncludeMe.TabIndex = 11;
            this.chkIncludeMe.Text = "Include Me";
            this.chkIncludeMe.UseVisualStyleBackColor = true;
            this.chkIncludeMe.CheckedChanged += new System.EventHandler(this.chkIncludeMe_CheckedChanged);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.slStatusLabel});
            this.statusStrip1.Location = new System.Drawing.Point(0, 487);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(634, 22);
            this.statusStrip1.SizingGrip = false;
            this.statusStrip1.TabIndex = 4;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // slStatusLabel
            // 
            this.slStatusLabel.Name = "slStatusLabel";
            this.slStatusLabel.Size = new System.Drawing.Size(0, 17);
            // 
            // lblMainText
            // 
            this.lblMainText.Location = new System.Drawing.Point(12, 9);
            this.lblMainText.Name = "lblMainText";
            this.lblMainText.Size = new System.Drawing.Size(610, 32);
            this.lblMainText.TabIndex = 3;
            this.lblMainText.Text = resources.GetString("lblMainText.Text");
            // 
            // btnLogin
            // 
            this.btnLogin.Location = new System.Drawing.Point(18, 443);
            this.btnLogin.Name = "btnLogin";
            this.btnLogin.Size = new System.Drawing.Size(105, 41);
            this.btnLogin.TabIndex = 2;
            this.btnLogin.Text = "Login";
            this.btnLogin.UseVisualStyleBackColor = true;
            this.btnLogin.Click += new System.EventHandler(this.btnLogin_Click);
            // 
            // btnOK
            // 
            this.btnOK.Enabled = false;
            this.btnOK.Location = new System.Drawing.Point(129, 443);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(105, 41);
            this.btnOK.TabIndex = 1;
            this.btnOK.Text = "Download";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // ttRateLimit
            // 
            this.ttRateLimit.AutoPopDelay = 10000;
            this.ttRateLimit.InitialDelay = 500;
            this.ttRateLimit.ReshowDelay = 100;
            this.ttRateLimit.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.ttRateLimit.ToolTipTitle = "Prevent Rate Limit";
            // 
            // ttLogoutLogin
            // 
            this.ttLogoutLogin.AutoPopDelay = 15000;
            this.ttLogoutLogin.InitialDelay = 500;
            this.ttLogoutLogin.ReshowDelay = 100;
            this.ttLogoutLogin.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.ttLogoutLogin.ToolTipTitle = "Logout/Login";
            // 
            // ttCallPerSecond
            // 
            this.ttCallPerSecond.AutoPopDelay = 10000;
            this.ttCallPerSecond.InitialDelay = 500;
            this.ttCallPerSecond.ReshowDelay = 100;
            this.ttCallPerSecond.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.ttCallPerSecond.ToolTipTitle = "One call per second";
            // 
            // OKDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(634, 509);
            this.Controls.Add(this.linkLabel1);
            this.Controls.Add(this.grpAttributes);
            this.Controls.Add(this.grpOptions);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.lblMainText);
            this.Controls.Add(this.btnLogin);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OKDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "OK Network Importer";
            this.Load += new System.EventHandler(this.OKDialog_Load);
            this.grpAttributes.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgAttributes)).EndInit();
            this.grpOptions.ResumeLayout(false);
            this.grpOptions.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnCancel;
        public System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnLogin;
        private System.Windows.Forms.Label lblMainText;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel slStatusLabel;
        private System.Windows.Forms.GroupBox grpAttributes;
        public System.Windows.Forms.CheckBox chkSelectAll;
        public System.Windows.Forms.DataGridView dgAttributes;
        private System.Windows.Forms.GroupBox grpOptions;
        private System.Windows.Forms.DataGridViewTextBoxColumn attributeColumn;
        private System.Windows.Forms.DataGridViewCheckBoxColumn includeColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn attributeValueColumn;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.ToolTip ttRateLimit;
        private System.Windows.Forms.ToolTip ttLogoutLogin;
        private System.Windows.Forms.ToolTip ttCallPerSecond;
        private System.Windows.Forms.CheckBox chkIncludeMe;
    }
}