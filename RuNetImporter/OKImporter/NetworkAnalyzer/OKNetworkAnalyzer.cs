using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Xml;
using rcsir.net.common.Network;
using Smrf.AppLib;
using Smrf.NodeXL.GraphDataProviders;
using Smrf.SocialNetworkLib;
using rcsir.net.ok.importer.Controllers;

namespace rcsir.net.ok.importer.NetworkAnalyzer
{
    public class OKNetworkAnalyzer : HttpNetworkAnalyzerBase
    {
        public OkController controller;

        public OkController Controller { set { controller = value; } }
        
        private VertexCollection<String> vertices;
        private EdgeCollection<String> edges;
        private AttributesDictionary<bool> dialogAttributes;
        private AttributesDictionary<string> graphAttributes;

        private string egoId;
        public string EgoId { set { egoId = value; } }

        public void SetGraph(VertexCollection<String> okVertices, EdgeCollection<String> okEdges, AttributesDictionary<bool> okDialogAttributes, AttributesDictionary<string> okDraphAttributes)
        {
            vertices = okVertices;
            edges = okEdges;
            dialogAttributes = okDialogAttributes;
            graphAttributes = okDraphAttributes;
        }
        //*************************************************************************
        //  Method: GetNetworkAsync()
        //
        /// <summary>
        /// Asynchronously gets a directed network of Facebook friends.
        /// </summary>
        ///
        /// <param name="s_accessToken">
        /// The access_token needed for the authentication in Facebook API.
        /// </param>
        ///
        /// <param name="includeMe">
        /// Specifies whether the ego should be included in the network.
        /// </param>
        ///
        /// <remarks>
        /// When the analysis completes, the <see
        /// cref="HttpNetworkAnalyzerBase.AnalysisCompleted" /> event fires.  The
        /// <see cref="RunWorkerCompletedEventArgs.Result" /> property will return
        /// an XmlDocument containing the network as GraphML.
        ///
        /// <para>
        /// To cancel the analysis, call <see
        /// cref="HttpNetworkAnalyzerBase.CancelAsync" />.
        /// </para>
        ///
        /// </remarks>
        //*************************************************************************

        public void
        GetNetworkAsync
        (
            List<NetworkType> oEdgeType,
            bool bIncludeMe,
            DateTime oStartDate,
            DateTime oEndDate,
            bool bDownloadFirstPosts = false,
            bool bDownloadBetweenDates = false,
            bool bEgoTimeline = false,
            bool bFriendsTimeline = false,
            int iNrOfFirstPosts = 1000,
            bool bLimitCommentsLikes = false,
            int iNrLimit = 1000,
            bool bGetTooltips = true
        )
        {
            AssertValid();
            const string MethodName = "GetNetworkAsync";
            CheckIsBusy(MethodName);

            GetNetworkAsyncArgs oGetNetworkAsyncArgs = new GetNetworkAsyncArgs();
            oGetNetworkAsyncArgs.Attributes = dialogAttributes;
            oGetNetworkAsyncArgs.EdgeType = oEdgeType;
            oGetNetworkAsyncArgs.DownloadFirstPosts = bDownloadFirstPosts;
            oGetNetworkAsyncArgs.DownloadBetweenDates = bDownloadBetweenDates;
            oGetNetworkAsyncArgs.EgoTimeline = bEgoTimeline;
            oGetNetworkAsyncArgs.FriendsTimeline = bFriendsTimeline;
            oGetNetworkAsyncArgs.NrOfFirstPosts = iNrOfFirstPosts;
            oGetNetworkAsyncArgs.StartDate = oStartDate;
            oGetNetworkAsyncArgs.EndDate = oEndDate;
            oGetNetworkAsyncArgs.LimitCommentsLikes = bLimitCommentsLikes;
            oGetNetworkAsyncArgs.Limit = iNrLimit;
            oGetNetworkAsyncArgs.GetTooltips = bGetTooltips;
            oGetNetworkAsyncArgs.IncludeMe = bIncludeMe;

            m_oBackgroundWorker.RunWorkerAsync(oGetNetworkAsyncArgs);
        }

