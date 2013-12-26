using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using Newtonsoft.Json.Linq;
using Smrf.AppLib;
using Smrf.NodeXL.GraphDataProviders;
using Smrf.SocialNetworkLib;
using rcsir.net.common.Network;
using rcsir.net.ok.importer.GraphDataProvider;
using rcsir.net.ok.importer.Api;

namespace rcsir.net.ok.importer.NetworkAnalyzer
{
    public class OKNetworkAnalyzer : OKNetworkAnalyzerBase
    {
/*        private OKRestClient _okRestClient;
        public OKRestClient okRestClient { set { _okRestClient = value; }}

        private OKRestApi okRestApi;

        private static AutoResetEvent readyEvent = new AutoResetEvent(false);

        private bool includeEgo = false; // include ego vertex and edges, should be controled by UI
        private List<string> friendIds = new List<string>();*/
//        private Vertex egoVertex;
        private VertexCollection vertices = new VertexCollection();
        private EdgeCollection edges = new EdgeCollection();
/*

        // process load user info response
        public void OnLoadUserInfo(JObject ego, string cookie = null)
        {
            AttributesDictionary<String> attributes = createAttributes(ego);
            egoVertex = new Vertex(ego["uid"].ToString(), ego["name"].ToString(), "Ego", attributes);
            // add ego to the vertices
            if (includeEgo)
                vertices.Add(egoVertex);
        }
        // process load user friends response
        public void OnLoadFriends(JArray data, string cookie = null)
        {
            foreach (var friend in data) {
                AttributesDictionary<String> attributes = createAttributes(friend.ToObject<JObject>());
                vertices.Add(new Vertex(friend["uid"].ToString(), friend["name"].ToString(), "Friend", attributes));
            }
        }
        // process get mutual response
        public void OnGetAre(JArray friendsDict, string cookie = null)
        {
            foreach (var friend in friendsDict)
                if (friend["are_friends"].ToString().ToLower() == "true") {
                    CreateEdge(friend["uid1"].ToString(), friend["uid2"].ToString());
                    Debug.WriteLine(friend["uid1"] + " AreFriends: " + friend["uid2"]);
                }
        }
        // process get mutual response
        public void OnGetMutual(JArray friendsDict, string friendId, string cookie = null)
        {
            foreach (var subFriend in friendsDict) {
                Debug.WriteLine("Mutual: " + subFriend);
                CreateEdge(friendId, subFriend.ToString());
            }
        }
*/
        
        
        public void MakeTestXml(VertexCollection vertices, EdgeCollection edges, string EgoId)
        {
            // create default attributes (values will be empty)
            var attributes = new AttributesDictionary<String>();
            var graph = GenerateNetworkDocument(vertices, edges, attributes);
            if (graph != null)
                graph.Save("OKNetwork_" + EgoId + ".graphml");
        }

        public XmlDocument analyze(string userId, string authToken)
        {



 /*           OKRestContext context = new OKRestContext(userId, authToken);

            okRestApi.CallOKFunction(OKFunction.LoadUserInfo, context);

            // wait for the user data
            readyEvent.WaitOne();
            context.parameters = "fields=uid,first_name,last_name,nickname,sex,bdate,city,country,education";
            okRestApi.CallOKFunction(OKFunction.LoadFriends, context);

            // wait for the friends data
            readyEvent.WaitOne();
            foreach (string targetId in friendIds)
            {
                StringBuilder sb = new StringBuilder("target_uid=");
                // Append target friend ids
                sb.Append(targetId);

                context.parameters = sb.ToString();
                context.cookie = targetId; // pass target id in the cookie context field
                okRestApi.CallOKFunction(OKFunction.GetMutualGraph, context);
                // wait for the mutual data
                readyEvent.WaitOne();

                // play nice, sleep for 1/3 sec to stay within 3 requests/second limit
                // TODO: account for time spent in processing
                Thread.Sleep(100);
            }

            if (includeEgo)
                CreateIncludeMeEdges(edges, vertices);*/
            // create default attributes (values will be empty)
            AttributesDictionary<String> attributes = new AttributesDictionary<String>();
            return GenerateNetworkDocument(vertices, edges, attributes);
        }
/*
        private void CreateIncludeMeEdges(EdgeCollection edges, VertexCollection vertices)
        {
            List<Vertex> friends = vertices.Where(x => x.Type == "Friend").ToList();
            Vertex ego = vertices.FirstOrDefault(x => x.Type == "Ego");

            if (ego != null)
                foreach (Vertex oFriend in friends)
                    edges.Add(new Edge(ego, oFriend, "", "Friend", "", 1));
        }

        private void CreateEdge(String friendId, String friendFriendsId)
        {
            Vertex friend = vertices.FirstOrDefault(x => x.ID == friendId);
            Vertex friendsFriend = vertices.FirstOrDefault(x => x.ID == friendFriendsId);

            if (friend != null && friendsFriend != null)
                edges.Add(new Edge(friend, friendsFriend, "", "Friend", "", 1));
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
                    attributes[key] = value;
                }
            }

            return attributes;
        }

        // Network details
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
*/


