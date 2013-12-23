using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Net;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading;
using Smrf.AppLib;
using rcsir.net.common.NetworkAnalyzer;
using rcsir.net.common.Network;
using rcsir.net.vk.importer.api;
using Smrf.NodeXL.GraphDataProviders;
using Smrf.SocialNetworkLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace rcsir.net.vk.importer.NetworkAnalyzer
{
    public class VKNetworkAnalyzer : HttpNetworkAnalyzerBase
    {
        private VKRestApi vkRestApi;

        private static AutoResetEvent readyEvent = new AutoResetEvent(false);

        private bool includeEgo = false; // include ego vertex and edges, should be controled by UI
        private Vertex egoVertex;
        private List<string> friendIds = new List<string>();
        private VertexCollection vertices = new VertexCollection();
        private EdgeCollection edges = new EdgeCollection();

        public VKNetworkAnalyzer()
        {
            vkRestApi = new VKRestApi();
            // set up data handler
            vkRestApi.OnData += new VKRestApi.DataHandler(OnData);
            // set up error handler
            vkRestApi.OnError += new VKRestApi.ErrorHandler(OnError);
        }

        // main data handler
        public void OnData(object vkRestApi, OnDataEventArgs onDataArgs)
        {
            switch (onDataArgs.function)
            {
                case VKFunction.LoadUserInfo:
                    OnLoadUserInfo(onDataArgs.data);
                    break;
                case VKFunction.LoadFriends:
                    OnLoadFriends(onDataArgs.data);
                    break;
                case VKFunction.GetMutual:
                    OnGetMutual(onDataArgs.data, onDataArgs.cookie);
                    break;
                default:
                    Debug.WriteLine("Error, unknown function.");
                    break;
            }

            // indicate that data is ready and we can continue
            readyEvent.Set();
        }

        // main error handler
        public void OnError(object vkRestApi, OnErrorEventArgs onErrorArgs)
        {
            // TODO: notify user about the error
            Debug.WriteLine("Function " + onErrorArgs.function + ", returned error: " + onErrorArgs.error);
            
            // indicate that we can continue
            readyEvent.Set();
        }

        // process load user info response
        private void OnLoadUserInfo(JObject data)
        {
            if (data[VKRestApi.RESPONSE_BODY].Count() > 0)
            {
                JObject ego = data[VKRestApi.RESPONSE_BODY][0].ToObject<JObject>();
                Console.WriteLine("Ego: " + ego.ToString());

                // ok, create the ego object here
                AttributesDictionary<String> attributes = createAttributes(ego);

                this.egoVertex = new Vertex(ego["uid"].ToString(),
                    ego["first_name"].ToString() + " " + ego["last_name"].ToString(),
                    "Ego", attributes);

                // add ego to the vertices
                if(includeEgo)
                {
                    this.vertices.Add(this.egoVertex);
                }
            }
        }

        // process load user friends response
        private void OnLoadFriends(JObject data)
        {
            if (data[VKRestApi.RESPONSE_BODY].Count() > 0)
            {

                for (int i = 0; i < data[VKRestApi.RESPONSE_BODY].Count(); ++i)
                {
                    JObject friend = data[VKRestApi.RESPONSE_BODY][i].ToObject<JObject>();
                    // uid, first_name, last_name, nickname, sex, bdate, city, country, timezone
                    Console.WriteLine(i.ToString() + ") friend: " + friend.ToString());

                    string uid = friend["uid"].ToString();
                    // add user id to the friends list
                    this.friendIds.Add(uid);

                    // add friend vertex
                    AttributesDictionary<String> attributes = createAttributes(friend);

                    this.vertices.Add(new Vertex(uid,
                        friend["first_name"].ToString() + " " + friend["last_name"].ToString(),
                        "Friend", attributes));
                }
            }
        }

        // process get mutual response
        private void OnGetMutual(JObject data, String cookie)
        {
            if (data[VKRestApi.RESPONSE_BODY].Count() > 0)
            {
                List<String> friendFriendsIds = new List<string>();

                for (int i = 0; i < data[VKRestApi.RESPONSE_BODY].Count(); ++i)
                {
                    String friendFriendsId = data[VKRestApi.RESPONSE_BODY][i].ToString();

                    CreateFriendsMutualEdge(cookie, // target id we passed as a param
                                            friendFriendsId,
                                            this.edges,
                                            this.vertices);
                }
            }
        }

        public XmlDocument analyze(String userId, String authToken)
        {
            VKRestContext context = new VKRestContext(userId, authToken);

            vkRestApi.CallVKFunction(VKFunction.LoadUserInfo, context);

            // wait for the user data
            readyEvent.WaitOne();
            context.parameters = "fields=uid,first_name,last_name,nickname,sex,bdate,city,country,education";
            vkRestApi.CallVKFunction(VKFunction.LoadFriends, context);

            // wait for the friends data
            readyEvent.WaitOne();
            foreach (string targetId in this.friendIds)
            {
                StringBuilder sb = new StringBuilder("target_uid=");
                // Append target friend ids
                sb.Append(targetId);

                context.parameters = sb.ToString();
                context.cookie = targetId; // pass target id in the cookie context field
                vkRestApi.CallVKFunction(VKFunction.GetMutual, context);

                // wait for the mutual data
                readyEvent.WaitOne();

                // play nice, sleep for 1/3 sec to stay within 3 requests/second limit
                // TODO: account for time spent in processing
                Thread.Sleep(333);
            }

            if (includeEgo)
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

        private void CreateFriendsMutualEdge(String friendId, String friendFriendsId, EdgeCollection edges, VertexCollection vertices)
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

        public override String
        ExceptionToMessage
        (
            Exception oException
        )
        {
            Debug.Assert(oException != null);
            AssertValid();

            String sMessage = null;

            const String TimeoutMessage =
                "The VK Web service didn't respond.";


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
            public String AccessToken;
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
