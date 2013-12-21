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
using rcsir.net.vk.importer.Dialogs;
using rcsir.net.vk.importer.api;
using rcsir.net.vk.importer.NetworkAnalyzer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TestVKImporter
{
    public partial class TestVKImpotrerForm : Form
    {
        private VKLoginDialog vkLoginDialog;
        private VKRestApi vkRestApi;

        public TestVKImpotrerForm()
        {
            InitializeComponent();

            vkLoginDialog = new VKLoginDialog();
            // subscribe for login events
            vkLoginDialog.OnUserLogin += new VKLoginDialog.UserLoginHandler(UserLogin);

            vkRestApi = new VKRestApi();
            // set up data handler
            vkRestApi.OnData += new VKRestApi.DataHandler(OnData);
            // set up error handler
            vkRestApi.OnError += new VKRestApi.ErrorHandler(OnError);
        }

        public void OnData(object vkRestApi, OnDataEventArgs onDataArgs)
        {
            switch (onDataArgs.function)
            {
                case VKFunction.LoadUserInfo:
                    OnLoadUserInfo(onDataArgs.data);
                    break;
                    /*
                case VKFunction.LoadFriends:
                    OnLoadFriends(onDataArgs.data);
                    break;
                case VKFunction.GetMutual:
                    OnGetMutual(onDataArgs.data);
                    break;
                     */
                default:
                    Debug.WriteLine("Error, unknown function.");
                    break;
            }
        }

        // main error handler
        public void OnError(object vkRestApi, OnErrorEventArgs onErrorArgs)
        {
            // TODO: notify user about the error
            Debug.WriteLine("Function " + onErrorArgs.function + ", returned error: " + onErrorArgs.error);
        }

        // process load user info response
        private void OnLoadUserInfo(JObject data)
        {
            if (data[VKRestApi.RESPONSE_BODY].Count() > 0)
            {
                JObject ego = data[VKRestApi.RESPONSE_BODY][0].ToObject<JObject>();
                Console.WriteLine("Ego: " + ego.ToString());
            }
        }

        private void AuthButton_Click(object sender, EventArgs e)
        {
            vkLoginDialog.Login();
        }

        public void UserLogin(object loginDialog, UserLoginEventArgs loginArgs)
        {
            Debug.WriteLine("User Logged In: " + loginArgs.ToString());
        }

        private void LoadUserInfoButton_Click(object sender, EventArgs e)
        {
            VKRestContext context = new VKRestContext(vkLoginDialog.userId, vkLoginDialog.authToken);
            vkRestApi.callVKFunction(VKFunction.LoadUserInfo, context);
        }

        private void LoadFriendsButton_Click(object sender, EventArgs e)
        {
            vkRestApi.LoadFriends(vkLoginDialog.userId);
        }

        private void GetMutualButton_Click(object sender, EventArgs e)
        {
            vkRestApi.GetMutual(vkLoginDialog.userId, vkLoginDialog.authToken);
        }

        private void GenerateGraphButton_Click(object sender, EventArgs e)
        {
            VKNetworkAnalyzer vkNetworkAnalyzer = new VKNetworkAnalyzer();

            XmlDocument graph = vkNetworkAnalyzer.analyze(vkLoginDialog.userId, vkLoginDialog.authToken);

            if (graph != null)
            {
                graph.Save("VKNetwork_" + vkLoginDialog.userId + ".graphml");
            }
        }
    }
}
