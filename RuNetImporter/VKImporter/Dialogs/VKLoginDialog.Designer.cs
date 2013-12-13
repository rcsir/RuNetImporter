namespace rcsir.net.vk.importer.Dialogs
{
    partial class VKLoginDialog
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
            this.webBrowserLogin = new System.Windows.Forms.WebBrowser();
            this.SuspendLayout();
            // 
            // webBrowserLogin
            // 
            this.webBrowserLogin.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webBrowserLogin.Location = new System.Drawing.Point(0, 0);
            this.webBrowserLogin.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowserLogin.Name = "webBrowserLogin";
            this.webBrowserLogin.Size = new System.Drawing.Size(690, 328);
            this.webBrowserLogin.TabIndex = 0;
            this.webBrowserLogin.DocumentCompleted += new System.Windows.Forms.WebBrowserDocumentCompletedEventHandler(this.webBrowserLogin_DocumentCompleted);
            // 
            // VKLoginDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(690, 328);
            this.Controls.Add(this.webBrowserLogin);
            this.MinimumSize = new System.Drawing.Size(600, 300);
            this.Name = "VKLoginDialog";
            this.Text = "VKLoginDialog";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.WebBrowser webBrowserLogin;
    }
}