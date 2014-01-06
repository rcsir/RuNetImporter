using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using rcsir.net.ok.importer.Storages;

namespace rcsir.net.ok.importer.Api
{
    class PostRequests
    {
        private RequestParametersStorage parametersStorage;

        internal PostRequests(RequestParametersStorage storage)
        {
            parametersStorage = storage;
        }

        internal string MakeApiRequest(string requestString)
        {
            string postedData = parametersStorage.MakePostedData(requestString);
            return MakeRequest(postedData);
        }

        internal string MakeRequest(string postedData, bool isApiRequest = true)
        {
            var response = PostMethod(postedData, isApiRequest ? parametersStorage.ApiUrl : parametersStorage.TokenUrl);
            if (response == null)
                return null;

            var strreader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            string responseToString = strreader.ReadToEnd();
            return responseToString;
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

        private void HandleWebException(WebException Ex)
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

        /*
        private void handleWebException(OKFunction function, WebException Ex)
        {
            if (Ex.Status == WebExceptionStatus.ProtocolError)
            {
                int StatusCode = (int)((HttpWebResponse)Ex.Response).StatusCode;
                Stream ResponseStream = null;
                ResponseStream = ((HttpWebResponse)Ex.Response).GetResponseStream();
                string responseText = (new StreamReader(ResponseStream)).ReadToEnd();

                if (StatusCode == 500)
                {
                    Debug.WriteLine("Error 500 - " + responseText);
                }
                else
                {
                    // Do Something for other status codes
                    Debug.WriteLine("Error " + StatusCode);
                }

                // Error - notify listeners
                if (OnError != null)
                {
                    StringBuilder errorsb = new StringBuilder();
                    errorsb.Append("StatusCode: ").Append(StatusCode).Append(',');
                    errorsb.Append("Error: \'").Append(responseText).Append("\'");
                    OnErrorEventArgs args = new OnErrorEventArgs(function, errorsb.ToString());
                    OnError(this, args);
                }
            }
            else
            {
                throw (Ex); // Or check for other WebExceptionStatus
            }
        }
    }*/
    }
}
