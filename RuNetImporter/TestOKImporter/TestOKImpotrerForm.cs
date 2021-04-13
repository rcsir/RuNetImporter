using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Smrf.AppLib;
using rcsir.net.common.Utilities;
using rcsir.net.ok.importer.Controllers;
using rcsir.net.ok.importer.Dialogs;
using rcsir.net.ok.importer.Events;
using rcsir.net.ok.importer.GraphDataProvider;
using rcsir.net.ok.importer.NetworkAnalyzer;
using ErrorEventArgs = rcsir.net.ok.importer.Events.ErrorEventArgs;

namespace TestOKImporter
{
    public partial class TestOKImpotrerForm : Form,  ICommandEventDispatcher
    {
        private readonly OKNetworkAnalyzer analyzer;

        private DateTime areStartTime;
        private DateTime mutualStartTime;

        private readonly OKLoginDialog okLoginDialog;

        public OKLoginDialog LoginDialog { get { return okLoginDialog; } }

        private AttributesDictionary<bool> dialogAttributes;

        public AttributesDictionary<bool> DialogAttributes { set { dialogAttributes = value; } }

        public event EventHandler<CommandEventArgs> CommandEventHandler;

        public TestOKImpotrerForm()
        {
            InitializeComponent();
//            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
            analyzer = new OKNetworkAnalyzer();
            okLoginDialog = new OKLoginDialog();
            new OkController(this);
        }
/*
        void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            Debug.WriteLine(e);
        }
*/
       
        protected virtual void DispatchEvent(CommandEventArgs e)
        {
            EventHandler<CommandEventArgs> handler = CommandEventHandler;
            if (handler != null)
                handler(this, e);
        }

        public void OnData(object obj, GraphEventArgs graphEvent = null)
        {
            switch (graphEvent.Type) {
                case GraphEventArgs.Types.UserInfoLoaded:
                    onLoadUserInfo(graphEvent.JData);
                    break;
                case GraphEventArgs.Types.FriendsLoaded:
                    onLoadFriends();
                    break;
                case GraphEventArgs.Types.AreGraphLoaded:
                    onGetFriends(false);
                    break;
                case GraphEventArgs.Types.MutualGraphLoaded:
                    onGetFriends();
                    break;
                case GraphEventArgs.Types.GraphGenerated:
                    onGenerateGraph(graphEvent);
                    break;
            }
        }

        public void OnRequestError(object obj, ErrorEventArgs onErrorArgs)
        {
            MessageBox.Show("Error type: " + onErrorArgs.Type + "\nReturned error: " + onErrorArgs.Description, "Test OKImpotrer ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Debug.WriteLine("Error type: " + onErrorArgs.Type + ", returned error: " + onErrorArgs.Description);
        }

        private void onLoadUserInfo(JSONObject ego)
        {
            userInfoTextBox.Clear();
            userInfoTextBox.AppendText(ego.Dictionary["name"].String + "\n");
            userInfoTextBox.AppendText(ego.Dictionary["age"].String + "лет; ");
            userInfoTextBox.AppendText("д.р.: " + ego.Dictionary["birthday"].String + "\n");
            userInfoTextBox.AppendText(ego.Dictionary["gender"].String + "\n");
            userInfoTextBox.AppendText(ego.Dictionary["location"].Dictionary["country"].String + ": ");
            userInfoTextBox.AppendText(ego.Dictionary["location"].Dictionary["city"].String + "\n");
            userInfoTextBox.AppendText(ego.Dictionary["pic_1"].String + "\n");
            if (ego.Dictionary.ContainsKey("current_status"))
            userInfoTextBox.AppendText("Статус: " + ego.Dictionary["current_status"].String);

            pictureBox.ImageLocation = ego.Dictionary["pic_2"].String;
            userIdTextBox.Clear();
            userIdTextBox.Text = analyzer.EgoId = ego.Dictionary["uid"].String;

            LoginDialog.Close();
            ActivateAuthControls(true);
        }

        private void onLoadFriends()
        {
            ActivateFriendsControls(true);
        }

        private void onGetFriends(bool isMutual = true)
        {
            timer.Stop();
            if (isMutual)
                mutualTimeTextBox.Text = (DateTime.Now - mutualStartTime).ToString();
            else
                areTimeTextBox.Text = (DateTime.Now - areStartTime).ToString();
        }

        private void onGenerateGraph(GraphEventArgs graphEvent)
        {
            analyzer.SetGraph(graphEvent.Vertices, graphEvent.Edges, graphEvent.DialogAttributes, graphEvent.GraphAttributes);
            analyzer.MakeTestXml();
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
            LoginDialog.Login();
        }

        private void LoadFriendsButton_Click(object sender, EventArgs e)
        {
            var evnt = new CommandEventArgs(CommandEventArgs.Commands.LoadFriends);
            DispatchEvent(evnt);
        }

        private void GeByAreFriendsButton_Click(object sender, EventArgs e)
        {
            areStartTime = DateTime.Now;
            var evnt = new CommandEventArgs(CommandEventArgs.Commands.GetGraphByAreFriends);
            DispatchEvent(evnt);
        }

        private void GetMutualButton_Click(object sender, EventArgs e)
        {
            mutualStartTime = DateTime.Now;
            var evnt = new CommandEventArgs(CommandEventArgs.Commands.GetGraphByMutualFriends);
            DispatchEvent(evnt);
        }

        private void GenerateGraphAreButton_Click(object sender, EventArgs e)
        {
            Enabled = false;
            areStartTime = DateTime.Now;
            var evnt = new CommandEventArgs(CommandEventArgs.Commands.GenerateGraphByAreFriends);
            DispatchEvent(evnt);
        }

        private void GenerateGraphMutualButton_Click(object sender, EventArgs e)
        {
            Enabled = false;
            mutualStartTime = DateTime.Now;
            var evnt = new CommandEventArgs(CommandEventArgs.Commands.GenerateGraphByMutualFriends);
            DispatchEvent(evnt);
        }

        private void TestAllButton_Click(object sender, EventArgs e)
        {
            OKGraphDataProvider fbGraph = new OKGraphDataProvider();
            string data;
            fbGraph.TryGetGraphDataAsTemporaryFile(out data);
            TextWriter tw = new StreamWriter("FanPageASGraphML.txt");
            tw.WriteLine(data);
            tw.Close();
        }
    }
}
