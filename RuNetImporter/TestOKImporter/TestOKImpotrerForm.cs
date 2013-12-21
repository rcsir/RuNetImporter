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
        private OKRestClient okRestClient;

        public TestOKImpotrerForm()
        {
            InitializeComponent();
        }

        private void AuthButton_Click(object sender, EventArgs e)
        {
            if (okLoginDialog == null) {
                okLoginDialog = new OKLoginDialog();
                okRestClient = okLoginDialog.OkRestClient;
            }
            okLoginDialog.Login();
        }

        private void LoadUserInfoButton_Click(object sender, EventArgs e)
        {
            if (okLoginDialog == null)
            {
                Debug.WriteLine("Please authorize first!");
                return;
            }

            okRestClient.GetAreFriends();
        }

        private void LoadFriendsButton_Click(object sender, EventArgs e)
        {
            if (okLoginDialog == null)
            {
                Debug.WriteLine("Please authorize first!");
                return;
            }

            okRestClient.LoadFriends();

        }

        private void GetMutualButton_Click(object sender, EventArgs e)
        {
            if (okLoginDialog == null)
            {
                Debug.WriteLine("Please authorize first!");
                return;
            }

            okRestClient.GetMutualFriends();

        }

        private void GenerateGraphButton_Click(object sender, EventArgs e)
        {
            if (okLoginDialog == null)
            {
                Debug.WriteLine("Please authorize first!");
                return;
            }

            OKNetworkAnalyzer okNetworkAnalyzer = new OKNetworkAnalyzer();
            okNetworkAnalyzer.okRestClient = okRestClient;
            XmlDocument graph = okNetworkAnalyzer.analyze(false);

            if (graph != null)
            {
                graph.Save("OKNetwork_" + okRestClient.userId + ".xml");
            }
        }

        private void GenerateGraphAreButton_Click(object sender, EventArgs e)
        {
            if (okLoginDialog == null)
            {
                Debug.WriteLine("Please authorize first!");
                return;
            }

            OKNetworkAnalyzer okNetworkAnalyzer = new OKNetworkAnalyzer();
            okNetworkAnalyzer.okRestClient = okRestClient;
            XmlDocument graph = okNetworkAnalyzer.analyze();

            if (graph != null)
            {
                graph.Save("OKNetwork_" + okRestClient.userId + ".xml");
            }
        }
    }
}
