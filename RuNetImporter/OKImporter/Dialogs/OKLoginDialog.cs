using System.Diagnostics;
using System.Windows.Forms;
using rcsir.net.ok.importer.Api;
using rcsir.net.ok.importer.Controllers;

namespace rcsir.net.ok.importer.Dialogs
{
    public partial class OKLoginDialog : Form
    {
        private Authorization auth = new Authorization();
        private readonly OkController controller;
 
        public OKLoginDialog()
        {
            InitializeComponent();
            auth.deleteCookies();
        }

        public OKLoginDialog(OkController controller)
        {
            InitializeComponent();
            auth.deleteCookies();
            this.controller = controller;
        }

        public void Login()
        {
            Debug.WriteLine("Navigate");
            webBrowserLogin.Navigate(auth.AuthUri);
            ShowDialog();
        }

        public void Logout()
        {
            webBrowserLogin.Navigate("http://www.odnoklassniki.ru/");
            ShowDialog();
        }

        private void webBrowserLogin_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            Debug.WriteLine("DocumentCompleted");
            string stringUrl = webBrowserLogin.Url.ToString();
            Debug.WriteLine(stringUrl);
            string code = auth.GetCode(stringUrl);
            if (code == null)
                return;
//          DisableComponents(fcbDialog);
            Close();
            controller.CallOkFunction(OKFunction.GetAccessToken);
        }
    }
}
