using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Xml;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using rcsir.net.vk.importer.api.entity;
using rcsir.net.vk.importer.Dialogs;
using rcsir.net.vk.importer.api;
using rcsir.net.vk.groups.Dialogs;
using rcsir.net.vk.groups.NetworkAnalyzer;
using Group = rcsir.net.vk.importer.api.entity.Group;

namespace VKGroups
{
    public partial class VKGroupsForm : Form
    {
        private static readonly int POSTS_PER_REQUEST = 100;
        private static readonly int MEMBERS_PER_REQUEST = 1000;
        private static readonly int LIKES_PER_REQUEST = 1000;
        private static readonly string PROFILE_FIELDS = "first_name,last_name,screen_name,bdate,city,country,photo_50,sex,relation,status,education";
        private static readonly string GROUP_FIELDS = "members_count,city,country,description,status";

        private VKLoginDialog vkLoginDialog;
        private VkRestApi vkRestApi;
        private String userId;
        private String authToken;
        private long expiresAt;
        private decimal groupId;
        private bool isGroup;
        private bool isWorkingFolderSet;
        private bool isAuthorized;
        private static readonly AutoResetEvent ReadyEvent = new AutoResetEvent(false);
        private volatile bool isRunning;
        private volatile bool isMembersRunning;
        private volatile bool isNetworkRunning;
        private volatile bool isEgoNetWorkRunning;

        // document
        StreamWriter groupPostsWriter;
        StreamWriter groupCommentsWriter;
        StreamWriter groupMembersWriter;
        StreamWriter groupVisitorsWriter;
        // error log
        StreamWriter errorLogWriter;

        // network analyzer document
        GroupNetworkAnalyzer groupNetworkAnalyzer;

        // progress
        long totalCount;
        long currentOffset;
        int step;

        // group's temp collections
        readonly List<long> postsWithComments = new List<long>();
        readonly List<Like> likes = new List<Like>();
        readonly Dictionary<long, Poster> posters = new Dictionary<long, Poster>();
        readonly HashSet<long> memberIds = new HashSet<long>();
        readonly HashSet<long> posterIds = new HashSet<long>();
        readonly List<long> visitorIds = new List<long>();

        // group posts date range
        DateTime postsFromDate;
        DateTime postsToDate;

        // network analyzer document
        readonly EgoNetworkAnalyzer egoNetAnalyzer = new EgoNetworkAnalyzer();
        readonly HashSet<long> friendIds = new HashSet<long>();

        // group stats 
        private class GroupStats
        {
            public GroupStats()
            {
                day = DateTime.Today;
                views = 0;
                visitors = 0;
                reach = 0;
                reach_subscribers = 0;
                subscribed = 0;
                unsubscribed = 0;
            }

            public DateTime day { get; set; }
            public uint views { get; set; }
            public uint visitors { get; set; }
            public uint reach { get; set; }
            public uint reach_subscribers { get; set; }
            public uint subscribed { get; set; }
            public uint unsubscribed { get; set; }

        };

        // poster info 
        private class Poster
        {
            public Poster()
            {
                posts = 0;
                comments = 0;
                rec_likes = 0;
                likes = 0;
                friends = 0;
            }

            public long posts { get; set; }
            public long comments { get; set; }
            public long rec_likes { get; set; }
            public long likes { get; set; }
            public long friends { get; set; }
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

            public decimal gid { get; set;  }
            public DateTime from { get; set;  }
            public DateTime to { get; set; }
            public Boolean justStats { get; set; }
        };

        // Constructor

        public VKGroupsForm()
        {
            InitializeComponent();
            userIdTextBox.Text = @"Please authorize";
            groupId = 0;
            isAuthorized = false;
            isWorkingFolderSet = false;
        
            vkLoginDialog = new VKLoginDialog();
            // subscribe for login events
            vkLoginDialog.OnUserLogin += OnUserLogin;

            vkRestApi = new VkRestApi();
            // set up data handler
            vkRestApi.OnData += OnData;
            // set up error handler
            vkRestApi.OnError += OnError;

            // setup background group posts worker handlers
            backgroundGroupsWorker.DoWork
                += GroupsWork;

            backgroundGroupsWorker.ProgressChanged
                += GroupsWorkProgressChanged;

            backgroundGroupsWorker.RunWorkerCompleted
                += GroupsWorkCompleted;

            // setup background group members worker handlers
            backgroundMembersWorker.DoWork
                += MembersWork;

            backgroundMembersWorker.ProgressChanged
                += MembersWorkProgressChanged;

            backgroundMembersWorker.RunWorkerCompleted
                += MembersWorkCompleted;


            // setup background group network worker handlers
            backgroundNetworkWorker.DoWork
                += NetworkWork;

            backgroundNetworkWorker.ProgressChanged
                += NetworkWorkProgressChanged;

            backgroundNetworkWorker.RunWorkerCompleted
                += NetworkWorkCompleted;

            // setup background Ego net worker handlers
            backgroundEgoNetWorker.DoWork
                += EgoNetWork;

            backgroundEgoNetWorker.ProgressChanged
                += EgoNetWorkProgressChanged;

            backgroundEgoNetWorker.RunWorkerCompleted
                += EgoNetWorkCompleted;

            // this.folderBrowserDialog1.ShowNewFolderButton = false;
            // Default to the My Documents folder. 
            folderBrowserDialog1.RootFolder = Environment.SpecialFolder.Personal;
            // this.folderBrowserDialog1.RootFolder = Environment.SpecialFolder.MyComputer;
            WorkingFolderTextBox.Text = folderBrowserDialog1.SelectedPath;

            GroupsProgressBar.Minimum = 0;
            GroupsProgressBar.Maximum = 100 * POSTS_PER_REQUEST;
            GroupsProgressBar.Step = 1;

            ActivateControls();
        }