        //*************************************************************************
        //  Method: GetNetwork()
        //
        /// <summary>
        /// Synchronously gets a directed network of Facebook friends.
        /// </summary>
        ///
        /// <param name="s_accessToken">
        /// The access_token needed for the authentication in Facebook API.
        /// </param>
        ///
        /// <param name="includeMe">
        /// Specifies whether the ego should be included in the network.
        /// </param>
        ///
        /// <remarks>
        /// When the analysis completes, the <see
        /// cref="HttpNetworkAnalyzerBase.AnalysisCompleted" /> event fires.  The
        /// <see cref="RunWorkerCompletedEventArgs.Result" /> property will return
        /// an XmlDocument containing the network as GraphML.
        ///
        /// <para>
        /// To cancel the analysis, call <see
        /// cref="HttpNetworkAnalyzerBase.CancelAsync" />.
        /// </para>
        ///
        /// </remarks>
        //*************************************************************************
/*
        public XmlDocument
        GetNetwork
        (
            string s_accessToken,
            List<NetworkType> oEdgeType,
            bool bDownloadFirstPosts,
            bool bDownloadBetweenDates,
            bool bEgoTimeline,
            bool bFriendsTimeline,
            int iNrOfFirstPosts,
            DateTime oStartDate,
            DateTime oEndDate,
            bool bLimitCommentsLikes,
            int iNrLimit,
            bool bGetTooltips,
            bool bIncludeMe,
            AttributesDictionary<bool> attributes
        )
        {
            Debug.Assert(!string.IsNullOrEmpty(s_accessToken));
            AssertValid();


            return (GetFriendsNetworkInternal(s_accessToken, oEdgeType, bDownloadFirstPosts,
                                                bDownloadBetweenDates, bEgoTimeline, bFriendsTimeline,
                                                iNrOfFirstPosts, oStartDate, oEndDate,
                                                bLimitCommentsLikes, iNrLimit, bGetTooltips,
                                                bIncludeMe, attributes));
        }
*/
        //*************************************************************************
        //  Method: GetFriendsNetworkInternal()
        //
        /// <summary>
        /// Gets the friends network from Facebook.
        /// </summary>
        ///
        /// <param name="sAccessToken">
        /// The access_token needed to execute queries in Facebook.
        /// </param>
        ///
        /// <returns>
        /// An XmlDocument containing the network as GraphML.
        /// </returns>
        //*************************************************************************
/*
        protected XmlDocument
        GetFriendsNetworkInternal
        (
            string sAccessToken,
            List<NetworkType> oEdgeType,
            bool bDownloadFirstPosts,
            bool bDownloadBetweenDates,
            bool bEgoTimeline,
            bool bFriendsTimeline,
            int iNrOfFirstPosts,
            DateTime oStartDate,
            DateTime oEndDate,
            bool bLimitCommentsLikes,
            int iNrLimit,
            bool bGetTooltips,
            bool bIncludeMe,
            AttributesDictionary<bool> attributes
        )
        {
            Debug.Assert(!string.IsNullOrEmpty(sAccessToken));
            AssertValid();

            //Set the total nr of steps
            if (bGetTooltips) NrOfSteps++;

            oTimer.Elapsed += new System.Timers.ElapsedEventHandler(oTimer_Elapsed);

            oGraphMLXmlDocument = CreateGraphMLXmlDocument(attributes);
            RequestStatistics oRequestStatistics = new RequestStatistics();
*/

/*
            var fb = new FacebookAPI(sAccessToken);
            m_oFb = fb;

            Dictionary<string, string> friends = new Dictionary<string, string>();
            List<string> friendsUIDs = new List<string>();
            XmlNode oVertexXmlNode;
            string attributeValue = "";
            Dictionary<string, List<Dictionary<string, object>>> statusUpdates = new Dictionary<string, List<Dictionary<string, object>>>();
            Dictionary<string, List<Dictionary<string, object>>> wallPosts = new Dictionary<string, List<Dictionary<string, object>>>();

            string currentStatusUpdate = "";
            string currentWallPost = "";
            string currentWallTags = "";
            string currentStatusTags = "";
            List<Dictionary<string, object>>.Enumerator en;
            bool bGetUsersTagged = oEdgeType.Contains(NetworkType.TimelineUserTagged);
            bool bGetCommenters = oEdgeType.Contains(NetworkType.TimelineUserComments);
            bool bGetLikers = oEdgeType.Contains(NetworkType.TimelineUserLikes);
            bool bGetPostAuthors = oEdgeType.Contains(NetworkType.TimelinePostAuthors);
            bool bGetPosts = oEdgeType.Count > 0;

            DownloadFriends();

            if (bEgoTimeline || bIncludeMe)
            {
                GetEgo();
            }

            oVerticesToQuery = VerticesToQuery(bEgoTimeline, bFriendsTimeline);

            if (bGetPosts)
            {
                DownloadPosts(bDownloadFirstPosts, bDownloadBetweenDates, iNrOfFirstPosts,
                                oStartDate, oEndDate, bGetUsersTagged, bGetCommenters,
                                bGetLikers);
            }

            DownloadVertices(bGetUsersTagged, bGetCommenters, bGetLikers, bGetPosts, bLimitCommentsLikes, iNrLimit);

            DownloadAttributes(attributes);

            if (bGetTooltips)
            {
                GetTooltips();
            }

            CreateEdges(bGetUsersTagged, bGetCommenters, bGetLikers, bGetPostAuthors, bIncludeMe);

            AddVertices();

            ReportProgress(string.Format("Completed downloading {0} friends data", friends.Count));

            AddEdges();




            ReportProgress("Importing downloaded network into NodeXL");

            //After successfull download of the network
            //get the network description
            OnNetworkObtainedWithoutTerminatingException(oGraphMLXmlDocument, oRequestStatistics,
                GetNetworkDescription(oEdgeType, bDownloadFirstPosts, bDownloadBetweenDates,
                                        iNrOfFirstPosts, oStartDate, oEndDate,
                                        bLimitCommentsLikes, iNrLimit, oGraphMLXmlDocument));
*/
/*

            return oGraphMLXmlDocument;

        }
*/
        //*************************************************************************
        //  Method: CreateGraphMLXmlDocument()
        //
        /// <summary>
        /// Creates a GraphMLXmlDocument representing a network of friends in Facebook.
        /// </summary>
        ///        
        /// <returns>
        /// A GraphMLXmlDocument representing a network of Facebook friends.  The
        /// document includes GraphML-attribute definitions but no vertices or
        /// edges.
        /// </returns>
        //*************************************************************************
/*

        protected GraphMLXmlDocument
        CreateGraphMLXmlDocument
        (
            AttributesDictionary<bool> attributes
        )
        {
            AssertValid();

            GraphMLXmlDocument oGraphMLXmlDocument = new GraphMLXmlDocument(false);

            DefineImageFileGraphMLAttribute(oGraphMLXmlDocument);
            DefineCustomMenuGraphMLAttributes(oGraphMLXmlDocument);
            oGraphMLXmlDocument.DefineGraphMLAttribute(false, TooltipID,
            "Tooltip", "string", null);
            oGraphMLXmlDocument.DefineGraphMLAttribute(false, "type", "Type", "string", null);

            oGraphMLXmlDocument.DefineGraphMLAttribute(true, "e_type", "Edge Type", "string", null);
            oGraphMLXmlDocument.DefineGraphMLAttribute(true, "e_comment", "Tweet", "string", null);
            oGraphMLXmlDocument.DefineGraphMLAttribute(true, "e_origin", "Feed of Origin", "string", null);
            oGraphMLXmlDocument.DefineGraphMLAttribute(true, "e_timestamp", "Timestamp", "string", null);
            DefineRelationshipGraphMLAttribute(oGraphMLXmlDocument);

            foreach (KeyValuePair<AttributeUtils.Attribute, bool> kvp in attributes)
            {
                if (kvp.Value)
                {
                    if (kvp.Key.value.Equals("hometown_location"))
                    {
                        oGraphMLXmlDocument.DefineGraphMLAttribute(false, "hometown",
                        "Hometown", "string", null);
                        oGraphMLXmlDocument.DefineGraphMLAttribute(false, "hometown_city",
                        "Hometown City", "string", null);
                        oGraphMLXmlDocument.DefineGraphMLAttribute(false, "hometown_state",
                        "Hometown State", "string", null);
                        oGraphMLXmlDocument.DefineGraphMLAttribute(false, "hometown_country",
                        "Hometown Country", "string", null);
                    }
                    else if (kvp.Key.value.Equals("current_location"))
                    {
                        oGraphMLXmlDocument.DefineGraphMLAttribute(false, "location",
                        "Current Location", "string", null);
                        oGraphMLXmlDocument.DefineGraphMLAttribute(false, "location_city",
                        "Current Location City", "string", null);
                        oGraphMLXmlDocument.DefineGraphMLAttribute(false, "location_state",
                        "Current Location State", "string", null);
                        oGraphMLXmlDocument.DefineGraphMLAttribute(false, "location_country",
                        "Current Location Country", "string", null);
                    }
                    else
                    {
                        oGraphMLXmlDocument.DefineGraphMLAttribute(false, kvp.Key.value,
                        kvp.Key.name, "string", null);
                    }
                }
            }

            return (oGraphMLXmlDocument);
        }
*/
        public void MakeTestXml()
        {
            var graph = GenerateNetworkDocument(vertices, edges, graphAttributes);
            if (graph != null)
                graph.Save("OKNetwork_" + egoId + ".graphml");
        }

