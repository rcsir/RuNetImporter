using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;
using rcsir.net.ok.importer.Dialogs;
using rcsir.net.ok.importer.GraphDataProvider;
using rcsir.net.ok.importer.NetworkAnalyzer;

namespace TestOKImporter
{
    public partial class TestOKImpotrerForm : Form
    {
        private OKLoginDialog vkLoginDialog;
        private OKRestClient vkRestClient = new OKRestClient();

        public TestOKImpotrerForm()
        {
            InitializeComponent();
        }

        private void AuthButton_Click(object sender, EventArgs e)
        {
            if (vkLoginDialog == null)
                vkLoginDialog = new OKLoginDialog();

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

            vkRestClient.GetMutual(vkLoginDialog.userId, vkLoginDialog.authToken);

        }

        private void GenerateGraphButton_Click(object sender, EventArgs e)
        {
            if (vkLoginDialog == null)
            {
                Debug.WriteLine("Please authorize first!");
                return;
            }

            OKNetworkAnalyzer vkNetworkAnalyzer = new OKNetworkAnalyzer();

            XmlDocument graph = vkNetworkAnalyzer.analyze(vkLoginDialog.userId, vkLoginDialog.authToken);

            if (graph != null)
            {
                graph.Save("OKNetwork_" + vkLoginDialog.userId + ".xml");
            }
        }
    }
}
