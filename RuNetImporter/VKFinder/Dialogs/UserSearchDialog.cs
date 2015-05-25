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
        public SearchParameters searchParameters { get; set; }

        private Dictionary<string, List<VkCity>> citiesByDistrict;


        public UserSearchDialog(Dictionary<string, List<VkCity>> citiesByDistrict)
        {
            this.citiesByDistrict = citiesByDistrict;
            InitializeComponent();

            // one click selection
            this.townsCheckedListBox.CheckOnClick = true;
        }

        private void UserSearchDialog_Load(object sender, EventArgs e)
        {
            // districs combo box
            this.districtsComboBox.Items.Add("All"); // all districts item 
            var districts = new List<string>(citiesByDistrict.Keys);
            foreach (var d in districts)
            {
                this.districtsComboBox.Items.Add(d);

                // townd checked combo box
                var cities = citiesByDistrict[d];
                this.townsCheckedListBox.Items.AddRange(cities.ToArray());
            }

            this.districtsComboBox.SelectedIndex = 0; // all is selected

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

            this.searchParameters.cities = new List<VkCity>();
            if (this.townsCheckedListBox.CheckedItems.Count > 0)
            {
                foreach (var itemChecked in this.townsCheckedListBox.CheckedItems)
                {
                    this.searchParameters.cities.Add((VkCity)itemChecked);
                }
            }
            else
            {
                this.searchParameters.cities.Add( new VkCity(0,"Any")); // add any city item
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

                if (String.Compare(selection, "all", true) == 0)
                {
                    // show all items
                    var districts = new List<string>(citiesByDistrict.Keys);
                    foreach (var d in districts)
                    {
                        var cities = citiesByDistrict[d];
                        this.townsCheckedListBox.Items.AddRange(cities.ToArray());
                    }
                }
                else
                {
                    // add cities for a district
                    if(citiesByDistrict.ContainsKey(selection))
                    {
                        var cities = citiesByDistrict[selection];
                        this.townsCheckedListBox.Items.AddRange(cities.ToArray());
                    }
                }

                this.townsCheckedListBox.EndUpdate();
            }
        }

    }

    public class SearchParameters
    {
        public String query;
        public List<VkCity> cities;
        public VkSex sex;
        public decimal yearStart;
        public decimal yearEnd;
        public decimal monthStart;
        public decimal monthEnd;
        public bool withPhone;
        public bool useSlowSearch;
    };

}
