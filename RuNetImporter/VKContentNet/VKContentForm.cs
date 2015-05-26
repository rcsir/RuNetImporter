using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Xml;
using Newtonsoft.Json.Linq;
using rcsir.net.vk.content.NetworkAnalyzer;
using rcsir.net.vk.importer.Dialogs;
using rcsir.net.vk.importer.api;
using rcsir.net.vk.importer.api.entity;
using rcsir.net.vk.content.Dialogs;
using Smrf.XmlLib;
using Group = rcsir.net.vk.importer.api.entity.Group;

namespace VKContentNet
{
// ReSharper disable once InconsistentNaming
    public partial class VKContentForm : Form
    {
        private const int POSTS_PER_REQUEST = 100;
        private const int MEMBERS_PER_REQUEST = 1000;
        private const int LIKES_PER_REQUEST = 1000;
        private const string PROFILE_FIELDS = "first_name,last_name,screen_name,bdate,city,country,photo_50,sex,relation,status,education";
        private const string GROUP_FIELDS = "members_count,city,country,description,status";
        private const int MAX_EMPTY_RETRY = 10;

        private readonly VKLoginDialog vkLoginDialog;
        private readonly VkRestApi vkRestApi;
        private String userId;
        private String authToken;

        private decimal groupId;
        private bool isGroup;
        private bool isWorkingFolderSet;
        private bool isAuthorized;
        private volatile bool isRunning;
        readonly private static AutoResetEvent ReadyEvent = new AutoResetEvent(false);

        // group posts date range
        DateTime postsFromDate;
        DateTime postsToDate;

        // progress
        private long totalCount;
        private long currentOffset;
        private int step;
        private int totalEmptyRetry;

        // group's working collections
        private readonly List<long> postsWithComments = new List<long>();
        private readonly List<Like> likes = new List<Like>();
        private readonly Dictionary<long, Poster> posters = new Dictionary<long, Poster>();
        private readonly HashSet<long> posterIds = new HashSet<long>();
        private readonly List<long> visitorIds = new List<long>();
        private readonly List<BoardTopic> topics = new List<BoardTopic>();

        // group's U2U collections
        private class PostCount
        {
            public PostCount(int likes, int comments, int reply)
            {
                Likes = likes;
                Comments = comments;
                Replies = reply;
            }

            public void Increment(int like, int comment, int reply)
            {
                Likes += like;
                Comments += comment;
                Replies += reply;
            }

            public int Likes { get; private set; }
            public int Comments { get; private set; }
            public int Replies { get; private set; }
        };

        readonly Dictionary<long, PostInfo> postInfo = new Dictionary<long, PostInfo>();
        readonly Dictionary<long, PostInfo> boardPostInfo = new Dictionary<long, PostInfo>();
        readonly Dictionary<long, Dictionary<long, PostCount>> u2uMatrix = new Dictionary<long, Dictionary<long, PostCount>>();
        
        // document
        private StreamWriter groupPostsWriter;
        private StreamWriter groupVisitorsWriter;
        private StreamWriter groupCommentsWriter;
        private StreamWriter groupBoardTopicsWriter;
        private StreamWriter groupBoardCommentsWriter;
        private StreamWriter errorLogWriter;

        // Network Analyzer document
        private ContentNetworkAnalyzer contentNetworkAnalyzer;

        // reply to pattern regex 
        const string replyToPattern = @"\[id(\d+)[:\|]";
        Regex replyToRegex = new Regex(replyToPattern, RegexOptions.IgnoreCase);

        private class PostInfo
        {
            public PostInfo(long id, long uid)
            {
                this.id = id;
                this.user_id = uid;
            }

            public long id { get; set; }
            public long user_id { get; set; }
        };

        // poster info 
        private class Poster
        {
            public Poster()
            {
                posts = 0;
                rec_comments = 0;
                comments = 0;
                rec_likes = 0;
                likes = 0;
                friends = 0;
                board_comments = 0;
            }

            public long posts { get; set; }
            public long rec_comments { get; set; }
            public long comments { get; set; }
            public long rec_likes { get; set; }
            public long likes { get; set; }
            public long friends { get; set; }
            public long board_comments { get; set; }
        };

        private class GroupPostsParam
        {
            public GroupPostsParam(decimal gid,
                DateTime from, DateTime to, Boolean justStats)
            {
                this.gid = gid;
                this.from = from;
                this.to = to;
                this.justStats = justStats;
            }

            public decimal gid { get; set; }
            public DateTime from { get; set; }
            public DateTime to { get; set; }
            public Boolean justStats { get; set; }
        };


        private class GraphParam
        {
            public enum GraphType
            {
                Comments = 1,
                Likes = 2,
                Reply = 3,
                Combined = 4
            };

            public GraphParam()
            {
                this.Type = GraphType.Combined;
            }

            public GraphParam(GraphType t)
            {
                this.Type = t;
            }

            public GraphType Type { get; private set; }

        };

        public VKContentForm()
        {
            InitializeComponent();
            this.userIdTextBox.Text = "Please authorize";
            this.groupId = 0;
            this.isAuthorized = false;
            this.isWorkingFolderSet = false;

            vkLoginDialog = new VKLoginDialog();
            // subscribe for login events
            vkLoginDialog.OnUserLogin += new VKLoginDialog.UserLoginHandler(OnUserLogin);

            vkRestApi = new VkRestApi();
            // set up data handler
            vkRestApi.OnData += new VkRestApi.DataHandler(OnData);
            // set up error handler
            vkRestApi.OnError += new VkRestApi.ErrorHandler(OnError);

            // setup background group posts worker handlers
            this.backgroundGroupsWorker.DoWork
                += new DoWorkEventHandler(GroupsWork);

            this.backgroundGroupsWorker.ProgressChanged
                += new ProgressChangedEventHandler(groupsWorkProgressChanged);

            this.backgroundGroupsWorker.RunWorkerCompleted
                += new RunWorkerCompletedEventHandler(groupsWorkCompleted);

            // setup background communication network worker handlers
            this.backgroundNetWorker.DoWork
                += new DoWorkEventHandler(netWork);

            this.backgroundNetWorker.ProgressChanged
                += new ProgressChangedEventHandler(netWorkProgressChanged);

            this.backgroundNetWorker.RunWorkerCompleted
                += new RunWorkerCompletedEventHandler(netWorkCompleted);

            ActivateControls();
        }

