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
        public OnDataEventArgs(VKFunction function, JObject data)
        {
            this.function = function;
            this.data = data;
        }

        public readonly VKFunction function;
        public readonly JObject data;
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
        }

        public readonly String userId;
        public readonly String authToken;
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

        // remove
        private List<string> friendIds = new List<string>();
        private VertexCollection vertices = new VertexCollection();
        private EdgeCollection edges = new EdgeCollection();

        public VKRestApi()
        {
        }

        public VertexCollection GetVertices()
        {
            return this.vertices;
        }

        public EdgeCollection GetEdges()
        {
            return this.edges;
        }

        // VK API
        public void callVKFunction(VKFunction function, VKRestContext context)
        {
            switch (function)
            {
                case VKFunction.LoadUserInfo:
                    LoadUserInfo(context.userId, context.authToken);
                    break;
                case VKFunction.LoadFriends:
                    break;
                case VKFunction.GetMutual:
                    break;
                default:
                    break;
            }
        }


        private void LoadUserInfo(String userId, String authToken)
        {
            //https://api.vk.com/method/getProfiles?uid=66748&access_token=533bacf01e11f55b536a565b57531ac114461ae8736d6506a3;

            StringBuilder sb = new StringBuilder(api_url);
            sb.Append("/method/getProfiles");
            sb.Append('?');
            sb.Append("uid=").Append(userId).Append('&');
            sb.Append("access_token=").Append(authToken);

            makeRestCall(VKFunction.LoadUserInfo, sb.ToString());
        }

        public void LoadFriends(String userId)
        {
            StringBuilder sb = new StringBuilder(api_url);
            sb.Append("/method/friends.get");
            sb.Append('?');
            sb.Append("user_id=").Append(userId).Append('&');
            sb.Append("fields=").Append("uid,first_name,last_name,nickname,sex,bdate,city,country,education");

                // Create URI 
                Uri address = new Uri(sb.ToString());

                Debug.WriteLine("getFriends: " + address.ToString());

                // Create the web request 
                HttpWebRequest request = WebRequest.Create(address) as HttpWebRequest;

                // Set type to Get 
                request.Method = "Get";
                request.ContentType = "application/json; charset=utf-8";
                request.Accept = "application/json"; // Determines the response type as XML or JSON etc

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

                        if (o["response"] != null)
                        {
                            if (o["response"].Count() > 0)
                            {

                                for (int i = 0; i < o["response"].Count(); ++i)
                                {
                                    JObject friend = o["response"][i].ToObject<JObject>();
                                    // uid, first_name, last_name, nickname, sex, bdate, city, country, timezone
                                    Console.WriteLine(i.ToString() + ") friend: " + friend.ToString());

                                    string uid = friend["uid"].ToString();
                                    // add user id to the friends list
                                    this.friendIds.Add(uid);

                                    // add friend vertex
                                    AttributesDictionary<String> attributes = createAttributes(friend);

                                    this.vertices.Add(new Vertex(uid,
                                        friend["first_name"].ToString() + " " + friend["last_name"].ToString(),
                                        "Friend", attributes));
                                }
                            }
                        }
                        else if (o["error"] != null)
                        {
                            Debug.WriteLine("Error " + o["error"].ToString());
                        }
                    }

            }
        }

        public void GetMutual(String userId, String authToken)
        {
            StringBuilder mainsb = new StringBuilder(api_url);
            mainsb.Append("/method/friends.getMutual");
            mainsb.Append('?');
            mainsb.Append("source_uid=").Append(userId).Append('&');
            mainsb.Append("access_token=").Append(authToken).Append('&');

                foreach (string targetId in this.friendIds)
                {
                    StringBuilder sb = new StringBuilder(mainsb.ToString());

                    // Append target friend ids
                    sb.Append("target_uid=").Append(targetId);

                    // Create URI 
                    Uri address = new Uri(sb.ToString());

                    Debug.WriteLine("getMutual: " + address.ToString());

                    // Create the web request 
                    HttpWebRequest request = WebRequest.Create(address) as HttpWebRequest;

                    // Set type to Get 
                    request.Method = "Get";
                    request.ContentType = "application/json; charset=utf-8";
                    request.Accept = "application/json"; // Determines the response type as XML or JSON etc

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
                            if (o["response"] != null)
                            {
                                Debug.WriteLine("Mutual: " + o.ToString());

                                if(o["response"].Count() > 0) 
                                {
                                    List<String> friendFriendsIds = new List<string>();

                                    for (int i = 0; i < o["response"].Count(); ++i)
                                    {
                                        String friendFriendsId = o["response"][i].ToString();

                                        CreateFriendsMutualEdge(targetId,
                                                                friendFriendsId,
                                                                this.edges,
                                                                this.vertices);
                                    }

                                }
                            }
                            else if (o["error"] != null)
                            {
                                Debug.WriteLine("Error " + o["error"].ToString());
                            }
                        }
                    }

                    // play nice, sleep for 1/3 sec to stay within 3 requests/second limit
                    Thread.Sleep(333);
                }
        }

        private void CreateFriendsMutualEdge(String friendId, String friendFriendsId, EdgeCollection edges, VertexCollection vertices)
        {
            Vertex friend = vertices.FirstOrDefault(x => x.ID == friendId);
            Vertex friendsFriend = vertices.FirstOrDefault(x => x.ID == friendFriendsId);

            if (friend != null && friendsFriend != null)
            {
                 edges.Add(new Edge(friend, friendsFriend, "", "Friend", "", 1));
            }
        }


        private AttributesDictionary<String> createAttributes(JObject obj) 
        {
            AttributesDictionary<String> attributes = new AttributesDictionary<String>();
            List<AttributeUtils.Attribute> keys = new List<AttributeUtils.Attribute>(attributes.Keys);
            foreach (AttributeUtils.Attribute key in keys)
            {
                String name = key.value;

                if (obj[name] != null)
                {
                    // assert it is null?
                    String value = obj[name].ToString(); 
                    attributes[key] =  value;
                }
            }

            return attributes;
        }

        private void makeRestCall(VKFunction function, String uri)
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
                                OnDataEventArgs args = new OnDataEventArgs(function, o);
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
