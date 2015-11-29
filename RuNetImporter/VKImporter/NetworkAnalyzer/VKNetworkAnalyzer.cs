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
        private VkRestApi vkRestApi;

        private static AutoResetEvent readyEvent = new AutoResetEvent(false);

        private bool includeEgo = false; // include ego vertex and edges, should be controled by UI
        private Vertex<String> egoVertex;
        private List<string> friendIds = new List<string>();
        private VertexCollection<String> vertices = new VertexCollection<String>();
        private EdgeCollection<String> edges = new EdgeCollection<String>();
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
            vkRestApi = new VkRestApi();
            // set up data handler
            vkRestApi.OnData += new VkRestApi.DataHandler(OnData);
            // set up error handler
            vkRestApi.OnError += new VkRestApi.ErrorHandler(OnError);
        }

        // main data handler
        public void OnData(object vkRestApi, OnDataEventArgs onDataArgs)
        {
            switch (onDataArgs.Function)
            {
                case VkFunction.GetProfiles:
                    OnLoadUserInfo(onDataArgs.Data);
                    break;
                case VkFunction.LoadFriends:
                    OnLoadFriends(onDataArgs.Data);
                    break;
                case VkFunction.FriendsGetMutual:
                    OnGetMutual(onDataArgs.Data, onDataArgs.Cookie);
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
        public void OnError(object vkRestApi, VkRestApi.OnErrorEventArgs onErrorArgs)
        {
            // TODO: notify user about the error
            Debug.WriteLine("Function " + onErrorArgs.Function + ", returned error: " + onErrorArgs.Error);

            // this.error = new Exception("Function " + onErrorArgs.function + ", returned error: " + onErrorArgs.error);
            this.requestStatistics.OnUnexpectedException(new Exception("Function " + onErrorArgs.Function + ", returned error: " + onErrorArgs.Error));

            // indicate that we can continue
            readyEvent.Set();
        }

        // process load user info response
        private void OnLoadUserInfo(JObject data)
        {
            if (data[VkRestApi.ResponseBody].Count() > 0)
            {
                JObject ego = data[VkRestApi.ResponseBody][0].ToObject<JObject>();
                Console.WriteLine("Ego: " + ego.ToString());

                // ok, create the ego object here
                AttributesDictionary<String> attributes = createAttributes(ego);

                this.egoVertex = new Vertex<String>(ego["uid"].ToString(),
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
            if (data[VkRestApi.ResponseBody].Count() > 0)
            {

                for (int i = 0; i < data[VkRestApi.ResponseBody].Count(); ++i)
                {
                    JObject friend = data[VkRestApi.ResponseBody][i].ToObject<JObject>();
                    // uid, first_name, last_name, nickname, sex, bdate, city, country, timezone
                    Console.WriteLine(i.ToString() + ") friend: " + friend.ToString());

                    string uid = friend["uid"].ToString();
                    // add user id to the friends list
                    this.friendIds.Add(uid);

                    // add friend vertex
                    AttributesDictionary<String> attributes = createAttributes(friend);

                    this.vertices.Add(new Vertex<String>(uid,
                        friend["first_name"].ToString() + " " + friend["last_name"].ToString(),
                        "Friend", attributes));
                }
            }
        }

        // process get mutual response
        private void OnGetMutual(JObject data, String cookie)
        {
            if (data[VkRestApi.ResponseBody].Count() > 0)
            {
                List<String> friendFriendsIds = new List<string>();

                for (int i = 0; i < data[VkRestApi.ResponseBody].Count(); ++i)
                {
                    String friendFriendsId = data[VkRestApi.ResponseBody][i].ToString();

                    CreateFriendsMutualEdge(cookie, // target id we passed as a param
                                            friendFriendsId);
                }
            }
        }

        public XmlDocument analyze(String userId, String authToken)
        {
            var context = new VkRestApi.VkRestContext(userId, authToken);

            this.requestStatistics = new RequestStatistics();

            vkRestApi.CallVkFunction(VkFunction.GetProfiles, context);

            // wait for the user data
            readyEvent.WaitOne();
            context.Parameters = "fields=uid,first_name,last_name,sex,bdate,photo_50,city,country,relation,nickname";
            vkRestApi.CallVkFunction(VkFunction.LoadFriends, context);

            // wait for the friends data
            readyEvent.WaitOne();
            foreach (string targetId in this.friendIds)
            {
                StringBuilder sb = new StringBuilder("target_uid=");
                // Append target friend ids
                sb.Append(targetId);

                context.Parameters = sb.ToString();
                context.Cookie = targetId; // pass target id in the cookie context field
                vkRestApi.CallVkFunction(VkFunction.FriendsGetMutual, context);

                // wait for the mutual data
                readyEvent.WaitOne();

                // play nice, sleep for 1/3 sec to stay within 3 requests/second limit
                // TODO: account for time spent in processing
                Thread.Sleep(333);
            }

            if (includeEgo)
            {
                CreateIncludeMeEdges();
            }

            // create default attributes (values will be empty)
            AttributesDictionary<String> attributes = new AttributesDictionary<String>(VKAttributes);

            return GenerateNetworkDocument(vertices, edges, attributes);
        }

        private void CreateIncludeMeEdges()
        {
            List<Vertex<String>> friends = vertices.Where(x => x.Type == "Friend").ToList();
            Vertex<String> ego = vertices.FirstOrDefault(x => x.Type == "Ego");

            if (ego != null)
            {
                foreach (Vertex<String> oFriend in friends)
                {
                    edges.Add(new Edge<String>(ego, oFriend, "", "Friend", "", 1));
                }
            }
        }

        private void CreateFriendsMutualEdge(String friendId, String friendFriendsId)
        {
            Vertex<String> friend = vertices.FirstOrDefault(x => x.ID == friendId);
            Vertex<String> friendsFriend = vertices.FirstOrDefault(x => x.ID == friendFriendsId);

            if (friend != null && friendsFriend != null)
            {
                edges.Add(new Edge<String>(friend, friendsFriend, "", "Friend", "", 1));
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

        protected override void AddVertexImageAttribute(XmlNode oVertexXmlNode, Vertex<String> oVertex, GraphMLXmlDocument oGraphMLXmlDocument)
        {
            // add picture
            if (oVertex.Attributes.ContainsKey("photo_50") &&
                oVertex.Attributes["photo_50"] != null)
            {
                oGraphMLXmlDocument.AppendGraphMLAttributeValue(oVertexXmlNode, ImageFileID, oVertex.Attributes["photo_50"].ToString());
            }
        }


        // Network details
        public Vertex<String> GetEgo()
        {
            return this.egoVertex;
        }

        public VertexCollection<String> GetVertices()
        {
            return this.vertices;
        }

        public EdgeCollection<String> GetEdges()
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
                var context = new VkRestApi.VkRestContext(args.userId, args.accessToken);

                // get ego node
                vkRestApi.CallVkFunction(VkFunction.GetProfiles, context);

                // wait for the user data
                readyEvent.WaitOne();

                CheckCancellationPending();
                ReportProgress("Retrieving friends");

                // prepare fields context parameter
                StringBuilder sb = new StringBuilder("fields=");
                sb.Append(args.fields);
                context.Parameters = sb.ToString();

                // get friends node
                vkRestApi.CallVkFunction(VkFunction.LoadFriends, context);

                // wait for the friends data
                readyEvent.WaitOne();

                int total = this.friendIds.Count;
                int current = 0;

                foreach (string targetId in this.friendIds)
                {
                    CheckCancellationPending();
                    current++;
                    ReportProgress("Retrieving friends mutual " + current + " out of " + total);

                    // Append target friend ids
                    sb.Length = 0;
                    sb.Append("target_uid=");
                    sb.Append(targetId);

                    context.Parameters = sb.ToString();
                    context.Cookie = targetId; // pass target id in the context's cookie field
                    
                    // get mutual friends 
                    vkRestApi.CallVkFunction(VkFunction.FriendsGetMutual, context);

                    // wait for the mutual data
                    readyEvent.WaitOne();

                    // play it nice, sleep for 1/3 sec to stay within 3 requests/second limit
                    // TODO: account for time spent in processing
                    Thread.Sleep(333);
                }

                if (includeEgo)
                {
                    CreateIncludeMeEdges();
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
