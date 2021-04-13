using System;
using System.IO;
using System.Net;
using System.Text;
using rcsir.net.common.Utilities;
using rcsir.net.ok.importer.Storages;

namespace rcsir.net.ok.importer.Api
{
    class PostRequests
    {
        private RequestParametersStorage parametersStorage;

        internal event EventHandler<Events.ErrorEventArgs> OnError;

        internal PostRequests(RequestParametersStorage storage)
        {
            parametersStorage = storage;
        }

        internal JSONObject MakeApiRequest(string requestString)
        {
            string postedData = parametersStorage.MakePostedData(requestString);
            return MakeRequest(postedData);
        }

        internal JSONObject MakeRequest(string postedData, bool isApiRequest = true)
        {
            var response = PostMethod(postedData, isApiRequest ? parametersStorage.ApiUrl : parametersStorage.TokenUrl);
            if (response == null)
                return null;

            var strreader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            string responseToString = strreader.ReadToEnd();
            return JSONObject.CreateFromString(responseToString);
        }

        protected virtual void DispatchErrorEvent(string type, string description)
        {
            var evnt = new Events.ErrorEventArgs(type, description);
            var handler = OnError;
            if (handler != null)
                handler(this, evnt);
        }

        private HttpWebResponse PostMethod(string postedData, string postUrl)
        {
            UTF8Encoding encoding = new UTF8Encoding();
            var bytes = encoding.GetBytes(postedData);
            try {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(postUrl);
                request.Method = "POST";
                request.Credentials = CredentialCache.DefaultCredentials;
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = bytes.Length;

                return handleValidResponse(request, bytes);
            } catch (WebException e) {
                handleWebException(e);
                return null;
            }
        }

        private HttpWebResponse handleValidResponse(HttpWebRequest request, byte[] bytes)
        {
            using (var newStream = request.GetRequestStream()) {
                newStream.Write(bytes, 0, bytes.Length);
                newStream.Close();
            }
            return (HttpWebResponse) request.GetResponse();
        }

        private void handleWebException(WebException exception)
        {
            try {
                int StatusCode = (int)((HttpWebResponse)exception.Response).StatusCode;
                Stream ResponseStream = null;
                ResponseStream = ((HttpWebResponse)exception.Response).GetResponseStream();
                string responseText = (new StreamReader(ResponseStream)).ReadToEnd();
                if (exception.Status == WebExceptionStatus.ProtocolError) {
                    DispatchErrorEvent("Server Error: " + StatusCode, responseText);
/*                    if (StatusCode == 500) {
                        Debug.WriteLine("Error 500 - " + responseText);
                    } else {
                        Debug.WriteLine("Error " + StatusCode);
                    }*/
                } else
                    DispatchErrorEvent("Client Error: " + StatusCode, responseText);
            } catch (Exception e) {
                DispatchErrorEvent("Unknown Error", e.ToString());
            }
        }
    }
}
