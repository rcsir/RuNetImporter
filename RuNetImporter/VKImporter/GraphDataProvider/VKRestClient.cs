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

namespace rcsir.net.vk.importer.GraphDataProvider
{
    public class VKRestClient
    {
        // API usrl
        private readonly String api_url = "https://api.vk.com";

        private List<string> friendIds = new List<string>();

        public VKRestClient()
        {
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
                        Console.WriteLine("Name: " + o["response"][0]["first_name"]);
                        Console.WriteLine("Last Name: " + o["response"][0]["last_name"]);
                        Console.WriteLine("UID: " + o["response"][0]["uid"]);
                    }
                }

            }
            catch (WebException Ex)
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
                        if (o["response"].Count() > 0)
                        {

                            for (int i = 0; i < o["response"].Count(); ++i)
                            {
                                // uid, first_name, last_name, nickname, sex, bdate, city, country, timezone
                                string uid = o["response"][i]["uid"].ToString();
                                Console.WriteLine(i.ToString() + ") UID: " + uid);
                                Console.WriteLine(" Name: " + o["response"][i]["first_name"]);
                                Console.WriteLine(" Last Name: " + o["response"][i]["last_name"]);
                                Console.WriteLine(" nick: " + o["response"][i]["nickname"]);
                                Console.WriteLine(" b. date: " + o["response"][i]["bdate"]);
                                Console.WriteLine(" sex: " + o["response"][i]["sex"]);
                                Console.WriteLine(" city: " + o["response"][i]["city"]);
                                Console.WriteLine(" country: " + o["response"][i]["country"]);
                                Console.WriteLine(" timezone: " + o["response"][i]["timezone"]);
                                Console.WriteLine(" education: " + o["response"][i]["education"]);

                                // add user id to the friends list
                                this.friendIds.Add(uid);
                            }
                        }
                    }
                }

            }
            catch (WebException Ex)
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

        public void getMutual(String userId, String authToken)
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
                            Debug.WriteLine("Mutual: " + o.ToString());
                        }
                    }

                    // play nice, sleep for 1/3 sec to stay within 3 requests/second limit
                    Thread.Sleep(333);
                }
            }
            catch (WebException Ex)
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
}
