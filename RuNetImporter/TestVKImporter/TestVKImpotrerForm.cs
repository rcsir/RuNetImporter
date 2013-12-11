using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using rcsir.net.vk.importer.Dialogs;
using rcsir.net.vk.importer.GraphDataProvider;


namespace TestVKImporter
{
    public partial class TestVKImpotrerForm : Form
    {
        private VKLoginDialog vkLoginDialog;
        private VKRestClient vkRestClient = new VKRestClient();

        public TestVKImpotrerForm()
        {
            InitializeComponent();
        }

        private void AuthButton_Click(object sender, EventArgs e)
        {
            if (vkLoginDialog == null)
                vkLoginDialog = new VKLoginDialog();

            vkLoginDialog.Login();
        }

        private void LoadUserInfoButton_Click(object sender, EventArgs e)
        {
            if (vkLoginDialog == null)
            {
                Debug.WriteLine("Please authorize first!");
                return;
            }

            vkRestClient.LoadUserInfo(vkLoginDialog.userId, vkLoginDialog.authToken);
        }

        private void LoadFriendsButton_Click(object sender, EventArgs e)
        {
            if (vkLoginDialog == null)
            {
                Debug.WriteLine("Please authorize first!");
                return;
            }

            vkRestClient.LoadFriends(vkLoginDialog.userId);

        }

        private void GetMutualButton_Click(object sender, EventArgs e)
        {
            if (vkLoginDialog == null)
            {
                Debug.WriteLine("Please authorize first!");
                return;
            }

            vkRestClient.getMutual(vkLoginDialog.userId, vkLoginDialog.authToken);

        }
    }
}
