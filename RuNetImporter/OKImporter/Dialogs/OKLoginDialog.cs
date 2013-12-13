using System;
using System.Net;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using rcsir.net.ok.importer.GraphDataProvider;

namespace rcsir.net.ok.importer.Dialogs
{
    public partial class OKLoginDialog : Form
    {
        private readonly String auth_url = "http://www.odnoklassniki.ru/oauth/authorize";
        private readonly String client_id = "201872896"; // application id
        private readonly String scope = "(SET_STATUS;VALUABLE_ACCESS;)"; // permissions
        private readonly String display = "page"; // authorization windows appearence: page, popup, touch, wap
        // private readonly String v = ""; // API version
        private readonly String response_type = "code"; // Response type

        private long _expiresIn;
        public long expiresIn { get { return _expiresIn; } set { _expiresIn = value; } }

        private OKRestClient _okRestClient = new OKRestClient();
        public OKRestClient okRestClient { get { return _okRestClient; } }

        public OKLoginDialog()
        {
            InitializeComponent();
            deleteCookies();
        }

        public void Login()
        {
            Debug.WriteLine("Navigate");

            StringBuilder sb = new StringBuilder(auth_url);
            sb.Append('?');
            sb.Append("client_id=").Append(client_id).Append('&');
            sb.Append("scope=").Append(scope).Append('&');
            sb.Append("redirect_uri=").Append(OKRestClient.redirect_url).Append('&');
            sb.Append("display=").Append(display).Append('&');
            sb.Append("response_type=").Append(response_type);

            String navigateUri = sb.ToString();
            Debug.WriteLine("Navigate uri = " + navigateUri);
            webBrowserLogin.Navigate(navigateUri);

            ShowDialog();
        }

        private void webBrowserLogin_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            Debug.WriteLine("DocumentCompleted");

            String stringUrl = webBrowserLogin.Url.ToString();
            Debug.WriteLine(stringUrl);

            // Good response example
            // https://oauth.vk.com/blank.html#access_token=20660ffedf0d1d48533bee9b0931b2f0649b90c4a6592810de9b4961e30866910fd2cf2c7fb0a67878e7b&expires_in=86400&user_id=2927314

            if (stringUrl.StartsWith(OKRestClient.redirect_url))
            {
                String code = GetValue(stringUrl);
//                DisableComponents(fcbDialog);
                Close();
                okRestClient.GetAccessToken(code);
            }
//              String[] tokens = System.Text.RegularExpressions.Regex.Split(stringUrl, "[=&#]");
        }

        // Utils
        private void deleteCookies()
        {
            DirectoryInfo folder = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.Cookies));
            //Get the list file in temporary
            FileInfo[] files = folder.GetFiles();
            foreach (FileInfo file in files)
            {
                try
                {
                    File.Delete(file.FullName);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }

            }
        }

        private static string GetValue(string stringUrl)
        {
            int index = stringUrl.IndexOf("=");
            int index2 = stringUrl.Length;
            return stringUrl.Substring(index + 1, index2 - index - 1);
        }

    }
}
