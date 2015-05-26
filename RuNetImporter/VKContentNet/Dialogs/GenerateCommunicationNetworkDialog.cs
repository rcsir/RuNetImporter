using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using VKContentNet;

namespace rcsir.net.vk.content.Dialogs
{
    public partial class GenerateCommunicationNetworkDialog : Form
    {
        public int type { get; set; }

        public GenerateCommunicationNetworkDialog()
        {
            InitializeComponent();
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            var checkedButton = GraphTypeGroupBox.Controls.OfType<RadioButton>()
                                      .FirstOrDefault(r => r.Checked);
            if (checkedButton != null)
            {
                switch (checkedButton.Text)
                {
                    case "Comments":
                        type = 1;
                        break;
                    case "Likes":
                        type = 2;
                        break;
                    case "Reply":
                        type = 3;
                        break;
                    case "Combined":
                        type = 4;
                        break;
                    default:
                        type = 4;
                        break;
                }
            }
        }

        private void Dialog_Load(object sender, EventArgs e)
        {

        }
    }
}
