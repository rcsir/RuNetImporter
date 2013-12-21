using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace rcsir.net.ok.importer.Api
{
    class Authorization
    {
        internal static string ClientId { get { return "201872896"; } } // application id
        internal static string RedirectUrl { get { return "http://rcsoc.spbu.ru/"; } }

        private readonly String auth_url = "http://www.odnoklassniki.ru/oauth/authorize";
        private readonly String scope = "(SET_STATUS;VALUABLE_ACCESS;)"; // permissions
        private readonly String display = "page"; // authorization windows appearence: page, popup, touch, wap
        private readonly String response_type = "code"; // Response type

        public string AuthUri
        {
            get
            {
                StringBuilder sb = new StringBuilder(auth_url);
                sb.Append('?');
                sb.Append("client_id=").Append(ClientId).Append('&');
                sb.Append("scope=").Append(scope).Append('&');
                sb.Append("redirect_uri=").Append(RedirectUrl).Append('&');
                sb.Append("display=").Append(display).Append('&');
                sb.Append("response_type=").Append(response_type);
                return sb.ToString();
            }
        }

/*
        private readonly String v = ""; // API version     
        private long _expiresIn;
        public long expiresIn { get { return _expiresIn; } set { _expiresIn = value; } }
*/
        public void deleteCookies()
        {
            DirectoryInfo folder = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.Cookies));
            FileInfo[] files = folder.GetFiles();
            foreach (FileInfo file in files) {
                try {
                    File.Delete(file.FullName);
                } catch (Exception e) {
                    MessageBox.Show(e.Message);
                }
            }
        }

        public string GetCode(string stringUrl)
        {
            if (!stringUrl.StartsWith(RedirectUrl))
                return null;

            int index = stringUrl.IndexOf("=");
            int index2 = stringUrl.Length;
            return stringUrl.Substring(index + 1, index2 - index - 1);
        }
    }
}
