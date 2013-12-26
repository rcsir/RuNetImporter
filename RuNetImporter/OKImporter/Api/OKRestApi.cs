using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;
using rcsir.net.ok.importer.Controllers;
using rcsir.net.ok.importer.NetworkAnalyzer;

namespace rcsir.net.ok.importer.Api
{
    // Define implemented OK rest API
    public enum OKFunction
    {
        GetAccessToken,
        LoadUserInfo,
        LoadFriends,
        GetAreGraph,
        GetMutualGraph,
        GenerateAreGraph,
        GenerateMutualGraph,
        GenerateGraph
    };

    // onData event arguments
    public class OnDataEventArgs : EventArgs
    {
        public OnDataEventArgs(OKFunction function, JObject data = null, String cookie = null)
        {
            this.function = function;
            this.data = data;
            this.cookie = cookie;
        }

        public readonly OKFunction function;
        public readonly JObject data;
        public readonly String cookie; // parameters from the request
    }

    // onError event arguments
    public class OnErrorEventArgs : EventArgs
    {
        public OnErrorEventArgs(OKFunction function, String error)
        {
            this.function = function;
            this.error = error;
        }

        public readonly OKFunction function;
        public readonly String error;
    }
/*
    public class OKRestContext 
    {
        public OKRestContext(String userId, String authToken)
        {
            this.userId = userId;
            this.authToken = authToken;
            this.parameters = null;
            this.cookie = null;
        }

        public OKRestContext(String userId, String authToken, String parameters)
        {
            this.userId = userId;
            this.authToken = authToken;
            this.parameters = parameters;
            this.cookie = null;
        }

        public OKRestContext(String userId, String authToken, String parameters, String cookie)
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

    public class OKRestApi
    {
        private readonly RequestController proxy = new RequestController();
        private readonly OKNetworkAnalyzer analyzer = new OKNetworkAnalyzer();
        private List<string> friendIds = new List<string>();
        public DataHandler OnData;
        public ErrorHandler OnError;
        // define OnData delegate
        public delegate void DataHandler(object OKRestApi, OnDataEventArgs onDataArgs);
       // define OnError delegate
        public delegate void ErrorHandler(object OKRestApi, OnErrorEventArgs onErrorArgs);

        // API usrl
        private readonly String api_url = "https://api.vk.com";

        // Request parameters
        public static readonly String GET_METHOD = "Get";
        public static readonly String CONTENT_TYPE = "application/json; charset=utf-8";
        public static readonly String CONTENT_ACCEPT = "application/json"; // Determines the response type as XML or JSON etc
        public static readonly String RESPONSE_BODY = "response";
        public static readonly String ERROR_BODY = "error";

        public OKRestApi()
        {
        }

        // OK API
        public void CallOKFunction(OKFunction function, OKRestContext context = null)
        {
            switch (function) {
                case OKFunction.GetAccessToken:
                    GetAccessToken();
                    break;
/*                case OKFunction.LoadUserInfo:
                    LoadUserInfo(context.userId, context.authToken);
                    break;#1#
                case OKFunction.LoadFriends:
                    LoadFriends();
                    break;
                case OKFunction.GetMutualGraph:
                    GetMutual(context.userId, context.authToken, context.parameters, context.cookie);
                    break;
                default:
                    break;
            }
        }

        private void GetAccessToken()
        {
 /*           // Valid response string, f.e.: "{\"token_type\":\"session\",\"refresh_token\":\"e19530afe4f7c094d20f966078e2d0a16896a5_561692396161_138785\",\"access_token\":\"63ipa.949evrsa14g4i039194d1f3bh3kd\"}"
            string responseString = proxy.GetToken();
            JObject o = JObject.Parse(responseString);
            PostRequests.AuthToken = o["access_token"].ToString();
            CreateEgoVertex();#1#
        }
/*
        private void LoadUserInfo(String userId, String authToken)
        {
            StringBuilder sb = new StringBuilder(api_url);
            sb.Append("/method/getProfiles");
            sb.Append('?');
            sb.Append("uid=").Append(userId).Append('&');
            sb.Append("access_token=").Append(authToken);

            makeRestCall(OKFunction.LoadUserInfo, sb.ToString());
        }
#1#
        private void LoadFriends()
        {
 /*           JArray friends = JArray.Parse(proxy.GetFriends()); // fid=160539089447&fid=561967133371&fid=561692396161&
            string friendUids = ""; // userId;
            foreach (var friend in friends)
            {
//                JObject friendDict = JObject.Parse(MakeRequest("method=friends.getMutualFriends&target_id=" + friend));  //  &source_id=160539089447
                friendUids += "," + friend;
                friendIds.Add(friend.ToString());
            }#1#
//            CreateFriendsVertices(friendUids);
        }

        private void GetMutual(String userId, String authToken, String parameters, String cookie)
        {
            StringBuilder sb = new StringBuilder(api_url);
            sb.Append("/method/friends.getMutual");
            sb.Append('?');
            sb.Append("source_uid=").Append(userId).Append('&');
            sb.Append("access_token=").Append(authToken).Append('&');
            sb.Append(parameters);

            makeRestCall(OKFunction.GetMutualGraph, sb.ToString(), cookie);
        }

        private void CreateEgoVertex()
        {
/*            JObject ego = JObject.Parse(proxy.GetEgoInfo());
            if (OnData != null) {
                OnDataEventArgs args = new OnDataEventArgs(OKFunction.LoadUserInfo, ego, null);
                OnData(this, args);
            }#1#
 //           analyzer.GetEgo();
/*            userId = ego["uid"].ToString();
            AttributesDictionary<String> attributes = createAttributes(ego);
            egoVertex = new Vertex(ego["uid"].ToString(), ego["name"].ToString(), "Ego", attributes);
            vertices.Add(egoVertex);#1#
        }

        private void makeRestCall(OKFunction function, String uri, String cookie = null)
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
