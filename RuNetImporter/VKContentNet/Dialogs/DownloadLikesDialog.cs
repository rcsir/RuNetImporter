using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace rcsir.net.vk.content.Dialogs
{
    public partial class DownloadLikesDialog : Form
    {
        public decimal GroupId { get; set; }
        public bool IsGroup { get; set; }
        public string[] PostIDs { get; set; }

        public DownloadLikesDialog()
        {
            InitializeComponent();
        }

        private void DownloadLikesDialog_Load(object sender, EventArgs e)
        {
            groupIdNumeric.Value = GroupId;
            isGroupcheckBox.Checked = IsGroup;
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            PostIDs = postsTextBox.Lines;
        }
    }
}
