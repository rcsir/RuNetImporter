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
    public partial class DownloadGroupMembersDialog : Form
    {
        public decimal groupId { get; set; }

        public DownloadGroupMembersDialog()
        {
            InitializeComponent();
            this.groupIdNumeric.Value = groupId;
        }

        private void OKButton_Click(object sender, EventArgs e)
        {   
            groupId = this.groupIdNumeric.Value;
            // note: group id is negative in the group's wall get request
            if (this.isGroupcheckBox.Checked)
            {
                groupId *= -1;
            }
        }
    }
}
