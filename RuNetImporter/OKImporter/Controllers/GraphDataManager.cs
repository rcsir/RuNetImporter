using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using rcsir.net.ok.importer.Events;
using rcsir.net.ok.importer.Storages;
using Smrf.AppLib;

namespace rcsir.net.ok.importer.Controllers
{
    public class GraphDataManager
    {
        private readonly GraphStorage graphStorage;
        private readonly AttributesStorage attributeStorage;

        internal List<string> FriendIds { get { return graphStorage.FriendIds; } }

        internal int FriendsCount { get { return graphStorage.FriendIds.Count; } }

        internal AttributesDictionary<bool> OkDialogAttributes { get { return attributeStorage.OkDialogAttributes; } }

        internal AttributesDictionary<string> GraphAttributes { get { return attributeStorage.GraphAttributes; } }

        public event EventHandler<GraphEventArgs> OnData;

        public GraphDataManager() //  GraphStorage storage
        {
            graphStorage = new GraphStorage();
            attributeStorage = new AttributesStorage();
        }

        internal void AddFriendId(string id)
        {
            graphStorage.AddFriendId(id);
        }

        internal void SendEgo(JObject ego, string cookie = null)
        {
//            graphStorage.AddEgoVertexIfNeeded(ego, attributeStorage.CreateVertexAttributes(ego));
            GraphEventArgs evnt = new GraphEventArgs(GraphEventArgs.Types.UserInfoLoaded, ego);
            DispatchEvent(evnt);
        }

        internal void AddFriends(JArray friends, string cookie = null)
        {
            foreach (var friend in friends)
                graphStorage.AddFriendVertex(friend.ToObject<JObject>(), attributeStorage.CreateVertexAttributes(friend));

            GraphEventArgs evnt = new GraphEventArgs(GraphEventArgs.Types.FriendsLoaded);
            DispatchEvent(evnt);
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
            var evnt = new GraphEventArgs(graphStorage.Vertices, graphStorage.Edges, attributeStorage.OkDialogAttributes, attributeStorage.GraphAttributes);
            DispatchEvent(evnt);
        }

        internal void MakeGraphAttributes(DataGridViewRow[] rows)
        {
            foreach (DataGridViewRow row in rows)
                attributeStorage.UpdateDialogAttributes(row.Cells[2].Value.ToString(), (bool)row.Cells[1].Value);
            attributeStorage.MakeGraphAttributes();
        }

        internal string CreateRequiredFieldsString()
        {
            return attributeStorage.CreateRequiredFieldsString();
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
