namespace rcsir.net.vk.content.Dialogs
{
    partial class GenerateCommunicationNetworkDialog
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
            this.Comments = new System.Windows.Forms.RadioButton();
            this.Likes = new System.Windows.Forms.RadioButton();
            this.Combined = new System.Windows.Forms.RadioButton();
            this.GraphTypeGroupBox = new System.Windows.Forms.GroupBox();
            this.Reply = new System.Windows.Forms.RadioButton();
            this.GraphTypeGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // CancelSearchButton
            // 
            this.CancelSearchButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelSearchButton.Location = new System.Drawing.Point(217, 289);
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
            this.OKButton.Location = new System.Drawing.Point(325, 289);
            this.OKButton.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.OKButton.Name = "OKButton";
            this.OKButton.Size = new System.Drawing.Size(100, 28);
            this.OKButton.TabIndex = 2;
            this.OKButton.Text = "OK";
            this.OKButton.UseVisualStyleBackColor = true;
            this.OKButton.Click += new System.EventHandler(this.OKButton_Click);
            // 
            // Comments
            // 
            this.Comments.AutoSize = true;
            this.Comments.Location = new System.Drawing.Point(29, 33);
            this.Comments.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Comments.Name = "Comments";
            this.Comments.Size = new System.Drawing.Size(95, 21);
            this.Comments.TabIndex = 4;
            this.Comments.Text = "Comments";
            this.Comments.UseVisualStyleBackColor = true;
            // 
            // Likes
            // 
            this.Likes.AutoSize = true;
            this.Likes.Location = new System.Drawing.Point(29, 76);
            this.Likes.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Likes.Name = "Likes";
            this.Likes.Size = new System.Drawing.Size(62, 21);
            this.Likes.TabIndex = 5;
            this.Likes.Text = "Likes";
            this.Likes.UseVisualStyleBackColor = true;
            // 
            // Combined
            // 
            this.Combined.AutoSize = true;
            this.Combined.Checked = true;
            this.Combined.Location = new System.Drawing.Point(29, 162);
            this.Combined.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Combined.Name = "Combined";
            this.Combined.Size = new System.Drawing.Size(92, 21);
            this.Combined.TabIndex = 6;
            this.Combined.TabStop = true;
            this.Combined.Text = "Combined";
            this.Combined.UseVisualStyleBackColor = true;
            // 
            // GraphTypeGroupBox
            // 
            this.GraphTypeGroupBox.Controls.Add(this.Reply);
            this.GraphTypeGroupBox.Controls.Add(this.Likes);
            this.GraphTypeGroupBox.Controls.Add(this.Combined);
            this.GraphTypeGroupBox.Controls.Add(this.Comments);
            this.GraphTypeGroupBox.Location = new System.Drawing.Point(8, 8);
            this.GraphTypeGroupBox.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.GraphTypeGroupBox.Name = "GraphTypeGroupBox";
            this.GraphTypeGroupBox.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.GraphTypeGroupBox.Size = new System.Drawing.Size(417, 208);
            this.GraphTypeGroupBox.TabIndex = 7;
            this.GraphTypeGroupBox.TabStop = false;
            this.GraphTypeGroupBox.Text = "Communication Graph Type";
            // 
            // Reply
            // 
            this.Reply.AutoSize = true;
            this.Reply.Location = new System.Drawing.Point(29, 119);
            this.Reply.Margin = new System.Windows.Forms.Padding(2);
            this.Reply.Name = "Reply";
            this.Reply.Size = new System.Drawing.Size(65, 21);
            this.Reply.TabIndex = 7;
            this.Reply.Text = "Reply";
            this.Reply.UseVisualStyleBackColor = true;
            // 
            // GenerateCommunicationNetworkDialog
            // 
            this.AcceptButton = this.OKButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelSearchButton;
            this.ClientSize = new System.Drawing.Size(432, 332);
            this.Controls.Add(this.GraphTypeGroupBox);
            this.Controls.Add(this.CancelSearchButton);
            this.Controls.Add(this.OKButton);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "GenerateCommunicationNetworkDialog";
            this.Text = "Generate Communication Network";
            this.Load += new System.EventHandler(this.Dialog_Load);
            this.GraphTypeGroupBox.ResumeLayout(false);
            this.GraphTypeGroupBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button CancelSearchButton;
        private System.Windows.Forms.Button OKButton;
        private System.Windows.Forms.RadioButton Comments;
        private System.Windows.Forms.RadioButton Likes;
        private System.Windows.Forms.RadioButton Combined;
        private System.Windows.Forms.GroupBox GraphTypeGroupBox;
        private System.Windows.Forms.RadioButton Reply;
    }
}