        // form load
        private void VKGroupsForm_Load(object sender, EventArgs e)
        {

        }

        // form closing
        private void VKGroupsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (errorLogWriter != null)
            {
                errorLogWriter.Flush();
                errorLogWriter.Close();
            }
        }

        private void AuthorizeButton_Click(object sender, EventArgs e)
        {
            //var reLogin = true; // TODO: if true - will delete cookies and relogin, use false for dev.
            var reLogin = false; 
            vkLoginDialog.Login("friends", reLogin); // default permission - friends
        }

        private void WorkingFolderButton_Click(object sender, EventArgs e)
        {
            // Show the FolderBrowserDialog.
            var result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                WorkingFolderTextBox.Text = folderBrowserDialog1.SelectedPath;
                isWorkingFolderSet = true;
                
                // if error log exists - close it and create new one
                if (errorLogWriter != null)
                {
                    errorLogWriter.Close();
                    errorLogWriter = null;
                }
                var fileName = GenerateErrorLogFileName();
                errorLogWriter = File.CreateText(fileName);
                errorLogWriter.AutoFlush = true;
                printErrorLogHeader(errorLogWriter);

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
                var gid = groupsDialog.groupId;
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

        private void DownloadGroupPosts_Click(object sender, EventArgs e)
        {
            var postsDialog = new DownloadGroupPostsDialog();
            postsDialog.groupId = groupId; // pass saved groupId
            postsDialog.isGroup = isGroup; 

            if (postsDialog.ShowDialog() == DialogResult.OK)
            {
                UpdateStatus(-1, "Start");
                var gid = isGroup ? decimal.Negate(groupId) : groupId;

                var param = new GroupPostsParam(gid, 
                    postsDialog.fromDate, postsDialog.toDate, postsDialog.justGroupStats);

                isRunning = true;
                backgroundGroupsWorker.RunWorkerAsync(param);
                ActivateControls();
            }
            else
            {
                Debug.WriteLine("Download posts canceled");
            }
        }

        private void DownloadGroupMembers_Click(object sender, EventArgs e)
        {
            var membersDialog = new DownloadGroupMembersDialog();
            membersDialog.groupId = groupId; // pass saved groupId
            membersDialog.isGroup = isGroup;

            if (membersDialog.ShowDialog() == DialogResult.OK)
            {
                UpdateStatus(-1, "Start");
                var gid = isGroup ? decimal.Negate(groupId) : groupId;

                isMembersRunning = true;
                backgroundMembersWorker.RunWorkerAsync(gid);
                ActivateControls();
            }
            else
            {
                Debug.WriteLine("Download members canceled");
            }
        }

        private void DownloadMembersNetwork_Click(object sender, EventArgs e)
        {
            var networkDialog = new DownloadMembersNetworkDialog();
            networkDialog.groupId = groupId; // pass saved groupId
            networkDialog.isGroup = isGroup;

            if (networkDialog.ShowDialog() == DialogResult.OK)
            {
                UpdateStatus(-1, "Start");
                var gid = isGroup ? decimal.Negate(groupId) : groupId;

                isNetworkRunning = true;
                backgroundNetworkWorker.RunWorkerAsync(gid);
                ActivateControls();
            }
            else
            {
                Debug.WriteLine("Download members network canceled");
            }
        }

        private void DownloadEgoNets_Click(object sender, EventArgs e)
        {
            UpdateStatus(-1, "Start");
            var gid = isGroup ? decimal.Negate(groupId) : groupId;

            isEgoNetWorkRunning = true;
            backgroundEgoNetWorker.RunWorkerAsync(gid);
            ActivateControls();
        }

        private void CancelJobButton_Click(object sender, EventArgs e)
        {
            if (isRunning)
                backgroundGroupsWorker.CancelAsync();

            if (isMembersRunning)
                backgroundMembersWorker.CancelAsync();

            if (isNetworkRunning)
                backgroundNetworkWorker.CancelAsync();
        }

        private void ActivateControls()
        {
            if (isAuthorized)
            {
                // enable user controls
                if (isWorkingFolderSet)
                {
                    var shouldActivate = groupId != 0;
                    var isBusy = isRunning || isMembersRunning || isNetworkRunning || isEgoNetWorkRunning;

                    FindGroupsButton.Enabled = true && !isBusy;

                    // activate group buttons
                    DownloadGroupPosts.Enabled = shouldActivate && !isBusy;
                    DownloadGroupMembers.Enabled = shouldActivate && !isBusy;
                    
                    // these buttons should be activated only when members or visitors downloaded
                    var hasMemebers = memberIds.Any() || visitorIds.Any();

                    DownloadMembersNetwork.Enabled = shouldActivate && !isBusy && hasMemebers;
                    DownloadEgoNets.Enabled = shouldActivate && !isBusy && hasMemebers;
                    
                    CancelJobBurron.Enabled = isBusy; 
                }
            }
            else
            {
                // disable user controls
                FindGroupsButton.Enabled = false;
                DownloadGroupPosts.Enabled = false;
                DownloadGroupMembers.Enabled = false;
                DownloadMembersNetwork.Enabled = false;
                DownloadEgoNets.Enabled = false;
                CancelJobBurron.Enabled = false;
            }
        }

        private void OnUserLogin(object loginDialog, UserLoginEventArgs loginArgs)
        {
            Debug.WriteLine("User Logged In: " + loginArgs);

            userId = loginArgs.userId;
            authToken = loginArgs.authToken;
            expiresAt = loginArgs.expiersIn; // todo: calc expiration time

            isAuthorized = true;

            userIdTextBox.Clear();
            userIdTextBox.Text = "Authorized " + loginArgs.userId;
            
            ActivateControls();
        }

        private void OnData(object vkRestApi, OnDataEventArgs onDataArgs)
        {
            switch (onDataArgs.Function)
            {
                case VkFunction.WallGet:
                    OnWallGet(onDataArgs.Data);
                    break;
                case VkFunction.WallGetComments:
                    OnWallGetComments(onDataArgs.Data, onDataArgs.Cookie);
                    break;
                case VkFunction.GroupsGetMembers:
                    OnGroupsGetMembers(onDataArgs.Data, onDataArgs.Cookie);
                    break;
                case VkFunction.FriendsGet:
                    OnGetFriends(onDataArgs.Data, onDataArgs.Cookie);
                    break;
                case VkFunction.GroupsGetById:
                    OnGroupsGetById(onDataArgs.Data, onDataArgs.Cookie);
                    break;
                case VkFunction.GetProfiles:
                    OnLoadUserInfo(onDataArgs.Data);
                    break;
                case VkFunction.LikesGetList:
                    OnLikesGetList(onDataArgs.Data);
                    break;
                case VkFunction.UsersGet:
                    OnUsersGet(onDataArgs.Data);
                    break;
                case VkFunction.LoadFriends:
                    OnLoadFriends(onDataArgs.Data);
                    break;
                case VkFunction.FriendsGetMutual:
                    OnGetMutual(onDataArgs.Data, onDataArgs.Cookie);
                    break;
                case VkFunction.StatsGet:
                    OnStatsGet(onDataArgs.Data);
                    break;
                default:
                    Debug.WriteLine("Error, unknown function.");
                    break;
            }

            // indicate that data is ready and we can continue
            ReadyEvent.Set();
        }

        // main error handler
        private void OnError(object vkApi, VkRestApi.OnErrorEventArgs onErrorArgs)
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
                        Utils.sleepTime(0);
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

        private void OnGroupsGetById(JObject data, String cookie)
        {
            if (data[VkRestApi.ResponseBody] == null)
            {
                // todo: show err
                Debug.WriteLine("Group is not found");
                return;
            }

            var gId = cookie; // group id sent as a cooky

            // now calculate items in response
            var count = data[VkRestApi.ResponseBody].Count();

            var groups = new List<Group>();
            // process response body
            for (var i = 0; i < count; ++i)
            {
                var groupObject = data[VkRestApi.ResponseBody][i].ToObject<JObject>();

                if (groupObject == null)
                    continue;

                var g = new Group();

                g.id = Utils.getLongField("id", groupObject);
                g.name = Utils.getStringField("name", groupObject);
                g.screen_name = Utils.getStringField("screen_name", groupObject);
                g.is_closed = Utils.getStringField("is_closed", groupObject);
                g.type = Utils.getStringField("type", groupObject);
                g.members_count = Utils.getStringField("members_count", groupObject);
                g.city = Utils.getStringField("city", "title", groupObject);
                g.country = Utils.getStringField("country", "title", groupObject);
                g.photo = Utils.getStringField("photo_50", groupObject);
                g.description = Utils.getTextField("description", groupObject);
                g.status = Utils.getTextField("status", groupObject);

                groups.Add(g);
            }

            if (groups.Count > 0)
            {
                // update group id and group info
                groupId = groups[0].id;

                // group members network document
                groupNetworkAnalyzer = new GroupNetworkAnalyzer();

                var fileName = GenerateGroupFileName(groupId);
                var writer = File.CreateText(fileName);
                printGroupHeader(writer);
                updateGroupFile(groups, writer);
                writer.Close();

                groupId2.Text = groupId.ToString();
                groupDescription.Text = groups[0].name;
                groupDescription.AppendText("\r\n type: " + groups[0].type);
                groupDescription.AppendText("\r\n members: " + groups[0].members_count);
                groupDescription.AppendText("\r\n " + groups[0].description);
            }
            else
            {
                groupId = 0;
                groupId2.Text = gId;
                groupDescription.Text = "Not found";
            }

            ActivateControls();

            ReadyEvent.Set();
        }

        // process load user info response
        private void OnLoadUserInfo(JObject data)
        {
            if (data[VkRestApi.ResponseBody].Any())
            {
                var ego = data[VkRestApi.ResponseBody][0].ToObject<JObject>();
                Console.WriteLine("Ego: " + ego);

                // group members network document
                groupNetworkAnalyzer = new GroupNetworkAnalyzer();

                // update group id and group info
                groupId = decimal.Parse(ego["uid"].ToString());

                groupId2.Text = groupId.ToString();
                groupDescription.Text = ego["first_name"] + " " + ego["last_name"];
            }
            else
            {
                groupId = 0;
                groupId2.Text = "user";
                groupDescription.Text = "Not found";
            }

            ActivateControls();

            ReadyEvent.Set();
        }

        // Async workers
        
        private void GroupsWork(Object sender, DoWorkEventArgs args)
        {
            // Do not access the form's BackgroundWorker reference directly. 
            // Instead, use the reference provided by the sender parameter.
            var bw = sender as BackgroundWorker;

            // Extract the argument.
            var param = args.Argument as GroupPostsParam;
            var groupId = param.gid;
            if (param.from <= param.to)
            {
                postsFromDate = param.from;
                postsToDate = param.to;
            }
            else
            {
                postsFromDate = param.to;
                postsToDate = param.from;
            }

            var context = new VkRestApi.VkRestContext(userId, authToken);
            var sb = new StringBuilder();
            isRunning = true;

            // gather group statistics
            if (param.justStats)
            {

                bw.ReportProgress(1, "Getting group stats");

                sb.Length = 0;
                sb.Append("group_id=").Append(Math.Abs(groupId).ToString()).Append("&");
                sb.Append("date_from=").Append(this.postsFromDate.ToString("yyyy-MM-dd")).Append("&");
                sb.Append("date_to=").Append(this.postsToDate.ToString("yyyy-MM-dd"));
                context.Parameters = sb.ToString();
                Debug.WriteLine("Download parameters: " + context.Parameters);

                // call VK REST API
                vkRestApi.CallVkFunction(VkFunction.StatsGet, context);

                return;
            }

            // create stream writers
            // 1) group posts
            // group posts
            IEntity e = new Post();
            String fileName = Utils.GenerateFileName(WorkingFolderTextBox.Text, groupId, e);
            groupPostsWriter = File.CreateText(fileName);
            Utils.PrintFileHeader(groupPostsWriter, e);

            postsWithComments.Clear(); // reset comments reference list
            likes.Clear(); // reset likes
            posters.Clear(); // reset posters
            posterIds.Clear(); // clear poster ids
            visitorIds.Clear(); // clear visitor ids

            totalCount = 0;
            currentOffset = 0;
            step = 1;

            long timeLastCall = 0;

            // get group posts 100 at a time and store them in the file
            // request group posts
            while (isRunning)
            {
                if (bw.CancellationPending)
                    break;

                if(currentOffset > totalCount)
                {
                    // done
                    break;
                }

                bw.ReportProgress(step, "Getting " + currentOffset + " posts out of " + totalCount);

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

                currentOffset += POSTS_PER_REQUEST;
            }

            groupPostsWriter.Close();

            if (postsWithComments.Count > 0)
            {
                // group comments
                e = new Comment();
                fileName = Utils.GenerateFileName(WorkingFolderTextBox.Text, groupId, e);
                groupCommentsWriter = File.CreateText(fileName);
                Utils.PrintFileHeader(groupCommentsWriter, e);

                // request group comments
                bw.ReportProgress(-1, "Getting comments");
                step = 10000 / postsWithComments.Count;

                timeLastCall = 0;

                for (var i = 0; i < postsWithComments.Count; i++)
                {
                    isRunning = true;
                    totalCount = 0;
                    currentOffset = 0;

                    bw.ReportProgress(step, "Getting " + (i + 1) + " post comments out of " + postsWithComments.Count);

                    while (isRunning)
                    {
                        if (bw.CancellationPending)
                            break;

                        if (currentOffset > totalCount)
                        {
                            // done
                            break;
                        }

                        sb.Length = 0;
                        sb.Append("owner_id=").Append(groupId.ToString()).Append("&"); // group id
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

                        currentOffset += POSTS_PER_REQUEST;
                    }
                }

                groupCommentsWriter.Close();
            }

            // process the likers - they will be added to the posters
            if (likes.Count > 0)
            {
                // request likers ids
                bw.ReportProgress(-1, "Getting likers");
                step = 10000 / likes.Count;

                timeLastCall = 0;

                for (int i = 0; i < this.likes.Count; i++)
                {
                    isRunning = true;
                    //this.totalCount = 0;
                    //this.currentOffset = 0;

                    bw.ReportProgress(step, "Getting " + (i + 1) + " likes out of " + this.likes.Count);

                    if (bw.CancellationPending)
                        break;

                    sb.Length = 0;
                    sb.Append("type=").Append(likes[i].type).Append("&"); // group id
                    sb.Append("owner_id=").Append(likes[i].owner_id).Append("&"); // group id
                    sb.Append("item_id=").Append(likes[i].item_id).Append("&"); // post id
                    sb.Append("count=").Append(LIKES_PER_REQUEST).Append("&");
                    context.Parameters = sb.ToString();
                    Debug.WriteLine("Request parameters: " + context.Parameters);

                    // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                    timeLastCall = Utils.sleepTime(timeLastCall);
                    // call VK REST API
                    vkRestApi.CallVkFunction(VkFunction.LikesGetList, context);

                    // wait for the user data
                    ReadyEvent.WaitOne();
                }
            }

            // now collect visitors (not members, who left a post or a comment or a like) 
            foreach (var p in posterIds)
            {
                if (!memberIds.Contains(p))
                {
                    // this is a visitor poster
                    visitorIds.Add(p);
                }
            }

            if (visitorIds.Count > 0)
            {
                // group visitors profiles
                e = new Profile();
                fileName = Utils.GenerateFileName(this.WorkingFolderTextBox.Text, groupId, e, "visitor");
                groupVisitorsWriter = File.CreateText(fileName);
                Utils.PrintFileHeader(groupVisitorsWriter, e);

                // request visitors info
                bw.ReportProgress(-1, "Getting visitors");
                step = 10000 / visitorIds.Count;

                timeLastCall = 0;

                for (var i = 0; i < visitorIds.Count; i += 100)
                {
                    isRunning = true;
                    //this.totalCount = 0;
                    //this.currentOffset = 0;

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

            //args.Result = TimeConsumingOperation(bw, arg);

            // If the operation was canceled by the user,  
            // set the DoWorkEventArgs.Cancel property to true. 
            if (bw.CancellationPending)
            {
                args.Cancel = true;
            }
        }

        private void GroupsWorkProgressChanged(Object sender, ProgressChangedEventArgs args)
        {
            var status = args.UserState as String;
            var progress = args.ProgressPercentage;
            UpdateStatus(progress, status);
        }

        private void GroupsWorkCompleted(object sender, RunWorkerCompletedEventArgs args)
        {
            isRunning = false;

            if (args.Error != null)
            {
                MessageBox.Show(args.Error.Message);
                UpdateStatus(0, "Error");
            }
            else if (args.Cancelled)
            {
                MessageBox.Show("Search canceled!");
                UpdateStatus(0, "Canceled");
            }
            else
            {
                //MessageBox.Show("Search complete!");
                UpdateStatus(10000, "Done");
            }

            ActivateControls();
        }

        // Members work async handlers
        private void MembersWork(Object sender, DoWorkEventArgs args)
        {
            // Do not access the form's BackgroundWorker reference directly. 
            // Instead, use the reference provided by the sender parameter.
            var bw = sender as BackgroundWorker;

            isMembersRunning = true;

            // Extract the argument. 
            var groupId = (decimal)args.Argument;

            // process group members
            IEntity e = new Profile();
            var fileName = Utils.GenerateFileName(this.WorkingFolderTextBox.Text, groupId, e, "members");
            groupMembersWriter = File.CreateText(fileName);
            Utils.PrintFileHeader(groupMembersWriter, e);

            var context = new VkRestApi.VkRestContext(this.userId, this.authToken);
            var sb = new StringBuilder();

            memberIds.Clear(); // clear member ids

            totalCount = 0;
            currentOffset = 0;
            step = 1;

            long timeLastCall = 0;

            // request group members
            while (isMembersRunning)
            {
                if (bw.CancellationPending)
                    break;

                if (currentOffset > totalCount)
                {
                    // done
                    break;
                }

                bw.ReportProgress(step, "Getting " + currentOffset + " members out of " + totalCount);

                sb.Length = 0;
                decimal gid = Math.Abs(groupId); // in this request, group id must be positive
                sb.Append("group_id=").Append(gid.ToString()).Append("&");
                sb.Append("sort=").Append("id_asc").Append("&");
                sb.Append("offset=").Append(currentOffset).Append("&");
                sb.Append("count=").Append(MEMBERS_PER_REQUEST).Append("&");
                sb.Append("fields=").Append(PROFILE_FIELDS).Append("&");
                context.Parameters = sb.ToString();
                Debug.WriteLine("Request parameters: " + context.Parameters);

                context.Cookie = currentOffset.ToString();

                // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                timeLastCall = Utils.sleepTime(timeLastCall);
                // call VK REST API
                vkRestApi.CallVkFunction(VkFunction.GroupsGetMembers, context);

                // wait for the members data
                ReadyEvent.WaitOne();

                currentOffset += MEMBERS_PER_REQUEST;
            }

            groupMembersWriter.Close();

            //args.Result = TimeConsumingOperation(bw, arg);

            // If the operation was canceled by the user,  
            // set the DoWorkEventArgs.Cancel property to true. 
            if (bw.CancellationPending)
            {
                args.Cancel = true;
            }
        }

        private void MembersWorkProgressChanged(Object sender, ProgressChangedEventArgs args)
        {
            var status = args.UserState as String;
            var progress = args.ProgressPercentage;
            UpdateStatus(progress, status);
        }

        private void MembersWorkCompleted(object sender, RunWorkerCompletedEventArgs args)
        {
            isMembersRunning = false;

            if (args.Error != null)
            {
                MessageBox.Show(args.Error.Message);
                UpdateStatus(0, "Error");
            }
            else if (args.Cancelled)
            {
                MessageBox.Show("Memebers Search canceled!");
                UpdateStatus(0, "Canceled");
            }
            else
            {
                //MessageBox.Show("Search complete!");
                UpdateStatus(10000, "Done");
            }

            ActivateControls();
        }


        // Members Network work async handlers
        private void NetworkWork(Object sender, DoWorkEventArgs args)
        {
            // Do not access the form's BackgroundWorker reference directly. 
            // Instead, use the reference provided by the sender parameter.
            var bw = sender as BackgroundWorker;

            isNetworkRunning = true;

            // Extract the argument. 
            // var groupId = (decimal)args.Argument;

            // gather the list of all users friends to build the group network
            totalCount = 0;
            currentOffset = 0;
            step = 1;

            var context = new VkRestApi.VkRestContext(userId, authToken);
            var sb = new StringBuilder();

            // consolidate all group ids
            var members = this.memberIds;
            // add all posters visitors ids
            foreach (var mId in visitorIds)
            {
                members.Add(mId); // could be a member or a visitor
            }

            // request members friends
            bw.ReportProgress(-1, "Getting friends network");
            step = 10000 / members.Count;

            long timeLastCall = 0;
            long l = 0;
            foreach (var mId in members)
            {
                if (bw.CancellationPending || !isNetworkRunning)
                    break;

                bw.ReportProgress(step, "Getting friends: " + (++l) + " out of " + members.Count);

                sb.Length = 0;
                sb.Append("user_id=").Append(mId);
                context.Parameters = sb.ToString();
                context.Cookie = mId.ToString(); // pass member id as a cookie
                Debug.WriteLine("Request parameters: " + context.Parameters);

                // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                timeLastCall = Utils.sleepTime(timeLastCall);
                vkRestApi.CallVkFunction(VkFunction.FriendsGet, context);

                // wait for the friends data
                ReadyEvent.WaitOne();
            }

            args.Result = groupNetworkAnalyzer.GenerateGroupNetwork();

            // If the operation was canceled by the user,  
            // set the DoWorkEventArgs.Cancel property to true. 
            if (bw.CancellationPending)
            {
                args.Cancel = true;
            }
        }

        private void NetworkWorkProgressChanged(Object sender, ProgressChangedEventArgs args)
        {
            var status = args.UserState as String;
            var progress = args.ProgressPercentage;
            UpdateStatus(progress, status);
        }

        private void NetworkWorkCompleted(object sender, RunWorkerCompletedEventArgs args)
        {
            isNetworkRunning = false;

            if (args.Error != null)
            {
                MessageBox.Show(args.Error.Message);
                UpdateStatus(0, "Error");
            }
            else if (args.Cancelled)
            {
                MessageBox.Show("Members Search canceled!");
                UpdateStatus(0, "Canceled");
            }
            else
            {
                // save network document
                XmlDocument network = args.Result as XmlDocument;
                if (network != null)
                {
                    UpdateStatus(0, "Generate Network Graph File");
                    network.Save(generateGroupMembersNetworkFileName(this.groupId));
                }
                else
                {
                    MessageBox.Show("Network document is empty!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                //MessageBox.Show("Search complete!");
                UpdateStatus(10000, "Done");
            }

            ActivateControls();
        }

        // Ego Net work async handlers
        private void EgoNetWork(Object sender, DoWorkEventArgs args)
        {
            // Do not access the form's BackgroundWorker reference directly. 
            // Instead, use the reference provided by the sender parameter.
            var bw = sender as BackgroundWorker;

            isEgoNetWorkRunning = true;

            // Extract the argument. 
            var groupId = (decimal)args.Argument;

            // gather the list of all users friends to build the group network
            totalCount = 0;
            currentOffset = 0;
            step = 1;

            // request group comments
            bw.ReportProgress(-1, "Getting members Ego network");
            step = 10000 / memberIds.Count;

            long timeLastCall = 0;
            long l = 0;
            foreach (long mId in memberIds)
            {
                if (bw.CancellationPending || !isEgoNetWorkRunning)
                    break;

                bw.ReportProgress(step, "Getting ego nets: " + (++l) + " out of " + memberIds.Count);

                // reset friends
                friendIds.Clear();
                
                // reset ego net analyzer
                egoNetAnalyzer.Clear();

                // for each member get his ego net
                var context = new VkRestApi.VkRestContext(mId.ToString(), authToken);
                var sb = new StringBuilder();

                sb.Length = 0;
                sb.Append("fields=").Append(PROFILE_FIELDS);
                context.Parameters = sb.ToString();
                vkRestApi.CallVkFunction(VkFunction.LoadFriends, context);

                // wait for the friends data
                ReadyEvent.WaitOne();

                foreach (long targetId in friendIds)
                {
                    sb.Length = 0;
                    sb.Append("target_uid=").Append(targetId); // Append target friend ids
                    context.Parameters = sb.ToString();
                    context.Cookie = targetId.ToString(); // pass target id in the cookie context field

                    // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                    timeLastCall = Utils.sleepTime(timeLastCall);
                    vkRestApi.CallVkFunction(VkFunction.FriendsGetMutual, context);

                    // wait for the friends data
                    ReadyEvent.WaitOne();
                }

                // save ego net document
                XmlDocument egoNet = egoNetAnalyzer.GenerateEgoNetwork();
                egoNet.Save(GenerateEgoNetworkFileName(groupId, mId));

            }

            // args.Result = this.groupNetworkAnalyzer.GeneratePostersNetwork();

            // If the operation was canceled by the user,  
            // set the DoWorkEventArgs.Cancel property to true. 
            if (bw.CancellationPending)
            {
                args.Cancel = true;
            }
        }

        private void EgoNetWorkProgressChanged(Object sender, ProgressChangedEventArgs args)
        {
            var status = args.UserState as String;
            var progress = args.ProgressPercentage;
            UpdateStatus(progress, status);
        }

        private void EgoNetWorkCompleted(object sender, RunWorkerCompletedEventArgs args)
        {
            isEgoNetWorkRunning = false;

            if (args.Error != null)
            {
                MessageBox.Show(args.Error.Message);
                UpdateStatus(0, "Error");
            }
            else if (args.Cancelled)
            {
                MessageBox.Show("Ego Net canceled!");
                UpdateStatus(0, "Canceled");
            }
            else
            {
                //MessageBox.Show("Search complete!");
                UpdateStatus(10000, "Done");
            }

            ActivateControls();
        }

        // Status report
        private void UpdateStatus(int progress, String status)
        {
            // if 0 - ignore progress parameter
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
            if (data[VkRestApi.ResponseBody] == null)
            {
                isRunning = false;
                return;
            }

            if(totalCount == 0)
            {
                totalCount = data[VkRestApi.ResponseBody]["count"].ToObject<long>();
                if (totalCount == 0)
                {
                    isRunning = false;
                    return;
                }
                step = (int)(10000 * POSTS_PER_REQUEST / totalCount);
            }

            // now calculate items in response
            var count = data[VkRestApi.ResponseBody]["items"].Count();

            //this.backgroundGroupsWorker.ReportProgress(0, "Processing next " + count + " posts out of " + totalCount);

            var gId = (long)(isGroup ? decimal.Negate(groupId) : groupId);
            var posts = new List<Post>();

            // process response body
            for (var i = 0; i < count; ++i)
            {
                var postObj = data[VkRestApi.ResponseBody]["items"][i].ToObject<JObject>();

                // see if post is in the range
                var dt = Utils.getDateField("date", postObj);

                if(dt < postsFromDate ||
                    dt > postsToDate)
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
                    postsWithComments.Add(post.id); // add post's id to the ref list for comments processing
                }

                // likes
                post.likes = Utils.getLongField("likes", "count", postObj);
                if (post.likes > 0)
                {
                    var like = new Like();
                    like.type = "post";
                    like.owner_id = gId;
                    like.item_id = post.id;
                    likes.Add(like);
                }

                // reposts
                post.reposts = Utils.getLongField("reposts", "count", postObj);
                
                // attachments count
                if(postObj["attachments"] != null)
                {
                    post.attachments = postObj["attachments"].ToArray().Length;
                }

                // post text
                post.text = Utils.getTextField("text", postObj);

                // update posters if different from the group
                if (post.from_id != gId)
                {
                    if (!posters.ContainsKey(post.from_id))
                    {
                        posters[post.from_id] = new Poster();
                    }

                    posters[post.from_id].posts += 1; // increment number of posts
                    posters[post.from_id].rec_likes += post.likes;
                    
                    // add to the poster ids
                    posterIds.Add(post.from_id);
                }

                // if post has a signer - update posters
                if (post.signer_id > 0 && post.signer_id != post.from_id)
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

                posts.Add(post);
            }

            if (posts.Count > 0)
            {
                // save the posts list
                Utils.PrintFileContent(groupPostsWriter, posts);
            }
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
                totalCount = data[VkRestApi.ResponseBody]["count"].ToObject<long>();
            }
            
            // now calculate items in response
            var count = data[VkRestApi.ResponseBody]["items"].Count();

            var gId = (long)(isGroup ? decimal.Negate(groupId) : groupId);
            var comments = new List<Comment>();

            // process response body
            for (var i = 0; i < count; ++i)
            {
                var postObj = data[VkRestApi.ResponseBody]["items"][i].ToObject<JObject>();
                
                var comment = new Comment();
                comment.id = Utils.getLongField("id", postObj);
                comment.post_id = Convert.ToInt64(cookie); // passed as a cookie
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
                    like.owner_id = gId;
                    like.item_id = comment.id;
                    likes.Add(like);
                }
 
                // attachments count
                if (postObj["attachments"] != null)
                {
                    comment.attachments = postObj["attachments"].ToArray().Length;
                }
        
                // post text
                comment.text = Utils.getTextField("text", postObj);

                // update posters
                if (comment.from_id != gId)
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
            }

            if (comments.Count > 0)
            {
                // save the posts list
                Utils.PrintFileContent(groupCommentsWriter, comments);
            }
        }

        // process likes user list
        private void OnLikesGetList(JObject data)
        {
            if (data[VkRestApi.ResponseBody] == null)
            {
                isRunning = false;
                return;
            }

            // now calculate items in response
            var count = data[VkRestApi.ResponseBody]["items"].Count();

            // process response body
            for (var i = 0; i < count; ++i)
            {
                var likerId = data[VkRestApi.ResponseBody]["items"][i].ToObject<long>();
                // this user liked the subject - add him to the posters
                if (!posters.ContainsKey(likerId))
                {
                    posters[likerId] = new Poster();
                }

                posters[likerId].likes += 1; // increment poster's likes count

                posterIds.Add(likerId);
            }
        }

        // process group member
        private void OnGroupsGetMembers(JObject data, String cookie)
        {
            if (data[VkRestApi.ResponseBody] == null)
            {
                isMembersRunning = false;
                return;
            }

            if (totalCount == 0)
            {
                totalCount = data[VkRestApi.ResponseBody]["count"].ToObject<long>();
                step = (int)(10000 * MEMBERS_PER_REQUEST / totalCount);
            }

            // now calculate items in response
            var count = data[VkRestApi.ResponseBody]["items"].Count();

            var profiles = new List<Profile>();

            // process response body
            for (var i = 0; i < count; ++i)
            {
                var profileObj = data[VkRestApi.ResponseBody]["items"][i].ToObject<JObject>();

                if (profileObj != null)
                {
                    var type = Utils.getStringField("type", profileObj);

                    if (!string.IsNullOrEmpty(type) && !type.Equals("profile")) 
                    {
                        Debug.WriteLine("Ignoring member with type " + type);
                        continue; // must be profile
                    }

                    var profile = new Profile();

                    profile.id = Utils.getLongField("id", profileObj);

                    if (profile.id <= 0)
                    {
                        // probably blocked or deleted account, continue
                        Debug.WriteLine("Ignoring member with bad profile id " + profile.id);
                        continue;
                    }

                    profile.first_name = Utils.getTextField("first_name", profileObj);
                    profile.last_name = Utils.getTextField("last_name", profileObj);
                    profile.screen_name = Utils.getTextField("screen_name", profileObj);
                    profile.deactivated = Utils.getTextField("deactivated", profileObj);
                    profile.bdate = Utils.getTextField("bdate", profileObj);
                    profile.city = Utils.getStringField("city", "title", profileObj);
                    profile.country = Utils.getStringField("country", "title", profileObj);

                    profile.photo = Utils.getStringField("photo_50", profileObj);
                    profile.sex = Utils.getStringField("sex", profileObj);
                    profile.relation = Utils.getStringField("relation", profileObj);
          
                    // university name - text
                    profile.education = Utils.getTextField("university_name", profileObj);

                    // status text
                    profile.status = Utils.getTextField("status", profileObj);

                    profiles.Add(profile);

                    // add graph member vertex
                    memberIds.Add(profile.id);

                    groupNetworkAnalyzer.addVertex(profile.id, profile.first_name + " " + profile.last_name, "Member", profileObj);
                }
            }

            if (profiles.Count > 0)
            {
                // save the posts list
                Utils.PrintFileContent(groupMembersWriter, profiles);
            }
        }

        // process friends list
        private void OnGetFriends(JObject data, String cookie)
        {
            if (data[VkRestApi.ResponseBody] == null)
            {
                isNetworkRunning = false;
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

            posters[mId].friends = count;
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

        // process user info
        private void OnUsersGet(JObject data)
        {
            if (data[VkRestApi.ResponseBody] == null)
            {
                isRunning = false;
                return;
            }

            // now calculate items in response
            var count = data[VkRestApi.ResponseBody].Count();
            Debug.WriteLine("Processing " + count + " users");

            var profiles = new List<Profile>();
            
            // process response body
            for (var i = 0; i < count; ++i)
            {
                var userObj = data[VkRestApi.ResponseBody][i].ToObject<JObject>();
            
                var profile = new Profile();
                profile.id = Utils.getLongField("id", userObj);

                profile.first_name = Utils.getTextField("first_name", userObj);
                profile.last_name = Utils.getTextField("last_name", userObj);
                profile.screen_name = Utils.getTextField("screen_name", userObj);
                profile.deactivated = Utils.getTextField("deactivated", userObj);
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
                groupNetworkAnalyzer.addVertex(profile.id, profile.first_name + " " + profile.last_name, "Visitor", userObj);
            }

            if (profiles.Count > 0)
            {
                // update posters
                Utils.PrintFileContent(groupVisitorsWriter, profiles);
            }
        }

        // for user's EGO nets
        // process load user friends response
        private void OnLoadFriends(JObject data)
        {
            if (data[VkRestApi.ResponseBody] == null)
            {
                isEgoNetWorkRunning = false;
                return;
            }
            
            // now calculate items in response
            var count = data[VkRestApi.ResponseBody]["items"].Count();

            // process response body
            for (int i = 0; i < count; ++i)
            {
                var friend = data[VkRestApi.ResponseBody]["items"][i].ToObject<JObject>();

                var id = friend["id"].ToObject<long>();
                // add user id to the friends list
                friendIds.Add(id);

                // add friend vertex
                egoNetAnalyzer.AddFriendVertex(friend);
            }
        }

        // process get mutual response
        private void OnGetMutual(JObject data, String cookie)
        {
            if (data[VkRestApi.ResponseBody] == null)
            {
                isEgoNetWorkRunning = false;
                return;
            }

            long mId = Convert.ToInt64(cookie); // members id passed as a cookie
            if (data[VkRestApi.ResponseBody].Any())
            {
                for (int i = 0; i < data[VkRestApi.ResponseBody].Count(); ++i)
                {
                    var friendFriendsId = data[VkRestApi.ResponseBody][i].ToObject<long>();
                    // add friend vertex
                    egoNetAnalyzer.AddFriendsEdge(mId, friendFriendsId); // member id is in the cookie
                }
            }
        }

        // process stats
        private void OnStatsGet(JObject data)
        {
            if (data[VkRestApi.ResponseBody] == null)
            {
                isRunning = false;
                return;
            }

            // now calculate items in response
            var count = data[VkRestApi.ResponseBody].Count();
            Debug.WriteLine("Processing " + count + " stats days");

            //List<DayStats> profiles = new List<DayStats>();

            // process response body
            for (int i = 0; i < count; ++i)
            {
                var statsObj = data[VkRestApi.ResponseBody][i].ToObject<JObject>();
                var date = Utils.getStringField("day", statsObj);
            }
        }

        // Group file name
        private string GenerateGroupFileName(decimal groupId)
        {
            var fileName = new StringBuilder(WorkingFolderTextBox.Text);

            fileName.Append("\\").Append(Math.Abs(groupId).ToString()).Append("-group");
            fileName.Append(".txt");

            return fileName.ToString();
        }

        private void printGroupHeader(StreamWriter writer)
        {
            writer.WriteLine("\"{0}\"\t\"{1}\"\t\"{2}\"\t\"{3}\"\t\"{4}\"\t\"{5}\"\t\"{6}\"\t\"{7}\"\t\"{8}\"\t\"{9}\"\t\"{10}\"",
                    "id", "name", "screen_name", "is_closed", "type", "members_count", "city", "country", "photo", "description", "status");
        }

        private void updateGroupFile(List<Group> groups, StreamWriter writer)
        {
            foreach (var g in groups)
            {
                writer.WriteLine("{0}\t{1}\t{2}\t\"{3}\"\t{4}\t{5}\t{6}\t{7}\t\"{8}\"\t\"{9}\"\t\"{10}\"",
                    g.id, g.name, g.screen_name, g.is_closed, g.type, g.members_count, g.city, g.country, g.photo, g.description, g.status);
            }
        }

        // Group members Network file
        private string generateGroupMembersNetworkFileName(decimal groupId)
        {
            var fileName = new StringBuilder(this.WorkingFolderTextBox.Text);

            fileName.Append("\\").Append(Math.Abs(groupId).ToString()).Append("-group-members-network").Append(".graphml");

            return fileName.ToString();
        }

        // Ego Network file
        private string GenerateEgoNetworkFileName(decimal groupId, long memberId)
        {
            var fileName = new StringBuilder(WorkingFolderTextBox.Text);

            fileName.Append("\\").Append(Math.Abs(groupId)).Append("-").Append(memberId).Append("-ego-networkd.graphml");

            return fileName.ToString();
        }

        // Error log file
        private string GenerateErrorLogFileName()
        {
            var fileName = new StringBuilder(WorkingFolderTextBox.Text);
            return fileName.Append("\\").Append("error-log").Append(".txt").ToString();
        }

        private void printErrorLogHeader(StreamWriter writer)
        {
            writer.WriteLine("{0}\t{1}\t{2}\t{3}",
                    "function", "error_code", "error", "details");
        }

        private void updateErrorLogFile(VkRestApi.OnErrorEventArgs error, StreamWriter writer)
        {
            writer.WriteLine("{0}\t{1}\t{2}\t{3}",
                error.Function, error.Code, error.Error, error.Details);
        }

        private Dictionary<String, String> dictionaryFromPoster(Poster poster) 
        {
            var dic = new Dictionary<String, String>();

            dic.Add("posts", poster.posts.ToString());
            dic.Add("comments", poster.comments.ToString());
            dic.Add("rec_likes", poster.rec_likes.ToString());
            dic.Add("likes", poster.likes.ToString());
            dic.Add("friends", poster.friends.ToString());

            return dic;
        }
    }
}
