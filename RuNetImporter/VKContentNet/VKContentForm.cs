using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Xml;
using Newtonsoft.Json.Linq;
using rcsir.net.vk.content.NetworkAnalyzer;
using rcsir.net.vk.importer.Dialogs;
using rcsir.net.vk.importer.api;
using rcsir.net.vk.content.Dialogs;

namespace VKContentNet
{
    public partial class VKContentForm : Form
    {
        private static readonly int POSTS_PER_REQUEST = 100;
        private static readonly int MEMBERS_PER_REQUEST = 1000;
        private static readonly int LIKES_PER_REQUEST = 1000;
        private static readonly string PROFILE_FIELDS = "first_name,last_name,screen_name,bdate,city,country,photo_50,sex,relation,status,education";
        private static readonly string GROUP_FIELDS = "members_count,city,country,description,status";

        readonly private VKLoginDialog vkLoginDialog;
        readonly private VKRestApi vkRestApi;

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
        long totalCount;
        long currentOffset;
        int step;

        // group's temp collections
        readonly List<long> postsWithComments = new List<long>();
        readonly List<Like> likes = new List<Like>();
        readonly Dictionary<long, Poster> posters = new Dictionary<long, Poster>();
        readonly HashSet<long> posterIds = new HashSet<long>();
        readonly List<long> visitorIds = new List<long>();

        // group's U2U collections
        readonly Dictionary<long, PostInfo> postInfo = new Dictionary<long, PostInfo>();
        readonly Dictionary<long, Dictionary<long, int>> u2uMatrix = new Dictionary<long, Dictionary<long, int>>();
        
        // document
        StreamWriter groupPostsWriter;
        StreamWriter groupVisitorsWriter;
        StreamWriter groupCommentsWriter;

        // Network Analyzer documetn
        private ContentNetworkAnalyzer contentNetworkAnalyzer;
        
        // error log
        StreamWriter errorLogWriter;

        // group's info
        private class Group
        {
            public Group()
            {
                id = 0;
                name = "";
                screen_name = "";
                is_closed = "";
                type = "";
                members_count = "";
                city = "";
                country = "";
                photo = "";
                description = "";
                status = "";
            }

            public long id { get; set; }
            public string name { get; set; }
            public string screen_name { get; set; }
            public string is_closed { get; set; }
            public string type { get; set; }
            public string members_count { get; set; }
            public string city { get; set; }
            public string country { get; set; }
            public string photo { get; set; }
            public string description { get; set; }
            public string status { get; set; }
        };

        // group's post
        private class Post
        {
            public Post()
            {
                id = 0;
                owner_id = 0;
                from_id = 0;
                signer_id = 0;
                date = "";
                post_type = "";
                comments = 0;
                likes = 0;
                reposts = 0;
                attachments = 0;
                text = "";
            }

            public long id { get; set; }
            public long owner_id { get; set; }
            public long from_id { get; set; }
            public long signer_id { get; set; }
            public string date { get; set; }
            public string post_type { get; set; }
            public long comments { get; set; }
            public long likes { get; set; }
            public long reposts { get; set; }
            public long attachments { get; set; }
            public string text { get; set; }
        };

        private class PostInfo
        {
            public PostInfo(long id, long timestamp, long uid)
            {
                this.id = id;
                this.timestamp = timestamp;
                this.user_id = uid;
            }

            public long id { get; set; }
            public long timestamp { get; set; }
            public long user_id { get; set; }
        };

        // group's member profile
        private class Profile
        {
            public Profile()
            {
                id = 0;
                first_name = "";
                last_name = "";
                screen_name = "";
                bdate = "";
                city = "";
                country = "";
                photo = "";
                sex = "";
                relation = "";
                education = "";
                status = "";
            }