        private void OnUserLogin(object loginDialog, UserLoginEventArgs loginArgs)
        {
            Debug.WriteLine("User Logged In: " + loginArgs.ToString());

            this.userId = loginArgs.userId;
            this.authToken = loginArgs.authToken;
            //this.expiresAt = loginArgs.expiersIn; // todo: calc expiration time

            isAuthorized = true;

            this.userIdTextBox.Clear();
            this.userIdTextBox.Text = "Authorized " + loginArgs.userId;

            this.ActivateControls();
        }

        private void OnData(object restApi, OnDataEventArgs onDataArgs)
        {
            switch (onDataArgs.Function)
            {
                case VkFunction.WallGet:
                    OnWallGet(onDataArgs.Data);
                    break;
                case VkFunction.WallGetComments:
                    OnWallGetComments(onDataArgs.Data, onDataArgs.Cookie);
                    break;
                case VkFunction.GroupsGetById:
                    OnGroupsGetById(onDataArgs.Data, onDataArgs.Cookie);
                    break;
                case VkFunction.GetProfiles:
                    //OnLoadUserInfo(onDataArgs.data);
                    break;
                case VkFunction.LikesGetList:
                    OnLikesGetList(onDataArgs.Data, onDataArgs.Cookie);
                    break;
                case VkFunction.UsersGet:
                    OnUsersGet(onDataArgs.Data);
                    break;
                case VkFunction.BoardGetTopics:
                    OnBoardGetTopics(onDataArgs.Data, onDataArgs.Cookie);
                    break;
                case VkFunction.BoardGetComments:
                    OnBoardGetComments(onDataArgs.Data, onDataArgs.Cookie);
                    break;
                case VkFunction.PhotosSearch:
                    // OnPhotosSearch(onDataArgs.Data, onDataArgs.Cookie);
                    break;
                case VkFunction.StatsGet:
                    //OnStatsGet(onDataArgs.data);
                    break;
                default:
                    Debug.WriteLine("Error, unknown function.");
                    break;
            }

            // indicate that data is ready and we can continue
            ReadyEvent.Set();
        }

        // main error handler
        private void OnError(object restApi, VkRestApi.OnErrorEventArgs onErrorArgs)
        {
            // TODO: notify user about the error
            Debug.WriteLine("Function " + onErrorArgs.Function + ", returned error: " + onErrorArgs.Details);

            if (errorLogWriter != null)
            {
                Utils.PrintFileContent(errorLogWriter, onErrorArgs);
            }

            /*
             * till we can distinguish between critical errors and warnings, just print errors in ths log file
            MessageBox.Show(onErrorArgs.error,
                onErrorArgs.function.ToString() + " Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            */
            // indicate that data is ready and we can continue
            ReadyEvent.Set();
        }

        private void ActivateControls()
        {
            if (isAuthorized)
            {
                // enable user controls
                if (isWorkingFolderSet)
                {
                    bool shouldActivate = this.groupId != 0;
                    bool isBusy = this.isRunning;
                    
                    this.FindGroupsButton.Enabled = true && !isBusy;

                    // activate group buttons
                    this.DownloadGroupPosts.Enabled = shouldActivate && !isBusy;
                    this.GenerateCommunicatinoNetwork.Enabled = shouldActivate && !isBusy && this.u2uMatrix.Count > 0;
                    this.CancelOperation.Enabled = isBusy; // todo: activate only when running
                }
            }
            else
            {
                // disable user controls
                this.FindGroupsButton.Enabled = false;
                this.DownloadGroupPosts.Enabled = false;
                this.GenerateCommunicatinoNetwork.Enabled = false;
                this.CancelOperation.Enabled = false;
            }
        }

        private void VKContentForm_Load(object sender, EventArgs e)
        {

        }

        private void AuthorizeButton_Click(object sender, EventArgs e)
        {
            // If true - will delete cookies and relogin, use false for dev. only !!!
            vkLoginDialog.Login("friends", false); // default permission - friends
        }

        private void WorkingFolderButton_Click(object sender, EventArgs e)
        {
            // Show the FolderBrowserDialog.
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                this.WorkingFolderTextBox.Text = folderBrowserDialog1.SelectedPath;
                isWorkingFolderSet = true;

                // if error log exists - close it and create new one
                if (errorLogWriter != null)
                {
                    errorLogWriter.Close();
                    errorLogWriter = null;
                }

                IEntity temp = new VkRestApi.OnErrorEventArgs();
                String fileName = Utils.GenerateFileName(this.WorkingFolderTextBox.Text, 0, temp, "", "log");
                errorLogWriter = File.CreateText(fileName);
                errorLogWriter.AutoFlush = true;
                Utils.PrintFileContent(errorLogWriter, temp);

                ActivateControls();
            }

        }

        private void FindGroupsButton_Click(object sender, EventArgs e)
        {
            var groupsDialog = new FindGroupsDialog();

            if (groupsDialog.ShowDialog() == DialogResult.OK)
            {
                //SearchParameters searchParameters = groupsDialog.searchParameters;
                //this.backgroundFinderWorker.RunWorkerAsync(searchParameters);
                decimal gid = groupsDialog.groupId;
                isGroup = groupsDialog.isGroup;

                if (isGroup)
                {
                    // lookup a group by id
                    var context = new VkRestApi.VkRestContext(this.userId, this.authToken);
                    var sb = new StringBuilder();
                    sb.Append("group_id=").Append(gid).Append("&");
                    sb.Append("fields=").Append(GROUP_FIELDS).Append("&");
                    context.Parameters = sb.ToString();
                    context.Cookie = groupsDialog.groupId.ToString();
                    Debug.WriteLine("Download parameters: " + context.Parameters);

                    // call VK REST API
                    vkRestApi.CallVkFunction(VkFunction.GroupsGetById, context);

                    // wait for the user data
                    ReadyEvent.WaitOne();
                }
                else
                {
                    var context = new VkRestApi.VkRestContext(gid.ToString(), this.authToken);
                    vkRestApi.CallVkFunction(VkFunction.GetProfiles, context);
                    ReadyEvent.WaitOne();
                }
            }
            else
            {
                Debug.WriteLine("Search canceled");
            }
        }

