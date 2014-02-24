using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using rcsir.net.vk.importer.Dialogs;
using rcsir.net.vk.importer.api;

namespace VKGroups
{
    public partial class VKGroupsForm : Form
    {
        private static readonly int ITEMS_PER_REQUEST = 1000;
        private long currentOffset;
        private long totalCount;

        private VKLoginDialog vkLoginDialog;
        private VKRestApi vkRestApi;
        private String userId;
        private String authToken;
        private long expiresAt;
        private static AutoResetEvent readyEvent = new AutoResetEvent(false);
        private volatile bool run;

        public VKGroupsForm()
        {
            InitializeComponent();
        }

        private void AuthorizeButton_Click(object sender, EventArgs e)
        {
            bool reLogin = false; // TODO: if true - will delete cookies and relogin, use false for dev.
            vkLoginDialog.Login("friends", reLogin); // default permission - friends
        }

        private void UserLogin(object loginDialog, UserLoginEventArgs loginArgs)
        {
            Debug.WriteLine("User Logged In: " + loginArgs.ToString());

            this.userId = loginArgs.userId;
            this.authToken = loginArgs.authToken;
            this.expiresAt = loginArgs.expiersIn; // todo: calc expiration time

            this.userIdTextBox.Clear();
            this.userIdTextBox.Text = "Authorized " + loginArgs.userId;
            this.ActivateControls();
        }

        private void ActivateControls()
        {
            bool isAuthorized = (this.userId != null && this.userId.Length > 0);

            if (isAuthorized)
            {
                // enable user controls
                //this.FindUsersButton.Enabled = true;
                //this.CancelButton.Enabled = true;
            }
            else
            {
                // disable user controls
                //this.FindUsersButton.Enabled = false;
                //this.CancelButton.Enabled = false;
            }
        }

    }
}
