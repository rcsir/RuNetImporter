using System;
using System.Collections.Generic;
using System.Diagnostics;
using rcsir.net.common.Utilities;
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

        internal void SendEgo(JSONObject ego)
        {
            GraphEventArgs evnt = new GraphEventArgs(GraphEventArgs.Types.UserInfoLoaded, ego);
            DispatchEvent(evnt);
        }

        internal void MakeEgo(JSONObject ego)
        {
            graphStorage.MakeEgoVertex(ego, attributeStorage.CreateVertexAttributes(ego));
        }

        internal void AddFriends(JSONObject[] friends)
        {
            foreach (var friend in friends)
                graphStorage.AddFriendVertex(friend, attributeStorage.CreateVertexAttributes(friend));

            var evnt = new GraphEventArgs(GraphEventArgs.Types.FriendsLoaded);
            DispatchEvent(evnt);
        }

        internal void AddAreFriends(JSONObject[] friendsDict)
        {
            foreach (var friend in friendsDict)
                if (friend.Dictionary["are_friends"].Boolean) {
                    graphStorage.AddEdge(friend.Dictionary["uid1"].String, friend.Dictionary["uid2"].String);
                    Debug.WriteLine(friend.Dictionary["uid1"].String + " AreFriends: " + friend.Dictionary["uid2"].String);
                }
        }

        internal void AddFriends(string userId, JSONObject[] friendsDict)
        {
            foreach (var subFriend in friendsDict) {
                graphStorage.AddEdge(userId, subFriend.String);
                Debug.WriteLine("Mutual: " + subFriend.ToDisplayableString());
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
