using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace rcsir.net.vk.content.Dialogs
{
    public partial class GenerateCommunicationNetworkDialog : Form
    {
        public decimal groupId { get; set; }
        public bool isGroup { get; set; }

        public GenerateCommunicationNetworkDialog()
        {
            InitializeComponent();
        }

        private void OKButton_Click(object sender, EventArgs e)
        {   
        }

        private void DownloadMembersNetworkDialog_Load(object sender, EventArgs e)
        {
            this.groupIdNumeric.Value = groupId;
            this.isGroupcheckBox.Checked = isGroup;
        }
    }
}
