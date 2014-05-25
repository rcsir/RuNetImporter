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

using rcsir.net.vk.importer.Dialogs;
using rcsir.net.vk.importer.api;
using rcsir.net.vk.groups.Dialogs;
using rcsir.net.vk.groups.NetworkAnalyzer;

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
        private VKRestApi vkRestApi;
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
        private volatile bool isPostersNetwork; // todo: do it better

        // document
        StreamWriter groupPostsWriter;
        StreamWriter groupVisitorsWriter;
        StreamWriter groupCommentsWriter;
        StreamWriter groupMembersWriter;
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

        // network analyzer document
        EgoNetworkAnalyzer egoNetAnalyzer = new EgoNetworkAnalyzer();
        HashSet<long> friendIds = new HashSet<long>();

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
            bool reLogin = false; // TODO: if true - will delete cookies and relogin, use false for dev.
            vkLoginDialog.Login("friends", reLogin); // default permission - friends
        }

        private void button1_Click(object sender, EventArgs e)
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
            FindGroupsDialog groupsDialog = new FindGroupsDialog();

            if (groupsDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                //SearchParameters searchParameters = groupsDialog.searchParameters;
                //this.backgroundFinderWorker.RunWorkerAsync(searchParameters);
                decimal gid = groupsDialog.groupId;
                isGroup = groupsDialog.isGroup;

                if (isGroup)
                {
                    // lookup a group by id
                    VKRestContext context = new VKRestContext(this.userId, this.authToken);
                    StringBuilder sb = new StringBuilder();
                    sb.Append("group_id=").Append(gid).Append("&");
                    sb.Append("fields=").Append(GROUP_FIELDS).Append("&");
                    context.parameters = sb.ToString();
                    context.cookie = groupsDialog.groupId.ToString();
                    Debug.WriteLine("Download parameters: " + context.parameters);

                    // call VK REST API
                    vkRestApi.CallVKFunction(VKFunction.GroupsGetById, context);

                    // wait for the user data
                    readyEvent.WaitOne();

                } 
                else
                {
                    VKRestContext context = new VKRestContext(gid.ToString(), this.authToken);
                    vkRestApi.CallVKFunction(VKFunction.LoadUserInfo, context);
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
            DownloadGroupPostsDialog postsDialog = new DownloadGroupPostsDialog();
            postsDialog.groupId = this.groupId; // pass saved groupId
            postsDialog.isGroup = this.isGroup; 

            if (postsDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                updateStatus(-1, "Start");
                decimal gid = this.isGroup ? decimal.Negate(this.groupId) : this.groupId;

                isRunning = true;
                this.backgroundGroupsWorker.RunWorkerAsync(gid);
                ActivateControls();
            }
            else
            {
                Debug.WriteLine("Download posts canceled");
            }
        }

        private void DownloadGroupMembers_Click(object sender, EventArgs e)
        {
            DownloadGroupMembersDialog membersDialog = new DownloadGroupMembersDialog();
            membersDialog.groupId = this.groupId; // pass saved groupId
            membersDialog.isGroup = this.isGroup;

            if (membersDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
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
            DownloadMembersNetworkDialog networkDialog = new DownloadMembersNetworkDialog();
            networkDialog.groupId = this.groupId; // pass saved groupId
            networkDialog.isGroup = this.isGroup;

            if (networkDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                updateStatus(-1, "Start");
                decimal gid = this.isGroup ? decimal.Negate(this.groupId) : this.groupId;

                isNetworkRunning = true;
                isPostersNetwork = false; // note this flag is FALSE!
                this.backgroundNetworkWorker.RunWorkerAsync(gid);
                ActivateControls();
            }
            else
            {
                Debug.WriteLine("Download members network canceled");
            }
        }

        private void DownloadPostersNetwork_Click(object sender, EventArgs e)
        {
            DownloadPostersNetworkDialog postersDialog = new DownloadPostersNetworkDialog();
            postersDialog.groupId = this.groupId; // pass saved groupId
            postersDialog.isGroup = this.isGroup;

            if (postersDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                updateStatus(-1, "Start");
                decimal gid = this.isGroup ? decimal.Negate(this.groupId) : this.groupId;
                
                isNetworkRunning = true;
                isPostersNetwork = true; // note this flag is TRUE!
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

        private void CancelJobBurron_Click(object sender, EventArgs e)
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
                    this.DownloadPostersNetwork.Enabled = shouldActivate && !isBusy;
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
                this.DownloadPostersNetwork.Enabled = false;
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
            switch (onDataArgs.function)
            {
                case VKFunction.WallGet:
                    OnWallGet(onDataArgs.data);
                    break;
                case VKFunction.WallGetComments:
                    OnWallGetComments(onDataArgs.data, onDataArgs.cookie);
                    break;
                case VKFunction.GroupsGetMembers:
                    OnGroupsGetMembers(onDataArgs.data, onDataArgs.cookie);
                    break;
                case VKFunction.GetFriends:
                    OnGetFriends(onDataArgs.data, onDataArgs.cookie);
                    break;
                case VKFunction.GroupsGetById:
                    OnGroupsGetById(onDataArgs.data, onDataArgs.cookie);
                    break;
                case VKFunction.LoadUserInfo:
                    OnLoadUserInfo(onDataArgs.data);
                    break;
                case VKFunction.LikesGetList:
                    OnLikesGetList(onDataArgs.data);
                    break;
                case VKFunction.UsersGet:
                    OnUsersGet(onDataArgs.data);
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
        private void OnError(object vkRestApi, OnErrorEventArgs onErrorArgs)
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
            readyEvent.Set();
        }

        private void OnGroupsGetById(JObject data, String cookie)
        {
            if (data[VKRestApi.RESPONSE_BODY] == null)
            {
                // todo: show err
                Debug.WriteLine("Group is not found");
                return;
            }

            String gId = cookie; // gropu id sent as a cooky

            // now calc items in response
            int count = data[VKRestApi.RESPONSE_BODY].Count();

            List<Group> groups = new List<Group>();
            // process response body
            for (int i = 0; i < count; ++i)
            {
                JObject groupObject = data[VKRestApi.RESPONSE_BODY][i].ToObject<JObject>();

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
            if (data[VKRestApi.RESPONSE_BODY].Count() > 0)
            {
                JObject ego = data[VKRestApi.RESPONSE_BODY][0].ToObject<JObject>();
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
            decimal groupId = (decimal)args.Argument;

            isRunning = true;

            // create stream writers
            // 1) group posts
            String fileName = generateGroupPostsFileName(groupId);
            groupPostsWriter = File.CreateText(fileName);
            printGroupPostsHeader(groupPostsWriter);

            VKRestContext context = new VKRestContext(this.userId, this.authToken);

            // get group posts 100 at a time and store them in the file
            StringBuilder sb = new StringBuilder();

            this.postsWithComments.Clear(); // reset comments reference list
            this.likes.Clear(); // reset likes
            this.posters.Clear(); // reset posters
            this.posterIds.Clear(); // clear poster ids

            this.totalCount = 0;
            this.currentOffset = 0;
            this.step = 1;

            long timeLastCall = 0;

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
                context.parameters = sb.ToString();
                Debug.WriteLine("Download parameters: " + context.parameters);

                context.cookie = currentOffset.ToString();

                // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                timeLastCall = sleepTime(timeLastCall); 
                // call VK REST API
                vkRestApi.CallVKFunction(VKFunction.WallGet, context);

                // wait for the user data
                readyEvent.WaitOne();

                currentOffset += POSTS_PER_REQUEST;
            }

            groupPostsWriter.Close();

            if (postsWithComments.Count > 0)
            {
                // gropu comments
                fileName = generateGroupCommentsFileName(groupId);
                groupCommentsWriter = File.CreateText(fileName);
                printGroupCommentsHeader(groupCommentsWriter);

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
                        context.parameters = sb.ToString();
                        context.cookie = postsWithComments[i].ToString(); // pass post id as a cookie
                        Debug.WriteLine("Request parameters: " + context.parameters);

                        // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                        timeLastCall = sleepTime(timeLastCall);
                        // call VK REST API
                        vkRestApi.CallVKFunction(VKFunction.WallGetComments, context);

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
                    context.parameters = sb.ToString();
                    Debug.WriteLine("Request parameters: " + context.parameters);

                    // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                    timeLastCall = sleepTime(timeLastCall);
                    // call VK REST API
                    vkRestApi.CallVKFunction(VKFunction.LikesGetList, context);

                    // wait for the user data
                    readyEvent.WaitOne();
                }
            }

            // now collect visitors posters (not members, who left a post or a comment or a like) 
            List<long> visitors = new List<long>();
            foreach (long p in posterIds)
            {
                if (!memberIds.Contains(p))
                {
                    // this is a visitor poster
                    visitors.Add(p);
                }
            }

            // group visitors profiles
            fileName = generateGroupVisitorsFileName(groupId);
            groupVisitorsWriter = File.CreateText(fileName);
            printGroupVisitorsHeader(groupVisitorsWriter);

            // request visitors info
            bw.ReportProgress(-1, "Getting visitors");
            this.step = (int)(10000 / visitors.Count);

            timeLastCall = 0;

            for (int i = 0; i < visitors.Count; i+=100)
            {
                isRunning = true;
                //this.totalCount = 0;
                //this.currentOffset = 0;

                bw.ReportProgress(step, "Getting " + (i + 1) + " visitors out of " + visitors.Count);

                if (bw.CancellationPending)
                    break;

                sb.Length = 0;

                sb.Append("user_ids=");

                for (int j = i; j < visitors.Count && j < i + 100; ++j)
                {
                    sb.Append(visitors[j]).Append(","); // users
                }

                sb.Append("&").Append("fields=").Append(PROFILE_FIELDS);

                context.parameters = sb.ToString();
                Debug.WriteLine("Request parameters: " + context.parameters);

                // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                timeLastCall = sleepTime(timeLastCall);
                // call VK REST API
                vkRestApi.CallVKFunction(VKFunction.UsersGet, context);

                // wait for the user data
                readyEvent.WaitOne();

            }

            groupVisitorsWriter.Close();

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
            BackgroundWorker bw = sender as BackgroundWorker;

            isMembersRunning = true;

            // Extract the argument. 
            decimal groupId = (decimal)args.Argument;

            // process group members
            String fileName = generateGroupMembersFileName(groupId);
            groupMembersWriter = File.CreateText(fileName);
            printGroupMembersHeader(groupMembersWriter);

            VKRestContext context = new VKRestContext(this.userId, this.authToken);
            StringBuilder sb = new StringBuilder();

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
                context.parameters = sb.ToString();
                Debug.WriteLine("Request parameters: " + context.parameters);

                context.cookie = currentOffset.ToString();

                // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                timeLastCall = sleepTime(timeLastCall);
                // call VK REST API
                vkRestApi.CallVKFunction(VKFunction.GroupsGetMembers, context);

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
            decimal groupId = (decimal)args.Argument;

            // gather the list of all users friends to build the group network
            this.totalCount = 0;
            this.currentOffset = 0;
            this.step = 1;

            VKRestContext context = new VKRestContext(this.userId, this.authToken);
            StringBuilder sb = new StringBuilder();

            HashSet<long> members;
            
            if(isPostersNetwork) 
            {
                members = this.posterIds;

                // add all posters vertices first
                foreach (long mId in members)
                {
                    this.groupNetworkAnalyzer.addPosterVertex(mId); // could be a member or a visitor
                }
            }
            else 
            {
                members = this.memberIds;
            }

            // request group comments
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
                context.parameters = sb.ToString();
                context.cookie = mId.ToString(); // pass member id as a cookie
                Debug.WriteLine("Request parameters: " + context.parameters);

                // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                timeLastCall = sleepTime(timeLastCall);
                vkRestApi.CallVKFunction(VKFunction.GetFriends, context);

                // wait for the friends data
                readyEvent.WaitOne();
            }

            if (isPostersNetwork)
            {
                args.Result = this.groupNetworkAnalyzer.GeneratePostersNetwork();
            }
            else
            {
                args.Result = this.groupNetworkAnalyzer.GenerateGroupNetwork();
            }

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
                    if (isPostersNetwork)
                    {
                        network.Save(generateGroupPostersNetworkFileName(this.groupId));
                    }
                    else
                    {
                        network.Save(generateGroupMembersNetworkFileName(this.groupId));
                    }
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
                VKRestContext context = new VKRestContext(mId.ToString(), this.authToken);
                StringBuilder sb = new StringBuilder();

                sb.Length = 0;
                sb.Append("fields=").Append(PROFILE_FIELDS);
                context.parameters = sb.ToString();
                vkRestApi.CallVKFunction(VKFunction.LoadFriends, context);

                // wait for the friends data
                readyEvent.WaitOne();

                foreach (long targetId in this.friendIds)
                {
                    sb.Length = 0;
                    sb.Append("target_uid=").Append(targetId); // Append target friend ids
                    context.parameters = sb.ToString();
                    context.cookie = targetId.ToString(); // pass target id in the cookie context field

                    // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                    timeLastCall = sleepTime(timeLastCall);
                    vkRestApi.CallVKFunction(VKFunction.GetMutual, context);

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
            if (data[VKRestApi.RESPONSE_BODY] == null)
            {
                this.isRunning = false;
                return;
            }

            if(this.totalCount == 0)
            {
                this.totalCount = data[VKRestApi.RESPONSE_BODY]["count"].ToObject<long>();
                this.step = (int)(10000 * POSTS_PER_REQUEST / this.totalCount);
            }

            // now calc items in response
            int count = data[VKRestApi.RESPONSE_BODY]["items"].Count();

            //this.backgroundGroupsWorker.ReportProgress(0, "Processing next " + count + " posts out of " + totalCount);

            long gId = (long)(this.isGroup ? decimal.Negate(this.groupId) : this.groupId);
            List<Post> posts = new List<Post>();

            // process response body
            for (int i = 0; i < count; ++i)
            {
                JObject postObj = data[VKRestApi.RESPONSE_BODY]["items"][i].ToObject<JObject>();

                Post post = new Post();
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
                    Like like = new Like();
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
                updateGroupPostsFile(posts, groupPostsWriter);
            }
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

            long gId = (long)(this.isGroup ? decimal.Negate(this.groupId) : this.groupId);
            List<Comment> comments = new List<Comment>();

            // process response body
            for (int i = 0; i < count; ++i)
            {
                JObject postObj = data[VKRestApi.RESPONSE_BODY]["items"][i].ToObject<JObject>();
                
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
                updateGroupCommentsFile(comments, groupCommentsWriter);
            }
        }

        // process likes user list
        private void OnLikesGetList(JObject data)
        {
            if (data[VKRestApi.RESPONSE_BODY] == null)
            {
                this.isRunning = false;
                return;
            }

            // now calc items in response
            int count = data[VKRestApi.RESPONSE_BODY]["items"].Count();

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
            }
        }

        // process group member
        private void OnGroupsGetMembers(JObject data, String cookie)
        {
            if (data[VKRestApi.RESPONSE_BODY] == null)
            {
                this.isMembersRunning = false;
                return;
            }

            if (this.totalCount == 0)
            {
                this.totalCount = data[VKRestApi.RESPONSE_BODY]["count"].ToObject<long>();
                this.step = (int)(10000 * MEMBERS_PER_REQUEST / this.totalCount);
            }

            // now calc items in response
            int count = data[VKRestApi.RESPONSE_BODY]["items"].Count();

            List<Profile> profiles = new List<Profile>();

            // process response body
            for (int i = 0; i < count; ++i)
            {
                JObject profileObj = data[VKRestApi.RESPONSE_BODY]["items"][i].ToObject<JObject>();

                if (profileObj != null)
                {
                    String type = getStringField("type", profileObj);

                    if (!type.Equals("profile")) 
                    {
                        Debug.WriteLine("Ignoring member with type " + type);
                        continue; // must be profile
                    }

                    Profile profile = new Profile();

                    profile.id = getLongField("id", profileObj);

                    if (profile.id <= 0)
                    {
                        // probably blocked or deleted account, continue
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

                    this.groupNetworkAnalyzer.addMemberVertex(profileObj);
                }
            }

            if (profiles.Count > 0)
            {
                // save the posts list
                updateGroupMembersFile(profiles, groupMembersWriter);
            }
        }

        // process friends list
        private void OnGetFriends(JObject data, String cookie)
        {
            if (data[VKRestApi.RESPONSE_BODY] == null)
            {
                this.isNetworkRunning = false;
                return;
            }

            String memberId = cookie; // memeber id sent as a cooky
            long mId = Convert.ToInt64(memberId);

            // now calc items in response
            int count = data[VKRestApi.RESPONSE_BODY].Count();
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
                long friendId = data[VKRestApi.RESPONSE_BODY][i].ToObject<long>();
                if (isPostersNetwork)
                {
                    this.groupNetworkAnalyzer.AddPostersEdge(mId, friendId); // if friendship exists, the new edge will be added
                }
                else
                {
                    this.groupNetworkAnalyzer.AddFriendsEdge(mId, friendId); // if friendship exists, the new edge will be added
                }
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

            List<Profile> profiles = new List<Profile>();
            
            // process response body
            for (int i = 0; i < count; ++i)
            {
                JObject userObj = data[VKRestApi.RESPONSE_BODY][i].ToObject<JObject>();
            
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
                this.groupNetworkAnalyzer.addVisitorVertex(userObj);
            }

            if (profiles.Count > 0)
            {
                // update posters
                updateGroupVisitorsFile(profiles, groupVisitorsWriter);
            }
        }

        // for user's EGO nets
        // process load user friends response
        private void OnLoadFriends(JObject data)
        {
            if (data[VKRestApi.RESPONSE_BODY] == null)
            {
                this.isEgoNetWorkRunning = false;
                return;
            }
            
            // now calc items in response
            int count = data[VKRestApi.RESPONSE_BODY]["items"].Count();

            // process response body
            for (int i = 0; i < count; ++i)
            {
                JObject friend = data[VKRestApi.RESPONSE_BODY]["items"][i].ToObject<JObject>();

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
            if (data[VKRestApi.RESPONSE_BODY] == null)
            {
                this.isEgoNetWorkRunning = false;
                return;
            }

            long mId = Convert.ToInt64(cookie); // members id passed as a cookie
            if (data[VKRestApi.RESPONSE_BODY].Count() > 0)
            {
                List<String> friendFriendsIds = new List<string>();

                for (int i = 0; i < data[VKRestApi.RESPONSE_BODY].Count(); ++i)
                {
                    long friendFriendsId = data[VKRestApi.RESPONSE_BODY][i].ToObject<long>();
                    // add friend vertex
                    this.egoNetAnalyzer.AddFriendsEdge(mId, friendFriendsId); // member id is in the cookie
                }
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

        // Group posts file
        private string generateGroupPostsFileName(decimal groupId)
        {
            StringBuilder fileName = new StringBuilder(this.WorkingFolderTextBox.Text);

            fileName.Append("\\").Append(Math.Abs(groupId).ToString()).Append("-group-posts");
            fileName.Append(".txt");

            return fileName.ToString();
        }

        private void printGroupPostsHeader(StreamWriter writer)
        {
            writer.WriteLine("{0}\t\"{1}\"\t\"{2}\"\t\"{3}\"\t\"{4}\"\t\"{5}\"\t\"{6}\"\t\"{7}\"\t\"{8}\"\t\"{9}\"\t\"{10}\"",
                    "id", "owner", "from", "signer", "date", "post_type", "comments", "likes", "reposts", "attachments", "text");
        }

        private void updateGroupPostsFile(List<Post> posts, StreamWriter writer)
        {
            foreach (Post p in posts)
            {
                writer.WriteLine("{0}\t{1}\t{2}\t{3}\t\"{4}\"\t\"{5}\"\t{6}\t{7}\t{8}\t{9}\t\"{10}\"",
                    p.id, p.owner_id, p.from_id, p.signer_id, p.date, p.post_type, p.comments, p.likes, p.reposts, p.attachments, p.text);
            }
        }

        // Group visitors profiles file
        private string generateGroupVisitorsFileName(decimal groupId)
        {
            StringBuilder fileName = new StringBuilder(this.WorkingFolderTextBox.Text);

            fileName.Append("\\").Append(Math.Abs(groupId).ToString()).Append("-group-visitors");
            fileName.Append(".txt");

            return fileName.ToString();
        }

        private void printGroupVisitorsHeader(StreamWriter writer)
        {
            writer.WriteLine("\"{0}\"\t\"{1}\"\t\"{2}\"\t\"{3}\"\t\"{4}\"\t\"{5}\"",
                    "id", "first_name", "last_name", "screen_name", "sex", "photo");
        }

        private void updateGroupVisitorsFile(List<Profile> profiles, StreamWriter writer)
        {
            foreach (Profile p in profiles)
            {
                writer.WriteLine("{0}\t\"{1}\"\t\"{2}\"\t\"{3}\"\t{4}\t\"{5}\"",
                    p.id, p.first_name, p.last_name, p.screen_name, p.sex, p.photo);
            }
        }

        // Group members file
        private string generateGroupMembersFileName(decimal groupId)
        {
            StringBuilder fileName = new StringBuilder(this.WorkingFolderTextBox.Text);

            fileName.Append("\\").Append(Math.Abs(groupId).ToString()).Append("-group-members");
            fileName.Append(".txt");

            return fileName.ToString();
        }

        private void printGroupMembersHeader(StreamWriter writer)
        {
            writer.WriteLine("\"{0}\"\t\"{1}\"\t\"{2}\"\t\"{3}\"\t\"{4}\"\t\"{5}\"\t\"{6}\"\t\"{7}\"\t\"{8}\"\t\"{9}\"\t\"{10}\"\t\"{11}\"",
                    "id", "first_name", "last_name", "screen_name", "bdate", "city", "country", "photo", "sex", "relation",  "education", "status");
        }

        private void updateGroupMembersFile(List<Profile> profiles, StreamWriter writer)
        {
            foreach (Profile m in profiles)
            {
                writer.WriteLine("{0}\t\"{1}\"\t\"{2}\"\t\"{3}\"\t\"{4}\"\t\"{5}\"\t\"{6}\"\t\"{7}\"\t\"{8}\"\t\"{9}\"\t\"{10}\"\t\"{11}\"",
                    m.id, m.first_name, m.last_name, m.screen_name, m.bdate, m.city, m.country, m.photo, m.sex, m.relation, m.education, m.status);
            }
        }

        // Group comments file name
        private string generateGroupCommentsFileName(decimal groupId)
        {
            StringBuilder fileName = new StringBuilder(this.WorkingFolderTextBox.Text);

            fileName.Append("\\").Append(Math.Abs(groupId).ToString()).Append("-group-comments");
            fileName.Append(".txt");

            return fileName.ToString();
        }
        
        private void printGroupCommentsHeader(StreamWriter writer)
        {
            writer.WriteLine("\"{0}\"\t\"{1}\"\t\"{2}\"\t\"{3}\"\t\"{4}\"\t\"{5}\"\t\"{6}\"\t\"{7}\"\t\"{8}\"",
                    "id", "post_id", "from", "date", "reply_to_user", "reply_to_comment", "likes", "attachments", "text");
        }

        private void updateGroupCommentsFile(List<Comment> comments, StreamWriter writer)
        {
            foreach (Comment c in comments)
            {
                writer.WriteLine("{0}\t{1}\t{2}\t\"{3}\"\t{4}\t{5}\t{6}\t{7}\t\"{8}\"",
                    c.id, c.post_id, c.from_id, c.date, c.reply_to_uid, c.reply_to_cid, c.likes, c.attachments, c.text);
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
            StringBuilder fileName = new StringBuilder(this.WorkingFolderTextBox.Text);
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

        // Utiliti
        private static DateTime timeToDateTime(long unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
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
    }
}
