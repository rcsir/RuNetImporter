namespace rcsir.net.vk.finder.Dialogs
{
    partial class UserSearchDialog
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
            this.CityComboBox = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.SexComboBox = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.AgeFrom = new System.Windows.Forms.NumericUpDown();
            this.AgeTo = new System.Windows.Forms.NumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            this.QueryTextBox = new System.Windows.Forms.TextBox();
            this.YearFrom = new System.Windows.Forms.NumericUpDown();
            this.label6 = new System.Windows.Forms.Label();
            this.YearTo = new System.Windows.Forms.NumericUpDown();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.MonthTo = new System.Windows.Forms.NumericUpDown();
            this.MonthFrom = new System.Windows.Forms.NumericUpDown();
            this.label9 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.AgeFrom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.AgeTo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.YearFrom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.YearTo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.MonthTo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.MonthFrom)).BeginInit();
            this.SuspendLayout();
            // 
            // OKButton
            // 
            this.OKButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.OKButton.Location = new System.Drawing.Point(292, 244);
            this.OKButton.Name = "OKButton";
            this.OKButton.Size = new System.Drawing.Size(75, 23);
            this.OKButton.TabIndex = 0;
            this.OKButton.Text = "OK";
            this.OKButton.UseVisualStyleBackColor = true;
            this.OKButton.Click += new System.EventHandler(this.OKButton_Click);
            // 
            // CancelSearchButton
            // 
            this.CancelSearchButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelSearchButton.Location = new System.Drawing.Point(211, 244);
            this.CancelSearchButton.Name = "CancelSearchButton";
            this.CancelSearchButton.Size = new System.Drawing.Size(75, 23);
            this.CancelSearchButton.TabIndex = 1;
            this.CancelSearchButton.Text = "Cancel";
            this.CancelSearchButton.UseVisualStyleBackColor = true;
            this.CancelSearchButton.Click += new System.EventHandler(this.CancelSearchButton_Click);
            // 
            // CityComboBox
            // 
            this.CityComboBox.FormattingEnabled = true;
            this.CityComboBox.Location = new System.Drawing.Point(126, 50);
            this.CityComboBox.Name = "CityComboBox";
            this.CityComboBox.Size = new System.Drawing.Size(241, 21);
            this.CityComboBox.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(38, 53);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(24, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "City";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(38, 89);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(42, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Gender";
            // 
            // SexComboBox
            // 
            this.SexComboBox.FormattingEnabled = true;
            this.SexComboBox.Location = new System.Drawing.Point(126, 89);
            this.SexComboBox.Name = "SexComboBox";
            this.SexComboBox.Size = new System.Drawing.Size(241, 21);
            this.SexComboBox.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(38, 200);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(52, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Age From";
            this.label3.Visible = false;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(225, 205);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(42, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Age To";
            this.label4.Visible = false;
            // 
            // AgeFrom
            // 
            this.AgeFrom.Location = new System.Drawing.Point(126, 198);
            this.AgeFrom.Name = "AgeFrom";
            this.AgeFrom.Size = new System.Drawing.Size(79, 20);
            this.AgeFrom.TabIndex = 8;
            this.AgeFrom.Visible = false;
            // 
            // AgeTo
            // 
            this.AgeTo.Location = new System.Drawing.Point(292, 198);
            this.AgeTo.Name = "AgeTo";
            this.AgeTo.Size = new System.Drawing.Size(75, 20);
            this.AgeTo.TabIndex = 9;
            this.AgeTo.Visible = false;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(38, 19);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(35, 13);
            this.label5.TabIndex = 10;
            this.label5.Text = "Name";
            // 
            // QueryTextBox
            // 
            this.QueryTextBox.Location = new System.Drawing.Point(126, 16);
            this.QueryTextBox.Name = "QueryTextBox";
            this.QueryTextBox.Size = new System.Drawing.Size(241, 20);
            this.QueryTextBox.TabIndex = 11;
            // 
            // YearFrom
            // 
            this.YearFrom.Location = new System.Drawing.Point(126, 135);
            this.YearFrom.Maximum = new decimal(new int[] {
            3000,
            0,
            0,
            0});
            this.YearFrom.Name = "YearFrom";
            this.YearFrom.Size = new System.Drawing.Size(79, 20);
            this.YearFrom.TabIndex = 13;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(38, 137);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(55, 13);
            this.label6.TabIndex = 12;
            this.label6.Text = "Year From";
            this.label6.Click += new System.EventHandler(this.label6_Click);
            // 
            // YearTo
            // 
            this.YearTo.Location = new System.Drawing.Point(292, 135);
            this.YearTo.Maximum = new decimal(new int[] {
            3000,
            0,
            0,
            0});
            this.YearTo.Name = "YearTo";
            this.YearTo.Size = new System.Drawing.Size(75, 20);
            this.YearTo.TabIndex = 14;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(225, 137);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(45, 13);
            this.label7.TabIndex = 15;
            this.label7.Text = "Year To";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(225, 168);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(53, 13);
            this.label8.TabIndex = 19;
            this.label8.Text = "Month To";
            // 
            // MonthTo
            // 
            this.MonthTo.Location = new System.Drawing.Point(292, 166);
            this.MonthTo.Maximum = new decimal(new int[] {
            12,
            0,
            0,
            0});
            this.MonthTo.Name = "MonthTo";
            this.MonthTo.Size = new System.Drawing.Size(75, 20);
            this.MonthTo.TabIndex = 18;
            // 
            // MonthFrom
            // 
            this.MonthFrom.Location = new System.Drawing.Point(126, 166);
            this.MonthFrom.Maximum = new decimal(new int[] {
            12,
            0,
            0,
            0});
            this.MonthFrom.Name = "MonthFrom";
            this.MonthFrom.Size = new System.Drawing.Size(79, 20);
            this.MonthFrom.TabIndex = 17;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(38, 168);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(63, 13);
            this.label9.TabIndex = 16;
            this.label9.Text = "Month From";
            // 
            // UserSearchDialog
            // 
            this.AcceptButton = this.OKButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(379, 279);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.MonthTo);
            this.Controls.Add(this.MonthFrom);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.YearTo);
            this.Controls.Add(this.YearFrom);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.QueryTextBox);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.AgeTo);
            this.Controls.Add(this.AgeFrom);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.SexComboBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.CityComboBox);
            this.Controls.Add(this.CancelSearchButton);
            this.Controls.Add(this.OKButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UserSearchDialog";
            this.Text = "UserSearchDialog";
            this.Validated += new System.EventHandler(this.UserSearchDialog_Validated);
            ((System.ComponentModel.ISupportInitialize)(this.AgeFrom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.AgeTo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.YearFrom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.YearTo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.MonthTo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.MonthFrom)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button OKButton;
        private System.Windows.Forms.Button CancelSearchButton;
        private System.Windows.Forms.ComboBox CityComboBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox SexComboBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown AgeFrom;
        private System.Windows.Forms.NumericUpDown AgeTo;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox QueryTextBox;
        private System.Windows.Forms.NumericUpDown YearFrom;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.NumericUpDown YearTo;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.NumericUpDown MonthTo;
        private System.Windows.Forms.Label label9;
        internal System.Windows.Forms.NumericUpDown MonthFrom;
    }
}