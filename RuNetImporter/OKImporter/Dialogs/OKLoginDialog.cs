using System.Diagnostics;
using System.Windows.Forms;
using rcsir.net.ok.importer.Api;
using rcsir.net.ok.importer.GraphDataProvider;

namespace rcsir.net.ok.importer.Dialogs
{
    public partial class OKLoginDialog : Form
    {
        private Authorization auth = new Authorization();

        private OKRestClient okRestClient = new OKRestClient();
        public OKRestClient OkRestClient { get { return okRestClient; } }

        public OKLoginDialog()
        {
            InitializeComponent();
            auth.deleteCookies();
        }

        public void Login()
        {
            Debug.WriteLine("Navigate");
            webBrowserLogin.Navigate(auth.AuthUri);
            ShowDialog();
        }

        private void webBrowserLogin_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            Debug.WriteLine("DocumentCompleted");
            string stringUrl = webBrowserLogin.Url.ToString();
            Debug.WriteLine(stringUrl);
            string code = auth.GetCode(stringUrl);
            if (code != null) {
//                DisableComponents(fcbDialog);
                Close();
                OkRestClient.GetAccessToken(code);
            }
        }
    }
}