        //*************************************************************************
        //  Method: BackgroundWorker_DoWork()
        //
        /// <summary>
        /// Handles the DoWork event on the BackgroundWorker object.
        /// </summary>
        ///
        /// <param name="sender">
        /// Source of the event.
        /// </param>
        ///
        /// <param name="e">
        /// Standard mouse event arguments.
        /// </param>
        //*************************************************************************

        protected override void
        BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {

            Debug.Assert(sender is BackgroundWorker);

            BackgroundWorker oBackgroundWorker = (BackgroundWorker)sender;

            Debug.Assert(e.Argument is GetNetworkAsyncArgs);

            GetNetworkAsyncArgs oGetNetworkAsyncArgs =
                (GetNetworkAsyncArgs)e.Argument;

            try
            {
                e.Result = this.analyze("","");

                /*
                    GetFriendsNetworkInternal
                    (
                    oGetNetworkAsyncArgs.AccessToken,
                    oGetNetworkAsyncArgs.EdgeType,
                    oGetNetworkAsyncArgs.DownloadFirstPosts,
                    oGetNetworkAsyncArgs.DownloadBetweenDates,
                    oGetNetworkAsyncArgs.EgoTimeline,
                    oGetNetworkAsyncArgs.FriendsTimeline,
                    oGetNetworkAsyncArgs.NrOfFirstPosts,
                    oGetNetworkAsyncArgs.StartDate,
                    oGetNetworkAsyncArgs.EndDate,
                    oGetNetworkAsyncArgs.LimitCommentsLikes,
                    oGetNetworkAsyncArgs.Limit,
                    oGetNetworkAsyncArgs.GetTooltips,
                    oGetNetworkAsyncArgs.IncludeMe,
                    oGetNetworkAsyncArgs.attributes
                    );
                     */
            }
            catch (CancellationPendingException)
            {
                e.Cancel = true;
            }

        }


        //*************************************************************************
        //  Method: AssertValid()
        //
        /// <summary>
        /// Asserts if the object is in an invalid state.  Debug-only.
        /// </summary>
        //*************************************************************************

        // [Conditional("DEBUG")]

        public override void
        AssertValid()
        {
            base.AssertValid();

            // (Do nothing else.)
        }

        //*************************************************************************
        //  Embedded class: GetNetworkAsyncArgs()
        //
        /// <summary>
        /// Contains the arguments needed to asynchronously get a network of Flickr
        /// users.
        /// </summary>
        //*************************************************************************

        protected class GetNetworkAsyncArgs : GetNetworkAsyncArgsBase
        {
            ///
            public AttributesDictionary<bool> attributes;
            ///           
            public List<NetworkType> EdgeType;
            ///
            public bool DownloadFirstPosts;
            ///
            public bool DownloadBetweenDates;
            ///
            public bool EgoTimeline;
            ///
            public bool FriendsTimeline;
            ///
            public int NrOfFirstPosts;
            ///
            public DateTime StartDate;
            ///
            public DateTime EndDate;
            ///
            public bool LimitCommentsLikes;
            ///
            public int Limit;
            ///
            public bool GetTooltips;
            ///
            public bool IncludeMe;
        };
/*
        public XmlDocument analyze(bool isMutual = true)
        {
//            _okRestClient.LoadUserInfo(userId, authToken);
            _okRestClient.LoadFriends();
            if (isMutual)
                _okRestClient.GetMutualFriends();
            else
                _okRestClient.GetAreFriends();

            VertexCollection vertices = _okRestClient.GetVertices();
            EdgeCollection edges = _okRestClient.GetEdges();

            AttributesDictionary<String> attributes = new AttributesDictionary<String>();
            return GenerateNetworkDocument(vertices, edges, attributes);
        }*/
    }
}
