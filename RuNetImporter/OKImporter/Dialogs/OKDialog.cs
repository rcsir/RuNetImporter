using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Smrf.NodeXL.GraphDataProviders;
using rcsir.net.ok.importer.Controllers;
using rcsir.net.ok.importer.Events;
using rcsir.net.ok.importer.NetworkAnalyzer;
using Smrf.AppLib;
using Smrf.SocialNetworkLib;

namespace rcsir.net.ok.importer.Dialogs
{
    public partial class OKDialog : GraphDataProviderDialogBase, ICommandEventDispatcher
    {
        private const int stageCount = 4;
      
        private OKNetworkAnalyzer analyzer { get { return (OKNetworkAnalyzer)m_oHttpNetworkAnalyzer; } }
        private string userName;
        private int friendsCount;
        private int currentStage = 0;
        
        private readonly OKLoginDialog loginDialog;

        public OKLoginDialog LoginDialog { get { return loginDialog; } }

        private AttributesDictionary<bool> dialogAttributes;

        public AttributesDictionary<bool> DialogAttributes { set { dialogAttributes = value; } }

        public event EventHandler<CommandEventArgs> CommandEventHandler;

        public OKDialog() : base(new OKNetworkAnalyzer())
        {
            InitializeComponent();
            loginDialog = new OKLoginDialog();
            analyzer.Controller = new OkController(this);
            addAttributes(dialogAttributes);
            FormUtil.ApplicationName = "OK Network Importer";
        }

