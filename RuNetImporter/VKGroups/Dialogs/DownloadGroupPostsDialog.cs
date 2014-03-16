﻿using System;
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

        public DownloadGroupPostsDialog()
        {
            InitializeComponent();
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            // note: group id is negative in the group's wall get request
            groupId = -1 * this.groupIdNumeric.Value;
        }
    }
}