        private void OnGroupsGetById(JObject data, String cookie)
        {
            String gId = cookie; // gropu id sent as a cooky

            if (data[VkRestApi.RESPONSE_BODY] == null ||
                data[VkRestApi.RESPONSE_BODY].Count() == 0)
            {
                this.groupId = 0;
                this.groupId2.Text = gId;
                this.groupDescription.Text = "Not found";
                Debug.WriteLine("Group is not found");
                return;
            }

            // process response body
            var groupObject = data[VkRestApi.RESPONSE_BODY][0].ToObject<JObject>();
            if (groupObject == null)
            {
                this.groupId = 0;
                this.groupId2.Text = gId;
                this.groupDescription.Text = "Group read error";
                Debug.WriteLine("Group object is empty");
                return;
            }

            var g = new Group
            {
                id = Utils.getLongField("id", groupObject),
                name = Utils.getStringField("name", groupObject),
                screen_name = Utils.getStringField("screen_name", groupObject),
                is_closed = Utils.getStringField("is_closed", groupObject),
                type = Utils.getStringField("type", groupObject),
                members_count = Utils.getStringField("members_count", groupObject),
                city = Utils.getStringField("city", "title", groupObject),
                country = Utils.getStringField("country", "title", groupObject),
                photo = Utils.getStringField("photo_50", groupObject),
                description = Utils.getTextField("description", groupObject),
                status = Utils.getTextField("status", groupObject)
            };

            // update group id and group info
            this.groupId = g.id > 0 ? Decimal.Negate(g.id) : g.id;// group id is negative number

            // create group members network document
            this.contentNetworkAnalyzer = new ContentNetworkAnalyzer();

            // add group as a vertex to the document (some posts come from group only)
            this.contentNetworkAnalyzer.addVertex((long)this.groupId,
                g.name, "User", groupObject);

            String fileName = Utils.GenerateFileName(this.WorkingFolderTextBox.Text, groupId, g);
            StreamWriter writer = File.CreateText(fileName);
            Utils.PrintFileHeader(writer, g);
            Utils.PrintFileContent(writer, g);
            writer.Close();

            this.groupId2.Text = g.id.ToString();
            this.groupDescription.Text = g.name;
            this.groupDescription.AppendText("\r\n type: " + g.type);
            this.groupDescription.AppendText("\r\n members: " + g.members_count);
            this.groupDescription.AppendText("\r\n " + g.description);

            ActivateControls();

            ReadyEvent.Set();
        }
        

        private void DownloadGroupPosts_Click(object sender, EventArgs e)
        {
            var postsDialog = new DownloadGroupPostsDialog {groupId = Math.Abs(this.groupId), isGroup = this.isGroup};

            if (postsDialog.ShowDialog() == DialogResult.OK)
            {
                updateStatus(-1, "Start");
                var param = new GroupPostsParam(this.groupId,
                    postsDialog.fromDate, postsDialog.toDate, postsDialog.justGroupStats);

                isRunning = true;
                this.backgroundGroupsWorker.RunWorkerAsync(param);
                ActivateControls();
            }
            else
            {
                Debug.WriteLine("Download posts canceled");
            }
        }

        private void GenerateCommunicatinoNetwork_Click(object sender, EventArgs e)
        {
            var communicationDialog = new GenerateCommunicationNetworkDialog();

            if (communicationDialog.ShowDialog() == DialogResult.OK)
            {
                updateStatus(-1, "Start");
                var param = new GraphParam((GraphParam.GraphType)communicationDialog.type);

                isRunning = true;
                this.backgroundNetWorker.RunWorkerAsync(param);
                ActivateControls();
            }
            else
            {
                Debug.WriteLine("Download posts canceled");
            }
        }

        private void CancelJobButton_Click(object sender, EventArgs e)
        {
            if (isRunning)
                this.backgroundGroupsWorker.CancelAsync();
        }

