using System;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;
using rcsir.net.vk.importer.Dialogs;
using rcsir.net.vk.importer.api;
using rcsir.net.vk.importer.NetworkAnalyzer;
using Newtonsoft.Json.Linq;

namespace TestVKImporter
{
    public partial class TestVKImpotrerForm : Form
    {
        private VKLoginDialog vkLoginDialog;
        private VkRestApi vkRestApi;

        public TestVKImpotrerForm()
        {
            InitializeComponent();

            vkLoginDialog = new VKLoginDialog();
            // subscribe for login events
            vkLoginDialog.OnUserLogin += new VKLoginDialog.UserLoginHandler(UserLogin);

            vkRestApi = new VkRestApi();
            // set up data handler
            vkRestApi.OnData += new VkRestApi.DataHandler(OnData);
            // set up error handler
            vkRestApi.OnError += new VkRestApi.ErrorHandler(OnError);
        }

        public void OnData(object vkRestApi, OnDataEventArgs onDataArgs)
        {
            switch (onDataArgs.Function)
            {
                case VkFunction.GetProfiles:
                    OnLoadUserInfo(onDataArgs.Data);
                    break;
                default:
                    Debug.WriteLine("Error, unknown function.");
                    break;
            }
        }

        // main error handler
        public void OnError(object vkRestApi, VkRestApi.OnErrorEventArgs onErrorArgs)
        {
            // TODO: notify user about the error
            Debug.WriteLine("Function " + onErrorArgs.Function + ", returned error: " + onErrorArgs.Error);
        }

        // process load user info response
        private void OnLoadUserInfo(JObject data)
        {
            if (data[VkRestApi.ResponseBody].Count() > 0)
            {
                JObject ego = data[VkRestApi.ResponseBody][0].ToObject<JObject>();
                Console.WriteLine("Ego: " + ego.ToString());

                this.userInfoTextBox.Clear();
                this.userInfoTextBox.AppendText(ego["uid"].ToString());
                this.userInfoTextBox.AppendText("\n");
                this.userInfoTextBox.AppendText(ego["first_name"].ToString());
                this.userInfoTextBox.AppendText("\n");
                this.userInfoTextBox.AppendText(ego["last_name"].ToString());
                this.userInfoTextBox.AppendText("\n");
            }
        }

        private void ActivateControls(bool activate)
        {
            this.userIdTextBox.Enabled = activate;
            this.LoadUserInfoButton.Enabled = activate;
            this.GenerateGraphButton.Enabled = activate;
        }

        private void AuthButton_Click(object sender, EventArgs e)
        {
            vkLoginDialog.Login("friends"); // default permission - friends
        }

        public void UserLogin(object loginDialog, UserLoginEventArgs loginArgs)
        {
            Debug.WriteLine("User Logged In: " + loginArgs.ToString());
            this.userIdTextBox.Clear();
            this.userIdTextBox.Text = loginArgs.userId;
            this.ActivateControls(true);
        }

        private void LoadUserInfoButton_Click(object sender, EventArgs e)
        {
            String userId = this.userIdTextBox.Text;
            if (userId == null ||
                userId.Length == 0)
            {
                userId = vkLoginDialog.userId;
                this.userIdTextBox.Text = userId;
            }

            var context = new VkRestApi.VkRestContext(userId, vkLoginDialog.authToken);
            vkRestApi.CallVkFunction(VkFunction.GetProfiles, context);
        }

        private void GenerateGraphButton_Click(object sender, EventArgs e)
        {
            String userId = this.userIdTextBox.Text;
            if (userId == null ||
                userId.Length == 0)
            {
                userId = vkLoginDialog.userId;
                this.userIdTextBox.Text = userId;
            }

            VKNetworkAnalyzer vkNetworkAnalyzer = new VKNetworkAnalyzer();
            XmlDocument graph = vkNetworkAnalyzer.analyze(userId, vkLoginDialog.authToken);

            if (graph != null)
            {
                graph.Save("VKNetwork_" + userId + ".graphml");
            }
        }

        private void testVkDialogButton_Click(object sender, EventArgs e)
        {
            VKDialog vkDialog = new VKDialog();
            vkDialog.ShowDialog();
        }
    }
}
