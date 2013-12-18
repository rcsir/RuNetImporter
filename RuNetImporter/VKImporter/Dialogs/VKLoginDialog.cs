using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

namespace rcsir.net.vk.importer.Dialogs
{
    // Login event arguments
    public class UserLoginEventArgs : EventArgs
    {
        public UserLoginEventArgs(String authToken, String userId, long expiersIn)
        {
            this.authToken = authToken;
            this.userId = userId;
            this.expiersIn = expiersIn;
        }

        public readonly String authToken;
        public readonly String userId;
        public readonly long expiersIn;
    }

    public partial class VKLoginDialog : Form
    {
        private readonly String auth_url = Secret.auth_url;
        private readonly String client_id = Secret.client_id; // application id
        private readonly String scope = "friends"; // permissions
        private readonly String redirect_url = Secret.redirect_url; // URL where access token will be passed to
        private readonly String display = "popup"; // authorization windows appearence: page, popup, touch, wap
        // private readonly String v = ""; // API version
        private readonly String response_type = "token"; // Response type

        private String _authToken;
        public String authToken { get { return this._authToken; } set { this._authToken = value;  } }
        private String _userId;
        public String userId { get { return this._userId; } set { this._userId = value; } }
        private long _expiresIn;
        public long expiresIn { get { return this._expiresIn; } set { this._expiresIn = value; } }

        // define OnLogin delegate
        public delegate void UserLoginHandler 
        (
            object VKLoginDialog,
            UserLoginEventArgs userLogin
        );

        public UserLoginHandler OnUserLogin;

        public VKLoginDialog()
        {
            InitializeComponent();
            // TODO: enable when not testing
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
            Debug.WriteLine("Navigate uri: " + navigateUri);
            webBrowserLogin.Navigate(navigateUri);

            this.ShowDialog();
        }

        public void Logout()
        {
            webBrowserLogin.Navigate("http://vk.com/");
            this.ShowDialog();

        }

        private void webBrowserLogin_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            Debug.WriteLine("DocumentCompleted");
            Size webSize = webBrowserLogin.Document.Window.Size;
            this.Size = webSize;

            String stringUrl = webBrowserLogin.Url.ToString();
            Debug.WriteLine(stringUrl);

            // Good response example
            // https://oauth.vk.com/blank.html#access_token=20660ffedf0d1d48533bee9b0931b2f0649b90c4a6592810de9b4961e30866910fd2cf2c7fb0a67878e7b&expires_in=86400&user_id=2927314

            if (stringUrl.StartsWith(redirect_url))
            {
                String[] tokens = System.Text.RegularExpressions.Regex.Split(stringUrl, "[=&#]");
                for(int i = 0; i < tokens.Length; ++i)
                {
                    Debug.WriteLine("Token = " + tokens[i]);
                    switch (tokens[i])
                    {
                        case "access_token":
                            if (i < tokens.Length)
                            {
                                this.authToken = tokens[++i];
                            }
                        break;
                        case "expires_in":
                            if (i < tokens.Length)
                            {
                                this.expiresIn = Convert.ToInt64(tokens[++i]);
                            }
                        break;
                        case "user_id":
                            if (i < tokens.Length)
                            {
                                this.userId = tokens[++i];
                            }
                        break;
                    }
                }

                // notify listeners
                if (OnUserLogin != null)
                {
                    UserLoginEventArgs args = new UserLoginEventArgs(this.authToken, this.userId, this.expiresIn);
                    OnUserLogin(this, args);
                }

                this.Close(); // close the dialog
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

        private void webBrowserLogin_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
        }
    }
}
