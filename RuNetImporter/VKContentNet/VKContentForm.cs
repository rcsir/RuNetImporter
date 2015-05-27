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
        private int totalCount;
        private int currentOffset;
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
        const string ReplyToPattern = @"\[id(\d+)[:\|]";
        readonly Regex replyToRegex = new Regex(ReplyToPattern, RegexOptions.IgnoreCase);

        private class PostInfo
        {
            public PostInfo(long id, long uid)
            {
                Id = id;
                UserId = uid;
            }

            public long Id { get; set; }
            public long UserId { get; set; }
        };

        // poster info 
        private class Poster
        {
            public Poster()
            {
                Posts = 0;
                RecComments = 0;
                Comments = 0;
                RecLikes = 0;
                Likes = 0;
                Friends = 0;
                BoardComments = 0;
                Replies = 0;
            }

            public long Posts { get; set; }
            public long RecComments { get; set; }
            public long Comments { get; set; }
            public long RecLikes { get; set; }
            public long Likes { get; set; }
            public long Friends { get; set; }
            public long BoardComments { get; set; }
            public long Replies { get; set; }
        };

        private class GroupPostsParam
        {
            public GroupPostsParam(decimal gid,
                DateTime from, DateTime to, Boolean justStats)
            {
                Gid = gid;
                From = from;
                To = to;
                JustStats = justStats;
            }

            public decimal Gid { get; set; }
            public DateTime From { get; set; }
            public DateTime To { get; set; }
            public Boolean JustStats { get; set; }
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
            Debug.WriteLine("User Logged In: " + loginArgs);

            this.userId = loginArgs.userId;
            this.authToken = loginArgs.authToken;
            //this.expiresAt = loginArgs.expiersIn; // TODO: calculate expiration time

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
                    var shouldActivate = groupId != 0;
                    var isBusy = isRunning;
                    
                    FindGroupsButton.Enabled = !isBusy;

                    // activate group buttons
                    DownloadGroupPosts.Enabled = shouldActivate && !isBusy;
                    GenerateCommunicatinoNetwork.Enabled = shouldActivate && !isBusy && this.u2uMatrix.Count > 0;
                    CancelOperation.Enabled = isBusy; // TODO: activate only when running
                }
            }
            else
            {
                // disable user controls
                FindGroupsButton.Enabled = false;
                DownloadGroupPosts.Enabled = false;
                GenerateCommunicatinoNetwork.Enabled = false;
                CancelOperation.Enabled = false;
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
            var result = folderBrowserDialog1.ShowDialog();
            if (result != DialogResult.OK) return;
            
            WorkingFolderTextBox.Text = folderBrowserDialog1.SelectedPath;
            isWorkingFolderSet = true;

            // if error log exists - close it and create new one
            if (errorLogWriter != null)
            {
                errorLogWriter.Close();
                errorLogWriter = null;
            }

            IEntity temp = new VkRestApi.OnErrorEventArgs();
            var fileName = Utils.GenerateFileName(this.WorkingFolderTextBox.Text, 0, temp, "", "log");
            errorLogWriter = File.CreateText(fileName);
            errorLogWriter.AutoFlush = true;
            Utils.PrintFileContent(errorLogWriter, temp);

            ActivateControls();
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
            String gId = cookie; // group id is sent as a cooky

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
            this.contentNetworkAnalyzer.AddVertex((long)this.groupId,
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
            var postsDialog = new DownloadGroupPostsDialog {GroupId = Math.Abs(this.groupId), IsGroup = this.isGroup};

            if (postsDialog.ShowDialog() == DialogResult.OK)
            {
                updateStatus(-1, "Start");
                var param = new GroupPostsParam(this.groupId,
                    postsDialog.FromDate, postsDialog.ToDate, postsDialog.JustGroupStats);

                isRunning = true;
                backgroundGroupsWorker.RunWorkerAsync(param);
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
                backgroundNetWorker.RunWorkerAsync(param);
                ActivateControls();
            }
            else
            {
                Debug.WriteLine("Generate communication network canceled");
            }
        }

        private void CancelJobButton_Click(object sender, EventArgs e)
        {
            if (isRunning)
                backgroundGroupsWorker.CancelAsync();
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
                throw new ArgumentException("Illegal arguments for Group Work");
            }

            if (param.From <= param.To)
            {
                postsFromDate = param.From;
                postsToDate = param.To;
            }
            else
            {
                postsFromDate = param.To;
                postsToDate = param.From;
            }

            // working directory
            var workingDir = WorkingFolderTextBox.Text;

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
            if (param.JustStats)
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
            var fileName = Utils.GenerateFileName(workingDir, groupId, e);
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

                for (int i = 0; i < postsWithComments.Count && !bw.CancellationPending; i++)
                {
                    ResetCountersAndGetReady();
                    step = CalcStep(postsWithComments.Count);

                    bw.ReportProgress(step,
                        "Getting " + (i + 1) + " comments out of " + postsWithComments.Count);

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

            // process the likes - they will be added to the posters
            if (likes.Count > 0 &&
                !bw.CancellationPending)
            {
                // request likers ids
                ResetCountersAndGetReady();
                bw.ReportProgress(-1, "Getting likers");

                timeLastCall = 0;
                step = CalcStep(likes.Count);

                for (var i = 0; i < likes.Count; i++)
                {
                    isRunning = true;
                    bw.ReportProgress(step, "Getting likes " + (i + 1) + " / " + likes.Count);

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
            for (var i = 0; i < topics.Count; i++)
            {
                if (bw.CancellationPending)
                    break; // canceled

                if (topics[i].is_closed || topics[i].comments == 0)
                    continue; // empty or closed topic - ignore

                ResetCountersAndGetReady();
                step = CalcStep(topics.Count);
                bw.ReportProgress(step, "Getting comments for board " + i + " / " + topics.Count);
                
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
                    bw.ReportProgress(0, "Getting comments " + currentOffset +" / " + totalCount +" for board " + i + " / " + topics.Count );
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
                step = CalcStep(visitorIds.Count, 100);

                timeLastCall = 0;

                for (var i = 0; i < visitorIds.Count; i += 100)
                {
                    isRunning = true;

                    bw.ReportProgress(step, "Getting " + (i + 1) + " visitors out of " + visitorIds.Count);

                    if (bw.CancellationPending)
                        break;

                    sb.Length = 0;

                    sb.Append("user_ids=");

                    for (var j = i; j < visitorIds.Count && j < i + 100; ++j)
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
                var attr = dictionaryFromPoster(groupPoster);
                // update poster vertex attributes
                contentNetworkAnalyzer.UpdateVertexAttributes((long)groupId, attr);
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
            totalEmptyRetry = 0;
        }

        private int CalcStep(int count, int bulk = 1)
        {
            var s = 10000*bulk/count;
            if (s == 0)
                s = 1;
            return s;
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
            contentNetworkAnalyzer.GraphName = param.Type.ToString();
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
                    network.Save(Utils.GenerateFileName(WorkingFolderTextBox.Text, groupId, contentNetworkAnalyzer.GraphName, "user-network", "graphml"));
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
                GroupsProgressBar.Increment(progress);
            }
            else if (progress < 0)
            {
                // reset 
                GroupsProgressBar.Value = 0;
            }

            groupsStripStatusLabel.Text = status;
        }

        // process group posts
        private void OnWallGet(JObject data)
        {
            if (data[VkRestApi.RESPONSE_BODY] == null)
            {
                isRunning = false;
                return;
            }

            if (totalCount == 0)
            {
                totalCount = data[VkRestApi.RESPONSE_BODY]["count"].ToObject<int>();
                if (totalCount == 0)
                {
                    isRunning = false;
                    return;
                }
                step = CalcStep(totalCount, POSTS_PER_REQUEST );
            }

            // calculate items in response
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

                    posters[post.signer_id].Posts += 1; // increment number of posts
                    posters[post.signer_id].RecLikes += post.likes;

                    // add to the poster ids
                    posterIds.Add(post.signer_id);
                } 
                else
                {
                    if (!posters.ContainsKey(post.from_id))
                    {
                        posters[post.from_id] = new Poster();
                    }

                    posters[post.from_id].Posts += 1; // increment number of posts
                    posters[post.from_id].RecLikes += post.likes;

                    // add to the poster ids if different from the group
                    if(post.from_id != this.groupId)
                        posterIds.Add(post.from_id);
                }

                posts.Add(post);

                // add to the post infos
                if (!postInfo.ContainsKey(post.id))
                {
                    var uid = post.signer_id > 0 ? post.signer_id : post.from_id; 
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
                isRunning = false;
                return;
            }

            if (totalCount == 0)
            {
                totalCount = data[VkRestApi.RESPONSE_BODY]["count"].ToObject<int>();
                if (totalCount == 0)
                {
                    isRunning = false;
                    return;
                }
                step = CalcStep(totalCount, POSTS_PER_REQUEST);
            }

            // calculate items in response
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
/*
                if (comment.from_id != this.groupId)
                {
*/
                if (!posters.ContainsKey(comment.from_id))
                {
                    posters[comment.from_id] = new Poster();
                }

                posters[comment.from_id].Comments += 1; // increment number of comments
                posters[comment.from_id].RecLikes += comment.likes;

                // add to the poster ids if different from the group
                if (comment.from_id != this.groupId)
                    posterIds.Add(comment.from_id);
/*
                }
*/
                comments.Add(comment);

                // add comment to post info
                if (!postInfo.ContainsKey(comment.id))
                {
                    postInfo[comment.id] = new PostInfo(comment.id, comment.from_id);
                }
                
                // update u2u matrix
                if (comment.reply_to_uid > 0)
                {
                    // direct
                    UpdateU2UMatrix(comment.from_id, comment.reply_to_uid, true);
                }
                else
                {
                    // from post
                    var to = GetPosterFromPostId(post_id);
                    if (to != 0)
                    {
                        UpdateU2UMatrix(comment.from_id, to, true);
                        // update receive comments
                        if (!posters.ContainsKey(to))
                        {
                            posters[to] = new Poster();
                        }

                        posters[to].RecComments += 1; // increment number of received comments

                    }
                }

                // parse reply to from comment text 
                /*
                 * For now, we are not going to process replies in wall comments, TBD
                long to = parseCommentForReplyTo(comment.text);
                if (to > 0 && to != comment.reply_to_uid)
                {
                    updateU2UMatrix(comment.from_id, to, true);
                    posters[comment.from_id].replies += 1;
                }
                */
            }

            // save the posts list
            Utils.PrintFileContent(groupCommentsWriter, comments);
        }

        // process likes user list
        private void OnLikesGetList(JObject data, string cookie)
        {
            if (data[VkRestApi.RESPONSE_BODY] == null)
            {
                isRunning = false;
                return;
            }

            // calculate items in response
            int count = data[VkRestApi.RESPONSE_BODY]["items"].Count();

            if (!CheckAndIncrement(count))
                return;
            
            var post_id = Convert.ToInt64(cookie); // passed as a cookie

            // process response body
            for (var i = 0; i < count; ++i)
            {
                var likerId = data[VkRestApi.RESPONSE_BODY]["items"][i].ToObject<long>();
                // this user liked the subject - add him to the posters
                if (!posters.ContainsKey(likerId))
                {
                    posters[likerId] = new Poster();
                }

                posters[likerId].Likes += 1; // increment poster's likes count

                posterIds.Add(likerId);

                // update u2u matrix
                var to = GetPosterFromPostId(post_id);
                if (to != 0)
                {
                    UpdateU2UMatrix(likerId, to, false);
                }
            }
        }

        // process group board topics
        private void OnBoardGetTopics(JObject data, string cookie)
        {
            if (data[VkRestApi.RESPONSE_BODY] == null)
            {
                isRunning = false;
                return;
            }

            if (totalCount == 0)
            {
                totalCount = data[VkRestApi.RESPONSE_BODY]["count"].ToObject<int>();
                if (totalCount == 0)
                {
                    isRunning = false;
                    return;
                }
                step = CalcStep(totalCount, POSTS_PER_REQUEST);
            }
            // calculate items in response
            var count = data[VkRestApi.RESPONSE_BODY]["items"].Count();

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
                isRunning = false;
                return;
            }

            if (totalCount == 0)
            {
                totalCount = data[VkRestApi.RESPONSE_BODY]["count"].ToObject<int>();
                if (totalCount == 0)
                {
                    isRunning = false;
                    return;
                }
                step = CalcStep(totalCount, POSTS_PER_REQUEST);
            }

            // calculate items in response
            var count = data[VkRestApi.RESPONSE_BODY]["items"].Count();

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
/*
                if (comment.from_id != this.groupId)
                {
*/
                if (!posters.ContainsKey(comment.from_id))
                {
                    posters[comment.from_id] = new Poster();
                }

                posters[comment.from_id].BoardComments += 1; // increment number of board comments
                posters[comment.from_id].RecLikes += comment.likes;

                // add to the poster ids
                if (comment.from_id != this.groupId)
                    posterIds.Add(comment.from_id);
/*
                }
*/
                comments.Add(comment);

                // add to board post info
                if (!boardPostInfo.ContainsKey(comment.id))
                {
                    boardPostInfo[comment.id] = new PostInfo(comment.id, comment.from_id);
                }

                // update u2u matrix for replies
                var to = ParseCommentForReplyTo(comment.text);
                if (to > 0)
                {
                    UpdateU2UMatrixForReply(comment.from_id, to);
                    posters[comment.from_id].Replies += 1;
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

        private long GetPosterFromPostId(long postId)
        {
            // lookup user id from post id
            return postInfo.ContainsKey(postId) ? postInfo[postId].UserId : 0;
        }

        private void UpdateU2UMatrix(long from, long to, bool isComment)
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

        private void UpdateU2UMatrixForReply(long from, long to)
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
        private long ParseCommentForReplyTo(String text)
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
                isRunning = false;
                return;
            }

            // calculate items in response
            var count = data[VkRestApi.RESPONSE_BODY].Count();
            Debug.WriteLine("Processing " + count + " users");

            var profiles = new List<Profile>();

            // process response body
            for (var i = 0; i < count; ++i)
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
                contentNetworkAnalyzer.AddVertex(profile.id, profile.first_name + " " + profile.last_name, "User", userObj);

                Poster p;
                if (posters.TryGetValue(profile.id, out p))
                {
                    var attr = dictionaryFromPoster(p);
                    // update poster vertex attributes
                    contentNetworkAnalyzer.UpdateVertexAttributes(profile.id, attr);
                }
                else
                {
                    Debug.WriteLine("User/Poster not found with id " + profile.id);
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

            dic.Add("posts", poster.Posts.ToString());
            dic.Add("rec_comments", poster.RecComments.ToString());
            dic.Add("comments", poster.Comments.ToString());
            dic.Add("rec_likes", poster.RecLikes.ToString());
            dic.Add("likes", poster.Likes.ToString());
            dic.Add("friends", poster.Friends.ToString());
            dic.Add("board_comments", poster.BoardComments.ToString());
            dic.Add("replies", poster.Replies.ToString());

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
