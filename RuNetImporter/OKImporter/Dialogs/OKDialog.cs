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
        private readonly OKLoginDialog loginDialog;

        public OKLoginDialog LoginDialog { get { return loginDialog; } }

        private AttributesDictionary<bool> dialogAttributes;

        public AttributesDictionary<bool> DialogAttributes { set { dialogAttributes = value; } }

        string authUri;

        public string AuthUri { set { authUri = value; } }

        public event EventHandler<CommandEventArgs> CommandEventHandler;

        public OKDialog() : base(new OKNetworkAnalyzer())
        {
            InitializeComponent();
/*            var graphDataManager = new GraphDataManager();
            var requestController = new RequestController();*/
            loginDialog = new OKLoginDialog(authUri);
            new OkController(this);
            loginDialog.AuthUri = authUri;
            addAttributes(dialogAttributes);
        }

        public void OnData(object obj, GraphEventArgs graphEvent)
        {
            switch (graphEvent.Type)
            {
                case GraphEventArgs.Types.UserInfoLoaded:
                    onLoadUserInfo(graphEvent.JData["uid"].ToString());
                    break;
/*               case GraphEventArgs.Types.FriendsLoaded:
                    onLoadFriends();
                    break;
                case GraphEventArgs.Types.AreGraphLoaded:
                    onGetFriends(false);
                    break;
                case GraphEventArgs.Types.MutualGraphLoaded:
                    onGetFriends();
                    break;*/
                case GraphEventArgs.Types.GraphGenerated:
                    onGenerateGraph(graphEvent);
                    break;
            }
        }
        // main error handler
        private void OnError(object obj, ErrorEventArgs onErrorArgs)
        {
            // TODO: notify user about the error
            Debug.WriteLine("Function " + onErrorArgs.Type + ", returned error: " + onErrorArgs.Error);
        }

        private void onLoadUserInfo(string id)
        {
            ((OKNetworkAnalyzer)m_oHttpNetworkAnalyzer).EgoId = id;
            btnOK.Enabled = true;
            LoginDialog.Close();
        }

        private void onGenerateGraph(GraphEventArgs graphEvent)
        {
            ((OKNetworkAnalyzer)m_oHttpNetworkAnalyzer).SetGraph(graphEvent.Vertices, graphEvent.Edges, graphEvent.DialogAttributes, graphEvent.GraphAttributes);
            try
            {
                List<NetworkType> oEdgeType = new List<NetworkType>();
                ((OKNetworkAnalyzer)m_oHttpNetworkAnalyzer).GetNetworkAsync(oEdgeType, chkIncludeMe.Checked, DateTime.Now, DateTime.Now);
            }
            catch (NullReferenceException e)
            {
                MessageBox.Show(e.Message);
            }
            Enabled = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            AssertValid();
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
            DispatchCommandEvent(CommandEventArgs.Commands.GenerateGraph);
/*            try {
                List<NetworkType> oEdgeType = new List<NetworkType>();
                DispatchCommandEvent(CommandEventArgs.Commands.GenerateGraph);
                ((OKNetworkAnalyzer)m_oHttpNetworkAnalyzer).GetNetworkAsync(oEdgeType, chkIncludeMe.Checked, DateTime.Now, DateTime.Now);
 //               ((OKNetworkAnalyzer)m_oHttpNetworkAnalyzer).analyze();
            } catch (NullReferenceException e) {
                MessageBox.Show(e.Message);
            }*/
        }

        protected virtual void DispatchCommandEvent(CommandEventArgs.Commands command, DataGridViewRow[] rows = null)
        {
            var evnt = new CommandEventArgs(command, rows);
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
            readAttributes();
//            (m_oHttpNetworkAnalyzer as OKNetworkAnalyzer).GraphDataManager = graphDataManager;
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
/*            if (loginDialog == null)
                loginDialog = new OKLoginDialog(requestController);*/
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
            var rows = new DataGridViewRow[dgAttributes.Rows.Count];
            dgAttributes.Rows.CopyTo(rows, 0);
            DispatchCommandEvent(CommandEventArgs.Commands.MakeAttributes, rows);
        }

        private void chkSelectAll_CheckedChanged(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgAttributes.Rows)
                row.Cells[1].Value = ((CheckBox)sender).Checked;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
/*            if (loginDialog == null)
                loginDialog = new OKLoginDialog(requestController);*/
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
    }
}