        // Async workers
        private void GroupsWork(Object sender, DoWorkEventArgs args)
        {
            // Do not access the form's BackgroundWorker reference directly. 
            // Instead, use the reference provided by the sender parameter.
            var bw = sender as BackgroundWorker;

            // Extract the argument.
            var param = args.Argument as GroupPostsParam;

            if (bw == null ||
                param == null)
            {
                throw new ArgumentException("Illegal argumets for Group Work");
            }

            if (param.from <= param.to)
            {
                this.postsFromDate = param.from;
                this.postsToDate = param.to;
            }
            else
            {
                this.postsFromDate = param.to;
                this.postsToDate = param.from;
            }

            // working directory
            String workingDir = this.WorkingFolderTextBox.Text;

            // clear all
            postsWithComments.Clear(); // reset reference list
            likes.Clear(); // reset likes
            posters.Clear(); // reset posters
            posterIds.Clear(); // clear poster ids
            visitorIds.Clear(); // clear visitor ids
            postInfo.Clear(); // clear post infos
            boardPostInfo.Clear(); // reset boards post infos
            topics.Clear(); // clear topics infos
            u2uMatrix.Clear(); // clear u2u matrix

            var context = new VkRestApi.VkRestContext(this.userId, this.authToken);
            var sb = new StringBuilder();
            long timeLastCall = 0;

            // gather group statistics
            if (param.justStats)
            {
                ResetCountersAndGetReady();

                bw.ReportProgress(1, "Getting group stats");

                sb.Length = 0;
                // TODO check if it takes negative number
                sb.Append("group_id=").Append(groupId).Append("&");
                sb.Append("date_from=").Append(this.postsFromDate.ToString("yyyy-MM-dd")).Append("&");
                sb.Append("date_to=").Append(this.postsToDate.ToString("yyyy-MM-dd"));
                context.Parameters = sb.ToString();
                Debug.WriteLine("Download parameters: " + context.Parameters);

                // call VK REST API
                vkRestApi.CallVkFunction(VkFunction.StatsGet, context);
                return;
            }

            // group posts
            IEntity e = new Post();
            String fileName = Utils.GenerateFileName(workingDir, groupId, e);
            groupPostsWriter = File.CreateText(fileName);
            Utils.PrintFileHeader(groupPostsWriter, e);

            ResetCountersAndGetReady();

            // request group posts
            bw.ReportProgress(-1, "Getting posts");
            while (ShellContinue(bw))
            {
                sb.Length = 0;
                sb.Append("owner_id=").Append(groupId.ToString()).Append("&");
                sb.Append("offset=").Append(currentOffset).Append("&");
                sb.Append("count=").Append(POSTS_PER_REQUEST).Append("&");
                context.Parameters = sb.ToString();
                Debug.WriteLine("Download parameters: " + context.Parameters);

                context.Cookie = currentOffset.ToString();

                // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                timeLastCall = Utils.sleepTime(timeLastCall);
                // call VK REST API
                vkRestApi.CallVkFunction(VkFunction.WallGet, context);

                // wait for the user data
                ReadyEvent.WaitOne();
                bw.ReportProgress(step, "Getting " + currentOffset + " posts out of " + totalCount);
            }

            groupPostsWriter.Close();

            if (postsWithComments.Count > 0 &&
                !bw.CancellationPending)
            {
                // group comments
                e = new Comment();
                fileName = Utils.GenerateFileName(workingDir, groupId, e);
                groupCommentsWriter = File.CreateText(fileName);
                Utils.PrintFileHeader(groupCommentsWriter, e);

                // request group comments
                bw.ReportProgress(-1, "Getting comments");

                timeLastCall = 0;

                for (int i = 0; i < this.postsWithComments.Count && !bw.CancellationPending; i++)
                {
                    ResetCountersAndGetReady();
                    this.step = (int)(10000 / this.postsWithComments.Count);

                    bw.ReportProgress(step,
                        "Getting " + (i + 1) + " post comments out of " + this.postsWithComments.Count);

                    while (ShellContinue(bw))
                    {
                        sb.Length = 0;
                        sb.Append("owner_id=").Append(groupId).Append("&"); // group id
                        sb.Append("post_id=").Append(postsWithComments[i]).Append("&"); // post id
                        sb.Append("need_likes=").Append(1).Append("&"); // request likes info
                        sb.Append("offset=").Append(currentOffset).Append("&");
                        sb.Append("count=").Append(POSTS_PER_REQUEST).Append("&");
                        context.Parameters = sb.ToString();
                        context.Cookie = postsWithComments[i].ToString(); // pass post id as a cookie
                        Debug.WriteLine("Request parameters: " + context.Parameters);

                        // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                        timeLastCall = Utils.sleepTime(timeLastCall);
                        // call VK REST API
                        vkRestApi.CallVkFunction(VkFunction.WallGetComments, context);

                        // wait for the user data
                        ReadyEvent.WaitOne();
                    }
                }

                groupCommentsWriter.Close();
            }

            // process the likers - they will be added to the posters
            if (likes.Count > 0 &&
                !bw.CancellationPending)
            {
                // request likers ids
                bw.ReportProgress(-1, "Getting likers");
                this.step = (int) (10000/this.likes.Count);

                timeLastCall = 0;

                for (int i = 0; i < this.likes.Count; i++)
                {
                    isRunning = true;

                    bw.ReportProgress(step, "Getting " + (i + 1) + " likes out of " + this.likes.Count);

                    if (bw.CancellationPending)
                        break;

                    sb.Length = 0;
                    sb.Append("type=").Append(likes[i].type).Append("&"); // type
                    sb.Append("owner_id=").Append(likes[i].owner_id).Append("&"); // group id
                    sb.Append("item_id=").Append(likes[i].item_id).Append("&"); // post id
                    sb.Append("count=").Append(LIKES_PER_REQUEST).Append("&");
                    context.Parameters = sb.ToString();
                    context.Cookie = likes[i].item_id.ToString(); // pass post/comment id as a cookie
                    Debug.WriteLine("Request parameters: " + context.Parameters);

                    // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                    timeLastCall = Utils.sleepTime(timeLastCall);
                    // call VK REST API
                    vkRestApi.CallVkFunction(VkFunction.LikesGetList, context);

                    // wait for the user data
                    ReadyEvent.WaitOne();
                }
            }


            // process group board topics and comments
            e = new BoardTopic();
            fileName = Utils.GenerateFileName(workingDir, groupId, e);
            groupBoardTopicsWriter = File.CreateText(fileName);
            Utils.PrintFileHeader(groupBoardTopicsWriter, e);

            e = new Comment();
            fileName = Utils.GenerateFileName(workingDir, groupId, e, "board");
            groupBoardCommentsWriter = File.CreateText(fileName);
            Utils.PrintFileHeader(groupBoardCommentsWriter, e);

            ResetCountersAndGetReady();

            bw.ReportProgress(-1, "Getting board topics");

            // get group board topics
            while (ShellContinue(bw))
            {
                sb.Length = 0;
                sb.Append("group_id=").Append(Math.Abs(groupId)).Append("&");
                sb.Append("offset=").Append(currentOffset).Append("&");
                sb.Append("count=").Append(POSTS_PER_REQUEST).Append("&");
                context.Parameters = sb.ToString();
                Debug.WriteLine("Download parameters: " + context.Parameters);

                context.Cookie = currentOffset.ToString();

                // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                timeLastCall = Utils.sleepTime(timeLastCall);
                // call VK REST API
                vkRestApi.CallVkFunction(VkFunction.BoardGetTopics, context);

                // wait for the user data
                ReadyEvent.WaitOne();
                bw.ReportProgress(step, "Getting board topics " + currentOffset + " out of " + totalCount);
            }

            bw.ReportProgress(-1, "Getting board comments");

            // collect comments from all board topics
            for (int i = 0; i < topics.Count; i++)
            {
                if (bw.CancellationPending)
                    break; // canceled

                if (topics[i].is_closed || topics[i].comments == 0)
                    continue; // empty or closed topic - ignore

                ResetCountersAndGetReady();

                while (ShellContinue(bw))
                {
                    sb.Length = 0;
                    sb.Append("group_id=").Append(Math.Abs(groupId)).Append("&"); // group id
                    sb.Append("topic_id=").Append(topics[i].id).Append("&"); // post id
                    sb.Append("offset=").Append(currentOffset).Append("&");
                    sb.Append("count=").Append(POSTS_PER_REQUEST).Append("&");
                    context.Parameters = sb.ToString();
                    context.Cookie = topics[i].id.ToString(); // pass topic id as a cookie
                    Debug.WriteLine("Request parameters: " + context.Parameters);

                    // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                    timeLastCall = Utils.sleepTime(timeLastCall);
                    // call VK REST API
                    vkRestApi.CallVkFunction(VkFunction.BoardGetComments, context);

                    // wait for the user data
                    ReadyEvent.WaitOne();
                    bw.ReportProgress(i*10000/topics.Count, "Getting " + currentOffset +" comments out of " + totalCount +" for board " + i + " out of " + topics.Count );
                }
            }

            groupBoardTopicsWriter.Close();
            groupBoardCommentsWriter.Close();

            // now collect info about posters (users or visitors who left post, comment or like) 
            visitorIds.AddRange(posterIds);

            if (visitorIds.Count > 0 &&
                !bw.CancellationPending)
            {
                // group visitors profiles
                e = new Profile();
                fileName = Utils.GenerateFileName(workingDir, groupId, e);
                groupVisitorsWriter = File.CreateText(fileName);
                Utils.PrintFileHeader(groupVisitorsWriter, e);

                // request visitors info
                bw.ReportProgress(-1, "Getting visitors");
                this.step = (int) (10000 * 100 / visitorIds.Count);

                timeLastCall = 0;

                for (int i = 0; i < visitorIds.Count; i += 100)
                {
                    isRunning = true;

                    bw.ReportProgress(step, "Getting " + (i + 1) + " visitors out of " + visitorIds.Count);

                    if (bw.CancellationPending)
                        break;

                    sb.Length = 0;

                    sb.Append("user_ids=");

                    for (int j = i; j < visitorIds.Count && j < i + 100; ++j)
                    {
                        sb.Append(visitorIds[j]).Append(","); // users
                    }

                    sb.Append("&").Append("fields=").Append(PROFILE_FIELDS);

                    context.Parameters = sb.ToString();
                    Debug.WriteLine("Request parameters: " + context.Parameters);

                    // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                    timeLastCall = Utils.sleepTime(timeLastCall);
                    // call VK REST API
                    vkRestApi.CallVkFunction(VkFunction.UsersGet, context);

                    // wait for the user data
                    ReadyEvent.WaitOne();
                }

                groupVisitorsWriter.Close();
            }


            // update group posts/likes count (posted from group id)
            Poster groupPoster;
            if (posters.TryGetValue((long)this.groupId, out groupPoster))
            {
                Dictionary<String, String> attr = dictionaryFromPoster(groupPoster);
                // update poster vertex attributes
                this.contentNetworkAnalyzer.updateVertexAttributes((long)this.groupId, attr);
            }


            // If the operation was canceled by the user,  
            // set the DoWorkEventArgs.Cancel property to true. 
            args.Cancel = bw.CancellationPending;

            // complete the job
            //args.Result = 
        }

