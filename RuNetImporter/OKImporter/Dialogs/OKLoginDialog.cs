using System;
using System.Diagnostics;
using System.Windows.Forms;
using rcsir.net.ok.importer.Events;
using rcsir.net.ok.importer.Storages;

namespace rcsir.net.ok.importer.Dialogs
{
    public partial class OKLoginDialog : Form
    {
        private string authUri;

        public string AuthUri { set { authUri = value; } }

        public event EventHandler<CommandEventArgs> CommandEventHandler;

        public OKLoginDialog()
        {
            InitializeComponent();
        }

        public void Login()
        {
            Debug.WriteLine("Navigate");
            webBrowserLogin.Navigate(authUri);
            ShowDialog();
        }

        public void Logout()
        {
            webBrowserLogin.Navigate(RequestParametersStorage.StartUrl);
            ShowDialog();
        }

        private void webBrowserLogin_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            Debug.WriteLine("DocumentCompleted");
            string stringUrl = webBrowserLogin.Url.ToString();
            Debug.WriteLine(stringUrl);
            var evnt = new CommandEventArgs(CommandEventArgs.Commands.GetAccessToken, stringUrl);
            DispatchEvent(evnt);
//          DisableComponents(fcbDialog);
        }

        protected virtual void DispatchEvent(CommandEventArgs e)
        {
            EventHandler<CommandEventArgs> handler = CommandEventHandler;
            if (handler != null)
                handler(this, e);
        }
    }
}
