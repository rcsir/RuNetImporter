using System;
using System.Collections;
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
using rcsir.net.vk.groups.NetworkAnalyzer;
using rcsir.net.vk.importer.Dialogs;
using rcsir.net.vk.importer.api;
using rcsir.net.vk.importer.api.entity;
using rcsir.net.vk.content.Dialogs;
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
        private JObject groupObject;
        private Group group;
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
        private readonly List<BoardTopic> topics = new List<BoardTopic>();

        // group's U2U collections
        private class PostCount
        {
            public enum Type
            {
                Like,
                Comment
            };

            public PostCount()
            {
                Likes = 0;
                Comments = 0;
            }

            public void Increment(Type type)
            {
                switch (type)
                {
                    case Type.Like:
                        Likes ++;
                        break;
                    case Type.Comment:
                        Comments ++;
                        break;
                }
            }

            public int Likes { get; private set; }
            public int Comments { get; private set; }
        };

        readonly Dictionary<long, PostInfo> postInfo = new Dictionary<long, PostInfo>();
        readonly Dictionary<long, PostInfo> boardPostInfo = new Dictionary<long, PostInfo>();
        readonly Dictionary<long, Dictionary<long, PostCount>> u2uMatrix = new Dictionary<long, Dictionary<long, PostCount>>();
        
        // document
        private StreamWriter groupPostsWriter;
        private StreamWriter groupPostsCopyHistoryWriter;
        private StreamWriter groupVisitorsWriter;
        private StreamWriter groupCommentsWriter;
        private StreamWriter groupBoardTopicsWriter;
        private StreamWriter groupBoardCommentsWriter;
        private StreamWriter errorLogWriter;

        // Network Analyzer document
        private ContentNetworkAnalyzer contentNetworkAnalyzer;
        private string networkEntities = "";
        // network analyzer document
        GroupNetworkAnalyzer groupNetworkAnalyzer;

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
                Comments = 0;
                RecComments = 0;
                Likes = 0;
                RecLikes = 0;
                Friends = 0;
                BoardComments = 0;
            }

            public long Posts { get; set; }
            public long RecComments { get; set; }
            public long Comments { get; set; }
            public long RecLikes { get; set; }
            public long Likes { get; set; }
            public long Friends { get; set; }
            public long BoardComments { get; set; }
        };

        private class GroupPostsParam
        {
            public GroupPostsParam(decimal gid,
                DateTime from, DateTime to, 
                Boolean groupWall,
                Boolean groupTopics)
            {
                Gid = gid;
                From = from;
                To = to;
                GroupWall = groupWall;
                GroupTopics = groupTopics;
            }

            public decimal Gid { get; set; }
            public DateTime From { get; set; }
            public DateTime To { get; set; }
            public Boolean GroupWall { get; set; }
            public Boolean GroupTopics { get; set; }
        };


        private class GraphParam
        {
            public enum GraphType
            {
                Comments = 1,
                Likes = 2,
                Combined = 3
            };

            public GraphParam(GraphType t)
            {
                Type = t;
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

            folderBrowserDialog1.RootFolder = Environment.SpecialFolder.Personal;

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
                += new ProgressChangedEventHandler(NetWorkProgressChanged);

            this.backgroundNetWorker.RunWorkerCompleted
                += new RunWorkerCompletedEventHandler(NetWorkCompleted);

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
                case VkFunction.FriendsGet:
                    OnGetFriends(onDataArgs.Data, onDataArgs.Cookie);
                    break;
                case VkFunction.BoardGetTopics:
                    OnBoardGetTopics(onDataArgs.Data, onDataArgs.Cookie);
                    break;
                case VkFunction.BoardGetComments:
                    OnBoardGetComments(onDataArgs.Data, onDataArgs.Cookie);
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
                updateErrorLogFile(onErrorArgs, errorLogWriter);
            }

            if (onErrorArgs.Code == VkRestApi.CriticalErrorCode)
            {
                var result = MessageBox.Show(
                    onErrorArgs.Details + "\n Please fix connection problem or just wait.\n Press \'Yes\' to continue with the current request" +
                    "\nPress \'No\' to continue with the next request.",
                    onErrorArgs.Error,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button1);

                if (result == DialogResult.Yes)
                {
                    vkRestApi.CallVkFunction(onErrorArgs.Function, onErrorArgs.Context);
                    return;
                }
            }
            else
            {
                switch (onErrorArgs.Code)
                {
                    case 6:
                        // this is too many requests error - repeat last API call
                        Utils.SleepTime(0);
                        vkRestApi.CallVkFunction(onErrorArgs.Function, onErrorArgs.Context);
                        return;
                    case 15:
                        // user is not found - continue
                        break;
                    default:
                        var result = MessageBox.Show(
                            onErrorArgs.Details + "\n Please fix connection problem or just wait.\n Press \'Yes\' to continue with the current request" +
                            "\nPress \'No\' to continue with the next request.",
                            onErrorArgs.Error,
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning,
                            MessageBoxDefaultButton.Button1);

                        if (result == DialogResult.Yes)
                        {
                            vkRestApi.CallVkFunction(onErrorArgs.Function, onErrorArgs.Context);
                            return;
                        }
                        break;
                }
            }

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
                    downloadLikesButton.Enabled = shouldActivate && !isBusy;
                    GenerateCommunicatinoNetwork.Enabled = shouldActivate && !isBusy && this.u2uMatrix.Count > 0;
                    CancelOperation.Enabled = isBusy; // TODO: activate only when running
                }
            }
            else
            {
                // disable user controls
                FindGroupsButton.Enabled = false;
                DownloadGroupPosts.Enabled = false;
                downloadLikesButton.Enabled = false;
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

            IEntity temp = new VkRestApi.OnErrorEventArgs(VkFunction.FriendsGet,null,0,"");
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

            if (data[VkRestApi.ResponseBody] == null ||
                data[VkRestApi.ResponseBody].Count() == 0)
            {
                this.groupId = 0;
                this.groupId2.Text = gId;
                this.groupDescription.Text = "Not found";
                Debug.WriteLine("Group is not found");
                return;
            }

            // process response body
            groupObject = data[VkRestApi.ResponseBody][0].ToObject<JObject>();
            if (groupObject == null)
            {
                this.groupId = 0;
                this.groupId2.Text = gId;
                this.groupDescription.Text = "Group read error";
                Debug.WriteLine("Group object is empty");
                return;
            }

            group = new Group
            {
                Id = Utils.GetLongField("id", groupObject),
                name = Utils.GetStringField("name", groupObject),
                ScreenName = Utils.GetStringField("screen_name", groupObject),
                IsClosed = Utils.GetStringField("is_closed", groupObject),
                Type = Utils.GetStringField("type", groupObject),
                MembersCount = Utils.GetStringField("members_count", groupObject),
                City = Utils.GetStringField("city", "title", groupObject),
                Country = Utils.GetStringField("country", "title", groupObject),
                Photo = Utils.GetStringField("photo_50", groupObject),
                Description = Utils.GetTextField("description", groupObject),
                Status = Utils.GetTextField("status", groupObject)
            };

            // update group id and group info
            this.groupId = group.Id > 0 ? Decimal.Negate(group.Id) : group.Id;// group id is negative number

            String fileName = Utils.GenerateFileName(this.WorkingFolderTextBox.Text, groupId, group);
            StreamWriter writer = File.CreateText(fileName);
            Utils.PrintFileHeader(writer, group);
            Utils.PrintFileContent(writer, group);
            writer.Close();

            this.groupId2.Text = group.Id.ToString();
            this.groupDescription.Text = group.name;
            this.groupDescription.AppendText("\r\n type: " + group.Type);
            this.groupDescription.AppendText("\r\n members: " + group.MembersCount);
            this.groupDescription.AppendText("\r\n " + group.Description);

            ActivateControls();

            ReadyEvent.Set();
        }
        

        private void DownloadGroupPosts_Click(object sender, EventArgs e)
        {
            var postsDialog = new DownloadGroupPostsDialog {GroupId = Math.Abs(this.groupId), IsGroup = this.isGroup};

            if (postsDialog.ShowDialog() == DialogResult.OK)
            {
                UpdateStatus(-1, "Group Posts Start");
                var param = new GroupPostsParam(this.groupId,
                    postsDialog.FromDate, postsDialog.ToDate,
                    postsDialog.GroupWall,
                    postsDialog.GroupTopics);

                isRunning = true;
                backgroundGroupsWorker.RunWorkerAsync(param);
                ActivateControls();
            }
            else
            {
                UpdateStatus(-1, "Download Group Posts Canceled");
            }
        }
        
        private void downloadLikesButton_Click(object sender, EventArgs e)
        {
            var likesDialog = new DownloadLikesDialog {GroupId = Math.Abs(this.groupId), IsGroup = this.isGroup};

            if (likesDialog.ShowDialog() == DialogResult.OK)
            {
                var postIds = likesDialog.PostIDs;

                if (postIds.Length > 0)
                {
                    UpdateStatus(-1, "Likes Started");
                    isRunning = true;
                    backgroundLikesWorker.RunWorkerAsync(postIds);
                }
                else
                {
                    UpdateStatus(-1, "Post ids is empty!");
                    isRunning = false;
                }
                ActivateControls();
            }
            else
            {
                UpdateStatus(-1, "Download Likes Canceled");                 
            }
        }

        private void GenerateCommunicatinoNetwork_Click(object sender, EventArgs e)
        {
            var communicationDialog = new GenerateCommunicationNetworkDialog();

            if (communicationDialog.ShowDialog() == DialogResult.OK)
            {
                UpdateStatus(-1, "Start");
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
            {
                if (backgroundGroupsWorker.IsBusy)
                    backgroundGroupsWorker.CancelAsync();
                
                if (backgroundNetWorker.IsBusy)
                    backgroundNetWorker.CancelAsync();

                if (backgroundLikesWorker.IsBusy)
                    backgroundLikesWorker.CancelAsync();
            }
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
            postInfo.Clear(); // clear post infos
            boardPostInfo.Clear(); // reset boards post infos
            topics.Clear(); // clear topics infos
            u2uMatrix.Clear(); // clear u2u matrix

            // create group members network document
            this.contentNetworkAnalyzer = new ContentNetworkAnalyzer();
            this.groupNetworkAnalyzer = new GroupNetworkAnalyzer();
            // add group as a vertex to the document (some posts come from group only)
            this.contentNetworkAnalyzer.AddVertex((long)this.groupId,
                group.name, "User", groupObject);

            var context = new VkRestApi.VkRestContext(this.userId, this.authToken);
            var sb = new StringBuilder();
            long timeLastCall = 0;

            networkEntities = param.GroupWall ? "wall" : "";
            networkEntities += param.GroupTopics ? "-topics" : "";

            IEntity e;
            string fileName;

            if (param.GroupWall)
            {
                // group posts
                e = new Post();
                fileName = Utils.GenerateFileName(workingDir, groupId, e);
                groupPostsWriter = File.CreateText(fileName);
                Utils.PrintFileHeader(groupPostsWriter, e);

                e = new PostCopyHistory(0, null);
                fileName = Utils.GenerateFileName(workingDir, groupId, e);
                groupPostsCopyHistoryWriter = File.CreateText(fileName);
                Utils.PrintFileHeader(groupPostsCopyHistoryWriter, e);

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
                    timeLastCall = Utils.SleepTime(timeLastCall);
                    // call VK REST API
                    vkRestApi.CallVkFunction(VkFunction.WallGet, context);

                    // wait for the user data
                    ReadyEvent.WaitOne();
                    bw.ReportProgress(step, "Getting " + currentOffset + " posts out of " + totalCount);
                }

                groupPostsWriter.Close();
                groupPostsCopyHistoryWriter.Close();

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
                            timeLastCall = Utils.SleepTime(timeLastCall);
                            // call VK REST API
                            vkRestApi.CallVkFunction(VkFunction.WallGetComments, context);

                            // wait for the user data
                            ReadyEvent.WaitOne();
                        }
                    }

                    groupCommentsWriter.Close();
                }
            }

            if (param.GroupTopics)
            {
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
                    timeLastCall = Utils.SleepTime(timeLastCall);
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

                    if (topics[i].IsClosed || topics[i].Comments == 0)
                        continue; // empty or closed topic - ignore

                    ResetCountersAndGetReady();
                    step = CalcStep(topics.Count);
                    bw.ReportProgress(step, "Getting comments for board " + i + " / " + topics.Count);

                    while (ShellContinue(bw))
                    {
                        sb.Length = 0;
                        sb.Append("group_id=").Append(Math.Abs(groupId)).Append("&"); // group id
                        sb.Append("topic_id=").Append(topics[i].Id).Append("&"); // post id
                        sb.Append("need_likes=").Append(1).Append("&"); // need likes count
                        sb.Append("offset=").Append(currentOffset).Append("&");
                        sb.Append("count=").Append(POSTS_PER_REQUEST).Append("&");
                        context.Parameters = sb.ToString();
                        context.Cookie = topics[i].Id.ToString(); // pass topic id as a cookie
                        Debug.WriteLine("Request parameters: " + context.Parameters);

                        // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                        timeLastCall = Utils.SleepTime(timeLastCall);
                        // call VK REST API
                        vkRestApi.CallVkFunction(VkFunction.BoardGetComments, context);

                        // wait for the user data
                        ReadyEvent.WaitOne();
                        bw.ReportProgress(0,
                            "Getting comments " + currentOffset + " / " + totalCount + " for board " + i + " / " +
                            topics.Count);
                    }
                }

                groupBoardTopicsWriter.Close();
                groupBoardCommentsWriter.Close();
            }

            // process the likes from wall and board topics
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
                    sb.Append("type=").Append(likes[i].Type).Append("&"); // type
                    sb.Append("owner_id=").Append(likes[i].OwnerId).Append("&"); // group id
                    sb.Append("item_id=").Append(likes[i].ItemId).Append("&"); // post id
                    sb.Append("count=").Append(LIKES_PER_REQUEST).Append("&");
                    context.Parameters = sb.ToString();
                    context.Cookie = likes[i].ItemId.ToString(); // pass post/comment id as a cookie
                    Debug.WriteLine("Request parameters: " + context.Parameters);

                    // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                    timeLastCall = Utils.SleepTime(timeLastCall);
                    // call VK REST API
                    vkRestApi.CallVkFunction(VkFunction.LikesGetList, context);

                    // wait for the user data
                    ReadyEvent.WaitOne();
                }
            }

            // now collect info about posters (users or visitors who left post, comment or like) 
            var visitorIds = posterIds.ToList();

            if (visitorIds.Count > 0 &&
                !bw.CancellationPending)
            {
                // group visitors profiles
                e = new Profile();
                fileName = Utils.GenerateFileName(workingDir, groupId, e, networkEntities);
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
                    timeLastCall = Utils.SleepTime(timeLastCall);
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
            UpdateStatus(progress, status);
        }

        private void groupsWorkCompleted(object sender, RunWorkerCompletedEventArgs args)
        {
            isRunning = false;

            if (args.Error != null)
            {
                MessageBox.Show(args.Error.Message);
                UpdateStatus(0, "Error");
            }
            else if (args.Cancelled)
            {
                MessageBox.Show("Work canceled!");
                UpdateStatus(0, "Canceled");
            }
            else
            {
                //var network = args.Result as XmlDocument;
                //MessageBox.Show("Group posts download complete!");
                UpdateStatus(10000, "Done");
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

                    // find edge in friend's network
                    var friendsEdge = groupNetworkAnalyzer.FindEdge(from, to);
                    var relationship = friendsEdge != null ? "Friends" : "";

                    switch (param.Type)
                    {
                        case GraphParam.GraphType.Combined:
                            if (weight.Comments > 0 || weight.Likes > 0)
                            {
                                contentNetworkAnalyzer.AddEdge(from, to, "LikesAndComments", relationship, "", weight.Comments + weight.Likes, 0);                                
                            }
                            break;
                        case GraphParam.GraphType.Comments:
                            if (weight.Comments > 0)
                            {
                                contentNetworkAnalyzer.AddEdge(from, to, "Comments", relationship, "", weight.Comments, 0);                                
                            }
                            break;
                        case GraphParam.GraphType.Likes:
                            if (weight.Likes > 0)
                            {
                                contentNetworkAnalyzer.AddEdge(from, to, "Likes", relationship, "", weight.Likes, 0);                                
                            }
                            break;
                    }
                }
            }

            // generate U2U network
            contentNetworkAnalyzer.GraphName = param.Type.ToString();
            args.Result = contentNetworkAnalyzer.GenerateU2UNetwork();
        }

        private void NetWorkProgressChanged(Object sender, ProgressChangedEventArgs args)
        {
            var status = args.UserState as String;
            var progress = args.ProgressPercentage;
            UpdateStatus(progress, status);
        }

        private void NetWorkCompleted(object sender, RunWorkerCompletedEventArgs args)
        {
            isRunning = false;

            if (args.Error != null)
            {
                MessageBox.Show(args.Error.Message);
                UpdateStatus(0, "Error");
            }
            else if (args.Cancelled)
            {
                MessageBox.Show("Communication Network Generation canceled!");
                UpdateStatus(0, "Canceled");
            }
            else
            {
                // save network document
                var network = args.Result as XmlDocument;
                if (network != null)
                {
                    UpdateStatus(0, "Save Network Graph File");
                    network.Save(Utils.GenerateFileName(WorkingFolderTextBox.Text, groupId,
                        contentNetworkAnalyzer.GraphName, 
                        networkEntities.Length == 0 ? "network" : networkEntities + "-network", "graphml"));
                }
                else
                {
                    MessageBox.Show("Network document is empty!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                //MessageBox.Show("Network generation complete!");
                UpdateStatus(10000, "Done");
            }

            ActivateControls();
        }

        private void UpdateStatus(int progress, String status)
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

        private void UpdateStatusAbs(int abs, String status)
        {
            // if 0 - ignore progress param
            if (abs > 0)
            {
                GroupsProgressBar.Value = abs;
            }
            else if (abs < 0)
            {
                // reset 
                GroupsProgressBar.Value = 0;
            }

            groupsStripStatusLabel.Text = status;
        }

        // process group posts
        private void OnWallGet(JObject data)
        {
            if (data[VkRestApi.ResponseBody] == null)
            {
                isRunning = false;
                return;
            }

            if (totalCount == 0)
            {
                totalCount = data[VkRestApi.ResponseBody]["count"].ToObject<int>();
                if (totalCount == 0)
                {
                    isRunning = false;
                    return;
                }
                step = CalcStep(totalCount, POSTS_PER_REQUEST );
            }

            // calculate items in response
            var count = data[VkRestApi.ResponseBody]["items"].Count();

            if (!CheckAndIncrement(count))
                return;

            var posts = new List<Post>();

            // process response body
            for (int i = 0; i < count; ++i)
            {
                var postObj = data[VkRestApi.ResponseBody]["items"][i].ToObject<JObject>();

                // see if post is in the range
                var dt = Utils.GetDateField("date", postObj);

                if (dt < this.postsFromDate ||
                    dt > this.postsToDate)
                {
                    continue;
                }

                var post = new Post();
                post.Id = Utils.GetLongField("id", postObj);
                post.OwnerId = Utils.GetLongField("owner_id", postObj);
                post.FromId = Utils.GetLongField("from_id", postObj);
                post.SignerId = Utils.GetLongField("signer_id", postObj);
                // post date
                post.Date = Utils.GetStringDateField("date", postObj);
                // post_type 
                post.PostType = Utils.GetStringField("post_type", postObj);
                // comments
                post.Comments = Utils.GetLongField("comments", "count", postObj);
                if (post.Comments > 0)
                {
                    this.postsWithComments.Add(post.Id); // add post's id to the ref list for comments processing
                }

                // likes
                post.Likes = Utils.GetLongField("likes", "count", postObj);
                if (post.Likes > 0)
                {
                    var like = new Like
                    {
                        Type = "post",
                        OwnerId = (long) this.groupId,
                        ItemId = post.Id,
                        Count = post.Likes
                    };
                    this.likes.Add(like);
                }

                // reposts
                post.Reposts = Utils.GetLongField("reposts", "count", postObj);

                // attachments count
                if (postObj["attachments"] != null)
                {
                    post.Attachments = postObj["attachments"].ToArray().Length;
                }

                // post text
                post.Text = Utils.GetTextField("text", postObj);

                // post may have a copy_history field, which is repost from other community - save it in a file
                var copyHistory = Utils.GetArray("copy_history", postObj);
                if (copyHistory != null)
                {
                    ProcessCopyHistory(post.Id, copyHistory);
                }

                // if post has a signer - update posters with a signer
                if (post.SignerId > 0)
                {
                    if (!posters.ContainsKey(post.SignerId))
                    {
                        posters[post.SignerId] = new Poster();
                    }

                    posters[post.SignerId].Posts += 1; // increment number of posts
                    posters[post.SignerId].RecLikes += post.Likes;

                    // add to the poster ids
                    posterIds.Add(post.SignerId);
                } 
                else
                {
                    if (!posters.ContainsKey(post.FromId))
                    {
                        posters[post.FromId] = new Poster();
                    }

                    posters[post.FromId].Posts += 1; // increment number of posts
                    posters[post.FromId].RecLikes += post.Likes;

                    // add to the poster ids if different from the group
                    if(post.FromId != this.groupId)
                        posterIds.Add(post.FromId);
                }

                posts.Add(post);

                // add to the post infos
                if (!postInfo.ContainsKey(post.Id))
                {
                    var uid = post.SignerId > 0 ? post.SignerId : post.FromId; 
                    postInfo[post.Id] = new PostInfo(post.Id, uid);
                }
            }

            // save the posts list
            Utils.PrintFileContent(groupPostsWriter, posts);
        }

        private void ProcessCopyHistory(long id, IEnumerable<JToken> arr)
        {
            var postCopyHistory = new List<PostCopyHistory>();

            foreach (var token in arr)
            {
                var postObj = token.ToObject<JObject>();
                var post = new Post();
                post.Id = Utils.GetLongField("id", postObj);
                post.OwnerId = Utils.GetLongField("owner_id", postObj);
                post.FromId = Utils.GetLongField("from_id", postObj);
                post.SignerId = Utils.GetLongField("signer_id", postObj);
                post.Date = Utils.GetStringDateField("date", postObj);
                post.PostType = Utils.GetStringField("post_type", postObj);
                post.Comments = Utils.GetLongField("comments", "count", postObj);
                post.Likes = Utils.GetLongField("likes", "count", postObj);
                post.Reposts = Utils.GetLongField("reposts", "count", postObj);
                // attachments count
                if (postObj["attachments"] != null)
                {
                    post.Attachments = postObj["attachments"].ToArray().Length;
                }
                // post text
                post.Text = Utils.GetTextField("text", postObj);
                postCopyHistory.Add(new PostCopyHistory(id, post));
            }


            Utils.PrintFileContent(groupPostsCopyHistoryWriter, postCopyHistory);
        }

        // process group comments
        private void OnWallGetComments(JObject data, String cookie)
        {
            if (data[VkRestApi.ResponseBody] == null)
            {
                isRunning = false;
                return;
            }

            if (totalCount == 0)
            {
                totalCount = data[VkRestApi.ResponseBody]["count"].ToObject<int>();
                if (totalCount == 0)
                {
                    isRunning = false;
                    return;
                }
                step = CalcStep(totalCount, POSTS_PER_REQUEST);
            }

            // calculate items in response
            var count = data[VkRestApi.ResponseBody]["items"].Count();

            if (!CheckAndIncrement(count))
                return;

            var comments = new List<Comment>();
            var post_id = Convert.ToInt64(cookie); // passed as a cookie

            // process response body
            for (int i = 0; i < count; ++i)
            {
                var postObj = data[VkRestApi.ResponseBody]["items"][i].ToObject<JObject>();

                var comment = new Comment();
                comment.Id = Utils.GetLongField("id", postObj);
                comment.PostId = post_id;
                comment.FromId = Utils.GetLongField("from_id", postObj);
                // post date
                comment.Date = Utils.GetStringDateField("date", postObj);

                comment.ReplyToUid = Utils.GetLongField("reply_to_uid", postObj);
                comment.ReplyToCid = Utils.GetLongField("reply_to_cid", postObj);

                // likes/dislikes
                comment.Likes = Utils.GetLongField("likes", "count", postObj);
                if (comment.Likes > 0)
                {
                    var like = new Like
                    {
                        Type = "comment",
                        OwnerId = (long) this.groupId,
                        ItemId = comment.Id,
                        Count = comment.Likes
                    };
                    this.likes.Add(like);
                }

                // attachments count
                if (postObj["attachments"] != null)
                {
                    comment.Attachments = postObj["attachments"].ToArray().Length;
                }

                // post text
                comment.Text = Utils.GetTextField("text", postObj);

                // update posters
                if (!posters.ContainsKey(comment.FromId))
                {
                    posters[comment.FromId] = new Poster();
                }

                posters[comment.FromId].Comments += 1; // increment number of comments
                posters[comment.FromId].RecLikes += comment.Likes;

                // add to the poster ids if different from the group
                if (comment.FromId != this.groupId)
                    posterIds.Add(comment.FromId);

                comments.Add(comment);

                // add comment to post info
                if (!postInfo.ContainsKey(comment.Id))
                {
                    postInfo[comment.Id] = new PostInfo(comment.Id, comment.FromId);
                }
                
                // update u2u matrix
                var to = comment.ReplyToUid;
                if (to <= 0)
                {
                    // parse reply to from comment text 
                    to = ParseCommentForReplyTo(comment.Text);
                }

                if (to > 0)
                {
                    UpdateU2UMatrix(comment.FromId, to, PostCount.Type.Comment); // reply is comment
                    posters[comment.FromId].Comments += 1;

                    // there must by a poster down there - update posters
                    if (!posters.ContainsKey(to))
                    {
                        posters[to] = new Poster();
                    }

                    posters[to].RecComments += 1; // increment number of received comments
                }
                else
                {
                    // try to get TO from the post
                    to = GetPosterFromPostId(post_id);
                    if (to != 0)
                    {
                        UpdateU2UMatrix(comment.FromId, to, PostCount.Type.Comment);
                        // update receive comments
                        if (!posters.ContainsKey(to))
                        {
                            posters[to] = new Poster();
                        }

                        posters[to].RecComments += 1; // increment number of received comments
                    }
                }
            }

            // save the posts list
            Utils.PrintFileContent(groupCommentsWriter, comments);
        }

        // process likes user list
        private void OnLikesGetList(JObject data, string cookie)
        {
            if (data[VkRestApi.ResponseBody] == null)
            {
                isRunning = false;
                return;
            }

            // calculate items in response
            if (totalCount == 0)
            {
                totalCount = data[VkRestApi.ResponseBody]["count"].ToObject<int>();
                if (totalCount == 0)
                {
                    isRunning = false;
                    return;
                }
                step = CalcStep(totalCount, POSTS_PER_REQUEST);
            }

            // calculate items in response
            var count = data[VkRestApi.ResponseBody]["items"].Count();

            if (!CheckAndIncrement(count))
                return;
            
            var post_id = Convert.ToInt64(cookie); // passed as a cookie
            var likers = new ArrayList(count);
            // process response body
            for (var i = 0; i < count; ++i)
            {
                var likerId = data[VkRestApi.ResponseBody]["items"][i].ToObject<long>();
                likers.Add(likerId);
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
                    UpdateU2UMatrix(likerId, to, PostCount.Type.Like);
                }
            }
        }

        // process group board topics
        private void OnBoardGetTopics(JObject data, string cookie)
        {
            if (data[VkRestApi.ResponseBody] == null)
            {
                isRunning = false;
                return;
            }

            if (totalCount == 0)
            {
                totalCount = data[VkRestApi.ResponseBody]["count"].ToObject<int>();
                if (totalCount == 0)
                {
                    isRunning = false;
                    return;
                }
                step = CalcStep(totalCount, POSTS_PER_REQUEST);
            }
            // calculate items in response
            var count = data[VkRestApi.ResponseBody]["items"].Count();

            if (!CheckAndIncrement(count))
                return;

            // process a cookie

            // process response body
            for (int i = 0; i < count; ++i)
            {
                var obj = data[VkRestApi.ResponseBody]["items"][i].ToObject<JObject>();

                var topic = new BoardTopic();
                topic.Id = Utils.GetLongField("id", obj);

                topic.Title = Utils.GetStringField("title", obj);
                topic.Created = Utils.GetStringDateField("created", obj);
                topic.CreatedBy = Utils.GetLongField("created_by", obj);
                topic.Updated = Utils.GetStringDateField("update", obj);
                topic.UpdatedBy = Utils.GetLongField("updated_by", obj);
                topic.IsClosed = Utils.GetLongField("is_closed", obj) == 1;
                topic.IsFixed = Utils.GetLongField("is_fixed", obj) == 1;
                topic.Comments = Utils.GetLongField("comments", obj);

                // according to the spec - we don't consider board creators/editors as posters

                topics.Add(topic);
            }

            // save the board topics
            Utils.PrintFileContent(groupBoardTopicsWriter, topics);
        }

        // process group board topic comments
        private void OnBoardGetComments(JObject data, string cookie)
        {
            if (data[VkRestApi.ResponseBody] == null)
            {
                isRunning = false;
                return;
            }

            if (totalCount == 0)
            {
                totalCount = data[VkRestApi.ResponseBody]["count"].ToObject<int>();
                if (totalCount == 0)
                {
                    isRunning = false;
                    return;
                }
                step = CalcStep(totalCount, POSTS_PER_REQUEST);
            }

            // calculate items in response
            var count = data[VkRestApi.ResponseBody]["items"].Count();

            if (!CheckAndIncrement(count))
                return;
            
            var comments = new List<Comment>();
            var board_id = Convert.ToInt64(cookie); // passed as a cookie

            // process response body
            for (int i = 0; i < count; ++i)
            {
                var postObj = data[VkRestApi.ResponseBody]["items"][i].ToObject<JObject>();

                var comment = new Comment();
                comment.Id = Utils.GetLongField("id", postObj);
                comment.PostId = board_id;
                comment.FromId = Utils.GetLongField("from_id", postObj);
                // post date
                comment.Date = Utils.GetStringDateField("date", postObj);
                comment.ReplyToUid = Utils.GetLongField("reply_to_uid", postObj);
                comment.ReplyToCid = Utils.GetLongField("reply_to_cid", postObj);
                comment.Likes = Utils.GetLongField("likes", "count", postObj);

                // likes
                if (comment.Likes > 0)
                {
                    var like = new Like
                    {
                        Type = "topic_comment",
                        OwnerId = (long) this.groupId,
                        ItemId = comment.Id,
                        Count = comment.Likes
                    };
                    likes.Add(like);
                }

                // attachments count
                if (postObj["attachments"] != null)
                {
                    comment.Attachments = postObj["attachments"].ToArray().Length;
                }

                // post text
                comment.Text = Utils.GetTextField("text", postObj);

                // update posters
                if (!posters.ContainsKey(comment.FromId))
                {
                    posters[comment.FromId] = new Poster();
                }

                posters[comment.FromId].BoardComments += 1; // increment number of board comments
                posters[comment.FromId].RecLikes += comment.Likes;

                // add to the poster ids
                if (comment.FromId != this.groupId)
                    posterIds.Add(comment.FromId);
    
                comments.Add(comment);

                // add to board post info
                if (!boardPostInfo.ContainsKey(comment.Id))
                {
                    boardPostInfo[comment.Id] = new PostInfo(comment.Id, comment.FromId);
                }

                // update u2u matrix for replies
                var to = ParseCommentForReplyTo(comment.Text);
                if (to > 0)
                {
                    UpdateU2UMatrix(comment.FromId, to, PostCount.Type.Comment); // reply is comment
                    posters[comment.FromId].Comments += 1;

                    // there must by a poster down there - update posters
                    if (!posters.ContainsKey(to))
                    {
                        posters[to] = new Poster();
                    }

                    posters[to].RecComments += 1; // increment number of received comments

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

        private void UpdateU2UMatrix(long from, long to, PostCount.Type type)
        {
            if (!u2uMatrix.ContainsKey(from))
            {
                u2uMatrix[from] = new Dictionary<long, PostCount>();
                u2uMatrix[from][to] = new PostCount();
            }
            else if (!u2uMatrix[from].ContainsKey(to))
            {
                u2uMatrix[from][to] = new PostCount();
            }
            
            u2uMatrix[from][to].Increment(type);
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
            if (data[VkRestApi.ResponseBody] == null)
            {
                isRunning = false;
                return;
            }

            // calculate items in response
            var count = data[VkRestApi.ResponseBody].Count();
            Debug.WriteLine("Processing " + count + " users");

            var profiles = new List<Profile>();

            // process response body
            for (var i = 0; i < count; ++i)
            {
                var userObj = data[VkRestApi.ResponseBody][i].ToObject<JObject>();

                var profile = new Profile();
                profile.Id = Utils.GetLongField("id", userObj);

                profile.FirstName = Utils.GetTextField("first_name", userObj);
                profile.LastName = Utils.GetTextField("last_name", userObj);
                profile.ScreenName = Utils.GetTextField("screen_name", userObj);
                profile.Bdate = Utils.GetTextField("bdate", userObj);
                profile.City = Utils.GetStringField("city", "title", userObj);
                profile.Country = Utils.GetStringField("country", "title", userObj);

                profile.Photo = Utils.GetStringField("photo_50", userObj);
                profile.Sex = Utils.GetStringField("sex", userObj);
                profile.Relation = Utils.GetStringField("relation", userObj);

                // university name - text
                profile.Education = Utils.GetTextField("university_name", userObj);

                // status text
                profile.Status = Utils.GetTextField("status", userObj);

                profiles.Add(profile);

                // add graph visitor vertex
                contentNetworkAnalyzer.AddVertex(profile.Id, profile.FirstName + " " + profile.LastName, "User", userObj);

                Poster p;
                if (posters.TryGetValue(profile.Id, out p))
                {
                    var attr = dictionaryFromPoster(p);
                    // update poster vertex attributes
                    contentNetworkAnalyzer.UpdateVertexAttributes(profile.Id, attr);
                    // update user vertex attribute
                    groupNetworkAnalyzer.addVertex(profile.Id, profile.FirstName + " " + profile.LastName, "User", userObj);
                }
                else
                {
                    Debug.WriteLine("User/Poster not found with id " + profile.Id);
                }
            }

            if (profiles.Count > 0)
            {
                // update posters
                Utils.PrintFileContent(groupVisitorsWriter, profiles);
            }
        }

        // process friends list
        private void OnGetFriends(JObject data, String cookie)
        {
            if (data[VkRestApi.ResponseBody] == null)
            {
                isRunning = false;
                return;
            }

            var memberId = cookie; // member id sent as a cooky
            var mId = Convert.ToInt64(memberId);

            // now calculate items in response
            var count = data[VkRestApi.ResponseBody]["count"].ToObject<long>();
            Debug.WriteLine("Processing " + count + " friends of user id " + memberId);

            // update vertex with friends count
            if (!posters.ContainsKey(mId))
            {
                posters[mId] = new Poster();
            }

            posters[mId].Friends = count;
            Dictionary<String, String> attr = dictionaryFromPoster(posters[mId]);
            // update poster vertex attributes
            groupNetworkAnalyzer.updateVertexAttributes(mId, attr);

            // process response body
            for (int i = 0; i < count; ++i)
            {
                var friendId = data[VkRestApi.ResponseBody]["items"][i].ToObject<long>();
                groupNetworkAnalyzer.AddFriendsEdge(mId, friendId); // if friendship exists, the new edge will be added
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

        private void updateErrorLogFile(VkRestApi.OnErrorEventArgs error, StreamWriter writer)
        {
            writer.WriteLine("{0}\t{1}\t{2}\t{3}",
                error.Function, error.Code, error.Error, error.Details);
        }

        private void backgroundLikesWorker_DoWork(object sender, DoWorkEventArgs args)
        {
            var bw = sender as BackgroundWorker;

            // Extract the argument.
            var param = args.Argument as string[];

            if (bw == null ||
                param == null)
            {
                throw new ArgumentException("Illegal arguments for Likes Work");
            }

            // working directory
            var workingDir = WorkingFolderTextBox.Text;

            foreach (var s in param)
            {
                if (bw.CancellationPending)
                    break;
                
                if (String.IsNullOrWhiteSpace(s))
                    continue;
                
                // clear all
                postsWithComments.Clear(); // reset reference list
                likes.Clear(); // reset likes
                posters.Clear(); // reset posters
                posterIds.Clear(); // clear poster ids
                postInfo.Clear(); // clear post infos
                boardPostInfo.Clear(); // reset boards post infos
                topics.Clear(); // clear topics infos
                u2uMatrix.Clear(); // clear u2u matrix

                contentNetworkAnalyzer = new ContentNetworkAnalyzer();
                groupNetworkAnalyzer = new GroupNetworkAnalyzer();
                // Do Not (!) add group as a vertex to the document for this test
                //contentNetworkAnalyzer.AddVertex((long)groupId,
                //    group.name, "User", groupObject);

                postsWithComments.Add(long.Parse(s));

                networkEntities = s; // post id

                IEntity e;
                string fileName;

                var context = new VkRestApi.VkRestContext(this.userId, this.authToken);
                var sb = new StringBuilder();
                long timeLastCall = 0;

                if (postsWithComments.Count > 0 &&
                    !bw.CancellationPending)
                {
                    // group comments
                    e = new Comment();
                    fileName = Utils.GenerateFileName(workingDir, groupId, e, s);
                    groupCommentsWriter = File.CreateText(fileName);
                    Utils.PrintFileHeader(groupCommentsWriter, e);

                    // request group comments
                    bw.ReportProgress(-1, "Getting comments");

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
                            timeLastCall = Utils.SleepTime(timeLastCall);
                            // call VK REST API
                            vkRestApi.CallVkFunction(VkFunction.WallGetComments, context);

                            // wait for the user data
                            ReadyEvent.WaitOne();
                        }
                    }
                    groupCommentsWriter.Close();
                }
/*
                var like = new Like {type = "post", owner_id = (long) groupId, item_id = long.Parse(s), count = 10000};
                likes.Add(like);
*/
                // process the likes
                if (likes.Count > 0 && !bw.CancellationPending)
                {
                    // request likers ids
                    step = CalcStep(likes.Count);

                    for (var i = 0; i < likes.Count; i++)
                    {
                        isRunning = true;
                        ResetCountersAndGetReady();

                        bw.ReportProgress(step, "Getting likes " + (i + 1) + " / " + likes.Count);

                        while (ShellContinue(bw))
                        {
                            sb.Length = 0;
                            sb.Append("type=").Append(likes[i].Type).Append("&"); // type
                            sb.Append("owner_id=").Append(likes[i].OwnerId).Append("&"); // group id
                            sb.Append("item_id=").Append(likes[i].ItemId).Append("&"); // post id
                            sb.Append("offset=").Append(currentOffset).Append("&");
                            sb.Append("count=").Append(LIKES_PER_REQUEST).Append("&");
                            context.Parameters = sb.ToString();
                            context.Cookie = likes[i].ItemId.ToString(); // pass post/comment id as a cookie
                            Debug.WriteLine("Request parameters: " + context.Parameters);

                            // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                            timeLastCall = Utils.SleepTime(timeLastCall);
                            // call VK REST API
                            vkRestApi.CallVkFunction(VkFunction.LikesGetList, context);

                            // wait for the user data
                            ReadyEvent.WaitOne();
                        }
                    }
                }

                // now collect info about posters (users or visitors who left post, comment or like) 
                var visitorIds = posterIds.ToList();
                
                if (visitorIds.Count == 0)
                    continue; // no posters/likers

                if (!bw.CancellationPending)
                {
                    // group visitors profiles
                    e = new Profile();
                    fileName = Utils.GenerateFileName(workingDir, groupId, e, networkEntities);
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
                        timeLastCall = Utils.SleepTime(timeLastCall);
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

                // request members friends
                bw.ReportProgress(-1, "Getting friends network");
                step = 10000 / visitorIds.Count;

                timeLastCall = 0;
                long l = 0;
                foreach (var mId in visitorIds)
                {
                    if (bw.CancellationPending || !isRunning)
                        break;

                    bw.ReportProgress(step, "Getting friends: " + (++l) + " out of " + visitorIds.Count);

                    sb.Length = 0;
                    sb.Append("user_id=").Append(mId);
                    context.Parameters = sb.ToString();
                    context.Cookie = mId.ToString(); // pass member id as a cookie
                    Debug.WriteLine("Request parameters: " + context.Parameters);

                    // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                    timeLastCall = Utils.SleepTime(timeLastCall);
                    vkRestApi.CallVkFunction(VkFunction.FriendsGet, context);

                    // wait for the friends data
                    ReadyEvent.WaitOne();
                }

                // generate friends network file
                XmlDocument network = groupNetworkAnalyzer.GenerateGroupNetwork();
                if (network != null)
                {
                    UpdateStatus(0, "Generate Network Graph File");
                    network.Save(generateGroupMembersNetworkFileName(this.groupId, s));
                }

                // save likes network
                GenerateLikesNetworkFile();

            }

            // If the operation was canceled by the user,  
            // set the DoWorkEventArgs.Cancel property to true. 
            args.Cancel = bw.CancellationPending;

            if (!args.Cancel)
            {
                // complete the job
                //args.Result = groupNetworkAnalyzer.GenerateGroupNetwork();
            }
        }

        private void backgroundLikesWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var status = e.UserState as String;
            var prog = e.ProgressPercentage;
            UpdateStatus(prog, status);
        }

        private void backgroundLikesWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            isRunning = false;

            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message);
                UpdateStatus(0, "Error");
            }
            else if (e.Cancelled)
            {
                MessageBox.Show("Likes Work Was Canceled!");
                UpdateStatus(0, "Likes Work Canceled");
            }
            else
            {
                // save network document
/*
                XmlDocument network = e.Result as XmlDocument;
                if (network != null)
                {
                    UpdateStatus(0, "Generate Network Graph File");
                    network.Save(generateGroupMembersNetworkFileName(this.groupId));
                }
                else
                {
                    MessageBox.Show("Network document is empty!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
*/

                UpdateStatus(10000, "Likes Work Done");
            }

            ActivateControls();
        }

        // Group members Network file
        private string generateGroupMembersNetworkFileName(decimal groupId, string postId = "")
        {
            var fileName = new StringBuilder(this.WorkingFolderTextBox.Text);

            fileName.Append("\\").Append(Math.Abs(groupId).ToString()).
                Append("-").
                Append(postId).
                Append("-friends-network").
                Append(".graphml");

            return fileName.ToString();
        }

        private void GenerateLikesNetworkFile()
        {
            ResetCountersAndGetReady();
            contentNetworkAnalyzer.ResetEdges(); // remove all edges

            var uTotal = u2uMatrix.Count;
            if (uTotal == 0)
                return;

            // generate U2U edges
            foreach (var entry in u2uMatrix)
            {
                var from = entry.Key;
                foreach (var entry2 in entry.Value)
                {
                    var to = entry2.Key;
                    var weight = entry2.Value;

                    // find edge in friend's network
                    var friendsEdge = groupNetworkAnalyzer.FindEdge(from, to);
                    var relationship = friendsEdge != null ? "Friends" : "";
                    if (weight.Likes > 0)
                    {
                        contentNetworkAnalyzer.AddEdge(from, to, "Likes", relationship, "", weight.Likes, 0);                                
                    }
                }
            }

            // generate U2U network
            contentNetworkAnalyzer.GraphName = "likes";
            var network = contentNetworkAnalyzer.GenerateU2UNetwork();
            if (network != null)
            {
                UpdateStatus(0, "Save Network Graph File");
                network.Save(Utils.GenerateFileName(WorkingFolderTextBox.Text, groupId,
                    contentNetworkAnalyzer.GraphName,
                    networkEntities.Length == 0 ? "network" : networkEntities + "-network", "graphml"));
            }
        }
    }
}