        // before new batch task
        private void ResetCountersAndGetReady()
        {
            isRunning = true;
            step = 1;
            totalCount = 0;
            currentOffset = 0;
        }

        // decide if running task shell continue to run
        private bool ShellContinue(BackgroundWorker bw)
        {
            if (!isRunning)
                return false;

            if (bw.CancellationPending)
                return false;

            return currentOffset < totalCount || totalCount <= 0;
        }

        private void groupsWorkProgressChanged(Object sender, ProgressChangedEventArgs args)
        {
            var status = args.UserState as String;
            var progress = args.ProgressPercentage;
            updateStatus(progress, status);
        }

        private void groupsWorkCompleted(object sender, RunWorkerCompletedEventArgs args)
        {
            isRunning = false;

            if (args.Error != null)
            {
                MessageBox.Show(args.Error.Message);
                updateStatus(0, "Error");
            }
            else if (args.Cancelled)
            {
                MessageBox.Show("Work canceled!");
                updateStatus(0, "Canceled");
            }
            else
            {
                //var network = args.Result as XmlDocument;
                //MessageBox.Show("Group posts download complete!");
                updateStatus(10000, "Done");
            }

            ActivateControls();
        }

        // Async Communication Network Worker
        private void netWork(Object sender, DoWorkEventArgs args)
        {
            // Do not access the form's BackgroundWorker reference directly. 
            // Instead, use the reference provided by the sender parameter.
            var bw = sender as BackgroundWorker;

            // Extract the argument.
            var param = args.Argument as GraphParam;

            if (bw == null ||
                param == null)
            {
                throw new ArgumentException("Invalid network job arguments");
            }

            ResetCountersAndGetReady();
            contentNetworkAnalyzer.ResetEdges(); // remove all edges


            var uTotal = u2uMatrix.Count;
            var uCount = 0;

            if (uTotal == 0)
            {
                args.Result = null;
                return;
            }

            // report progress up to 50%, the other 50% - graph generation
            this.step = (int)(5000 / uTotal);

            // generate U2U edges
            foreach (var entry in u2uMatrix)
            {
                bw.ReportProgress(step, "Processing matrix entry " + (++uCount) + " out of " + uTotal);

                var from = entry.Key;
                foreach (var entry2 in entry.Value)
                {
                    var to = entry2.Key;
                    var weight = entry2.Value;
                    switch (param.Type)
                    {
                        case GraphParam.GraphType.Combined:
                            if (weight.Comments > 0)
                            {
                                contentNetworkAnalyzer.AddEdge(from, to, "Link", "Comment", "", weight.Comments, 0);                                
                            }

                            if (weight.Likes > 0)
                            {
                                contentNetworkAnalyzer.AddEdge(from, to, "Link", "Like", "", weight.Likes, 0);
                            }

                            if (weight.Replies > 0)
                            {
                                contentNetworkAnalyzer.AddEdge(from, to, "Link", "Reply", "", weight.Replies, 0);
                            }
                            break;
                        case GraphParam.GraphType.Comments:
                            if (weight.Comments > 0)
                            {
                                contentNetworkAnalyzer.AddEdge(from, to, "Link", "Comment", "", weight.Comments, 0);                                
                            }
                            break;
                        case GraphParam.GraphType.Likes:
                            if (weight.Likes > 0)
                            {
                                contentNetworkAnalyzer.AddEdge(from, to, "Link", "Like", "", weight.Likes, 0);                                
                            }
                            break;
                        case GraphParam.GraphType.Reply:
                            if (weight.Replies > 0)
                            {
                                contentNetworkAnalyzer.AddEdge(from, to, "Link", "Reply", "", weight.Replies, 0);
                            }
                            break;
                    }
                }
            }

            // generate U2U network
            contentNetworkAnalyzer.graphName = param.Type.ToString();
            args.Result = contentNetworkAnalyzer.GenerateU2UNetwork();
        }

