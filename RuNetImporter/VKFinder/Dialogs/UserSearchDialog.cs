using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using rcsir.net.vk.importer.api;
using rcsir.net.vk.importer.api.entity;


namespace rcsir.net.vk.finder.Dialogs
{
    public partial class UserSearchDialog : Form
    {
        public SearchParameters searchParameters { get; set; }

        private readonly List<rcsir.net.vk.importer.api.entity.Region> regions; 
        private readonly List<City> cities;


        public UserSearchDialog(List<rcsir.net.vk.importer.api.entity.Region> regions, List<City> cities)
        {
            this.regions = regions;
            this.cities = cities;
            InitializeComponent();

            // one click selection
            this.townsCheckedListBox.CheckOnClick = true;
        }

        private void UserSearchDialog_Load(object sender, EventArgs e)
        {
            // district combo box
            foreach (var d in regions)
            {
                districtsComboBox.Items.Add(d);
            }

            const int regionId = 0; // major cities
            // town checked combo box
            foreach (var city in cities)
            {
                if (city.RegionId == regionId)
                {
                    this.townsCheckedListBox.Items.Add(city);
                }
            }

            // major cities selected
            this.districtsComboBox.SelectedIndex = regionId;

            // sex combo
            this.SexComboBox.Items.Add(new VkSex("any", 0));
            this.SexComboBox.Items.Add(new VkSex("female", 1));
            this.SexComboBox.Items.Add(new VkSex("male", 2));
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

            this.searchParameters.cities = new List<City>();
            if (this.townsCheckedListBox.CheckedItems.Count > 0)
            {
                foreach (var itemChecked in this.townsCheckedListBox.CheckedItems)
                {
                    this.searchParameters.cities.Add((City)itemChecked);
                }
            }
            else
            {
                this.searchParameters.cities.Add( new City(0,"Any")); // add any city item
            }

            if (this.SexComboBox.SelectedItem != null)
            {
                this.searchParameters.sex = (VkSex)this.SexComboBox.SelectedItem;
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

        private void districtsComboBox_SelectionChangeCommitted(object sender, EventArgs e)
        {
            ComboBox senderComboBox = (ComboBox)sender;

            // Change the length of the text box depending on what the user has  
            // selected and committed using the SelectionLength property. 
            if (senderComboBox.SelectionLength > 0)
            {
                string selection =
                    senderComboBox.SelectedItem.ToString();

                this.townsCheckedListBox.BeginUpdate();
                this.townsCheckedListBox.Items.Clear();

                var regionId = ((importer.api.entity.Region) senderComboBox.SelectedItem).Id;
                // add cities for the region
                foreach (var city in cities)
                {
                    if (city.RegionId == regionId)
                    {
                        this.townsCheckedListBox.Items.Add(city);
                    }
                }

                this.townsCheckedListBox.EndUpdate();
            }
        }

    }

    public class SearchParameters
    {
        public String query;
        public List<City> cities;
        public VkSex sex;
        public decimal yearStart;
        public decimal yearEnd;
        public decimal monthStart;
        public decimal monthEnd;
        public bool withPhone;
        public bool useSlowSearch;
    };

}
