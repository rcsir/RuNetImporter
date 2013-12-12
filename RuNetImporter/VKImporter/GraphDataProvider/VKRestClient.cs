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

namespace rcsir.net.vk.importer.GraphDataProvider
{
    public class VKRestClient
    {
        // API usrl
        private readonly String api_url = "https://api.vk.com";

        private Vertex egoVertex;
        private List<string> friendIds = new List<string>();
        private VertexCollection vertices = new VertexCollection();
        private EdgeCollection edges = new EdgeCollection();

        public VKRestClient()
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

        // VK API
        public void LoadUserInfo(String userId, String authToken)
        {
            //https://api.vk.com/method/getProfiles?uid=66748&access_token=533bacf01e11f55b536a565b57531ac114461ae8736d6506a3;

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
