using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using Smrf.AppLib;
using rcsir.net.common.Network;

namespace rcsir.net.vk.importer.api
{
    // Define implemented VK rest API
    public enum VKFunction
    {
        LoadUserInfo,
        LoadFriends,
        GetMutual

    };

    // onData event arguments
    public class OnDataEventArgs : EventArgs
    {
        public OnDataEventArgs(VKFunction function, JObject data, String cookie)
        {
            this.function = function;
            this.data = data;
            this.cookie = cookie;
        }

        public readonly VKFunction function;
        public readonly JObject data;
        public readonly String cookie; // parameters from the request
    }

    // onError event arguments
    public class OnErrorEventArgs : EventArgs
    {
        public OnErrorEventArgs(VKFunction function, String error)
        {
            this.function = function;
            this.error = error;
        }

        public readonly VKFunction function;
        public readonly String error;
    }

    public class VKRestContext 
    {
        public VKRestContext(String userId, String authToken)
        {
            this.userId = userId;
            this.authToken = authToken;
            this.parameters = null;
            this.cookie = null;
        }

        public VKRestContext(String userId, String authToken, String parameters)
        {
            this.userId = userId;
            this.authToken = authToken;
            this.parameters = parameters;
            this.cookie = null;
        }

        public VKRestContext(String userId, String authToken, String parameters, String cookie)
        {
            this.userId = userId;
            this.authToken = authToken;
            this.parameters = parameters;
            this.cookie = cookie;
        }

        public readonly String userId;
        public readonly String authToken;

        public String parameters {get; set;}
        public String cookie { get; set; }
    }

    public class VKRestApi
    {

        // define OnData delegate
        public delegate void DataHandler
        (
            object VKRestApi,
            OnDataEventArgs onDataArgs
        );

        public DataHandler OnData;

        // define OnError delegate
        public delegate void ErrorHandler
        (
            object VKRestApi,
            OnErrorEventArgs onErrorArgs
        );

        public ErrorHandler OnError;

        // API usrl
        private readonly String api_url = "https://api.vk.com";

        // Request parameters
        public static readonly String GET_METHOD = "Get";
        public static readonly String CONTENT_TYPE = "application/json; charset=utf-8";
        public static readonly String CONTENT_ACCEPT = "application/json"; // Determines the response type as XML or JSON etc
        public static readonly String RESPONSE_BODY = "response";
        public static readonly String ERROR_BODY = "error";

        public VKRestApi()
        {
        }

        // VK API
        public void CallVKFunction(VKFunction function, VKRestContext context)
        {
            switch (function)
            {
                case VKFunction.LoadUserInfo:
                    LoadUserInfo(context.userId, context.authToken);
                    break;
                case VKFunction.LoadFriends:
                    LoadFriends(context.userId, context.parameters);
                    break;
                case VKFunction.GetMutual:
                    GetMutual(context.userId, context.authToken, context.parameters, context.cookie);
                    break;
                default:
                    break;
            }
        }


        private void LoadUserInfo(String userId, String authToken)
        {
            StringBuilder sb = new StringBuilder(api_url);
            sb.Append("/method/getProfiles");
            sb.Append('?');
            sb.Append("uid=").Append(userId).Append('&');
            sb.Append("access_token=").Append(authToken);

            makeRestCall(VKFunction.LoadUserInfo, sb.ToString());
        }

        private void LoadFriends(String userId, String parameters)
        {
            StringBuilder sb = new StringBuilder(api_url);
            sb.Append("/method/friends.get");
            sb.Append('?');
            sb.Append("user_id=").Append(userId).Append('&');
            sb.Append(parameters);

            makeRestCall(VKFunction.LoadFriends, sb.ToString());
            
        }

        private void GetMutual(String userId, String authToken, String parameters, String cookie)
        {
            StringBuilder sb = new StringBuilder(api_url);
            sb.Append("/method/friends.getMutual");
            sb.Append('?');
            sb.Append("source_uid=").Append(userId).Append('&');
            sb.Append("access_token=").Append(authToken).Append('&');
            sb.Append(parameters);

            makeRestCall(VKFunction.GetMutual, sb.ToString(), cookie);
        }

        private void makeRestCall(VKFunction function, String uri, String cookie = null)
        {
            try
            {
                // Create URI 
                Uri address = new Uri(uri);
                Debug.WriteLine("REST call: " + address.ToString());

                // Create the web request 
                HttpWebRequest request = WebRequest.Create(address) as HttpWebRequest;

                // Set type to Get 
                request.Method = GET_METHOD;
                request.ContentType = CONTENT_TYPE;
                request.Accept = CONTENT_ACCEPT;

                // Get response 
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    Stream ResponseStream = null;
                    ResponseStream = response.GetResponseStream();
                    int responseCode = (int)response.StatusCode;
                    if (responseCode < 300)
                    {
                        string responseBody = ((new StreamReader(ResponseStream)).ReadToEnd());
                        string contentType = response.ContentType;
                        JObject o = JObject.Parse(responseBody);
                        if (o[RESPONSE_BODY] != null)
                        {
                            Debug.WriteLine("REST response: " + o[RESPONSE_BODY].ToString());
                            // OK - notify listeners
                            if (OnData != null)
                            {
                                OnDataEventArgs args = new OnDataEventArgs(function, o, cookie);
                                OnData(this, args);
                            }
                        }
                        else if (o[ERROR_BODY] != null)
                        {
                            Debug.WriteLine("REST error: " + o[ERROR_BODY].ToString());

                            // Error - notify listeners
                            if (OnError != null)
                            {
                                OnErrorEventArgs args = new OnErrorEventArgs(function, o[ERROR_BODY].ToString());
                                OnError(this, args);
                            }
                        }
                    }
                }
            }
            catch (WebException Ex)
            {
                handleWebException(function, Ex);
            }

        }

        private void handleWebException(VKFunction function, WebException Ex)
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
    }
}