            public long id { get; set; }
            public string first_name { get; set; }
            public string last_name { get; set; }
            public string screen_name { get; set; }
            public string bdate { get; set; }
            public string city { get; set; }
            public string country { get; set; }
            public string photo { get; set; }
            public string sex { get; set; }
            public string relation { get; set; }
            public string education { get; set; }
            public string status { get; set; }
        };

        // group's comment 
        private class Comment
        {
            public Comment()
            {
                id = 0;
                post_id = 0;
                from_id = 0;
                date = "";
                reply_to_uid = 0;
                reply_to_cid = 0;
                likes = 0;
                attachments = 0;
                text = "";
            }

            public long id { get; set; }
            public long post_id { get; set; }
            public long from_id { get; set; }
            public string date { get; set; }
            public long reply_to_uid { get; set; } // user id
            public long reply_to_cid { get; set; } // comment id 
            public long likes { get; set; }
            public long attachments { get; set; }
            public string text { get; set; }
        };

        // post's or comment's like info 
        private class Like
        {
            public Like()
            {
                type = "";
                owner_id = 0;
                item_id = 0;
            }

            public string type { get; set; } // post, comment etc.
            public long owner_id { get; set; }
            public long item_id { get; set; }
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

            public decimal gid { get; set; }
            public DateTime from { get; set; }
            public DateTime to { get; set; }
            public Boolean justStats { get; set; }
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

            vkRestApi = new VKRestApi();
            // set up data handler
            vkRestApi.OnData += new VKRestApi.DataHandler(OnData);
            // set up error handler
            vkRestApi.OnError += new VKRestApi.ErrorHandler(OnError);

            // setup background group posts worker handlers
            this.backgroundGroupsWorker.DoWork
                += new DoWorkEventHandler(groupsWork);

            this.backgroundGroupsWorker.ProgressChanged
                += new ProgressChangedEventHandler(groupsWorkProgressChanged);

            this.backgroundGroupsWorker.RunWorkerCompleted
                += new RunWorkerCompletedEventHandler(groupsWorkCompleted);


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
            switch (onDataArgs.function)
            {
                case VKFunction.WallGet:
                    OnWallGet(onDataArgs.data);
                    break;
                case VKFunction.WallGetComments:
                    OnWallGetComments(onDataArgs.data, onDataArgs.cookie);
                    break;
                case VKFunction.GroupsGetById:
                    OnGroupsGetById(onDataArgs.data, onDataArgs.cookie);
                    break;
                case VKFunction.LoadUserInfo:
                    //OnLoadUserInfo(onDataArgs.data);
                    break;
                case VKFunction.LikesGetList:
                    OnLikesGetList(onDataArgs.data, onDataArgs.cookie);
                    break;
                case VKFunction.UsersGet:
                    OnUsersGet(onDataArgs.data);
                    break;
                case VKFunction.StatsGet:
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
        private void OnError(object restApi, OnErrorEventArgs onErrorArgs)
        {
            // TODO: notify user about the error
            Debug.WriteLine("Function " + onErrorArgs.function + ", returned error: " + onErrorArgs.details);

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
                    this.Authorize.Enabled = isBusy; // todo: activate only when running
                }
            }
            else
            {
                // disable user controls
                this.FindGroupsButton.Enabled = false;
                this.DownloadGroupPosts.Enabled = false;
                this.Authorize.Enabled = false;
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
                String fileName = generateErrorLogFileName();
                errorLogWriter = File.CreateText(fileName);
                errorLogWriter.AutoFlush = true;
                printErrorLogHeader(errorLogWriter);

                ActivateControls();
            }

        }

