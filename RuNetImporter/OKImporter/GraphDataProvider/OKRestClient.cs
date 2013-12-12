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

namespace rcsir.net.ok.importer.GraphDataProvider
{
    public class OKRestClient
    {
        private readonly String api_url = "https://api.vk.com";
        public static string redirect_url = "http://rcsoc.spbu.ru/";

        private static string client_secret = "EDDB1A6D680BDFF8E49A179C";
        private static string client_open = "CBAHFJANABABABABA";
        private static string apiUrl = "http://api.odnoklassniki.ru/fb.do";
        private const string token_Url = "http://api.odnoklassniki.ru/oauth/token.do";

        private string postPrefix { get { return "application_key=" + client_open + "&access_token=" + authToken + "&"; } }
        private string sigSecret { get { return GetMD5Hash(string.Format("{0}{1}", authToken, client_secret)); } }

        private Vertex egoVertex;
        private List<string> friendIds = new List<string>();
        private VertexCollection vertices = new VertexCollection();
        private EdgeCollection edges = new EdgeCollection();

        private String _authToken;
        public String authToken { get { return this._authToken; } set { this._authToken = value; } }
        private String _userId;
        public String userId { get { return this._userId; } set { this._userId = value; } }

        public OKRestClient()
        {
        }


        public Vertex GetEgo()
        {
            return this.egoVertex;
        }

        public VertexCollection GetVertices()
        {
            return this.vertices;
        }

        public EdgeCollection GetEdges()
        {
            return this.edges;
        }

        // OK API

        public void GetAccessToken(string code)
        {
            string responseToString;
            string postedData = "client_id=201872896&grant_type=authorization_code&client_secret=" + client_secret +
                                "&code=" + code + "&redirect_uri=" + redirect_url + "&type=user_agent";
            var response = PostMethod(postedData, token_Url);
            if (response == null)
                return;

            var strreader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            responseToString = strreader.ReadToEnd();
            JObject o = JObject.Parse(responseToString);
            authToken = o["access_token"].ToString();
            Debug.WriteLine(authToken + "responseToString = " + responseToString);
        }

        private static HttpWebResponse PostMethod(string postedData, string postUrl)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(postUrl);
            request.Method = "POST";
            request.Credentials = CredentialCache.DefaultCredentials;

            UTF8Encoding encoding = new UTF8Encoding();
            var bytes = encoding.GetBytes(postedData);

            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = bytes.Length;

            using (var newStream = request.GetRequestStream())
            {
                newStream.Write(bytes, 0, bytes.Length);
                newStream.Close();
            }
            return (HttpWebResponse)request.GetResponse();
        }

        public void LoadUserInfo(String userId, String authToken)
        {
            JObject dict = MakeRequest("method=users.getCurrentUser");
            userId = dict["uid"].ToString();

            StringBuilder sb = new StringBuilder(api_url);
            sb.Append("/method/getProfiles");
            sb.Append('?');
            sb.Append("uid=").Append(userId).Append('&');
            sb.Append("access_token=").Append(authToken);

            try
            {
                // Create URI 
                Uri address = new Uri(sb.ToString());

                Debug.WriteLine("getProfiles: " + address.ToString());

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
                                JObject ego = o["response"][0].ToObject<JObject>();
                                Console.WriteLine("Ego: " + ego.ToString()); 

                                // ok, create the ego object here
                                AttributesDictionary<String> attributes = createAttributes(ego);

                                this.egoVertex = new Vertex(ego["uid"].ToString(),
                                    ego["first_name"].ToString() + " " + ego["last_name"].ToString(),
                                    "Ego", attributes);

                                // add ego to the vertices
                                this.vertices.Add(this.egoVertex);
                            }
                        }
                        else if(o["error"] != null)
                        {
                            Debug.WriteLine("Error " + o["error"].ToString());
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Error " + ((new StreamReader(ResponseStream)).ReadToEnd()));
                    }
                }
            }
            catch (WebException Ex)
            {
                handleWebException(Ex);
            }
        }

        public void LoadFriends(String userId)
        {
            StringBuilder sb = new StringBuilder(api_url);
            sb.Append("/method/friends.get");
            sb.Append('?');
            sb.Append("user_id=").Append(userId).Append('&');
            sb.Append("fields=").Append("uid,first_name,last_name,nickname,sex,bdate,city,country,education");

            try
            {
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
            catch (WebException Ex)
            {
                handleWebException(Ex);
            }
        }

        public void GetMutual(String userId, String authToken)
        {
            StringBuilder mainsb = new StringBuilder(api_url);
            mainsb.Append("/method/friends.getMutual");
            mainsb.Append('?');
            mainsb.Append("source_uid=").Append(userId).Append('&');
            mainsb.Append("access_token=").Append(authToken).Append('&');

            try
            {

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
            catch (WebException Ex)
            {
                handleWebException(Ex);
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


        private JObject MakeRequest(string requestString)
        {
            var sig = GetMD5Hash(string.Format("{0}{1}", "application_key=" + client_open + requestString.Replace("&", ""), sigSecret));
            string responseToString;
            string postedData = postPrefix + requestString + "&sig=" + sig;

            var response = PostMethod(postedData, apiUrl);
            var strreader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            responseToString = strreader.ReadToEnd();
            return JObject.Parse(responseToString); // JObject.CreateFromString(responseToString);
        }


        private static string GetMD5Hash(string input)
        {
            var x = new System.Security.Cryptography.MD5CryptoServiceProvider();
            var bs = Encoding.UTF8.GetBytes(input);
            bs = x.ComputeHash(bs);
            var s = new StringBuilder();
            foreach (var b in bs)
            {
                s.Append(b.ToString("x2").ToLower());
            }
            return s.ToString();
        }

        private void handleWebException(WebException Ex)
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
            }
            else
            {
                throw (Ex); // Or check for other WebExceptionStatus
            }
        }
    }
}
