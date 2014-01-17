using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Net;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading;
using Smrf.XmlLib;
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
        private RequestStatistics requestStatistics;

        private static List<AttributeUtils.Attribute> VKAttributes = new List<AttributeUtils.Attribute>()
        {
            new AttributeUtils.Attribute("Name","name", "friends", false),
            new AttributeUtils.Attribute("First Name","first_name", "friends", true),
            new AttributeUtils.Attribute("Last Name","last_name", "friends", true),
            new AttributeUtils.Attribute("Picture","photo_50", "friends", true),
            new AttributeUtils.Attribute("Sex","sex", "friends", true),
            new AttributeUtils.Attribute("Birth Date","bdate", "friends", true),
            new AttributeUtils.Attribute("Relation","relation", "friends", false),
            new AttributeUtils.Attribute("City","city", "friends", false),
            new AttributeUtils.Attribute("Country","country", "friends", false),

        };


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

            // 
            this.requestStatistics.OnSuccessfulRequest();

            // indicate that data is ready and we can continue
            readyEvent.Set();
        }

        // main error handler
        public void OnError(object vkRestApi, OnErrorEventArgs onErrorArgs)
        {
            // TODO: notify user about the error
            Debug.WriteLine("Function " + onErrorArgs.function + ", returned error: " + onErrorArgs.error);

            // this.error = new Exception("Function " + onErrorArgs.function + ", returned error: " + onErrorArgs.error);
            this.requestStatistics.OnUnexpectedException(new Exception("Function " + onErrorArgs.function + ", returned error: " + onErrorArgs.error));

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

            this.requestStatistics = new RequestStatistics();

            vkRestApi.CallVKFunction(VKFunction.LoadUserInfo, context);

            // wait for the user data
            readyEvent.WaitOne();
            context.parameters = "fields=uid,first_name,last_name,sex,bdate,photo_50,city,country,relation,nickname";
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
            AttributesDictionary<String> attributes = new AttributesDictionary<String>(VKAttributes);

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


        public override List<AttributeUtils.Attribute> GetDefaultNetworkAttributes()
        {
            return VKAttributes;
        }

        private AttributesDictionary<String> createAttributes(JObject obj)
        {
            AttributesDictionary<String> attributes = new AttributesDictionary<String>(VKAttributes);
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

        protected override void AddVertexImageAttribute(XmlNode oVertexXmlNode, Vertex oVertex, GraphMLXmlDocument oGraphMLXmlDocument)
        {
            // add picture
            if (oVertex.Attributes.ContainsKey("photo_50") &&
                oVertex.Attributes["photo_50"] != null)
            {
                oGraphMLXmlDocument.AppendGraphMLAttributeValue(oVertexXmlNode, ImageFileID, oVertex.Attributes["photo_50"].ToString());
            }
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
            BackgroundWorker bw = sender as BackgroundWorker;

            Debug.Assert(e.Argument is NetworkAsyncArgs);
            NetworkAsyncArgs args = e.Argument as NetworkAsyncArgs;

            this.requestStatistics = new RequestStatistics();

            try
            {
                CheckCancellationPending();
                ReportProgress("Starting");

                // shell include ego node in the graph
                this.includeEgo = args.includeMe;

                // prepare rest contexts
                VKRestContext context = new VKRestContext(args.userId, args.accessToken);

                // get ego node
                vkRestApi.CallVKFunction(VKFunction.LoadUserInfo, context);

                // wait for the user data
                readyEvent.WaitOne();

                CheckCancellationPending();
                ReportProgress("Retrieving friends");

                // prepare fields context parameter
                StringBuilder sb = new StringBuilder("fields=");
                sb.Append(args.fields);
                context.parameters = sb.ToString();

                // get friends node
                vkRestApi.CallVKFunction(VKFunction.LoadFriends, context);

                // wait for the friends data
                readyEvent.WaitOne();

                int total = this.friendIds.Count;
                int current = 0;

                foreach (string targetId in this.friendIds)
                {
                    CheckCancellationPending();
                    current++;
                    ReportProgress("Retrieving friends mutual " + current.ToString() + " out of " + total.ToString());

                    // Append target friend ids
                    sb.Length = 0;
                    sb.Append("target_uid=");
                    sb.Append(targetId);

                    context.parameters = sb.ToString();
                    context.cookie = targetId; // pass target id in the context's cookie field
                    
                    // get mutual friends 
                    vkRestApi.CallVKFunction(VKFunction.GetMutual, context);

                    // wait for the mutual data
                    readyEvent.WaitOne();

                    // play it nice, sleep for 1/3 sec to stay within 3 requests/second limit
                    // TODO: account for time spent in processing
                    Thread.Sleep(333);
                }

                if (includeEgo)
                {
                    CreateIncludeMeEdges(edges, vertices);
                }

                CheckCancellationPending();
                ReportProgress("Building network graph document");

                // create default attributes (values will be empty)
                AttributesDictionary<String> attributes = new AttributesDictionary<String>(VKAttributes);

                // build the file
                XmlDocument graph = GenerateNetworkDocument(vertices, edges, attributes);

                if (this.requestStatistics.UnexpectedExceptions > 0)
                {
                    // there was errors - pop up partial network dialog
                    throw new PartialNetworkException(graph, this.requestStatistics);
                }
                else
                {
                    // all good
                    e.Result = graph;
                }

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
        //  Embedded class: NetworkAsyncArgs
        //
        /// <summary>
        /// Contains the arguments needed to asynchronously get a network of VK users.
        /// </summary>
        //*************************************************************************

        public class NetworkAsyncArgs
        {
            ///
            public String accessToken;

            public String userId;
            ///
            public List<AttributeUtils.Attribute> attributes;

            public String fields;

            ///
            public bool includeMe;
        };
    }
}
