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
        private static AutoResetEvent readyEvent = new AutoResetEvent(false);
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
        List<long> postsWithComments = new List<long>();
        List<Like> likes = new List<Like>();
        Dictionary<long, Poster> posters = new Dictionary<long, Poster>();
        HashSet<long> memberIds = new HashSet<long>();
        HashSet<long> posterIds = new HashSet<long>();
        List<long> visitorIds = new List<long>();

        // group posts date range
        DateTime postsFromDate;
        DateTime postsToDate;

        // network analyzer document
        EgoNetworkAnalyzer egoNetAnalyzer = new EgoNetworkAnalyzer();
        HashSet<long> friendIds = new HashSet<long>();

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
                += new DoWorkEventHandler(groupsWork);

            this.backgroundGroupsWorker.ProgressChanged
                += new ProgressChangedEventHandler(groupsWorkProgressChanged);

            this.backgroundGroupsWorker.RunWorkerCompleted
                += new RunWorkerCompletedEventHandler(groupsWorkCompleted);

            // setup background group members worker handlers
            this.backgroundMembersWorker.DoWork
                += new DoWorkEventHandler(membersWork);

            this.backgroundMembersWorker.ProgressChanged
                += new ProgressChangedEventHandler(membersWorkProgressChanged);

            this.backgroundMembersWorker.RunWorkerCompleted
                += new RunWorkerCompletedEventHandler(membersWorkCompleted);


            // setup background group network worker handlers
            this.backgroundNetworkWorker.DoWork
                += new DoWorkEventHandler(networkWork);

            this.backgroundNetworkWorker.ProgressChanged
                += new ProgressChangedEventHandler(networkWorkProgressChanged);

            this.backgroundNetworkWorker.RunWorkerCompleted
                += new RunWorkerCompletedEventHandler(networkWorkCompleted);

            // setup background Ego net workder handlers
            this.backgroundEgoNetWorker.DoWork
                += new DoWorkEventHandler(egoNetWork);

            this.backgroundEgoNetWorker.ProgressChanged
                += new ProgressChangedEventHandler(egoNetWorkProgressChanged);

            this.backgroundEgoNetWorker.RunWorkerCompleted
                += new RunWorkerCompletedEventHandler(egoNetWorkCompleted);

            // this.folderBrowserDialog1.ShowNewFolderButton = false;
            // Default to the My Documents folder. 
            this.folderBrowserDialog1.RootFolder = Environment.SpecialFolder.Personal;
            // this.folderBrowserDialog1.RootFolder = Environment.SpecialFolder.MyComputer;
            this.WorkingFolderTextBox.Text = this.folderBrowserDialog1.SelectedPath;

            this.GroupsProgressBar.Minimum = 0;
            this.GroupsProgressBar.Maximum = 100 * POSTS_PER_REQUEST;
            this.GroupsProgressBar.Step = 1;

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
            //bool reLogin = true; // TODO: if true - will delete cookies and relogin, use false for dev.
            bool reLogin = false; 
            vkLoginDialog.Login("friends", reLogin); // default permission - friends
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
                String fileName = generateErrorLogFileName();
                errorLogWriter = File.CreateText(fileName);
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
                    readyEvent.WaitOne();

                } 
                else
                {
                    var context = new VkRestApi.VkRestContext(gid.ToString(), this.authToken);
                    vkRestApi.CallVkFunction(VkFunction.GetProfiles, context);
                    readyEvent.WaitOne();
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
            postsDialog.groupId = this.groupId; // pass saved groupId
            postsDialog.isGroup = this.isGroup; 

            if (postsDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                updateStatus(-1, "Start");
                decimal gid = this.isGroup ? decimal.Negate(this.groupId) : this.groupId;

                var param = new GroupPostsParam(gid, 
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

        private void DownloadGroupMembers_Click(object sender, EventArgs e)
        {
            var membersDialog = new DownloadGroupMembersDialog();
            membersDialog.groupId = this.groupId; // pass saved groupId
            membersDialog.isGroup = this.isGroup;

            if (membersDialog.ShowDialog() == DialogResult.OK)
            {
                updateStatus(-1, "Start");
                decimal gid = this.isGroup ? decimal.Negate(this.groupId) : this.groupId;

                isMembersRunning = true;
                this.backgroundMembersWorker.RunWorkerAsync(gid);
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
            networkDialog.groupId = this.groupId; // pass saved groupId
            networkDialog.isGroup = this.isGroup;

            if (networkDialog.ShowDialog() == DialogResult.OK)
            {
                updateStatus(-1, "Start");
                decimal gid = this.isGroup ? decimal.Negate(this.groupId) : this.groupId;

                isNetworkRunning = true;
                this.backgroundNetworkWorker.RunWorkerAsync(gid);
                ActivateControls();
            }
            else
            {
                Debug.WriteLine("Download members network canceled");
            }
        }

        private void DownloadEgoNets_Click(object sender, EventArgs e)
        {
            updateStatus(-1, "Start");
            decimal gid = this.isGroup ? decimal.Negate(this.groupId) : this.groupId;

            isEgoNetWorkRunning = true;
            this.backgroundEgoNetWorker.RunWorkerAsync(gid);
            ActivateControls();
        }

        private void CancelJobButton_Click(object sender, EventArgs e)
        {
            if (isRunning)
                this.backgroundGroupsWorker.CancelAsync();

            if (isMembersRunning)
                this.backgroundMembersWorker.CancelAsync();

            if (isNetworkRunning)
                this.backgroundNetworkWorker.CancelAsync();
        }

        private void ActivateControls()
        {
            if (isAuthorized)
            {
                // enable user controls
                if (isWorkingFolderSet)
                {
                    bool shouldActivate = this.groupId != 0;
                    bool isBusy = this.isRunning || this.isMembersRunning || this.isNetworkRunning || this.isEgoNetWorkRunning;

                    this.FindGroupsButton.Enabled = true && !isBusy;

                    // activate group buttons
                    this.DownloadGroupPosts.Enabled = shouldActivate && !isBusy;
                    this.DownloadGroupMembers.Enabled = shouldActivate && !isBusy;
                    this.DownloadMembersNetwork.Enabled = shouldActivate && !isBusy;
                    this.DownloadEgoNets.Enabled = shouldActivate && !isBusy;
                    this.CancelJobBurron.Enabled = isBusy; // todo: activate only when running
                }
            }
            else
            {
                // disable user controls
                this.FindGroupsButton.Enabled = false;
                this.DownloadGroupPosts.Enabled = false;
                this.DownloadGroupMembers.Enabled = false;
                this.DownloadMembersNetwork.Enabled = false;
                this.DownloadEgoNets.Enabled = false;
                this.CancelJobBurron.Enabled = false;
            }
        }

        private void OnUserLogin(object loginDialog, UserLoginEventArgs loginArgs)
        {
            Debug.WriteLine("User Logged In: " + loginArgs.ToString());

            this.userId = loginArgs.userId;
            this.authToken = loginArgs.authToken;
            this.expiresAt = loginArgs.expiersIn; // todo: calc expiration time

            isAuthorized = true;

            this.userIdTextBox.Clear();
            this.userIdTextBox.Text = "Authorized " + loginArgs.userId;
            
            this.ActivateControls();
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
            readyEvent.Set();
        }

        // main error handler
        private void OnError(object vkRestApi, VkRestApi.OnErrorEventArgs onErrorArgs)
        {
            // TODO: notify user about the error
            Debug.WriteLine("Function " + onErrorArgs.Function + ", returned error: " + onErrorArgs.Details);

            if (errorLogWriter != null)
            {
                updateErrorLogFile(onErrorArgs, errorLogWriter);
            }

            /*
             * till we can distinguish between critical errors and warnings, just print errors in ths log file
            MessageBox.Show(onErrorArgs.error,
                onErrorArgs.function.ToString() + " Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            */
            // indicate that data is ready and we can continue
            readyEvent.Set();
        }

        private void OnGroupsGetById(JObject data, String cookie)
        {
            if (data[VkRestApi.RESPONSE_BODY] == null)
            {
                // todo: show err
                Debug.WriteLine("Group is not found");
                return;
            }

            String gId = cookie; // group id sent as a cooky

            // now calc items in response
            int count = data[VkRestApi.RESPONSE_BODY].Count();

            List<Group> groups = new List<Group>();
            // process response body
            for (int i = 0; i < count; ++i)
            {
                JObject groupObject = data[VkRestApi.RESPONSE_BODY][i].ToObject<JObject>();

                if (groupObject == null)
                    continue;

                Group g = new Group();

                g.id = getLongField("id", groupObject);
                g.name = getStringField("name", groupObject);
                g.screen_name = getStringField("screen_name", groupObject);
                g.is_closed = getStringField("is_closed", groupObject);
                g.type = getStringField("type", groupObject);
                g.members_count = getStringField("members_count", groupObject);
                g.city = getStringField("city", "title", groupObject);
                g.country = getStringField("country", "title", groupObject);
                g.photo = getStringField("photo_50", groupObject);
                g.description = getTextField("description", groupObject);
                g.status = getTextField("status", groupObject);

                groups.Add(g);
            }

            if (groups.Count > 0)
            {
                // update group id and group info
                this.groupId = groups[0].id;

                // group members network document
                this.groupNetworkAnalyzer = new GroupNetworkAnalyzer();

                String fileName = generateGroupFileName(groupId);
                StreamWriter writer = File.CreateText(fileName);
                printGroupHeader(writer);
                updateGroupFile(groups, writer);
                writer.Close();

                this.groupId2.Text = this.groupId.ToString();
                this.groupDescription.Text = groups[0].name;
                this.groupDescription.AppendText("\r\n type: " + groups[0].type);
                this.groupDescription.AppendText("\r\n members: " + groups[0].members_count);
                this.groupDescription.AppendText("\r\n " + groups[0].description);
            }
            else
            {
                this.groupId = 0;
                this.groupId2.Text = gId;
                this.groupDescription.Text = "Not found";
            }

            ActivateControls();

            readyEvent.Set();
        }

        // process load user info response
        private void OnLoadUserInfo(JObject data)
        {
            if (data[VkRestApi.RESPONSE_BODY].Count() > 0)
            {
                JObject ego = data[VkRestApi.RESPONSE_BODY][0].ToObject<JObject>();
                Console.WriteLine("Ego: " + ego.ToString());

                // group members network document
                this.groupNetworkAnalyzer = new GroupNetworkAnalyzer();

                // update group id and group info
                this.groupId = decimal.Parse(ego["uid"].ToString());

                this.groupId2.Text = this.groupId.ToString();
                this.groupDescription.Text = ego["first_name"].ToString() + " " + ego["last_name"].ToString();
            }
            else
            {
                this.groupId = 0;
                this.groupId2.Text = "user";
                this.groupDescription.Text = "Not found";
            }

            ActivateControls();

            readyEvent.Set();
        }

        // Async workers
        
        private void groupsWork(Object sender, DoWorkEventArgs args)
        {
            // Do not access the form's BackgroundWorker reference directly. 
            // Instead, use the reference provided by the sender parameter.
            BackgroundWorker bw = sender as BackgroundWorker;

            // Extract the argument.
            GroupPostsParam param = args.Argument as GroupPostsParam;
            decimal groupId = param.gid;
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

            var context = new VkRestApi.VkRestContext(this.userId, this.authToken);
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
            String fileName = Utils.GenerateFileName(this.WorkingFolderTextBox.Text, groupId, e);
            groupPostsWriter = File.CreateText(fileName);
            Utils.PrintFileHeader(groupPostsWriter, e);

            this.postsWithComments.Clear(); // reset comments reference list
            this.likes.Clear(); // reset likes
            this.posters.Clear(); // reset posters
            this.posterIds.Clear(); // clear poster ids
            this.visitorIds.Clear(); // clear visitor ids

            this.totalCount = 0;
            this.currentOffset = 0;
            this.step = 1;

            long timeLastCall = 0;

            // get group posts 100 at a time and store them in the file
            // request group posts
            while (this.isRunning)
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
                timeLastCall = sleepTime(timeLastCall); 
                // call VK REST API
                vkRestApi.CallVkFunction(VkFunction.WallGet, context);

                // wait for the user data
                readyEvent.WaitOne();

                currentOffset += POSTS_PER_REQUEST;
            }

            groupPostsWriter.Close();

            if (postsWithComments.Count > 0)
            {
                // group comments
                e = new Comment();
                fileName = Utils.GenerateFileName(this.WorkingFolderTextBox.Text, groupId, e);
                groupCommentsWriter = File.CreateText(fileName);
                Utils.PrintFileHeader(groupCommentsWriter, e);

                // request group comments
                bw.ReportProgress(-1, "Getting comments");
                this.step = (int)(10000 / this.postsWithComments.Count);

                timeLastCall = 0;

                for (int i = 0; i < this.postsWithComments.Count; i++)
                {
                    isRunning = true;
                    this.totalCount = 0;
                    this.currentOffset = 0;

                    bw.ReportProgress(step, "Getting " + (i + 1) + " post comments out of " + this.postsWithComments.Count);

                    while (this.isRunning)
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
                        timeLastCall = sleepTime(timeLastCall);
                        // call VK REST API
                        vkRestApi.CallVkFunction(VkFunction.WallGetComments, context);

                        // wait for the user data
                        readyEvent.WaitOne();

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
                this.step = (int)(10000 / this.likes.Count);

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
                    timeLastCall = sleepTime(timeLastCall);
                    // call VK REST API
                    vkRestApi.CallVkFunction(VkFunction.LikesGetList, context);

                    // wait for the user data
                    readyEvent.WaitOne();
                }
            }

            // now collect visitors (not members, who left a post or a comment or a like) 
            foreach (long p in posterIds)
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
                this.step = (int)(10000 / visitorIds.Count);

                timeLastCall = 0;

                for (int i = 0; i < visitorIds.Count; i += 100)
                {
                    isRunning = true;
                    //this.totalCount = 0;
                    //this.currentOffset = 0;

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
                    timeLastCall = sleepTime(timeLastCall);
                    // call VK REST API
                    vkRestApi.CallVkFunction(VkFunction.UsersGet, context);

                    // wait for the user data
                    readyEvent.WaitOne();

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

        private void groupsWorkProgressChanged(Object sender, ProgressChangedEventArgs args)
        {
            String status = args.UserState as String;
            int progress = args.ProgressPercentage;
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
                MessageBox.Show("Search canceled!");
                updateStatus(0, "Canceled");
            }
            else
            {
                //MessageBox.Show("Search complete!");
                updateStatus(10000, "Done");
            }

            ActivateControls();
        }

        // Members work async handlers
        private void membersWork(Object sender, DoWorkEventArgs args)
        {
            // Do not access the form's BackgroundWorker reference directly. 
            // Instead, use the reference provided by the sender parameter.
            var bw = sender as BackgroundWorker;

            isMembersRunning = true;

            // Extract the argument. 
            var groupId = (decimal)args.Argument;

            // process group members
            IEntity e = new Profile();
            String fileName = Utils.GenerateFileName(this.WorkingFolderTextBox.Text, groupId, e, "members");
            groupMembersWriter = File.CreateText(fileName);
            Utils.PrintFileHeader(groupMembersWriter, e);

            var context = new VkRestApi.VkRestContext(this.userId, this.authToken);
            var sb = new StringBuilder();

            this.memberIds.Clear(); // clear member ids

            this.totalCount = 0;
            this.currentOffset = 0;
            this.step = 1;

            long timeLastCall = 0;

            // request group members
            while (this.isMembersRunning)
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
                timeLastCall = sleepTime(timeLastCall);
                // call VK REST API
                vkRestApi.CallVkFunction(VkFunction.GroupsGetMembers, context);

                // wait for the members data
                readyEvent.WaitOne();

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

        private void membersWorkProgressChanged(Object sender, ProgressChangedEventArgs args)
        {
            String status = args.UserState as String;
            int progress = args.ProgressPercentage;
            updateStatus(progress, status);
        }

        private void membersWorkCompleted(object sender, RunWorkerCompletedEventArgs args)
        {
            isMembersRunning = false;

            if (args.Error != null)
            {
                MessageBox.Show(args.Error.Message);
                updateStatus(0, "Error");
            }
            else if (args.Cancelled)
            {
                MessageBox.Show("Memebers Search canceled!");
                updateStatus(0, "Canceled");
            }
            else
            {
                //MessageBox.Show("Search complete!");
                updateStatus(10000, "Done");
            }

            ActivateControls();
        }


        // Members Network work async handlers
        private void networkWork(Object sender, DoWorkEventArgs args)
        {
            // Do not access the form's BackgroundWorker reference directly. 
            // Instead, use the reference provided by the sender parameter.
            BackgroundWorker bw = sender as BackgroundWorker;

            isNetworkRunning = true;

            // Extract the argument. 
            var groupId = (decimal)args.Argument;

            // gather the list of all users friends to build the group network
            this.totalCount = 0;
            this.currentOffset = 0;
            this.step = 1;

            var context = new VkRestApi.VkRestContext(this.userId, this.authToken);
            var sb = new StringBuilder();

            // consolidate all group ids
            HashSet<long> members = this.memberIds;
            // add all posters visitors ids
            foreach (long mId in visitorIds)
            {
                members.Add(mId); // could be a member or a visitor
            }

            // request members friends
            bw.ReportProgress(-1, "Getting friends network");
            this.step = (int)(10000 / members.Count);

            long timeLastCall = 0;
            long l = 0;
            foreach (long mId in members)
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
                timeLastCall = sleepTime(timeLastCall);
                vkRestApi.CallVkFunction(VkFunction.FriendsGet, context);

                // wait for the friends data
                readyEvent.WaitOne();
            }

            args.Result = this.groupNetworkAnalyzer.GenerateGroupNetwork();

            // If the operation was canceled by the user,  
            // set the DoWorkEventArgs.Cancel property to true. 
            if (bw.CancellationPending)
            {
                args.Cancel = true;
            }
        }

        private void networkWorkProgressChanged(Object sender, ProgressChangedEventArgs args)
        {
            String status = args.UserState as String;
            int progress = args.ProgressPercentage;
            updateStatus(progress, status);
        }

        private void networkWorkCompleted(object sender, RunWorkerCompletedEventArgs args)
        {
            isNetworkRunning = false;

            if (args.Error != null)
            {
                MessageBox.Show(args.Error.Message);
                updateStatus(0, "Error");
            }
            else if (args.Cancelled)
            {
                MessageBox.Show("Memebers Search canceled!");
                updateStatus(0, "Canceled");
            }
            else
            {
                // save network document
                XmlDocument network = args.Result as XmlDocument;
                if (network != null)
                {
                    updateStatus(0, "Generate Network Graph File");
                    network.Save(generateGroupMembersNetworkFileName(this.groupId));
                }
                else
                {
                    MessageBox.Show("Network document is empty!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                //MessageBox.Show("Search complete!");
                updateStatus(10000, "Done");
            }

            ActivateControls();
        }

        // Ego Net work async handlers
        private void egoNetWork(Object sender, DoWorkEventArgs args)
        {
            // Do not access the form's BackgroundWorker reference directly. 
            // Instead, use the reference provided by the sender parameter.
            BackgroundWorker bw = sender as BackgroundWorker;

            isEgoNetWorkRunning = true;

            // Extract the argument. 
            decimal groupId = (decimal)args.Argument;

            // gather the list of all users friends to build the group network
            this.totalCount = 0;
            this.currentOffset = 0;
            this.step = 1;

            // request group comments
            bw.ReportProgress(-1, "Getting members Ego network");
            this.step = (int)(10000 / memberIds.Count);

            long timeLastCall = 0;
            long l = 0;
            foreach (long mId in memberIds)
            {
                if (bw.CancellationPending || !isEgoNetWorkRunning)
                    break;

                bw.ReportProgress(step, "Getting ego nets: " + (++l) + " out of " + memberIds.Count);

                // reset friends
                this.friendIds.Clear();
                
                // reset ego net analyzer
                this.egoNetAnalyzer.Clear();

                // for each member get his ego net
                var context = new VkRestApi.VkRestContext(mId.ToString(), this.authToken);
                var sb = new StringBuilder();

                sb.Length = 0;
                sb.Append("fields=").Append(PROFILE_FIELDS);
                context.Parameters = sb.ToString();
                vkRestApi.CallVkFunction(VkFunction.LoadFriends, context);

                // wait for the friends data
                readyEvent.WaitOne();

                foreach (long targetId in this.friendIds)
                {
                    sb.Length = 0;
                    sb.Append("target_uid=").Append(targetId); // Append target friend ids
                    context.Parameters = sb.ToString();
                    context.Cookie = targetId.ToString(); // pass target id in the cookie context field

                    // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                    timeLastCall = sleepTime(timeLastCall);
                    vkRestApi.CallVkFunction(VkFunction.FriendsGetMutual, context);

                    // wait for the friends data
                    readyEvent.WaitOne();
                }

                // save ego net document
                XmlDocument egoNet = this.egoNetAnalyzer.GenerateEgoNetwork();
                egoNet.Save(generateEgoNetworkFileName(groupId, mId));

            }

            // args.Result = this.groupNetworkAnalyzer.GeneratePostersNetwork();

            // If the operation was canceled by the user,  
            // set the DoWorkEventArgs.Cancel property to true. 
            if (bw.CancellationPending)
            {
                args.Cancel = true;
            }
        }

        private void egoNetWorkProgressChanged(Object sender, ProgressChangedEventArgs args)
        {
            String status = args.UserState as String;
            int progress = args.ProgressPercentage;
            updateStatus(progress, status);
        }

        private void egoNetWorkCompleted(object sender, RunWorkerCompletedEventArgs args)
        {
            isEgoNetWorkRunning = false;

            if (args.Error != null)
            {
                MessageBox.Show(args.Error.Message);
                updateStatus(0, "Error");
            }
            else if (args.Cancelled)
            {
                MessageBox.Show("Ego Net canceled!");
                updateStatus(0, "Canceled");
            }
            else
            {
                //MessageBox.Show("Search complete!");
                updateStatus(10000, "Done");
            }

            ActivateControls();
        }

        // Status report
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

            if(this.totalCount == 0)
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

            //this.backgroundGroupsWorker.ReportProgress(0, "Processing next " + count + " posts out of " + totalCount);

            long gId = (long)(this.isGroup ? decimal.Negate(this.groupId) : this.groupId);
            var posts = new List<Post>();

            // process response body
            for (int i = 0; i < count; ++i)
            {
                var postObj = data[VkRestApi.RESPONSE_BODY]["items"][i].ToObject<JObject>();

                // see if post is in the range
                DateTime dt = getDateField("date", postObj);

                if(dt < this.postsFromDate ||
                    dt > this.postsToDate)
                {
                    continue;
                }

                var post = new Post();
                post.id = getLongField("id", postObj);
                post.owner_id = getLongField("owner_id", postObj);
                post.from_id = getLongField("from_id", postObj);
                post.signer_id = getLongField("signer_id", postObj);
                // post date
                post.date = getStringDateField("date", postObj);
                // post_type 
                post.post_type = getStringField("post_type", postObj); 
                // comments
                post.comments = getLongField("comments", "count", postObj);
                if (post.comments > 0)
                {
                    this.postsWithComments.Add(post.id); // add post's id to the ref list for comments processing
                }

                // likes
                post.likes = getLongField("likes", "count", postObj);
                if (post.likes > 0)
                {
                    var like = new Like();
                    like.type = "post";
                    like.owner_id = gId;
                    like.item_id = post.id;
                    this.likes.Add(like);
                }

                // reposts
                post.reposts = getLongField("reposts", "count", postObj);
                
                // attachments count
                if(postObj["attachments"] != null)
                {
                    post.attachments = postObj["attachments"].ToArray().Length;
                }

                // post text
                post.text = getTextField("text", postObj);

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

            long gId = (long)(this.isGroup ? decimal.Negate(this.groupId) : this.groupId);
            List<Comment> comments = new List<Comment>();

            // process response body
            for (int i = 0; i < count; ++i)
            {
                JObject postObj = data[VkRestApi.RESPONSE_BODY]["items"][i].ToObject<JObject>();
                
                Comment comment = new Comment();
                comment.id = getLongField("id", postObj);
                comment.post_id = Convert.ToInt64(cookie); // passed as a cookie
                comment.from_id = getLongField("from_id", postObj);
                // post date
                comment.date = getStringDateField("date", postObj);

                comment.reply_to_uid = getLongField("reply_to_uid", postObj);
                comment.reply_to_cid = getLongField("reply_to_cid", postObj);
                
                // likes/dislikes
                comment.likes = getLongField("likes", "count", postObj);
                if (comment.likes > 0)
                {
                    Like like = new Like();
                    like.type = "comment";
                    like.owner_id = gId;
                    like.item_id = comment.id;
                    this.likes.Add(like);
                }
 
                // attachments count
                if (postObj["attachments"] != null)
                {
                    comment.attachments = postObj["attachments"].ToArray().Length;
                }
        
                // post text
                comment.text = getTextField("text", postObj);

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
            if (data[VkRestApi.RESPONSE_BODY] == null)
            {
                this.isRunning = false;
                return;
            }

            // now calc items in response
            int count = data[VkRestApi.RESPONSE_BODY]["items"].Count();

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
            }
        }

        // process group member
        private void OnGroupsGetMembers(JObject data, String cookie)
        {
            if (data[VkRestApi.RESPONSE_BODY] == null)
            {
                this.isMembersRunning = false;
                return;
            }

            if (this.totalCount == 0)
            {
                this.totalCount = data[VkRestApi.RESPONSE_BODY]["count"].ToObject<long>();
                this.step = (int)(10000 * MEMBERS_PER_REQUEST / this.totalCount);
            }

            // now calc items in response
            int count = data[VkRestApi.RESPONSE_BODY]["items"].Count();

            List<Profile> profiles = new List<Profile>();

            // process response body
            for (int i = 0; i < count; ++i)
            {
                JObject profileObj = data[VkRestApi.RESPONSE_BODY]["items"][i].ToObject<JObject>();

                if (profileObj != null)
                {
                    String type = getStringField("type", profileObj);

                    if (type != null && type != "" && !type.Equals("profile")) 
                    {
                        Debug.WriteLine("Ignoring member with type " + type);
                        continue; // must be profile
                    }

                    Profile profile = new Profile();

                    profile.id = getLongField("id", profileObj);

                    if (profile.id <= 0)
                    {
                        // probably blocked or deleted account, continue
                        Debug.WriteLine("Ignoring member with bad profile id " + profile.id);
                        continue;
                    }

                    profile.first_name = getTextField("first_name", profileObj);
                    profile.last_name = getTextField("last_name", profileObj);
                    profile.screen_name = getTextField("screen_name", profileObj);
                    profile.bdate = getTextField("bdate", profileObj);
                    profile.city = getStringField("city", "title", profileObj);
                    profile.country = getStringField("country", "title", profileObj);

                    profile.photo = getStringField("photo_50", profileObj);
                    profile.sex = getStringField("sex", profileObj);
                    profile.relation = getStringField("relation", profileObj);
          
                    // university name - text
                    profile.education = getTextField("university_name", profileObj);

                    // status text
                    profile.status = getTextField("status", profileObj);

                    profiles.Add(profile);

                    // add graph member vertex
                    this.memberIds.Add(profile.id);

                    this.groupNetworkAnalyzer.addVertex(profile.id, profile.first_name + " " + profile.last_name, "Member", profileObj);
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
            if (data[VkRestApi.RESPONSE_BODY] == null)
            {
                this.isNetworkRunning = false;
                return;
            }

            String memberId = cookie; // member id sent as a cooky
            long mId = Convert.ToInt64(memberId);

            // now calc items in response
            var count = data[VkRestApi.RESPONSE_BODY]["count"].ToObject<long>();
            Debug.WriteLine("Processing " + count + " friends of user id " + memberId);

            // update vertex with friends count
            if (!posters.ContainsKey(mId))
            {
                posters[mId] = new Poster();
            }

            posters[mId].friends = count;
            Dictionary<String, String> attr = dictionaryFromPoster(posters[mId]);
            // update poster vertex attributes
            this.groupNetworkAnalyzer.updateVertexAttributes(mId, attr);

            // process response body
            for (int i = 0; i < count; ++i)
            {
                var friendId = data[VkRestApi.RESPONSE_BODY]["items"][i].ToObject<long>();
                this.groupNetworkAnalyzer.AddFriendsEdge(mId, friendId); // if friendship exists, the new edge will be added
            }
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

            List<Profile> profiles = new List<Profile>();
            
            // process response body
            for (int i = 0; i < count; ++i)
            {
                JObject userObj = data[VkRestApi.RESPONSE_BODY][i].ToObject<JObject>();
            
                Profile profile = new Profile();
                profile.id = getLongField("id", userObj);

                profile.first_name = getTextField("first_name", userObj);
                profile.last_name = getTextField("last_name", userObj);
                profile.screen_name = getTextField("screen_name", userObj);
                profile.bdate = getTextField("bdate", userObj);
                profile.city = getStringField("city", "title", userObj);
                profile.country = getStringField("country", "title", userObj);

                profile.photo = getStringField("photo_50", userObj);
                profile.sex = getStringField("sex", userObj);
                profile.relation = getStringField("relation", userObj);

                // university name - text
                profile.education = getTextField("university_name", userObj);

                // status text
                profile.status = getTextField("status", userObj);

                profiles.Add(profile);

                // add graph visitor vertex
                this.groupNetworkAnalyzer.addVertex(profile.id, profile.first_name + " " + profile.last_name, "Visitor", userObj);
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
            if (data[VkRestApi.RESPONSE_BODY] == null)
            {
                this.isEgoNetWorkRunning = false;
                return;
            }
            
            // now calc items in response
            int count = data[VkRestApi.RESPONSE_BODY]["items"].Count();

            // process response body
            for (int i = 0; i < count; ++i)
            {
                JObject friend = data[VkRestApi.RESPONSE_BODY]["items"][i].ToObject<JObject>();

                long id = friend["id"].ToObject<long>();
                // add user id to the friends list
                this.friendIds.Add(id);

                // add friend vertex
                this.egoNetAnalyzer.AddFriendVertex(friend);
            }
        }

        // process get mutual response
        private void OnGetMutual(JObject data, String cookie)
        {
            if (data[VkRestApi.RESPONSE_BODY] == null)
            {
                this.isEgoNetWorkRunning = false;
                return;
            }

            long mId = Convert.ToInt64(cookie); // members id passed as a cookie
            if (data[VkRestApi.RESPONSE_BODY].Count() > 0)
            {
                List<String> friendFriendsIds = new List<string>();

                for (int i = 0; i < data[VkRestApi.RESPONSE_BODY].Count(); ++i)
                {
                    long friendFriendsId = data[VkRestApi.RESPONSE_BODY][i].ToObject<long>();
                    // add friend vertex
                    this.egoNetAnalyzer.AddFriendsEdge(mId, friendFriendsId); // member id is in the cookie
                }
            }
        }

        // process stats
        private void OnStatsGet(JObject data)
        {
            if (data[VkRestApi.RESPONSE_BODY] == null)
            {
                this.isRunning = false;
                return;
            }

            // now calc items in response
            int count = data[VkRestApi.RESPONSE_BODY].Count();
            Debug.WriteLine("Processing " + count + " stats days");

            //List<DayStats> profiles = new List<DayStats>();

            // process response body
            for (int i = 0; i < count; ++i)
            {
                JObject statsObj = data[VkRestApi.RESPONSE_BODY][i].ToObject<JObject>();
                String date = getStringField("day", statsObj);
            }
        }

        // Group file name
        private string generateGroupFileName(decimal groupId)
        {
            StringBuilder fileName = new StringBuilder(this.WorkingFolderTextBox.Text);

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
            foreach (Group g in groups)
            {
                writer.WriteLine("{0}\t{1}\t{2}\t\"{3}\"\t{4}\t{5}\t{6}\t{7}\t\"{8}\"\t\"{9}\"\t\"{10}\"",
                    g.id, g.name, g.screen_name, g.is_closed, g.type, g.members_count, g.city, g.country, g.photo, g.description, g.status);
            }
        }

        // Group members Network file
        private string generateGroupMembersNetworkFileName(decimal groupId)
        {
            StringBuilder fileName = new StringBuilder(this.WorkingFolderTextBox.Text);

            fileName.Append("\\").Append(Math.Abs(groupId).ToString()).Append("-group-members-network").Append(".graphml");

            return fileName.ToString();
        }

        // Group posters Network file
        private string generateGroupPostersNetworkFileName(decimal groupId)
        {
            StringBuilder fileName = new StringBuilder(this.WorkingFolderTextBox.Text);
            fileName.Append("\\").Append(Math.Abs(groupId).ToString()).Append("-group-posters-network").Append(".graphml");
            return fileName.ToString();
        }

        // Ego Network file
        private string generateEgoNetworkFileName(decimal groupId, long memberId)
        {
            StringBuilder fileName = new StringBuilder(this.WorkingFolderTextBox.Text);

            fileName.Append("\\").Append(Math.Abs(groupId)).Append("-").Append(memberId).Append("-ego-networkd.graphml");

            return fileName.ToString();
        }

        // Error log file
        private string generateErrorLogFileName()
        {
            var fileName = new StringBuilder(this.WorkingFolderTextBox.Text);
            return fileName.Append("\\").Append("error-log").Append(".txt").ToString();
        }

        private void printErrorLogHeader(StreamWriter writer)
        {
            writer.WriteLine("\"{0}\"\t\"{1}\"\t\"{2}\"",
                    "function", "error_code", "error");
        }

        private void updateErrorLogFile(VkRestApi.OnErrorEventArgs error, StreamWriter writer)
        {
            writer.WriteLine("\"{0}\"\t\"{1}\"\t\"{2}\"",
                error.Function, error.Code, error.Error);
        }

        // Utility
        private static DateTime timeToDateTime(long unixTimeStamp)
        {
            // Unix time-stamp is seconds past epoch
            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        private static long DateTimeToUnixTimestamp(DateTime dateTime)
        {
            return (long)(dateTime - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;
        }

        long getTimeNowMillis()
        {
            return DateTime.Now.Ticks / 10000;
        }

        long sleepTime(long timeLastCall)
        {
            long timeToSleep = 339 - (getTimeNowMillis() - timeLastCall);
            if (timeToSleep > 0)
                Thread.Sleep((int)timeToSleep);

            return getTimeNowMillis();
        }

        // JObject utils
        private static String getStringField(String name, JObject o)
        {
            return o[name] != null ? o[name].ToString() : "";
        }

        private static long getLongField(String name, JObject o, long def = 0)
        {
            long result = def;

            if( o[name] != null) 
            {
                string value = o[name].ToString();

                try
                {
                    result = Convert.ToInt64(value);
                }
                catch (OverflowException)
                {
                    Debug.WriteLine("The value is outside the range of the Int64 type: " + value);
                }
                catch (FormatException)
                {
                    Debug.WriteLine("The value is not in a recognizable format: " + value);
                }
            } 

            return result;
        }

        private static String getStringField(String category, String name, JObject o)
        {
            if (o[category] != null &&
                o[category][name] != null)
            {
                return o[category][name].ToString();
            }
            return "";
        }

        private static long getLongField(String category, String name, JObject o, long def = 0)
        {
            long result = def;

            if (o[category] != null &&
                o[category][name] != null)
            {
                string value = o[category][name].ToString();

                try
                {
                    result = Convert.ToInt64(value);
                }
                catch (OverflowException)
                {
                    Debug.WriteLine("The value is outside the range of the Int64 type: " + value);
                }
                catch (FormatException)
                {
                    Debug.WriteLine("The value is not in a recognizable format: " + value);
                }
            }

            return result;
        }

        private static String getTextField(String name, JObject o)
        {
            String t = o[name] != null ? o[name].ToString() : "";
            if (t.Length > 0)
            {
                return Regex.Replace(t, @"\r\n?|\n", "");
            }
            return "";
        }

        private static String getStringDateField(String name, JObject o)
        {
            long l = o[name] != null ? o[name].ToObject<long>() : 0;
            DateTime d = timeToDateTime(l);
            return d.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private static DateTime getDateField(String name, JObject o)
        {
            long l = o[name] != null ? o[name].ToObject<long>() : 0;
            return timeToDateTime(l);
        }

        private Dictionary<String, String> dictionaryFromPoster(Poster poster) 
        {
            Dictionary<String, String> dic = new Dictionary<String, String>();

            dic.Add("posts", poster.posts.ToString());
            dic.Add("comments", poster.comments.ToString());
            dic.Add("rec_likes", poster.rec_likes.ToString());
            dic.Add("likes", poster.likes.ToString());
            dic.Add("friends", poster.friends.ToString());

            return dic;
        }

        private void userIdTextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void groupId2_TextChanged(object sender, EventArgs e)
        {

        }

        private void WorkingFolderTextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void groupDescription_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
