using System;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

namespace rcsir.net.ok.importer.Dialogs
{
    public partial class OKLoginDialog : Form
    {
        private readonly String auth_url = "https://oauth.vk.com/authorize";
        private readonly String client_id = "3838634"; // application id
        private readonly String scope = "friends"; // permissions
        private readonly String redirect_url = "https://oauth.vk.com/blank.html"; // URL where access token will be passed to
        private readonly String display = "page"; // authorization windows appearence: page, popup, touch, wap
        // private readonly String v = ""; // API version
        private readonly String response_type = "token"; // Response type

        private String _authToken;
        public String authToken { get { return this._authToken; } set { this._authToken = value;  } }
        private String _userId;
        public String userId { get { return this._userId; } set { this._userId = value; } }
        private long _expiresIn;
        public long expiresIn { get { return this._expiresIn; } set { this._expiresIn = value; } }

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
            sb.Append("redirect_uri=").Append(redirect_url).Append('&');
            sb.Append("display=").Append(display).Append('&');
            sb.Append("response_type=").Append(response_type);

            String navigateUri = sb.ToString();
            Debug.WriteLine("Navigate uri = " + navigateUri);
            webBrowserLogin.Navigate(navigateUri);

            this.ShowDialog();
        }

        private void webBrowserLogin_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            Debug.WriteLine("DocumentCompleted");

            String stringUrl = webBrowserLogin.Url.ToString();
            Debug.WriteLine(stringUrl);

            // Good response example
            // https://oauth.vk.com/blank.html#access_token=20660ffedf0d1d48533bee9b0931b2f0649b90c4a6592810de9b4961e30866910fd2cf2c7fb0a67878e7b&expires_in=86400&user_id=2927314

            if (stringUrl.StartsWith(redirect_url))
            {

                String[] tokens = System.Text.RegularExpressions.Regex.Split(stringUrl, "[=&#]");
                foreach (String s in tokens)
                {
                    Debug.WriteLine("Token = " + s);
                }

                if (tokens.Length == 7)
                {
                    this.authToken = tokens[2];
                    this.expiresIn = Convert.ToInt64(tokens[4]);
                    this.userId = tokens[6];
                }
            }
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
                    System.IO.File.Delete(file.FullName);
                }
                catch (System.Exception e)
                {
                    MessageBox.Show(e.Message);
                }

            }
        }
    }
}
