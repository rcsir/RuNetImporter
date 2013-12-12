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
        private OKLoginDialog okLoginDialog;
        private OKRestClient okRestClient = new OKRestClient();

        public TestOKImpotrerForm()
        {
            InitializeComponent();
        }

        private void AuthButton_Click(object sender, EventArgs e)
        {
            if (okLoginDialog == null)
                okLoginDialog = new OKLoginDialog();

            okLoginDialog.Login();
        }

        private void LoadUserInfoButton_Click(object sender, EventArgs e)
        {
            if (okLoginDialog == null)
            {
                Debug.WriteLine("Please authorize first!");
                return;
            }

            okRestClient.LoadUserInfo(okRestClient.userId, okRestClient.authToken);
        }

        private void LoadFriendsButton_Click(object sender, EventArgs e)
        {
            if (okLoginDialog == null)
            {
                Debug.WriteLine("Please authorize first!");
                return;
            }

            okRestClient.LoadFriends(okRestClient.userId);

        }

        private void GetMutualButton_Click(object sender, EventArgs e)
        {
            if (okLoginDialog == null)
            {
                Debug.WriteLine("Please authorize first!");
                return;
            }

            okRestClient.GetMutual(okRestClient.userId, okRestClient.authToken);

        }

        private void GenerateGraphButton_Click(object sender, EventArgs e)
        {
            if (okLoginDialog == null)
            {
                Debug.WriteLine("Please authorize first!");
                return;
            }

            OKNetworkAnalyzer vkNetworkAnalyzer = new OKNetworkAnalyzer();

            XmlDocument graph = vkNetworkAnalyzer.analyze(okRestClient.userId, okRestClient.authToken);

            if (graph != null)
            {
                graph.Save("OKNetwork_" + okRestClient.userId + ".xml");
            }
        }
    }
}
