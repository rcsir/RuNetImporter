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
            this.withPhone = new System.Windows.Forms.CheckBox();
            this.useSlowSearch = new System.Windows.Forms.CheckBox();
            this.districtsComboBox = new System.Windows.Forms.ComboBox();
            this.label10 = new System.Windows.Forms.Label();
            this.townsCheckedListBox = new System.Windows.Forms.CheckedListBox();
            this.label11 = new System.Windows.Forms.Label();
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
            this.OKButton.Location = new System.Drawing.Point(514, 455);
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
            this.CancelSearchButton.Location = new System.Drawing.Point(417, 455);
            this.CancelSearchButton.Name = "CancelSearchButton";
            this.CancelSearchButton.Size = new System.Drawing.Size(75, 23);
            this.CancelSearchButton.TabIndex = 1;
            this.CancelSearchButton.Text = "Cancel";
            this.CancelSearchButton.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(123, 255);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(42, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Gender";
            // 
            // SexComboBox
            // 
            this.SexComboBox.FormattingEnabled = true;
            this.SexComboBox.Location = new System.Drawing.Point(211, 255);
            this.SexComboBox.Name = "SexComboBox";
            this.SexComboBox.Size = new System.Drawing.Size(316, 21);
            this.SexComboBox.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(123, 359);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(52, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Age From";
            this.label3.Visible = false;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(385, 364);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(42, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Age To";
            this.label4.Visible = false;
            // 
            // AgeFrom
            // 
            this.AgeFrom.Location = new System.Drawing.Point(211, 357);
            this.AgeFrom.Name = "AgeFrom";
            this.AgeFrom.Size = new System.Drawing.Size(79, 20);
            this.AgeFrom.TabIndex = 8;
            this.AgeFrom.Visible = false;
            // 
            // AgeTo
            // 
            this.AgeTo.Location = new System.Drawing.Point(452, 357);
            this.AgeTo.Name = "AgeTo";
            this.AgeTo.Size = new System.Drawing.Size(75, 20);
            this.AgeTo.TabIndex = 9;
            this.AgeTo.Visible = false;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(123, 223);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(35, 13);
            this.label5.TabIndex = 10;
            this.label5.Text = "Name";
            // 
            // QueryTextBox
            // 
            this.QueryTextBox.Location = new System.Drawing.Point(211, 220);
            this.QueryTextBox.Name = "QueryTextBox";
            this.QueryTextBox.Size = new System.Drawing.Size(316, 20);
            this.QueryTextBox.TabIndex = 11;
            // 
            // YearFrom
            // 
            this.YearFrom.Location = new System.Drawing.Point(211, 294);
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
            this.label6.Location = new System.Drawing.Point(123, 296);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(55, 13);
            this.label6.TabIndex = 12;
            this.label6.Text = "Year From";
            // 
            // YearTo
            // 
            this.YearTo.Location = new System.Drawing.Point(452, 294);
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
            this.label7.Location = new System.Drawing.Point(385, 296);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(45, 13);
            this.label7.TabIndex = 15;
            this.label7.Text = "Year To";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(385, 327);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(53, 13);
            this.label8.TabIndex = 19;
            this.label8.Text = "Month To";
            // 
            // MonthTo
            // 
            this.MonthTo.Location = new System.Drawing.Point(452, 325);
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
            this.MonthFrom.Location = new System.Drawing.Point(211, 325);
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
            this.label9.Location = new System.Drawing.Point(123, 327);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(63, 13);
            this.label9.TabIndex = 16;
            this.label9.Text = "Month From";
            // 
            // withPhone
            // 
            this.withPhone.AutoSize = true;
            this.withPhone.Checked = true;
            this.withPhone.CheckState = System.Windows.Forms.CheckState.Checked;
            this.withPhone.Location = new System.Drawing.Point(377, 411);
            this.withPhone.Name = "withPhone";
            this.withPhone.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.withPhone.Size = new System.Drawing.Size(82, 17);
            this.withPhone.TabIndex = 20;
            this.withPhone.Text = "With Phone";
            this.withPhone.UseVisualStyleBackColor = true;
            // 
            // useSlowSearch
            // 
            this.useSlowSearch.AutoSize = true;
            this.useSlowSearch.Location = new System.Drawing.Point(189, 411);
            this.useSlowSearch.Name = "useSlowSearch";
            this.useSlowSearch.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.useSlowSearch.Size = new System.Drawing.Size(108, 17);
            this.useSlowSearch.TabIndex = 21;
            this.useSlowSearch.Text = "Use Slow Search";
            this.useSlowSearch.UseVisualStyleBackColor = true;
            // 
            // districtsComboBox
            // 
            this.districtsComboBox.FormattingEnabled = true;
            this.districtsComboBox.Location = new System.Drawing.Point(120, 21);
            this.districtsComboBox.Name = "districtsComboBox";
            this.districtsComboBox.Size = new System.Drawing.Size(469, 21);
            this.districtsComboBox.TabIndex = 22;
            this.districtsComboBox.SelectionChangeCommitted += new System.EventHandler(this.districtsComboBox_SelectionChangeCommitted);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(32, 21);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(44, 13);
            this.label10.TabIndex = 23;
            this.label10.Text = "Districts";
            // 
            // townsCheckedListBox
            // 
            this.townsCheckedListBox.FormattingEnabled = true;
            this.townsCheckedListBox.Location = new System.Drawing.Point(120, 55);
            this.townsCheckedListBox.Name = "townsCheckedListBox";
            this.townsCheckedListBox.Size = new System.Drawing.Size(469, 154);
            this.townsCheckedListBox.TabIndex = 24;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(32, 55);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(39, 13);
            this.label11.TabIndex = 25;
            this.label11.Text = "Towns";
            // 
            // UserSearchDialog
            // 
            this.AcceptButton = this.OKButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(601, 490);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.townsCheckedListBox);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.districtsComboBox);
            this.Controls.Add(this.useSlowSearch);
            this.Controls.Add(this.withPhone);
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
            this.Controls.Add(this.CancelSearchButton);
            this.Controls.Add(this.OKButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UserSearchDialog";
            this.Text = "UserSearchDialog";
            this.Load += new System.EventHandler(this.UserSearchDialog_Load);
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
        private System.Windows.Forms.CheckBox withPhone;
        private System.Windows.Forms.CheckBox useSlowSearch;
        private System.Windows.Forms.ComboBox districtsComboBox;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.CheckedListBox townsCheckedListBox;
        private System.Windows.Forms.Label label11;
    }
}