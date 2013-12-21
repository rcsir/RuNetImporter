using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;
using rcsir.net.common.Network;
using rcsir.net.ok.importer.Api;
using Smrf.AppLib;

namespace rcsir.net.ok.importer.GraphDataProvider
{
    public class OKRestClient
    {
        private OkProxy proxy = new OkProxy();
        private Vertex egoVertex;
        private List<string> friendIds = new List<string>();
        private VertexCollection vertices = new VertexCollection();
        private EdgeCollection edges = new EdgeCollection();

        private String _userId;
        public String userId { get { return _userId; } set { _userId = value; } }

        public Vertex GetEgo()
        {
            return egoVertex;
        }

        public VertexCollection GetVertices()
        {
            return vertices;
        }

        public EdgeCollection GetEdges()
        {
            return edges;
        }

        public void GetAccessToken(string code)
        {
            string responseString = proxy.GetToken(code);
            JObject o = JObject.Parse(responseString);
            PostRequests.AuthToken = o["access_token"].ToString();
            CreateEgoVertex();
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
        public void LoadFriends()
        {
            JArray friends = JArray.Parse(proxy.GetFriends()); // fid=160539089447&fid=561967133371&fid=561692396161&
            string friendUids = ""; // userId;
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
            CreateFriendsVertices(friendUids);
        }

        public void GetAreFriends()
        {
            var pares = MathUtil.GeneratePares(friendIds.ToArray());
            string[] uidsArr1 = pares[0].Split(',');
            string[] uidsArr2 = pares[1].Split(',');
            for (var i = 0; i < uidsArr1.Length; i += 100) {
                string uids1 = string.Join(",", uidsArr1.Skip(i).Take(100).ToArray());
                string uids2 = string.Join(",", uidsArr2.Skip(i).Take(100).ToArray());
                JArray friendsDict = JArray.Parse(proxy.GetAreFriends(uids1, uids2));
                foreach (var friend in friendsDict) {
                    if (friend["are_friends"].ToString().ToLower() == "true") {
                        CreateEdge(friend["uid1"].ToString(), friend["uid2"].ToString());
                        Debug.WriteLine("AreFriends: " + friend["uid1"], friend["uid2"]);
                    }
                }
                Thread.Sleep(100);
            }
        }

        public void GetMutualFriends()
        {
            string friendUids = userId;
            foreach (var friendId in friendIds) {
                JArray friendDict = JArray.Parse(proxy.GetMutualFriends(friendId));  //  &source_id=160539089447
                friendUids += "," + friendId;
                foreach (var subFriend in friendDict) {
                    Debug.WriteLine("Mutual: " + subFriend);
                    CreateEdge(friendId, subFriend.ToString());
                }
/*
                    if (friend.Integer > subFriend.Integer)
                        addEdge(friend.String, subFriend.String);*/
            }
        }

        private void CreateEgoVertex()
        {
            JObject ego = JObject.Parse(proxy.GetEgoInfo());
            userId = ego["uid"].ToString();
            AttributesDictionary<String> attributes = createAttributes(ego);
            egoVertex = new Vertex(ego["uid"].ToString(), ego["name"].ToString(), "Ego", attributes);
            vertices.Add(egoVertex);
        }

        private void CreateFriendsVertices(string uids)
        {
            string response = proxy.GetUsersInfo(uids);
            if (response == null)
                return;
            JArray dict = JArray.Parse(response);
            foreach (var friend in dict) {
                AttributesDictionary<String> attributes = createAttributes(friend.ToObject<JObject>());
                vertices.Add(new Vertex(friend["uid"].ToString(), friend["name"].ToString(), "Friend", attributes));
            }
        }

        private void CreateEdge(String friendId, String friendFriendsId)
        {
            Vertex friend = vertices.FirstOrDefault(x => x.ID == friendId);
            Vertex friendsFriend = vertices.FirstOrDefault(x => x.ID == friendFriendsId);

            if (friend != null && friendsFriend != null)
            {
                edges.Add(new Edge(friend, friendsFriend, "", "Friend", "", 1));
            }
        }

        private static AttributesDictionary<String> createAttributes(JObject obj) 
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
    }
}