        private void FindGroupsButton_Click(object sender, EventArgs e)
        {
            var groupsDialog = new FindGroupsDialog();

            if (groupsDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                //SearchParameters searchParameters = groupsDialog.searchParameters;
                //this.backgroundFinderWorker.RunWorkerAsync(searchParameters);
                decimal gid = groupsDialog.groupId;
                isGroup = groupsDialog.isGroup;

                if (isGroup)
                {
                    // lookup a group by id
                    var context = new VKRestContext(this.userId, this.authToken);
                    var sb = new StringBuilder();
                    sb.Append("group_id=").Append(gid).Append("&");
                    sb.Append("fields=").Append(GROUP_FIELDS).Append("&");
                    context.parameters = sb.ToString();
                    context.cookie = groupsDialog.groupId.ToString();
                    Debug.WriteLine("Download parameters: " + context.parameters);

                    // call VK REST API
                    vkRestApi.CallVKFunction(VKFunction.GroupsGetById, context);

                    // wait for the user data
                    ReadyEvent.WaitOne();
                }
                else
                {
                    var context = new VKRestContext(gid.ToString(), this.authToken);
                    vkRestApi.CallVKFunction(VKFunction.LoadUserInfo, context);
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

            if (data[VKRestApi.RESPONSE_BODY] == null ||
                data[VKRestApi.RESPONSE_BODY].Count() == 0)
            {
                this.groupId = 0;
                this.groupId2.Text = gId;
                this.groupDescription.Text = "Not found";
                Debug.WriteLine("Group is not found");
                return;
            }

            // process response body
            var groupObject = data[VKRestApi.RESPONSE_BODY][0].ToObject<JObject>();
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

            String fileName = generateGroupFileName(groupId);
            StreamWriter writer = File.CreateText(fileName);
            printGroupHeader(writer);
            updateGroupFile(new List<Group>{g}, writer);
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
            var postsDialog = new DownloadGroupPostsDialog();
            postsDialog.groupId = Math.Abs(this.groupId); // pass saved groupId
            postsDialog.isGroup = this.isGroup;

            if (postsDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
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

        private void CancelJobButton_Click(object sender, EventArgs e)
        {
            if (isRunning)
                this.backgroundGroupsWorker.CancelAsync();
        }

        // Async workers
        private void groupsWork(Object sender, DoWorkEventArgs args)
        {
            // Do not access the form's BackgroundWorker reference directly. 
            // Instead, use the reference provided by the sender parameter.
            var bw = sender as BackgroundWorker;

            // Extract the argument.
            var param = args.Argument as GroupPostsParam;
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

            var context = new VKRestContext(this.userId, this.authToken);
            var sb = new StringBuilder();
            isRunning = true;

            // gather group statistics
            if (param.justStats)
            {

                bw.ReportProgress(1, "Getting group stats");

                sb.Length = 0;
                // TODO check if it takes negative number
                sb.Append("group_id=").Append(groupId.ToString()).Append("&");
                sb.Append("date_from=").Append(this.postsFromDate.ToString("yyyy-MM-dd")).Append("&");
                sb.Append("date_to=").Append(this.postsToDate.ToString("yyyy-MM-dd"));
                context.parameters = sb.ToString();
                Debug.WriteLine("Download parameters: " + context.parameters);

                // call VK REST API
                vkRestApi.CallVKFunction(VKFunction.StatsGet, context);

                return;
            }

            // create stream writers
            // 1) group posts
            String fileName = generateGroupPostsFileName(groupId);
            groupPostsWriter = File.CreateText(fileName);
            printGroupPostsHeader(groupPostsWriter);

            this.postsWithComments.Clear(); // reset comments reference list
            this.likes.Clear(); // reset likes
            this.posters.Clear(); // reset posters
            this.posterIds.Clear(); // clear poster ids
            this.visitorIds.Clear(); // clear visitor ids
            this.postInfo.Clear(); // clear post infos
            this.u2uMatrix.Clear(); // clear u2u matrix

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

                if (currentOffset > totalCount)
                {
                    // done
                    break;
                }

                bw.ReportProgress(step, "Getting " + currentOffset + " posts out of " + totalCount);

                sb.Length = 0;
                sb.Append("owner_id=").Append(groupId.ToString()).Append("&");
                sb.Append("offset=").Append(currentOffset).Append("&");
                sb.Append("count=").Append(POSTS_PER_REQUEST).Append("&");
                context.parameters = sb.ToString();
                Debug.WriteLine("Download parameters: " + context.parameters);

                context.cookie = currentOffset.ToString();

                // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                timeLastCall = Utils.sleepTime(timeLastCall);
                // call VK REST API
                vkRestApi.CallVKFunction(VKFunction.WallGet, context);

                // wait for the user data
                ReadyEvent.WaitOne();

                currentOffset += POSTS_PER_REQUEST;
            }

            groupPostsWriter.Close();

            if (postsWithComments.Count > 0 &&
                !bw.CancellationPending)
            {
                // gropu comments
                fileName = generateGroupCommentsFileName(groupId);
                groupCommentsWriter = File.CreateText(fileName);
                printGroupCommentsHeader(groupCommentsWriter);

                // request group comments
                bw.ReportProgress(-1, "Getting comments");
                this.step = (int) (10000/this.postsWithComments.Count);

                timeLastCall = 0;

                for (int i = 0; i < this.postsWithComments.Count; i++)
                {
                    isRunning = true;
                    this.totalCount = 0;
                    this.currentOffset = 0;

                    bw.ReportProgress(step,
                        "Getting " + (i + 1) + " post comments out of " + this.postsWithComments.Count);

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
                        sb.Append("owner_id=").Append(groupId).Append("&"); // group id
                        sb.Append("post_id=").Append(postsWithComments[i]).Append("&"); // post id
                        sb.Append("need_likes=").Append(1).Append("&"); // request likes info
                        sb.Append("offset=").Append(currentOffset).Append("&");
                        sb.Append("count=").Append(POSTS_PER_REQUEST).Append("&");
                        context.parameters = sb.ToString();
                        context.cookie = postsWithComments[i].ToString(); // pass post id as a cookie
                        Debug.WriteLine("Request parameters: " + context.parameters);

                        // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                        timeLastCall = Utils.sleepTime(timeLastCall);
                        // call VK REST API
                        vkRestApi.CallVKFunction(VKFunction.WallGetComments, context);

                        // wait for the user data
                        ReadyEvent.WaitOne();

                        currentOffset += POSTS_PER_REQUEST;
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
                    context.parameters = sb.ToString();
                    context.cookie = likes[i].item_id.ToString(); // pass post/comment id as a cookie
                    Debug.WriteLine("Request parameters: " + context.parameters);

                    // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                    timeLastCall = Utils.sleepTime(timeLastCall);
                    // call VK REST API
                    vkRestApi.CallVKFunction(VKFunction.LikesGetList, context);

                    // wait for the user data
                    ReadyEvent.WaitOne();
                }
            }

            // now collect infor about posters (users or visitors who left post, comment or like) 
            visitorIds.AddRange(posterIds);

            if (visitorIds.Count > 0 &&
                !bw.CancellationPending)
            {
                // group visitors profiles
                fileName = generateGroupVisitorsFileName(groupId);
                groupVisitorsWriter = File.CreateText(fileName);
                printGroupVisitorsHeader(groupVisitorsWriter);

                // request visitors info
                bw.ReportProgress(-1, "Getting visitors");
                this.step = (int) (10000/visitorIds.Count);

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

                    context.parameters = sb.ToString();
                    Debug.WriteLine("Request parameters: " + context.parameters);

                    // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                    timeLastCall = Utils.sleepTime(timeLastCall);
                    // call VK REST API
                    vkRestApi.CallVKFunction(VKFunction.UsersGet, context);

                    // wait for the user data
                    ReadyEvent.WaitOne();
                }

                groupVisitorsWriter.Close();
            }

            // If the operation was canceled by the user,  
            // set the DoWorkEventArgs.Cancel property to true. 
            if (bw.CancellationPending)
            {
                args.Cancel = true;
            }

            // update group posts/likes count
            Poster groupPoster;
            if (posters.TryGetValue((long)this.groupId, out groupPoster))
            {
                Dictionary<String, String> attr = dictionaryFromPoster(groupPoster);
                // update poster vertex attributes
                this.contentNetworkAnalyzer.updateVertexAttributes((long)this.groupId, attr);
            }

            // generate U2U edges
            generateU2UEdges();
            // generate U2U network
            args.Result = this.contentNetworkAnalyzer.GenerateU2UNetwork();
        }

        private void generateU2UEdges()
        {
            foreach (var entry in u2uMatrix)
            {
                var from = entry.Key;
                foreach (var entry2 in entry.Value)
                {
                    var to = entry2.Key;
                    var weight = entry2.Value;
                    this.contentNetworkAnalyzer.AddEdge(from,to,"Link", "Post", "", weight, 0);
                }
            }            
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
                MessageBox.Show("Search canceled!");
                updateStatus(0, "Canceled");
            }
            else
            {
                // save network document
                var network = args.Result as XmlDocument;
                if (network != null)
                {
                    updateStatus(0, "Generate Network Graph File");
                    network.Save(generateU2UNetworkFileName(this.groupId));
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
            if (data[VKRestApi.RESPONSE_BODY] == null)
            {
                this.isRunning = false;
                return;
            }

            if (this.totalCount == 0)
            {
                this.totalCount = data[VKRestApi.RESPONSE_BODY]["count"].ToObject<long>();
                if (this.totalCount == 0)
                {
                    this.isRunning = false;
                    return;
                }
                this.step = (int)(10000 * POSTS_PER_REQUEST / this.totalCount);
            }

            // now calc items in response
            int count = data[VKRestApi.RESPONSE_BODY]["items"].Count();

            //this.backgroundGroupsWorker.ReportProgress(0, "Processing next " + count + " posts out of " + totalCount);

            var posts = new List<Post>();

            // process response body
            for (int i = 0; i < count; ++i)
            {
                var postObj = data[VKRestApi.RESPONSE_BODY]["items"][i].ToObject<JObject>();

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
                    postInfo[post.id] = new PostInfo(post.id, 0, uid);
                }
            }

            // save the posts list
            updateGroupPostsFile(posts, groupPostsWriter);
        }

        // process group comments
        private void OnWallGetComments(JObject data, String cookie)
        {
            if (data[VKRestApi.RESPONSE_BODY] == null)
            {
                this.isRunning = false;
                return;
            }

            if (this.totalCount == 0)
            {
                this.totalCount = data[VKRestApi.RESPONSE_BODY]["count"].ToObject<long>();
            }

            // now calc items in response
            int count = data[VKRestApi.RESPONSE_BODY]["items"].Count();

            var comments = new List<Comment>();
            var post_id = Convert.ToInt64(cookie); // passed as a cookie

            // process response body
            for (int i = 0; i < count; ++i)
            {
                var postObj = data[VKRestApi.RESPONSE_BODY]["items"][i].ToObject<JObject>();

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
                    postInfo[comment.id] = new PostInfo(comment.id, 0, comment.from_id);
                }

                // update u2u matrix
                updateU2UMatrixForPost(comment.from_id, post_id);

                if (comment.reply_to_uid > 0)
                {
                    // update direct u2u matrix
                    updateU2UMatrix(comment.from_id, comment.reply_to_uid);
                }
            }

            // save the posts list
            updateGroupCommentsFile(comments, groupCommentsWriter);
        }

        // process likes user list
        private void OnLikesGetList(JObject data, string cookie)
        {
            if (data[VKRestApi.RESPONSE_BODY] == null)
            {
                this.isRunning = false;
                return;
            }

            // now calc items in response
            int count = data[VKRestApi.RESPONSE_BODY]["items"].Count();
            long post_id = Convert.ToInt64(cookie); // passed as a cookie

            // process response body
            for (int i = 0; i < count; ++i)
            {
                long likerId = data[VKRestApi.RESPONSE_BODY]["items"][i].ToObject<long>();
                // this user liked the subject - add him to the posters
                if (!posters.ContainsKey(likerId))
                {
                    posters[likerId] = new Poster();
                }

                posters[likerId].likes += 1; // increment poster's likes count

                this.posterIds.Add(likerId);

                // update u2u matrix
                updateU2UMatrixForPost(likerId, post_id);
            }
        }


        private void updateU2UMatrixForPost(long from, long id)
        {
            long to = 0;
            if (postInfo.ContainsKey(id))
            {
                to = postInfo[id].user_id;
            }

            if (to == 0)
            {
                // TODO: warn
                return;
            }

            if (!u2uMatrix.ContainsKey(from))
            {
                u2uMatrix[from] = new Dictionary<long, int>();
                u2uMatrix[from][to] = 1;
            }
            else if (!u2uMatrix[from].ContainsKey(to))
            {
                u2uMatrix[from][to] = 1;
            }
            else
            {
                u2uMatrix[from][to] += 1;
            }
        }

        private void updateU2UMatrix(long from, long to)
        {
            if (to <= 0)
            {
                // TODO: warn
                return;
            }

            if (!u2uMatrix.ContainsKey(from))
            {
                u2uMatrix[from] = new Dictionary<long, int>();
                u2uMatrix[from][to] = 1;
            }
            else if (!u2uMatrix[from].ContainsKey(to))
            {
                u2uMatrix[from][to] = 1;
            }
            else
            {
                u2uMatrix[from][to] += 1;
            }
        }

        // process user info
        private void OnUsersGet(JObject data)
        {
            if (data[VKRestApi.RESPONSE_BODY] == null)
            {
                this.isRunning = false;
                return;
            }

            // now calc items in response
            int count = data[VKRestApi.RESPONSE_BODY].Count();
            Debug.WriteLine("Processing " + count + " users");

            var profiles = new List<Profile>();

            // process response body
            for (int i = 0; i < count; ++i)
            {
                var userObj = data[VKRestApi.RESPONSE_BODY][i].ToObject<JObject>();

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
                updateGroupVisitorsFile(profiles, groupVisitorsWriter);
            }
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

        // Error Log File
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

        private void updateErrorLogFile(OnErrorEventArgs error, StreamWriter writer)
        {
            writer.WriteLine("\"{0}\"\t\"{1}\"\t\"{2}\"",
                error.function, error.code, error.error);
        }

        private void VKContentForm_FormClosing(object sender, System.Windows.Forms.FormClosingEventArgs e)
        {
            if (errorLogWriter != null)
            {
                errorLogWriter.Flush();
                errorLogWriter.Close();
            }
        }

        // Group file name
        private string generateGroupFileName(decimal groupId)
        {
            var fileName = new StringBuilder(this.WorkingFolderTextBox.Text);

            fileName.Append("\\").Append(Math.Abs(groupId).ToString()).Append("-group");
            fileName.Append(".txt");

            return fileName.ToString();
        }

        private void printGroupHeader(StreamWriter writer)
        {
            writer.WriteLine("\"{0}\"\t\"{1}\"\t\"{2}\"\t\"{3}\"\t\"{4}\"\t\"{5}\"\t\"{6}\"\t\"{7}\"\t\"{8}\"\t\"{9}\"\t\"{10}\"",
                    "id", "name", "screen_name", "is_closed", "type", "members_count", "city", "country", "photo", "description", "status");
        }

        private void updateGroupFile(IEnumerable<Group> groups, StreamWriter writer)
        {
            foreach (var g in groups)
            {
                writer.WriteLine("{0}\t{1}\t{2}\t\"{3}\"\t{4}\t{5}\t{6}\t{7}\t\"{8}\"\t\"{9}\"\t\"{10}\"",
                    g.id, g.name, g.screen_name, g.is_closed, g.type, g.members_count, g.city, g.country, g.photo, g.description, g.status);
            }
        }
        // Group posts file
        private string generateGroupPostsFileName(decimal groupId)
        {
            var fileName = new StringBuilder(this.WorkingFolderTextBox.Text);

            fileName.Append("\\").Append(Math.Abs(groupId).ToString()).Append("-group-posts");
            fileName.Append(".txt");

            return fileName.ToString();
        }

        private void printGroupPostsHeader(StreamWriter writer)
        {
            writer.WriteLine("{0}\t\"{1}\"\t\"{2}\"\t\"{3}\"\t\"{4}\"\t\"{5}\"\t\"{6}\"\t\"{7}\"\t\"{8}\"\t\"{9}\"\t\"{10}\"",
                    "id", "owner", "from", "signer", "date", "post_type", "comments", "likes", "reposts", "attachments", "text");
        }

        private void updateGroupPostsFile(IEnumerable<Post> posts, StreamWriter writer)
        {
            foreach (var p in posts)
            {
                writer.WriteLine("{0}\t{1}\t{2}\t{3}\t\"{4}\"\t\"{5}\"\t{6}\t{7}\t{8}\t{9}\t\"{10}\"",
                    p.id, p.owner_id, p.from_id, p.signer_id, p.date, p.post_type, p.comments, p.likes, p.reposts, p.attachments, p.text);
            }
        }

        // Group visitors profiles file
        private string generateGroupVisitorsFileName(decimal groupId)
        {
            var fileName = new StringBuilder(this.WorkingFolderTextBox.Text);

            fileName.Append("\\").Append(Math.Abs(groupId).ToString()).Append("-group-visitors");
            fileName.Append(".txt");

            return fileName.ToString();
        }

        private void printGroupVisitorsHeader(StreamWriter writer)
        {
            writer.WriteLine("\"{0}\"\t\"{1}\"\t\"{2}\"\t\"{3}\"\t\"{4}\"\t\"{5}\"",
                    "id", "first_name", "last_name", "screen_name", "sex", "photo");
        }

        private void updateGroupVisitorsFile(IEnumerable<Profile> profiles, StreamWriter writer)
        {
            foreach (Profile p in profiles)
            {
                writer.WriteLine("{0}\t\"{1}\"\t\"{2}\"\t\"{3}\"\t{4}\t\"{5}\"",
                    p.id, p.first_name, p.last_name, p.screen_name, p.sex, p.photo);
            }
        }

        // Group comments file name
        private string generateGroupCommentsFileName(decimal groupId)
        {
            var fileName = new StringBuilder(this.WorkingFolderTextBox.Text);

            fileName.Append("\\").Append(Math.Abs(groupId).ToString()).Append("-group-comments");
            fileName.Append(".txt");

            return fileName.ToString();
        }

        private void printGroupCommentsHeader(StreamWriter writer)
        {
            writer.WriteLine("\"{0}\"\t\"{1}\"\t\"{2}\"\t\"{3}\"\t\"{4}\"\t\"{5}\"\t\"{6}\"\t\"{7}\"\t\"{8}\"",
                    "id", "post_id", "from", "date", "reply_to_user", "reply_to_comment", "likes", "attachments", "text");
        }

        private void updateGroupCommentsFile(IEnumerable<Comment> comments, StreamWriter writer)
        {
            foreach (var c in comments)
            {
                writer.WriteLine("{0}\t{1}\t{2}\t\"{3}\"\t{4}\t{5}\t{6}\t{7}\t\"{8}\"",
                    c.id, c.post_id, c.from_id, c.date, c.reply_to_uid, c.reply_to_cid, c.likes, c.attachments, c.text);
            }
        }

        // User 2 User  Network file
        private string generateU2UNetworkFileName(decimal groupId)
        {
            var fileName = new StringBuilder(WorkingFolderTextBox.Text);

            fileName.Append("\\").Append(Math.Abs(groupId).ToString()).Append("-group-user-network").Append(".graphml");

            return fileName.ToString();
        }
    }
}
