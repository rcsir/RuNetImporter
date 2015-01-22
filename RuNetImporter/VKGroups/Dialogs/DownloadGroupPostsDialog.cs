using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace rcsir.net.vk.groups.Dialogs
{
    public partial class DownloadGroupPostsDialog : Form
    {
        public decimal groupId { get; set; }
        public bool isGroup { get; set; }
        public DateTime fromDate { get; set; }
        public DateTime toDate { get; set; }
        public Boolean justGroupStats { get; set; }

        public DownloadGroupPostsDialog()
        {
            InitializeComponent();
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            this.fromDate = this.dateTimeFromPicker.Value;
            this.toDate = this.dateTimeToPicker.Value;
            this.justGroupStats = this.groupStatCheckBox.Checked;
        }

        private void DownloadGroupPostsDialog_Load(object sender, EventArgs e)
        {
            this.groupIdNumeric.Value = groupId;
            this.isGroupcheckBox.Checked = isGroup;

            this.dateTimeFromPicker.Value = DateTime.Today;
            this.dateTimeToPicker.Value = DateTime.Today.AddDays(1);
        }

        private void dateTimeToPicker_ValueChanged(object sender, EventArgs e)
        {

        }

    }
}