        private void netWorkProgressChanged(Object sender, ProgressChangedEventArgs args)
        {
            var status = args.UserState as String;
            var progress = args.ProgressPercentage;
            updateStatus(progress, status);
        }

        private void netWorkCompleted(object sender, RunWorkerCompletedEventArgs args)
        {
            isRunning = false;

            if (args.Error != null)
            {
                MessageBox.Show(args.Error.Message);
                updateStatus(0, "Error");
            }
            else if (args.Cancelled)
            {
                MessageBox.Show("Communication Network Generation canceled!");
                updateStatus(0, "Canceled");
            }
            else
            {
                // save network document
                var network = args.Result as XmlDocument;
                if (network != null)
                {
                    updateStatus(0, "Save Network Graph File");
                    network.Save(Utils.GenerateFileName(WorkingFolderTextBox.Text, groupId, contentNetworkAnalyzer.graphName, "user-network", "graphml"));
                }
                else
                {
                    MessageBox.Show("Network document is empty!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                //MessageBox.Show("Network generation complete!");
                updateStatus(10000, "Done");
            }

            ActivateControls();
        }

        private void updateStatus(int progress, String status)
        {
            // if 0 - ignore progress param
            if (progress > 0)
            {
                this.GroupsProgressBar.Increment(progress);
            }
            else if (progress < 0)
            {
                // reset 
                this.GroupsProgressBar.Value = 0;
            }

            this.groupsStripStatusLabel.Text = status;
        }

        // process group posts
        private void OnWallGet(JObject data)
        {
            if (data[VkRestApi.RESPONSE_BODY] == null)
            {
                this.isRunning = false;
                return;
            }

            if (this.totalCount == 0)
            {
                this.totalCount = data[VkRestApi.RESPONSE_BODY]["count"].ToObject<long>();
                if (this.totalCount == 0)
                {
                    this.isRunning = false;
                    return;
                }
                this.step = (int)(10000 * POSTS_PER_REQUEST / this.totalCount);
            }

            // now calc items in response
            int count = data[VkRestApi.RESPONSE_BODY]["items"].Count();

            if (!CheckAndIncrement(count))
                return;

            var posts = new List<Post>();

            // process response body
            for (int i = 0; i < count; ++i)
            {
                var postObj = data[VkRestApi.RESPONSE_BODY]["items"][i].ToObject<JObject>();

                // see if post is in the range
                var dt = Utils.getDateField("date", postObj);

                if (dt < this.postsFromDate ||
                    dt > this.postsToDate)
                {
                    continue;
                }

                var post = new Post();
                post.id = Utils.getLongField("id", postObj);
                post.owner_id = Utils.getLongField("owner_id", postObj);
                post.from_id = Utils.getLongField("from_id", postObj);
                post.signer_id = Utils.getLongField("signer_id", postObj);
                // post date
                post.date = Utils.getStringDateField("date", postObj);
                // post_type 
                post.post_type = Utils.getStringField("post_type", postObj);
                // comments
                post.comments = Utils.getLongField("comments", "count", postObj);
                if (post.comments > 0)
                {
                    this.postsWithComments.Add(post.id); // add post's id to the ref list for comments processing
                }

                // likes
                post.likes = Utils.getLongField("likes", "count", postObj);
                if (post.likes > 0)
                {
                    var like = new Like();
                    like.type = "post";
                    like.owner_id = (long)this.groupId;
                    like.item_id = post.id;
                    this.likes.Add(like);
                }

                // reposts
                post.reposts = Utils.getLongField("reposts", "count", postObj);

                // attachments count
                if (postObj["attachments"] != null)
                {
                    post.attachments = postObj["attachments"].ToArray().Length;
                }

                // post text
                post.text = Utils.getTextField("text", postObj);

                // if post has a signer - update posters with a signer
                if (post.signer_id > 0)
                {
                    if (!posters.ContainsKey(post.signer_id))
                    {
                        posters[post.signer_id] = new Poster();
                    }

                    posters[post.signer_id].posts += 1; // increment number of posts
                    posters[post.signer_id].rec_likes += post.likes;

                    // add to the poster ids
                    posterIds.Add(post.signer_id);
                } 
                else
                {
                    if (!posters.ContainsKey(post.from_id))
                    {
                        posters[post.from_id] = new Poster();
                    }

                    posters[post.from_id].posts += 1; // increment number of posts
                    posters[post.from_id].rec_likes += post.likes;

                    // add to the poster ids if different from the group
                    if(post.from_id != this.groupId)
                        posterIds.Add(post.from_id);
                }

                posts.Add(post);

                // add to the post infos
                if (!postInfo.ContainsKey(post.id))
                {
                    long uid = post.signer_id > 0 ? post.signer_id : post.from_id > 0 ? post.from_id : post.owner_id; 
                    postInfo[post.id] = new PostInfo(post.id, uid);
                }
            }

            // save the posts list
            Utils.PrintFileContent(groupPostsWriter, posts);
        }

        // process group comments
        private void OnWallGetComments(JObject data, String cookie)
        {
            if (data[VkRestApi.RESPONSE_BODY] == null)
            {
                this.isRunning = false;
                return;
            }

            if (this.totalCount == 0)
            {
                this.totalCount = data[VkRestApi.RESPONSE_BODY]["count"].ToObject<long>();
            }

            // now calc items in response
            int count = data[VkRestApi.RESPONSE_BODY]["items"].Count();

            if (!CheckAndIncrement(count))
                return;

            var comments = new List<Comment>();
            var post_id = Convert.ToInt64(cookie); // passed as a cookie

            // process response body
            for (int i = 0; i < count; ++i)
            {
                var postObj = data[VkRestApi.RESPONSE_BODY]["items"][i].ToObject<JObject>();

                var comment = new Comment();
                comment.id = Utils.getLongField("id", postObj);
                comment.post_id = post_id;
                comment.from_id = Utils.getLongField("from_id", postObj);
                // post date
                comment.date = Utils.getStringDateField("date", postObj);

                comment.reply_to_uid = Utils.getLongField("reply_to_uid", postObj);
                comment.reply_to_cid = Utils.getLongField("reply_to_cid", postObj);

                // likes/dislikes
                comment.likes = Utils.getLongField("likes", "count", postObj);
                if (comment.likes > 0)
                {
                    var like = new Like();
                    like.type = "comment";
                    like.owner_id = (long)this.groupId;
                    like.item_id = comment.id;
                    this.likes.Add(like);
                }

                // attachments count
                if (postObj["attachments"] != null)
                {
                    comment.attachments = postObj["attachments"].ToArray().Length;
                }

                // post text
                comment.text = Utils.getTextField("text", postObj);

                // update posters
                if (comment.from_id != this.groupId)
                {
                    if (!posters.ContainsKey(comment.from_id))
                    {
                        posters[comment.from_id] = new Poster();
                    }

                    posters[comment.from_id].comments += 1; // increment number of comments
                    posters[comment.from_id].rec_likes += comment.likes;

                    // add to the poster ids
                    posterIds.Add(comment.from_id);
                }

                comments.Add(comment);

                // add comment to post info
                if (!postInfo.ContainsKey(comment.id))
                {
                    postInfo[comment.id] = new PostInfo(comment.id, comment.from_id);
                }
                
                // update u2u matrix for post
                updateU2UMatrixForPost(comment.from_id, post_id, true);

                if (comment.reply_to_uid > 0)
                {
                    // update direct u2u matrix
                    updateU2UMatrix(comment.from_id, comment.reply_to_uid, true);
                }

                // parse reply to from comment text 
                long to = parseCommentForReplyTo(comment.text);
                if (to > 0 && to != comment.reply_to_uid)
                {
                    updateU2UMatrix(comment.from_id, to, true);
                }
            }

            // save the posts list
            Utils.PrintFileContent(groupCommentsWriter, comments);
        }

        // process likes user list
        private void OnLikesGetList(JObject data, string cookie)
        {
            if (data[VkRestApi.RESPONSE_BODY] == null)
            {
                this.isRunning = false;
                return;
            }

            // now calc items in response
            int count = data[VkRestApi.RESPONSE_BODY]["items"].Count();

            if (!CheckAndIncrement(count))
                return;
            
            long post_id = Convert.ToInt64(cookie); // passed as a cookie

            // process response body
            for (int i = 0; i < count; ++i)
            {
                long likerId = data[VkRestApi.RESPONSE_BODY]["items"][i].ToObject<long>();
                // this user liked the subject - add him to the posters
                if (!posters.ContainsKey(likerId))
                {
                    posters[likerId] = new Poster();
                }

                posters[likerId].likes += 1; // increment poster's likes count

                this.posterIds.Add(likerId);

                // update u2u matrix
                updateU2UMatrixForPost(likerId, post_id, false);
            }
        }

        // process group board topics
        private void OnBoardGetTopics(JObject data, string cookie)
        {
            if (data[VkRestApi.RESPONSE_BODY] == null)
            {
                this.isRunning = false;
                return;
            }

            if (this.totalCount == 0)
            {
                this.totalCount = data[VkRestApi.RESPONSE_BODY]["count"].ToObject<long>();
                if (this.totalCount == 0)
                {
                    this.isRunning = false;
                    return;
                }
                this.step = 1; // (int)(10000 * POSTS_PER_REQUEST / this.totalCount);
            }
            // now calc items in response
            int count = data[VkRestApi.RESPONSE_BODY]["items"].Count();

            if (!CheckAndIncrement(count))
                return;

            // process a cookie

            // process response body
            for (int i = 0; i < count; ++i)
            {
                var obj = data[VkRestApi.RESPONSE_BODY]["items"][i].ToObject<JObject>();

                var topic = new BoardTopic();
                topic.id = Utils.getLongField("id", obj);

                topic.title = Utils.getStringField("title", obj);
                topic.created = Utils.getStringDateField("created", obj);
                topic.created_by = Utils.getLongField("created_by", obj);
                topic.updated = Utils.getStringDateField("update", obj);
                topic.updated_by = Utils.getLongField("updated_by", obj);
                topic.is_closed = Utils.getLongField("is_closed", obj) == 1;
                topic.is_fixed = Utils.getLongField("is_fixed", obj) == 1;
                topic.comments = Utils.getLongField("comments", obj);

                // according to the spec - we don't consider board creators/editors as posters

                topics.Add(topic);
            }

            // save the board topics
            Utils.PrintFileContent(groupBoardTopicsWriter, topics);
        }

        // process group board topic comments
        private void OnBoardGetComments(JObject data, string cookie)
        {
            if (data[VkRestApi.RESPONSE_BODY] == null)
            {
                this.isRunning = false;
                return;
            }

            if (this.totalCount == 0)
            {
                this.totalCount = data[VkRestApi.RESPONSE_BODY]["count"].ToObject<long>();
                if (this.totalCount == 0)
                {
                    this.isRunning = false;
                    return;
                }
                this.step = 1; //(int)(10000 * POSTS_PER_REQUEST / this.totalCount);
            }

            // now calc items in response
            int count = data[VkRestApi.RESPONSE_BODY]["items"].Count();

            if (!CheckAndIncrement(count))
                return;
            
            var comments = new List<Comment>();
            var board_id = Convert.ToInt64(cookie); // passed as a cookie

            // process response body
            for (int i = 0; i < count; ++i)
            {
                var postObj = data[VkRestApi.RESPONSE_BODY]["items"][i].ToObject<JObject>();

                var comment = new Comment();
                comment.id = Utils.getLongField("id", postObj);
                comment.post_id = board_id;
                comment.from_id = Utils.getLongField("from_id", postObj);
                // post date
                comment.date = Utils.getStringDateField("date", postObj);
                comment.reply_to_uid = Utils.getLongField("reply_to_uid", postObj);
                comment.reply_to_cid = Utils.getLongField("reply_to_cid", postObj);
                comment.likes = Utils.getLongField("likes", "count", postObj);

                // likes
/*
                if (comment.likes > 0)
                {
                    var like = new Like();
                    like.type = "comment";
                    like.owner_id = (long)this.groupId;
                    like.item_id = comment.id;
                    this.likes.Add(like);
                }
*/

                // attachments count
                if (postObj["attachments"] != null)
                {
                    comment.attachments = postObj["attachments"].ToArray().Length;
                }

                // post text
                comment.text = Utils.getTextField("text", postObj);

                // update posters
                if (comment.from_id != this.groupId)
                {
                    if (!posters.ContainsKey(comment.from_id))
                    {
                        posters[comment.from_id] = new Poster();
                    }

                    posters[comment.from_id].board_comments += 1; // increment number of board comments
                    posters[comment.from_id].rec_likes += comment.likes;

                    // add to the poster ids
                    posterIds.Add(comment.from_id);
                }

                comments.Add(comment);

                // add to board post info

                if (!boardPostInfo.ContainsKey(comment.id))
                {
                    boardPostInfo[comment.id] = new PostInfo(comment.id, comment.from_id);
                }

                // TODO: update u2u matrix for replies
                long to = parseCommentForReplyTo(comment.text);
                if (to > 0)
                {
                    updateU2UMatrixForReply(comment.from_id, to);
                }
            }

            // save the board comments
            Utils.PrintFileContent(groupBoardCommentsWriter, comments);
        }


        private bool CheckAndIncrement(int count)
        {
            if (count == 0)
            {
                totalEmptyRetry++;
                return !(totalEmptyRetry > MAX_EMPTY_RETRY);
            }

            currentOffset += count;
            return true;
        }

        private void updateU2UMatrixForPost(long from, long post_id, bool isComment)
        {
            long to = 0;
            // lookup user id from post id
            if (postInfo.ContainsKey(post_id))
            {
                to = postInfo[post_id].user_id;
            }

            if (to == 0)
            {
                // TODO: warn
                return;
            }

            if (!u2uMatrix.ContainsKey(from))
            {
                u2uMatrix[from] = new Dictionary<long, PostCount>();
                u2uMatrix[from][to] = new PostCount(isComment ? 0:1, isComment ? 1:0, 0);
            }
            else if (!u2uMatrix[from].ContainsKey(to))
            {
                u2uMatrix[from][to] = new PostCount(isComment ? 0 : 1, isComment ? 1 : 0, 0);
            }
            else
            {
                u2uMatrix[from][to].Increment(isComment ? 0 : 1, isComment ? 1 : 0, 0);
            }
        }

        private void updateU2UMatrix(long from, long to, bool isComment)
        {
            if (!u2uMatrix.ContainsKey(from))
            {
                u2uMatrix[from] = new Dictionary<long, PostCount>();
                u2uMatrix[from][to] = new PostCount(isComment ? 0 : 1, isComment ? 1 : 0, 0);
            }
            else if (!u2uMatrix[from].ContainsKey(to))
            {
                u2uMatrix[from][to] = new PostCount(isComment ? 0 : 1, isComment ? 1 : 0, 0);
            }
            else
            {
                u2uMatrix[from][to].Increment(isComment ? 0 : 1, isComment ? 1 : 0, 0);
            }
        }

        private void updateU2UMatrixForReply(long from, long to)
        {
            if (!u2uMatrix.ContainsKey(from))
            {
                u2uMatrix[from] = new Dictionary<long, PostCount>();
                u2uMatrix[from][to] = new PostCount(0, 0, 1);
            }
            else if (!u2uMatrix[from].ContainsKey(to))
            {
                u2uMatrix[from][to] = new PostCount(0, 0, 1);
            }
            else
            {
                u2uMatrix[from][to].Increment(0, 0, 1);
            }
        }

        // sample: [id73232:bp-2742_53133|Мария] or [id3338353|Ольга]
        private long parseCommentForReplyTo(String text)
        {
            if (String.IsNullOrEmpty(text))
                return 0;

            MatchCollection matches = replyToRegex.Matches(text);
            if (matches.Count > 0)
            {
                String to = matches[0].Groups[1].Value;
                if (!String.IsNullOrEmpty(to))
                {
                    return long.Parse(to);
                }
            }
            return 0;
        }

        // process user info
        private void OnUsersGet(JObject data)
        {
            if (data[VkRestApi.RESPONSE_BODY] == null)
            {
                this.isRunning = false;
                return;
            }

            // now calc items in response
            int count = data[VkRestApi.RESPONSE_BODY].Count();
            Debug.WriteLine("Processing " + count + " users");

            var profiles = new List<Profile>();

            // process response body
            for (int i = 0; i < count; ++i)
            {
                var userObj = data[VkRestApi.RESPONSE_BODY][i].ToObject<JObject>();

                var profile = new Profile();
                profile.id = Utils.getLongField("id", userObj);

                profile.first_name = Utils.getTextField("first_name", userObj);
                profile.last_name = Utils.getTextField("last_name", userObj);
                profile.screen_name = Utils.getTextField("screen_name", userObj);
                profile.bdate = Utils.getTextField("bdate", userObj);
                profile.city = Utils.getStringField("city", "title", userObj);
                profile.country = Utils.getStringField("country", "title", userObj);

                profile.photo = Utils.getStringField("photo_50", userObj);
                profile.sex = Utils.getStringField("sex", userObj);
                profile.relation = Utils.getStringField("relation", userObj);

                // university name - text
                profile.education = Utils.getTextField("university_name", userObj);

                // status text
                profile.status = Utils.getTextField("status", userObj);

                profiles.Add(profile);

                // add graph visitor vertex
                this.contentNetworkAnalyzer.addVertex(profile.id, profile.first_name + " " + profile.last_name, "User", userObj);

                Poster p;
                if (posters.TryGetValue(profile.id, out p))
                {
                    Dictionary<String, String> attr = dictionaryFromPoster(p);
                    // update poster vertex attributes
                    this.contentNetworkAnalyzer.updateVertexAttributes(profile.id, attr);
                }
            }

            if (profiles.Count > 0)
            {
                // update posters
                Utils.PrintFileContent(groupVisitorsWriter, profiles);
            }
        }

        private Dictionary<String, String> dictionaryFromPoster(Poster poster)
        {
            var dic = new Dictionary<String, String>();

            dic.Add("posts", poster.posts.ToString());
            dic.Add("rec_comments", poster.rec_comments.ToString());
            dic.Add("comments", poster.comments.ToString());
            dic.Add("rec_likes", poster.rec_likes.ToString());
            dic.Add("likes", poster.likes.ToString());
            dic.Add("friends", poster.friends.ToString());
            dic.Add("board_comments", poster.board_comments.ToString());

            return dic;
        }

        private void VKContentForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (errorLogWriter != null)
            {
                errorLogWriter.Flush();
                errorLogWriter.Close();
            }
        }        
    }
}
