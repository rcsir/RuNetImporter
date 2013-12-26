using System;
using System.Diagnostics;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using rcsir.net.ok.importer.Api;
using rcsir.net.ok.importer.Controllers;
using rcsir.net.ok.importer.Dialogs;
using rcsir.net.ok.importer.NetworkAnalyzer;

namespace TestOKImporter
{
    public partial class TestOKImpotrerForm : Form
    {
        private readonly OkController controller;
        private OKLoginDialog okLoginDialog;
        private DateTime areStartTime;
        private DateTime mutualStartTime;

        public TestOKImpotrerForm()
        {
            InitializeComponent();
            controller = new OkController();
            controller.OnData += OnData;
            controller.OnError += OnError;
        }

        public void OnData(object okRestApi, OnDataEventArgs onDataArgs)
        {
            switch (onDataArgs.function) {
                case OKFunction.LoadUserInfo:
                    OnLoadUserInfo(onDataArgs.data);
                    break;
                case OKFunction.LoadFriends:
                    OnLoadFriends();
                    break;
                case OKFunction.GetAreGraph:
                    OnGetFriends(false);
                    break;
                case OKFunction.GetMutualGraph:
                    OnGetFriends();
                    break;
                case OKFunction.GenerateAreGraph:
                    OnGenerateGraph();
                    break;
                case OKFunction.GenerateMutualGraph:
                    OnGenerateGraph();
                    break;
                default:
                    Debug.WriteLine("Error, unknown function.");
                    break;
            }
        }

        // main error handler
        public void OnError(object okRestApi, OnErrorEventArgs onErrorArgs)
        {
            // TODO: notify user about the error
            Debug.WriteLine("Function " + onErrorArgs.function + ", returned error: " + onErrorArgs.error);
        }

        // process load user info response
        private void OnLoadUserInfo(JObject ego)
        {
            userInfoTextBox.Clear();
            userInfoTextBox.AppendText(ego["name"] + "\n");
            userInfoTextBox.AppendText(ego["age"] + "лет; ");
            userInfoTextBox.AppendText("д.р.: " +ego["birthday"] + "\n");
            userInfoTextBox.AppendText(ego["gender"] + "\n");
            userInfoTextBox.AppendText(ego["location"]["country"] + ": ");
            userInfoTextBox.AppendText(ego["location"]["city"] + "\n");
            userInfoTextBox.AppendText(ego["pic_1"] + "\n");
            userInfoTextBox.AppendText("Статус: " + ego["current_status"]);

            pictureBox.ImageLocation = ego["pic_2"].ToString();
            userIdTextBox.Clear();
            userIdTextBox.Text = ego["uid"].ToString();
            ActivateAuthControls(true);
        }
        // process load user friends response
        private void OnLoadFriends()
        {
            ActivateFriendsControls(true);
        }

        private void OnGetFriends(bool isMutual = true)
        {
            timer.Stop();
            if (isMutual)
                mutualTimeTextBox.Text = (DateTime.Now - mutualStartTime).ToString();
            else
                areTimeTextBox.Text = (DateTime.Now - areStartTime).ToString();
        }

        private void OnGenerateGraph()
        {
            Enabled = true;
        }
        private void ActivateFriendsControls(bool activate)
        {
            GeByAreFriendsButton.Enabled = activate;
            GetMutualButton.Enabled = activate;
        }

        private void ActivateAuthControls(bool activate)
        {
            LoadFriendsButton.Enabled = activate;
            GenerateGraphAreButton.Enabled = activate;
            GenerateGraphMutualButton.Enabled = activate;
        }

        private void AuthButton_Click(object sender, EventArgs e)
        {
            if (okLoginDialog == null)
                okLoginDialog = new OKLoginDialog(controller);
            
            okLoginDialog.Login();
        }

        private void LoadFriendsButton_Click(object sender, EventArgs e)
        {
            controller.CallOkFunction(OKFunction.LoadFriends);
        }

        private void GeByAreFriendsButton_Click(object sender, EventArgs e)
        {
            areStartTime = DateTime.Now;
            controller.CallOkFunction(OKFunction.GetAreGraph);
        }

        private void GetMutualButton_Click(object sender, EventArgs e)
        {
            mutualStartTime = DateTime.Now;
            controller.CallOkFunction(OKFunction.GetMutualGraph);
        }

        private void GenerateGraphAreButton_Click(object sender, EventArgs e)
        {
            Enabled = false;
            controller.CallOkFunction(OKFunction.GenerateAreGraph);
            OKNetworkAnalyzer analyzer = new OKNetworkAnalyzer();
            analyzer.MakeTestXml(controller.Vertices, controller.Edges, controller.EgoId);
        }

        private void GenerateGraphMutualButton_Click(object sender, EventArgs e)
        {
            Enabled = false;
            controller.CallOkFunction(OKFunction.GenerateMutualGraph);
            OKNetworkAnalyzer analyzer = new OKNetworkAnalyzer();
            analyzer.MakeTestXml(controller.Vertices, controller.Edges, controller.EgoId);
        }
    }
}
