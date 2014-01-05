using System.Text;
using Smrf.AppLib;

namespace rcsir.net.ok.importer.Storages
{
    class RequestParametersStorage
    {
        private const string clientSecret = "EDDB1A6D680BDFF8E49A179C";
        private const string clientOpen = "CBAHFJANABABABABA";
        private const string clientId = "201872896"; // application id
        private const string scope = "(SET_STATUS;VALUABLE_ACCESS;)"; // permissions
        private const string display = "page"; // authorization windows appearence: page, popup, touch, wap
        private const string response_type = "code"; // Response type
        private const string authUrl = "http://www.odnoklassniki.ru/oauth/authorize";

        internal const string StartUrl = "http://www.odnoklassniki.ru/";
        
        internal readonly string RedirectUrl = "http://rcsoc.spbu.ru/";
        internal readonly string TokenUrl = "http://api.odnoklassniki.ru/oauth/token.do";
        internal readonly string ApiUrl = "http://api.odnoklassniki.ru/fb.do";

        internal readonly string AllUserFields = "uid,name,first_name,last_name,age,gender,locale,current_status,pic_1,location,birthday,current_location,last_online,registered_date,url_profile,online,allows_anonym_access";
        internal readonly string HiddenFields = "uid,name";

        private string postPrefix { get { return "application_key=" + clientOpen + "&access_token=" + authToken + "&"; } }
        private string sigSecret { get { return StringUtil.GetMd5Hash(string.Format("{0}{1}", authToken, clientSecret)); } }

        private string refreshToken;
        private string authToken;

        private string code;

        internal string Code { set { code = value; } }

        internal string AuthUri
        {
            get
            {
                StringBuilder sb = new StringBuilder(authUrl);
                sb.Append('?');
                sb.Append("client_id=").Append(clientId).Append('&');
                sb.Append("scope=").Append(scope).Append('&');
                sb.Append("redirect_uri=").Append(RedirectUrl).Append('&');
                sb.Append("display=").Append(display).Append('&');
                sb.Append("response_type=").Append(response_type);
                return sb.ToString();
            }
        }
       
        internal string TokenParameters
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("client_id=").Append(clientId).Append('&');
                sb.Append("grant_type=authorization_code").Append('&');
                sb.Append("client_secret=").Append(clientSecret).Append('&');
                sb.Append("code=").Append(code).Append('&');
                sb.Append("redirect_uri=").Append(RedirectUrl).Append('&');
                sb.Append("type=user_agent");
                return sb.ToString();
            }
        }

        internal string MakePostedData(string requestString)
        {
            var sig = StringUtil.GetMd5Hash(string.Format("{0}{1}", "application_key=" + clientOpen + requestString.Replace("&", ""), sigSecret));
            return postPrefix + requestString + "&sig=" + sig;
        }

        internal void UpdateAuthTokens(object auth_token, object refresh_token)
        {
            authToken = auth_token.ToString();
            refreshToken = refresh_token.ToString();
        }

    }
}
