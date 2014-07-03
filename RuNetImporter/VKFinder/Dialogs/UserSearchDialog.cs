﻿using System;
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
        public SearchParameters searchParameters { get; set; }

        private List<VKRegion> regions;
        private List<VKCity> cities;

        public UserSearchDialog(List<VKRegion> regions, List<VKCity> cities)
        {
            this.regions = regions;
            this.cities = cities;
            InitializeComponent();
        }

        private void UserSearchDialog_Load(object sender, EventArgs e)
        {
            // city combo
            this.CityComboBox.Items.Add(new VKCity(0, "any"));
            this.CityComboBox.Items.Add(new VKCity(2, "Санкт-Петербург"));
            this.CityComboBox.Items.Add(new VKCity(1, "Москва"));
            this.CityComboBox.SelectedIndex = 1; // spb

            // regions combo box
            foreach (var region in this.regions)
            {
                this.regionsComboBox.Items.Add(region);
            }

            // townd checked combo box
            foreach (var town in this.cities)
            {
                this.townsCheckedListBox.Items.Add(town);
            }

            // sex combo
            this.SexComboBox.Items.Add(new VKSex("any", 0));
            this.SexComboBox.Items.Add(new VKSex("female", 1));
            this.SexComboBox.Items.Add(new VKSex("male", 2));
            this.SexComboBox.SelectedIndex = 2; // male
        }

        private void UserSearchDialog_Validated(object sender, EventArgs e)
        {

        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            StringBuilder builder = new StringBuilder();
            
            this.searchParameters = new SearchParameters();

            this.searchParameters.query = this.QueryTextBox.Text.Trim();

            if (this.CityComboBox.SelectedItem != null)
            {
                this.searchParameters.city = (VKCity)this.CityComboBox.SelectedItem;
            }
            else
            {
                this.searchParameters.city = null;
            }

            if (this.SexComboBox.SelectedItem != null)
            {
                this.searchParameters.sex = (VKSex)this.SexComboBox.SelectedItem;
            }
            else
            {
                this.searchParameters.sex = null;
            }

            this.searchParameters.yearStart = this.YearFrom.Value;

            this.searchParameters.yearEnd = this.YearTo.Value;

            this.searchParameters.monthStart = this.MonthFrom.Value;

            this.searchParameters.monthEnd = this.MonthTo.Value;

            this.searchParameters.withPhone = this.withPhone.Checked;

            this.searchParameters.useSlowSearch = this.useSlowSearch.Checked;

            /*
            if (this.AgeFrom.Value > 0)
            {
                builder.Append("age_from=").Append(this.AgeFrom.Value).Append("&");
            }

            if (this.AgeTo.Value > 0)
            {
                builder.Append("age_to=").Append(this.AgeTo.Value).Append("&");
            }
            */
        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void CancelSearchButton_Click(object sender, EventArgs e)
        {

        }

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

    }

    public class SearchParameters
    {
        public String query;
        public VKCity city;
        public VKSex sex;
        public decimal yearStart;
        public decimal yearEnd;
        public decimal monthStart;
        public decimal monthEnd;
        public bool withPhone;
        public bool useSlowSearch;
    };

}