        public void OnData(object obj, GraphEventArgs graphEvent)
        {
            switch (graphEvent.Type) {
                case GraphEventArgs.Types.UserInfoLoaded:
                    userName = graphEvent.JData.Dictionary["name"].String;
                    onLoadUserInfo(graphEvent.JData.Dictionary["uid"].String);
                    break;
                case GraphEventArgs.Types.FriendsListLoaded:
                    onLoadFriendsList(graphEvent.Count);
                    break;
                case GraphEventArgs.Types.FriendsLoaded:
                    onLoadFriends();
                    break;
                case GraphEventArgs.Types.AreGraphLoaded:
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
            ShowError("Error type: " + onErrorArgs.Type + "\nReturned error: " + onErrorArgs.Description);
            Debug.WriteLine("Error type: " + onErrorArgs.Type + ", returned error: " + onErrorArgs.Description);
        }

        private void onLoadUserInfo(string id)
        {
            analyzer.EgoId = id;
            btnOK.Enabled = true;
            LoginDialog.Close();
        }

        private void onLoadFriendsList(int friendsNumber)
        {
            friendsCount = friendsNumber;
            showProgress("Loading list of " + friendsCount + "  friends");
        }

        private void onLoadFriends()
        {
            showProgress("Loading " + friendsCount + " friends info...");
        }

        private void onGetFriends()
        {
            showProgress("Generating friends graph...");
        }

        private void onGenerateGraph(GraphEventArgs graphEvent)
        {
            analyzer.SetGraph(graphEvent.Vertices, graphEvent.Edges, graphEvent.DialogAttributes, graphEvent.GraphAttributes);
            Enabled = true;
            showProgress("Generating graph document...");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            AssertValid();
            m_oHttpNetworkAnalyzer.CancelAsync();
            Close();
        }
        //*************************************************************************
        //  Property: ToolStripStatusLabel
        //
        /// <summary>
        /// Gets the dialog's ToolStripStatusLabel control.
        /// </summary>
        ///
        /// <value>
        /// The dialog's ToolStripStatusLabel control, or null if the dialog
        /// doesn't have one.  The default is null.
        /// </value>
        ///
        /// <remarks>
        /// If the derived dialog overrides this property and returns a non-null
        /// ToolStripStatusLabel control, the control's text will automatically get
        /// updated when the HttpNetworkAnalyzer fires a ProgressChanged event.
        /// </remarks>
        //*************************************************************************

        protected override ToolStripStatusLabel ToolStripStatusLabel { get { AssertValid(); return (slStatusLabel); } }

        //*************************************************************************
        //  Method: DoDataExchange()
        //
        /// <summary>
        /// Transfers data between the dialog's fields and its controls.
        /// </summary>
        ///
        /// <param name="bFromControls">
        /// true to transfer data from the dialog's controls to its fields, false
        /// for the other direction.
        /// </param>
        ///
        /// <returns>
        /// true if the transfer was successful.
        /// </returns>
        //*************************************************************************

        protected override Boolean DoDataExchange (Boolean bFromControls)
        {
            if (bFromControls){
                // Validate the controls.
            } else{

            }
            return (true);
        }

        //*************************************************************************
        //  Method: StartAnalysis()
        //
        /// <summary>
        /// Starts the Flickr analysis.
        /// </summary>
        ///
        /// <remarks>
        /// It's assumed that DoDataExchange(true) was called and succeeded.
        /// </remarks>
        //*************************************************************************

        protected override void StartAnalysis()
        {
            AssertValid();
            m_oGraphMLXmlDocument = null;

            try {
                List<NetworkType> oEdgeType = new List<NetworkType>();
                analyzer.GetNetworkAsync(oEdgeType, chkIncludeMe.Checked, DateTime.Now, DateTime.Now);
            } catch (NullReferenceException e) {
                MessageBox.Show(e.Message);
            }
        }

        protected virtual void DispatchCommandEvent(CommandEventArgs.Commands command, bool[] rows = null, bool isMeIncluding = false)
        {
            var evnt = new CommandEventArgs(command, rows, isMeIncluding);
            EventHandler<CommandEventArgs> handler = CommandEventHandler;
            if (handler != null)
                handler(this, evnt);
        }

        //*************************************************************************
        //  Method: EnableControls()
        //
        /// <summary>
        /// Enables or disables the dialog's controls.
        /// </summary>
        //*************************************************************************

        protected override void EnableControls()
        {
            AssertValid();
            Boolean bIsBusy = m_oHttpNetworkAnalyzer.IsBusy;
            btnOK.Enabled = !bIsBusy;
            UseWaitCursor = bIsBusy;
        }

        //*************************************************************************
        //  Method: OnEmptyGraph()
        //
        /// <summary>
        /// Handles the case where a graph was successfully obtained by is empty.
        /// </summary>
        //*************************************************************************

        protected override void OnEmptyGraph()
        {
            AssertValid();
        }

        //*************************************************************************
        //  Method: btnOK_Click()
        //
        /// <summary>
        /// Handles the Click event on the btnOK button.
        /// </summary>
        ///
        /// <param name="sender">
        /// Standard event argument.
        /// </param>
        ///
        /// <param name="e">
        /// Standard event argument.
        /// </param>
        //*************************************************************************

        protected void btnOK_Click(object sender, EventArgs e)
        {
            AssertValid();
            ToolStripStatusLabel.Text = "Starting for user " + userName;
            readAttributes();
            OnOKClick();
        }

        //*************************************************************************
        //  Protected constants
        //*************************************************************************


        //*************************************************************************
        //  Protected fields
        //*************************************************************************

        // These are static so that the dialog's controls will retain their values
        // between dialog invocations.  Most NodeXL dialogs persist control values
        // via ApplicationSettingsBase, but this plugin does not have access to
        // that and so it resorts to static fields.

        /// Tag to get the related tags for.  Can be empty but not null.
//        protected static String m_sTag = "sociology";
        /// Network level to include.
        //protected static NetworkLevel m_eNetworkLevel = NetworkLevel.OnePointFive;
        /// true to include a sample thumbnail for each tag.
//        protected static Boolean m_bIncludeSampleThumbnails = false;

        private void addAttributes(AttributesDictionary<bool> dialogAttributes)
        {
            int i = 0;
            dgAttributes.Rows.Add(dialogAttributes.Count);
            foreach (KeyValuePair<AttributeUtils.Attribute, bool> kvp in dialogAttributes) {
                dgAttributes.Rows[i].Cells[0].Value = kvp.Key.name;
                dgAttributes.Rows[i].Cells[1].Value = kvp.Value;
                dgAttributes.Rows[i].Cells[2].Value = kvp.Key.value;
                i++;
            }
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            LoginDialog.Login();
        }
/*
        private void PrintAttributes()
        {
            string text = "";
            foreach (KeyValuePair<AttributeUtils.Attribute, bool> kvp in attributes)
            {
                text += kvp.Key.name + "=" + kvp.Value.ToString() + "\n";
            }

            this.ShowInformation(text);
        }
*/
        private void readAttributes()
        {
/*            var rows = new DataGridViewRow[dgAttributes.Rows.Count];
            dgAttributes.Rows.CopyTo(rows, 0);
            makeAttributesValue(dgAttributes.Rows);
            DispatchCommandEvent(CommandEventArgs.Commands.MakeAttributes, rows, chkIncludeMe.Checked);*/
            DispatchCommandEvent(CommandEventArgs.Commands.UpdateAllAttributes, makeAttributesValue(dgAttributes.Rows), chkIncludeMe.Checked);
        }

        private void chkSelectAll_CheckedChanged(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgAttributes.Rows)
                row.Cells[1].Value = ((CheckBox)sender).Checked;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            loginDialog.Logout();
        }

        private void chkIncludeMe_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void OKDialog_Load(object sender, EventArgs e)
        {
            dgAttributes.Columns[1].Width =
            TextRenderer.MeasureText(dgAttributes.Columns[1].HeaderText,
            dgAttributes.Columns[1].HeaderCell.Style.Font).Width + 25;
            //Get the column header cell bounds
            Rectangle rect = dgAttributes.GetCellDisplayRectangle(1, -1, true);
            //Change the location of the CheckBox to make it stay on the header
            chkSelectAll.Location = new Point(rect.Location.X + rect.Width - 20, rect.Location.Y + Math.Abs((rect.Height - chkSelectAll.Height) / 2));
            chkSelectAll.CheckedChanged += chkSelectAll_CheckedChanged;
            //Add the CheckBox into the DataGridView
            dgAttributes.Controls.Add(chkSelectAll);
        }

        private void showProgress(string message)
        {
            ToolStripStatusLabel.Text = "Stage " + ++currentStage + " / " + stageCount + ": " + message;
        }

        private bool[] makeAttributesValue(DataGridViewRowCollection rows)
        {
            var result = new bool[rows.Count];
            for (var i = 0; i < rows.Count; i++)
                result[i] = (bool) rows[i].Cells[1].Value;
            return result;
        }
    }
}