        public XmlDocument Analyze()
        {
            controller.generateGraph();
            var graph = GenerateNetworkDocument(vertices, edges, graphAttributes);
//  TEMP
            if (graph != null)
                graph.Save("OK_analyze_" + egoId + ".graphml");
//  END TEMP
            return graph;
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
                e.Result = this.Analyze();

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
        //  Method: ExceptionToMessage()
        //
        /// <summary>
        /// Converts an exception to an error message appropriate for a user
        /// interface.
        /// </summary>
        ///
        /// <param name="oException">
        /// The exception that occurred.
        /// </param>
        ///
        /// <returns>
        /// An error message appropriate for a user interface.
        /// </returns>
        //*************************************************************************

        public override string ExceptionToMessage(Exception oException)
        {
            Debug.Assert(oException != null);
            AssertValid();

            string sMessage = null;

            const string TimeoutMessage =
                "The OK Web service didn't respond.";


            if (oException is WebException)
            {
                WebException oWebException = (WebException)oException;

                if (oWebException.Response is HttpWebResponse)
                {
                    HttpWebResponse oHttpWebResponse =
                        (HttpWebResponse)oWebException.Response;

                    switch (oHttpWebResponse.StatusCode)
                    {
                        case HttpStatusCode.RequestTimeout:  // HTTP 408.

                            sMessage = TimeoutMessage;
                            break;

                        default:

                            break;
                    }
                }
                else
                {
                    switch (oWebException.Status)
                    {
                        case WebExceptionStatus.Timeout:

                            sMessage = TimeoutMessage;
                            break;

                        default:

                            break;
                    }
                }
            }

            if (sMessage == null)
            {
                sMessage = ExceptionUtil.GetMessageTrace(oException);
            }

            return (sMessage);
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

        protected class GetNetworkAsyncArgs
        {
            ///
//            public String AccessToken;
            ///
            public AttributesDictionary<bool> Attributes;
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
