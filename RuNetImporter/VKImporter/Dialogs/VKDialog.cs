using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Smrf.SocialNetworkLib;
using Smrf.AppLib;
using Smrf.NodeXL.GraphDataProviders;
using rcsir.net.vk.importer.NetworkAnalyzer;

namespace rcsir.net.vk.importer.Dialogs
{
    public partial class VKDialog : GraphDataProviderDialogBase
    {
        public VKDialog(): 
            base(new VKNetworkAnalyzer())
        {
            InitializeComponent();
            addAttributes();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            AssertValid();
            this.m_oHttpNetworkAnalyzer.CancelAsync();
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

        protected override ToolStripStatusLabel
        ToolStripStatusLabel
        {
            get
            {
                AssertValid();

                return (this.slStatusLabel);
            }
        }

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

        protected override Boolean
        DoDataExchange
        (
            Boolean bFromControls
        )
        {
            if (bFromControls)
            {
                // Validate the controls.
            }
            else
            {
           
            }

            return (true);
        }

        //*************************************************************************
        //  Method: StartAnalysis()
        //
        /// <summary>
        /// Starts the VK network analysis.
        /// </summary>
        ///
        /// <remarks>
        /// It's assumed that DoDataExchange(true) was called and succeeded.
        /// </remarks>
        //*************************************************************************

        protected override void
        StartAnalysis()
        {
            AssertValid();
            m_oGraphMLXmlDocument = null;            

            try
            {
                //accessToken = loginDialog.authToken;
                //String userId = loginDialog.userId;
                //((VKNetworkAnalyzer)m_oHttpNetworkAnalyzer).analyze(userId, accessToken);

                VKNetworkAnalyzer.NetworkAsyncArgs arguments = new VKNetworkAnalyzer.NetworkAsyncArgs();
                arguments.accessToken = loginDialog.authToken;
                arguments.userId = loginDialog.userId;
                arguments.attributes = attributes;
                arguments.fields = fields;
                arguments.includeMe = this.chkIncludeMe.Checked;
             
                // run the task in the background
                m_oHttpNetworkAnalyzer.RunAsync(arguments);
            }
            catch (NullReferenceException e)
            {
                MessageBox.Show(e.Message);
            }      
        
        }

        //*************************************************************************
        //  Method: EnableControls()
        //
        /// <summary>
        /// Enables or disables the dialog's controls.
        /// </summary>
        //*************************************************************************

        protected override void
        EnableControls()
        {
            AssertValid();

            Boolean bIsBusy = m_oHttpNetworkAnalyzer.IsBusy;

            //EnableControls(!bIsBusy, pnlUserInputs);
            btnOK.Enabled = !bIsBusy;
            this.UseWaitCursor = bIsBusy;
        }

        //*************************************************************************
        //  Method: OnEmptyGraph()
        //
        /// <summary>
        /// Handles the case where a graph was successfully obtained by is empty.
        /// </summary>
        //*************************************************************************

        protected override void
        OnEmptyGraph()
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

        protected void
        btnOK_Click
        (
            object sender,
            EventArgs e
        )
        {
            AssertValid();

            OnOKClick();
        }


        //*************************************************************************
        //  Method: AssertValid()
        //
        /// <summary>
        /// Asserts if the object is in an invalid state.  Debug-only.
        /// </summary>
        //*************************************************************************

        // [Conditional("DEBUG")]

        public override void
        AssertValid()
        {
            base.AssertValid();
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

        protected static String m_sTag = "sociology";

        /// Network level to include.
        //protected static NetworkLevel m_eNetworkLevel = NetworkLevel.OnePointFive;

        private VKLoginDialog loginDialog;

        private List<AttributeUtils.Attribute> attributes;

        private String permissions;

        private String fields;

        private void addAttributes()
        {
            attributes = new List<AttributeUtils.Attribute>(this.m_oHttpNetworkAnalyzer.GetDefaultNetworkAttributes());
            int i = 0;
            dgAttributes.Rows.Add(attributes.Count);            
            foreach (AttributeUtils.Attribute a in attributes)
            {
                dgAttributes.Rows[i].Cells[0].Value = a.name;
                dgAttributes.Rows[i].Cells[1].Value = a.required;                
                dgAttributes.Rows[i].Cells[2].Value = a.value;                
                i++;
            }
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            readAttributes();
            loginDialog = new VKLoginDialog();
            loginDialog.Login(this.permissions);
        }

        private void PrintAttributes()
        {
            string text = "";
            foreach (AttributeUtils.Attribute a in attributes)
            {
                text += a.name + "=" + a.required.ToString() + "\n";
            }

            this.ShowInformation(text);            
        }

        private void readAttributes()
        {
            if (attributes == null ||
                attributes.Count == 0)
            {
                return;
            }

            HashSet<String> permissions = new HashSet<string>();
            HashSet<String> fields = new HashSet<string>();

            foreach (DataGridViewRow row in dgAttributes.Rows)
            {
                int i = attributes.FindIndex(x => x.name.Equals(row.Cells[0].Value.ToString()));

                if (i >= 0)
                {
                    if(attributes[i].required != (Boolean)row.Cells[1].Value)
                    {
                        AttributeUtils.Attribute a = attributes[i];
                        a.required = (Boolean)row.Cells[1].Value;
                        attributes[i] = a;
                    }

                    if(attributes[i].required) 
                    {
                        // if required, add permission 
                        permissions.Add(attributes[i].permission);
                        fields.Add(attributes[i].value);
                    }
                }
            }
            
            // build permission string from permissions
            StringBuilder sb = new StringBuilder();
            foreach (String p in permissions)
            {
                if (sb.Length > 0)
                {
                    sb.Append(',');
                } 
                
                sb.Append(p);
            }

            this.permissions = sb.ToString();

            // build field string from fields
            sb.Length = 0;

            foreach (String f in fields)
            {
                if (sb.Length > 0)
                {
                    sb.Append(',');
                }

                sb.Append(f);
            }

            this.fields = sb.ToString();
        
        }

        private void chkSelectAll_CheckedChanged(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgAttributes.Rows)
            {
                row.Cells[1].Value = ((CheckBox)sender).Checked;
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (loginDialog == null)
                loginDialog = new VKLoginDialog();

            loginDialog.Logout();
        }

        private void chkIncludeMe_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void VKDialog_Load(object sender, EventArgs e)
        {
            dgAttributes.Columns[1].Width =
            TextRenderer.MeasureText(dgAttributes.Columns[1].HeaderText,
             dgAttributes.Columns[1].HeaderCell.Style.Font).Width + 25;
            //Get the column header cell bounds

            Rectangle rect =
                this.dgAttributes.GetCellDisplayRectangle(1, -1, true);

            //Change the location of the CheckBox to make it stay on the header

            chkSelectAll.Location =
                new Point(rect.Location.X + rect.Width - 20,
                    rect.Location.Y + Math.Abs((rect.Height - chkSelectAll.Height) / 2));

            chkSelectAll.CheckedChanged += new EventHandler(chkSelectAll_CheckedChanged);

            //Add the CheckBox into the DataGridView
            this.dgAttributes.Controls.Add(chkSelectAll);
        }    
    }
}
