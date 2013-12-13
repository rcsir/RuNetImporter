using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Diagnostics;
using System.ComponentModel;
using Smrf.AppLib;
using rcsir.net.common.NetworkAnalyzer;
using rcsir.net.common.Network;
using rcsir.net.vk.importer.GraphDataProvider;
using Smrf.NodeXL.GraphDataProviders;
using Smrf.SocialNetworkLib;

namespace rcsir.net.vk.importer.NetworkAnalyzer
{
    public class VKNetworkAnalyzer : VKNetworkAnalyzerBase
    {
        public XmlDocument analyze(String userId, String authToken)
        {
            VKRestClient vkRestClient = new VKRestClient();

            vkRestClient.LoadUserInfo(userId, authToken);
            vkRestClient.LoadFriends(userId);
            vkRestClient.GetMutual(userId, authToken);

            VertexCollection vertices = vkRestClient.GetVertices();
            EdgeCollection edges = vkRestClient.GetEdges();
            
            // TODO: make is optional, should be controlled by a UI flag 
            // let's disable it for now
            if (false)
            {
                CreateIncludeMeEdges(edges, vertices);
            }

            // create default attributes (values will be empty)
            AttributesDictionary<String> attributes = new AttributesDictionary<String>();

            return GenerateNetworkDocument(vertices, edges, attributes);
        }

        private void CreateIncludeMeEdges(EdgeCollection edges, VertexCollection vertices)
        {
            List<Vertex> friends = vertices.Where(x => x.Type == "Friend").ToList();
            Vertex ego = vertices.FirstOrDefault(x => x.Type == "Ego");

            if (ego != null)
            {
                foreach (Vertex oFriend in friends)
                {
                    edges.Add(new Edge(ego, oFriend, "", "Friend", "", 1));
                }
            }
        }


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
    }
}
