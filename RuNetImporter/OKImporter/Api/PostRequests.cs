using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using Smrf.AppLib;

namespace rcsir.net.ok.importer.Api
{
    public class PostRequests
    {
        public const string client_secret = "EDDB1A6D680BDFF8E49A179C";
        private const string client_open = "CBAHFJANABABABABA";

        private const string token_Url = "http://api.odnoklassniki.ru/oauth/token.do";
        private const string apiUrl = "http://api.odnoklassniki.ru/fb.do";

        private static string authToken;
        public static string AuthToken { set { authToken = value; } }

        private static string postPrefix { get { return "application_key=" + client_open + "&access_token=" + authToken + "&"; } }
        private static string sigSecret { get { return StringUtil.GetMd5Hash(string.Format("{0}{1}", authToken, client_secret)); } }

        public static string MakeApiRequest(string requestString)
        {
            var sig = StringUtil.GetMd5Hash(string.Format("{0}{1}", "application_key=" + client_open + requestString.Replace("&", ""), sigSecret));
            string postedData = postPrefix + requestString + "&sig=" + sig;
            return MakeRequest(postedData);
        }

        public static string MakeRequest(string postedData, bool isApiRequest = true)
        {
            var response = PostMethod(postedData, isApiRequest ? apiUrl : token_Url);
            if (response == null)
                return null;

            var strreader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            string responseToString = strreader.ReadToEnd();
            return responseToString; //JObject.Parse(responseToString); // JObject.CreateFromString(responseToString);
        }

        private static HttpWebResponse PostMethod(string postedData, string postUrl)
        {
            UTF8Encoding encoding = new UTF8Encoding();
            var bytes = encoding.GetBytes(postedData);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(postUrl);
            request.Method = "POST";
            request.Credentials = CredentialCache.DefaultCredentials;
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = bytes.Length;

            using (var newStream = request.GetRequestStream()) {
                newStream.Write(bytes, 0, bytes.Length);
                newStream.Close();
            }
            return (HttpWebResponse)request.GetResponse();
        }

        private static void HandleWebException(WebException Ex)
        {
            if (Ex.Status == WebExceptionStatus.ProtocolError) {
                int StatusCode = (int)((HttpWebResponse)Ex.Response).StatusCode;
                Stream ResponseStream = null;
                ResponseStream = ((HttpWebResponse)Ex.Response).GetResponseStream();
                string responseText = (new StreamReader(ResponseStream)).ReadToEnd();
                if (StatusCode == 500) {
                    Debug.WriteLine("Error 500 - " + responseText);
                } else {
                    // Do Something for other status codes
                    Debug.WriteLine("Error " + StatusCode);
                }
            } else {
                throw (Ex); // Or check for other WebExceptionStatus
            }
        }
    }
}
