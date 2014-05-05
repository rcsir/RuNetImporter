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
    public partial class FindGroupsDialog : Form
    {
        public decimal groupId { get; set; }
        public bool isGroup { get; set; }

        public FindGroupsDialog()
        {
            InitializeComponent();
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            groupId = this.groupIdNumeric.Value;
            isGroup = this.isGroupcheckBox.Checked;
        }
    }
}
