using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using rcsir.net.common.Network;
using rcsir.net.ok.importer.Data;

namespace rcsir.net.ok.importer.Controllers
{
    internal class GraphDataManager
    {
        private readonly GraphStorage graphStorage;

        internal string EgoId { get { return graphStorage.EgoId; } }

        internal List<string> FriendIds { get { return graphStorage.FriendIds; } }

        internal int FriendsCount { get { return graphStorage.FriendIds.Count; } }

        internal VertexCollection Vertices { get { return graphStorage.Vertices; } }

        internal EdgeCollection Edges { get { return graphStorage.Edges; } }

        internal GraphDataManager() //  GraphStorage storage
        {
            graphStorage = new GraphStorage();
        }

        internal void AddFriendId(string id)
        {
            graphStorage.AddFriendId(id);
        }

        internal void AddEgo(JObject ego, string cookie = null)
        {
            graphStorage.AddEgoVertexIfNeeded(ego);
        }

        internal void AddFriends(JArray friends, string cookie = null)
        {
            foreach (var friend in friends)
                graphStorage.AddFriendVertex(friend.ToObject<JObject>());
        }

        internal void AddAreFriends(JArray friendsDict, string cookie = null)
        {
            foreach (var friend in friendsDict)
                if (friend["are_friends"].ToString().ToLower() == "true") {
                    graphStorage.AddEdge(friend["uid1"].ToString(), friend["uid2"].ToString());
                    Debug.WriteLine(friend["uid1"] + " AreFriends: " + friend["uid2"]);
                }
        }

        internal void AddFriends(string userId, JArray friendsDict, string cookie = null)
        {
            foreach (var subFriend in friendsDict) {
                graphStorage.AddEdge(userId, subFriend.ToString());
                Debug.WriteLine("Mutual: " + subFriend);
            }
        }

        internal void ClearEdges()
        {
            graphStorage.ClearEdges();
        }

        internal void ClearVertices()
        {
            graphStorage.ClearVertices();
        }

        internal void AddMeIfNeeded()
        {
            graphStorage.AddIncludeMeEdgesIfNeeded();
            // create default attributes (values will be empty)
/*            AttributesDictionary<String> attributes = new AttributesDictionary<String>();
            return GenerateNetworkDocument(vertices, edges, attributes);*/
        }
    }
}
