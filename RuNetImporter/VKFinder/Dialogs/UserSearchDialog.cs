using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using rcsir.net.vk.importer.api;

namespace rcsir.net.vk.finder.Dialogs
{

    public partial class UserSearchDialog : Form
    {
        public String parameters {get;set;}

        public UserSearchDialog()
        {
            InitializeComponent();
            
            // city combo
            this.CityComboBox.Items.Add(new VKCity("any", 0));
            this.CityComboBox.Items.Add(new VKCity("Санкт-Петербург", 2));
            this.CityComboBox.Items.Add(new VKCity("Москва", 1));
            this.CityComboBox.SelectedIndex = 0;

            // sex combo
            this.SexComboBox.Items.Add(new VKSex("any", 0));
            this.SexComboBox.Items.Add(new VKSex("female", 1));
            this.SexComboBox.Items.Add(new VKSex("male", 2));
            this.SexComboBox.SelectedIndex = 0;

            
        }

        private void UserSearchDialog_Validated(object sender, EventArgs e)
        {

        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            StringBuilder builder = new StringBuilder();

            int value;

            if (this.QueryTextBox.Text.Length > 0)
            {
                builder.Append("q=").Append(this.QueryTextBox.Text).Append("&");
            }

            if (this.CityComboBox.SelectedItem != null)
            {
                value = ((VKCity)this.CityComboBox.SelectedItem).Value;
                if (value > 0)
                {
                    builder.Append("city=").Append(value).Append("&");
                }
            }

            if (this.SexComboBox.SelectedItem != null)
            {
                value = ((VKSex)this.SexComboBox.SelectedItem).Value;
                builder.Append("sex=").Append(value).Append("&");
            }

            if (this.YearFrom.Value > 1900)
            {
                builder.Append("birth_year=").Append(this.YearFrom.Value).Append("&");
            }

            if (this.YearTo.Value > 1900)
            {
               // builder.Append("=").Append(this.YearTo.Value).Append("&");
            }

            if (this.MonthFrom.Value > 0)
            {
                builder.Append("birth_month=").Append(this.MonthFrom.Value).Append("&");
            }

            if (this.MonthTo.Value > 0)
            {
                // builder.Append("=").Append(this.MonthTo.Value).Append("&");
            } 
            
            if (this.AgeFrom.Value > 0)
            {
                builder.Append("age_from=").Append(this.AgeFrom.Value).Append("&");
            }

            if (this.AgeTo.Value > 0)
            {
                builder.Append("age_to=").Append(this.AgeTo.Value).Append("&");
            }

            // set parameters
            this.parameters = builder.ToString();
        }

        private void label6_Click(object sender, EventArgs e)
        {

        }
    }
}
