using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using rcsir.net.ok.importer.Events;
using rcsir.net.ok.importer.Storages;
using Smrf.AppLib;

namespace rcsir.net.ok.importer.Controllers
{
    class GraphDataManager
    {
        private readonly GraphStorage graphStorage;
        private readonly AttributesStorage attributeStorage;

        internal List<string> FriendIds { get { return graphStorage.FriendIds; } }

        internal int FriendsCount { get { return graphStorage.FriendIds.Count; } }

        internal AttributesDictionary<bool> OkDialogAttributes { get { return attributeStorage.OkDialogAttributes; } }

        internal event EventHandler<GraphEventArgs> OnData;

        internal GraphDataManager()
        {
            graphStorage = new GraphStorage();
            attributeStorage = new AttributesStorage();
        }

        internal void AddFriendId(string id)
        {
            graphStorage.AddFriendId(id);
        }

        internal void SendEgo(JObject ego)
        {
            GraphEventArgs evnt = new GraphEventArgs(GraphEventArgs.Types.UserInfoLoaded, ego);
            DispatchEvent(evnt);
        }

        internal void MakeEgo(JToken ego)
        {
            graphStorage.MakeEgoVertex(ego, attributeStorage.CreateVertexAttributes(ego));
        }

        internal void AddFriends(JArray friends)
        {
            foreach (var friend in friends)
                graphStorage.AddFriendVertex(friend, attributeStorage.CreateVertexAttributes(friend));

            var evnt = new GraphEventArgs(GraphEventArgs.Types.FriendsLoaded);
            DispatchEvent(evnt);
        }

        internal void AddAreFriends(JArray friendsDict)
        {
            foreach (var friend in friendsDict)
                if (friend["are_friends"].ToString().ToLower() == "true") {
                    graphStorage.AddEdge(friend["uid1"].ToString(), friend["uid2"].ToString());
                    Debug.WriteLine(friend["uid1"] + " AreFriends: " + friend["uid2"]);
                }
        }

        internal void AddFriends(string userId, JArray friendsDict)
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
            var evnt = new GraphEventArgs(graphStorage.Vertices, graphStorage.Edges, attributeStorage.OkDialogAttributes, attributeStorage.GraphAttributes);
            DispatchEvent(evnt);
        }

        internal void UpdateAllAttributes(bool[] rows)
        {
            attributeStorage.UpdateAllAttributes(rows);
        }

        internal string CreateRequiredFieldsString()
        {
            return attributeStorage.CreateRequiredFieldsString();
        }

        internal void ResumeFriendsList()
        {
            var evnt = new GraphEventArgs(GraphEventArgs.Types.FriendsListLoaded, FriendsCount);
            DispatchEvent(evnt);
        }

        internal void ResumeGetGraph(bool isMutual = true)
        {
            GraphEventArgs evnt;
            if (isMutual)
                evnt = new GraphEventArgs(GraphEventArgs.Types.MutualGraphLoaded);
            else
                evnt = new GraphEventArgs(GraphEventArgs.Types.AreGraphLoaded);

            DispatchEvent(evnt);
        }

        protected virtual void DispatchEvent(GraphEventArgs e)
        {
            var handler = OnData;
            if (handler != null)
                handler(this, e);
        }
    }
}
