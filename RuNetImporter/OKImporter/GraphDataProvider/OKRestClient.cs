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
            Debug.WriteLine("authToken = " + authToken);
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
            JObject ego = JObject.Parse(MakeRequest("method=users.getCurrentUser"));
            Console.WriteLine("Ego: " + ego.ToString());
            this.userId = ego["uid"].ToString();

            AttributesDictionary<String> attributes = createAttributes(ego);
            egoVertex = new Vertex(ego["uid"].ToString(),
                ego["first_name"].ToString() + " " + ego["last_name"].ToString(),
                "Ego", attributes);
            // add ego to the vertices
            vertices.Add(this.egoVertex);
        }
/*
        private void GetMutualFriends()
        {
            JObject dict = MakeRequest("method=friends.get"); // fid=160539089447&fid=561967133371&fid=561692396161&
            JObject[] friends = dict.Array;
            string friendUids = staticUid;
            vertexCollection.Clear();
            edgeCollection.Clear();
            foreach (var friend in friends)
            {
                JObject friendDict = MakeRequest("method=friends.getMutualFriends&target_id=" + friend.String);
                    //  &source_id=160539089447
                friendUids += "," + friend.String;
                addVertex(friend.String);
                foreach (var subFriend in friendDict.Array)
                    if (friend.Integer > subFriend.Integer)
                        addEdge(friend.String, subFriend.String);
            }

            GetUserInfo(friendUids);
        }
*/
        public void LoadFriends(String userId)
        {
            JArray friends = JArray.Parse(MakeRequest("method=friends.get")); // fid=160539089447&fid=561967133371&fid=561692396161&
            string friendUids = userId;
            foreach (var friend in friends) {
//                JObject friendDict = JObject.Parse(MakeRequest("method=friends.getMutualFriends&target_id=" + friend));  //  &source_id=160539089447
                friendUids += "," + friend;
                friendIds.Add(friend.ToString());
            }

/*            vertexCollection.Clear();
            edgeCollection.Clear();
            foreach (var friend in friends) {
                JObject friendDict = JObject.Parse(MakeRequest("method=friends.getMutualFriends&target_id=" + friend));  //  &source_id=160539089447
                friendUids += "," + friend;
                addVertex(friend.String);
                foreach (var subFriend in friendDict.Array)
                    if (friend.Integer > subFriend.Integer)
                        addEdge(friend.String, subFriend.String);
            }
*/
            GetUserInfo(friendUids);
        }

        public void GetAreFriends()
        {
            var pares = GeneratePares(friendIds.ToArray());
            string[] uidsArr1 = pares[0].Split(',');
            string[] uidsArr2 = pares[1].Split(',');
            JArray friendsDict;

            for (var i = 0; i < uidsArr1.Length; i += 100) {
                string uids1 = string.Join(",", uidsArr1.Skip(i).Take(100).ToArray());
                string uids2 = string.Join(",", uidsArr2.Skip(i).Take(100).ToArray());
                friendsDict = JArray.Parse(MakeRequest("method=friends.areFriends&uids1=" + uids1 + "&uids2=" + uids2));
                foreach (var friend in friendsDict) {
                    if (friend["are_friends"].ToString().ToLower() == "true") {
                        CreateEdge(friend["uid1"].ToString(), friend["uid2"].ToString(), edges, vertices);
                        Debug.WriteLine("AreFriends: " + friend["uid1"].ToString(), friend["uid2"].ToString());
                    }
                }
                Thread.Sleep(100);
            }
        }

        public void GetMutualFriends(String userId, String authToken)
        {
/*          vertexCollection.Clear();
            edgeCollection.Clear();*/
            string friendUids = userId;

            foreach (var friendId in friendIds)
            {
                JArray friendDict = JArray.Parse(MakeRequest("method=friends.getMutualFriends&target_id=" + friendId));  //  &source_id=160539089447
                friendUids += "," + friendId;
//                addVertex(friend.String);
                foreach (var subFriend in friendDict) {
                    Debug.WriteLine("Mutual: " + subFriend.ToString()); // Debug.WriteLine(subFriend);
                    CreateEdge(friendId, subFriend.ToString(), edges, vertices);
                }
/*
                    if (friend.Integer > subFriend.Integer)
                        addEdge(friend.String, subFriend.String);*/
            }
        }

        private void GetUserInfo(string uids)
        {
            JArray dict = JArray.Parse(MakeRequest("fields=uid,name,first_name, last_name,age,gender,locale&method=users.getInfo&uids=" + uids));
            Console.WriteLine("_________________________________________________");
            foreach (var friend in dict) {
                AttributesDictionary<String> attributes = createAttributes(friend.ToObject<JObject>());

                vertices.Add(new Vertex(friend["uid"].ToString(),
                    friend["first_name"].ToString() + " " + friend["last_name"].ToString(),
                    "Friend", attributes));
            }
        }


        private void CreateEdge(String friendId, String friendFriendsId, EdgeCollection edges, VertexCollection vertices)
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
            foreach (AttributeUtils.Attribute key in keys) {
                String name = key.value;
                if (obj[name] != null) {
                    // assert it is null?
                    String value = obj[name].ToString(); 
                    attributes[key] =  value;
                }
            }

            return attributes;
        }

        private string MakeRequest(string requestString)
        {
            var sig = GetMD5Hash(string.Format("{0}{1}", "application_key=" + client_open + requestString.Replace("&", ""), sigSecret));
            string postedData = postPrefix + requestString + "&sig=" + sig;
            var response = PostMethod(postedData, apiUrl);
            var strreader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            string responseToString = strreader.ReadToEnd();
            return responseToString; //JObject.Parse(responseToString); // JObject.CreateFromString(responseToString);
        }


        private static string GetMD5Hash(string input)
        {
            var x = new System.Security.Cryptography.MD5CryptoServiceProvider();
            var bs = Encoding.UTF8.GetBytes(input);
            bs = x.ComputeHash(bs);
            var s = new StringBuilder();
            foreach (var b in bs)
                s.Append(b.ToString("x2").ToLower());

            return s.ToString();
        }

        private void handleWebException(WebException Ex)
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

        private static string[] GeneratePares(string[] friendUids)
        {
            int n = friendUids.Length - 1;
            int k = 2;
            string[] result = new string[k];
            int[] a = new int[k];
            for (int i = 0; i < k; i++) {
                a[i] = i + 1;
                result[i] = friendUids[a[i]];
            }
            int p = k - 1;
            while (p >= 0) {
                if (a[k - 1] == n)
                    p--;
                else p = k - 1;
                if (p >= 0) {
                    for (int i = k - 1; i >= p; i--)
                        a[i] = a[p] + i - p + 1;

                    result[0] += "," + friendUids[a[0]];
                    result[1] += "," + friendUids[a[1]];
                }
            }
            return result;
        }
    }
}
