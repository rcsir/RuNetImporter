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
        private static readonly string MEMBER_FIELDS = "first_name,last_name,screen_name,bdate,city,country,photo_50,sex,relation,status,education";
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

        // document
        StreamWriter groupPostsWriter;
        StreamWriter groupProfilesWriter;
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

        List<String> postsWithComments = new List<String>();

        // group's post
        private class Post
        {
            public Post()
            {
                id = "";
                owner_id = "";
                from_id = "";
                signer_id = "";
                date = "";
                post_type = "";
                comments = "";
                likes = "";
                reposts = "";
                attachments = "";
                text = "";
            }

            public string id { get; set; }
            public string owner_id { get; set; }
            public string from_id { get; set; }
            public string signer_id { get; set; }
            public string date { get; set; }
            public string post_type { get; set; }
            public string comments { get; set; }
            public string likes { get; set; }
            public string reposts { get; set; }
            public string attachments { get; set; } 
            public string text { get; set; }
        };

        // group's profile
        private class Profile
        {
            public Profile()
            {
                id = "";
                first_name = "";
                last_name = "";
                screen_name = "";
                sex = "";
                photo = "";
            }

            public string id { get; set; }
            public string first_name { get; set; }
            public string last_name { get; set; }
            public string screen_name { get; set; }
            public string sex { get; set; }
            public string photo { get; set; }
        };

        // group's member profile
        private class Member
        {
            public Member()
            {
                id = "";
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

            public string id { get; set; }
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
                id = "";
                post_id = "";
                from_id = "";
                date = "";
                reply_to_uid = "";
                reply_to_cid = "";
                likes = "";
                attachments = "";
                text = "";
            }

            public string id { get; set; }
            public string post_id { get; set; }
            public string from_id { get; set; }
            public string date { get; set; }
            public string reply_to_uid { get; set; } // user id
            public string reply_to_cid { get; set; } // comment id 
            public string likes { get; set; }
            public string attachments { get; set; }
            public string text { get; set; }
        };

        // group's info
        private class Group
        {
            public Group()
            {
                id = "";
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

            public string id { get; set; }
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
                    // look up a wall by id
                    // TODO:
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
                this.backgroundNetworkWorker.RunWorkerAsync(gid);
                ActivateControls();
            }
            else
            {
                Debug.WriteLine("Download members network canceled");
            }
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
                    bool isBusy = this.isRunning || this.isMembersRunning || this.isNetworkRunning;

                    this.FindGroupsButton.Enabled = true && !isBusy;

                    // activate group buttons
                    this.DownloadGroupPosts.Enabled = shouldActivate && !isBusy;
                    this.DownloadGroupMembers.Enabled = shouldActivate && !isBusy;
                    this.DownloadMembersNetwork.Enabled = shouldActivate && !isBusy;
                    this.DownloadPostersNetwork.Enabled = shouldActivate && !isBusy;
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

                g.id = getStringField("id", groupObject);
                g.name = getStringField("name", groupObject);
                g.screen_name = getStringField("screen_name", groupObject);
                g.is_closed = getStringField("is_closed", groupObject);
                g.type = getStringField("type", groupObject);
                g.members_count = getStringField("members_count", groupObject);
                g.city = getStringTitleField("city", groupObject);
                g.country = getStringTitleField("country", groupObject);
                g.photo = getStringField("photo_50", groupObject);
                g.description = getTextField("description", groupObject);
                g.status = getTextField("status", groupObject);

                groups.Add(g);
            }

            if (groups.Count > 0)
            {
                // update group id and group info
                this.groupId = decimal.Parse(groups[0].id);

                // group members network document
                String networkDocumentName = generateGroupMembersNetworkFileName(groupId);
                this.groupNetworkAnalyzer = new GroupNetworkAnalyzer(networkDocumentName);

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
            // 2) gropu profiles
            fileName = generateGroupProfilesFileName(groupId);
            groupProfilesWriter = File.CreateText(fileName);
            printGroupProfilesHeader(groupProfilesWriter);

            VKRestContext context = new VKRestContext(this.userId, this.authToken);

            // get group posts 100 at a time and store them in the file
            StringBuilder sb = new StringBuilder();

            this.postsWithComments.Clear(); // reset comments reference list

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

                bw.ReportProgress(step, "Getting posts");

                sb.Length = 0;
                sb.Append("owner_id=").Append(groupId.ToString()).Append("&");
                sb.Append("offset=").Append(currentOffset).Append("&");
                sb.Append("count=").Append(POSTS_PER_REQUEST).Append("&");
                sb.Append("extended=").Append(1).Append("&"); // request extended information - wall, profiles, groups
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
            groupProfilesWriter.Close();

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

                    while (this.isRunning)
                    {
                        if (bw.CancellationPending)
                            break;

                        if (currentOffset > totalCount)
                        {
                            // done
                            break;
                        }

                        bw.ReportProgress(step, "Getting comments");

                        sb.Length = 0;
                        sb.Append("owner_id=").Append(groupId.ToString()).Append("&"); // group id
                        sb.Append("post_id=").Append(postsWithComments[i]).Append("&"); // post id
                        sb.Append("need_likes=").Append(1).Append("&"); // request likes info
                        sb.Append("offset=").Append(currentOffset).Append("&");
                        sb.Append("count=").Append(POSTS_PER_REQUEST).Append("&");
                        context.parameters = sb.ToString();
                        context.cookie = postsWithComments[i]; // pass post id as a cookie
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

                bw.ReportProgress(step, "Getting group members");

                sb.Length = 0;
                decimal gid = Math.Abs(groupId); // in this request, group id must be positive
                sb.Append("group_id=").Append(gid.ToString()).Append("&");
                sb.Append("sort=").Append("id_asc").Append("&");
                sb.Append("offset=").Append(currentOffset).Append("&");
                sb.Append("count=").Append(MEMBERS_PER_REQUEST).Append("&");
                sb.Append("fields=").Append(MEMBER_FIELDS).Append("&");
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

            List<string> members = this.groupNetworkAnalyzer.GetMemeberIds();

            // request group comments
            bw.ReportProgress(-1, "Getting Group friends network");
            this.step = (int)(10000 / members.Count);

            long timeLastCall = 0;

            for (int i = 0; i < members.Count && isNetworkRunning; ++i)
            {
                if (bw.CancellationPending)
                    break;

                bw.ReportProgress(step, "Getting member's friends: " + (i + 1) + " out of " + members.Count);

                String memberId = members[i];

                sb.Length = 0;
                sb.Append("user_id=").Append(memberId);
                context.parameters = sb.ToString();
                context.cookie = memberId; // pass member id as a cookie
                Debug.WriteLine("Request parameters: " + context.parameters);

                // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                timeLastCall = sleepTime(timeLastCall);
                vkRestApi.CallVKFunction(VKFunction.GetFriends, context);

                // wait for the friends data
                readyEvent.WaitOne();
            }

            //args.Result = TimeConsumingOperation(bw, arg);

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
                //MessageBox.Show("Search complete!");
                updateStatus(10000, "Done");
            }

            // save whatever is collected
            if (this.groupNetworkAnalyzer != null)
            {
                updateStatus(0, "Generate Network Graph File");
                if (this.groupNetworkAnalyzer.SaveGroupNetwork())
                {
                    Debug.WriteLine("Group Network Graph file generated");
                }
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

            this.backgroundGroupsWorker.ReportProgress(0, "Processing next " + count + " posts out of " + totalCount);

            List<Post> posts = new List<Post>();

            String t; // temp string
            long l; // temp number
            DateTime d; // temp date obj
            // process response body
            for (int i = 0; i < count; ++i)
            {
                JObject postObj = data[VKRestApi.RESPONSE_BODY]["items"][i].ToObject<JObject>();

                Post post = new Post();
                post.id = postObj["id"].ToString();
                post.owner_id = postObj["owner_id"] != null ? postObj["owner_id"].ToString() : "";
                post.from_id = postObj["from_id"] != null ? postObj["from_id"].ToString() : "";
                post.signer_id = postObj["signer_id"] != null ? postObj["signer_id"].ToString() : "";
                // post date
                l = postObj["date"] != null ? postObj["date"].ToObject<long>() : 0;
                d = timeToDateTime(l);
                post.date = d.ToString("yyyy-MM-dd HH:mm:ss");
                // post_type 
                post.post_type = postObj["post_type"] != null ? postObj["post_type"].ToString() : ""; 
                // comments
                if(postObj["comments"] != null)
                {
                    post.comments = postObj["comments"]["count"].ToString();
                    l = postObj["comments"]["count"].ToObject<long>();
                    if (l > 0)
                    {
                        this.postsWithComments.Add(post.id); // add post's id to the ref list for comments processing
                    }
                }
                // likes/dislikes
                if(postObj["likes"] != null)
                {
                    int likes = postObj["likes"]["count"].ToObject<int>();
                    post.likes = likes.ToString();
                }
                // reposts
                if(postObj["reposts"] != null)
                {
                    post.reposts = postObj["reposts"]["count"].ToString();
                }
                // attachments count
                if(postObj["attachments"] != null)
                {
                    post.attachments = postObj["attachments"].ToArray().Length.ToString();
                }
                // post text
                t = postObj["text"] != null ? postObj["text"].ToString() : "";
                if(t.Length > 0)
                {
                    post.text = Regex.Replace(t, @"\r\n?|\n", "");
                }
            
                posts.Add(post);
            }

            if (posts.Count > 0)
            {
                // save the posts list
                updateGroupPostsFile(posts, groupPostsWriter);
            }

            // process extended information
            // profiles
            if(data[VKRestApi.RESPONSE_BODY]["profiles"] != null)
            {
                List<Profile> profiles = new List<Profile>();

                JArray profilesObj = (JArray)data[VKRestApi.RESPONSE_BODY]["profiles"];
                for (int i = 0; i < profilesObj.Count; i++ )
                {
                    JObject profileObj = profilesObj[i].ToObject<JObject>();

                    if(profileObj != null)
                    {
                        Profile profile = new Profile();

                        profile.id = profileObj["id"].ToString();

                        // ignore requester profile
                        if (profile.id.Equals(this.userId))
                            continue;

                        profile.first_name = profileObj["first_name"] != null ? profileObj["first_name"].ToString() : "";
                        profile.last_name = profileObj["last_name"] != null ? profileObj["last_name"].ToString() : "";
                        profile.screen_name = profileObj["screen_name"] != null ? profileObj["screen_name"].ToString() : "";
                        profile.sex = profileObj["sex"] != null ? profileObj["sex"].ToString() : "";
                        profile.photo = profileObj["photo_50"] != null ? profileObj["photo_50"].ToString() : "";

                        profiles.Add(profile);
                    }
                }

                if(profiles.Count > 0)
                {
                    updateGroupProfilesFile(profiles, groupProfilesWriter);
                }
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

            this.backgroundGroupsWorker.ReportProgress(0, "Processing next " + count + " comments");

            List<Comment> comments = new List<Comment>();

            String t; // temp string
            long l; // temp number
            DateTime d; // temp date obj
            // process response body
            for (int i = 0; i < count; ++i)
            {
                JObject postObj = data[VKRestApi.RESPONSE_BODY]["items"][i].ToObject<JObject>();
                
                //    "id", "post_id", "from", "date", "reply_to_user", "reply_to_comment", "likes", "attachments", "text");
                
                Comment comment = new Comment();
                comment.id = postObj["id"].ToString();
                comment.post_id = cookie; // passed as a cookie
                comment.from_id = postObj["from_id"] != null ? postObj["from_id"].ToString() : "";
                // post date
                l = postObj["date"] != null ? postObj["date"].ToObject<long>() : 0;
                d = timeToDateTime(l);
                comment.date = d.ToString("yyyy-MM-dd HH:mm:ss");
                comment.reply_to_uid = postObj["reply_to_uid"] != null ? postObj["reply_to_uid"].ToString() : "";
                comment.reply_to_cid = postObj["reply_to_cid"] != null ? postObj["reply_to_cid"].ToString() : "";
                
                // likes/dislikes
                if (postObj["likes"] != null)
                {
                    int likes = postObj["likes"]["count"].ToObject<int>();
                    comment.likes = likes.ToString();
                }
                // attachments count
                if (postObj["attachments"] != null)
                {
                    comment.attachments = postObj["attachments"].ToArray().Length.ToString();
                }
                // post text
                t = postObj["text"] != null ? postObj["text"].ToString() : "";
                if (t.Length > 0)
                {
                    comment.text = Regex.Replace(t, @"\r\n?|\n", "");
                }

                comments.Add(comment);
            }

            if (comments.Count > 0)
            {
                // save the posts list
                updateGroupCommentsFile(comments, groupCommentsWriter);
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
                this.step = (int)(10000 * POSTS_PER_REQUEST / this.totalCount);
            }

            // now calc items in response
            int count = data[VKRestApi.RESPONSE_BODY]["items"].Count();

            this.backgroundMembersWorker.ReportProgress(0, "Processing next " + count + " members out of " + totalCount);

            List<Member> members = new List<Member>();

            String t; // temp string
            long l; // temp number
            DateTime d; // temp date obj
            // process response body
            for (int i = 0; i < count; ++i)
            {
                JObject profileObj = data[VKRestApi.RESPONSE_BODY]["items"][i].ToObject<JObject>();

                if (profileObj != null)
                {
                    String type = profileObj["type"] != null ? profileObj["type"].ToString() : "";

                    if (!type.Equals("profile")) 
                    {
                        Debug.WriteLine("Ignoring member with type " + type);
                        continue; // must be profile
                    }

                    Member member = new Member();

                    member.id = profileObj["id"].ToString();

                    if (member.id.Length == 0)
                    {
                        // probably blocked or deleted account, continue
                        continue;
                    }

                    member.first_name = profileObj["first_name"] != null ? profileObj["first_name"].ToString() : "";
                    member.last_name = profileObj["last_name"] != null ? profileObj["last_name"].ToString() : "";
                    member.screen_name = profileObj["screen_name"] != null ? profileObj["screen_name"].ToString() : "";
                    member.bdate = profileObj["bdate"] != null ? profileObj["bdate"].ToString() : "";

                    if(profileObj["city"] != null)
                    {
                        member.city = profileObj["city"]["title"].ToString();
                    }
                    
                    if(profileObj["country"] != null)
                    {
                        member.country = profileObj["country"]["title"].ToString();
                    }

                    member.photo = profileObj["photo_50"] != null ? profileObj["photo_50"].ToString() : "";
                    member.sex = profileObj["sex"] != null ? profileObj["sex"].ToString() : "";
                    member.relation = profileObj["relation"] != null ? profileObj["relation"].ToString() : "";
          
                    // university name
                    t = profileObj["university_name"] != null ? profileObj["university_name"].ToString() : "";
                    if (t.Length > 0)
                    {
                        member.education = Regex.Replace(t, @"\r\n?|\n", "");
                    }

                    // status text
                    t = profileObj["status"] != null ? profileObj["status"].ToString() : "";
                    if (t.Length > 0)
                    {
                        member.status = Regex.Replace(t, @"\r\n?|\n", "");
                    }

                    members.Add(member);

                    // add graph member vertex
                    this.groupNetworkAnalyzer.addMemberVertex(profileObj);
                }
            }

            if (members.Count > 0)
            {
                // save the posts list
                updateGroupMembersFile(members, groupMembersWriter);
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

            // now calc items in response
            int count = data[VKRestApi.RESPONSE_BODY].Count();
            Debug.WriteLine("Processing " + count + " friends of user id " + memberId);

            // process response body
            for (int i = 0; i < count; ++i)
            {
                String friendId = data[VKRestApi.RESPONSE_BODY][i].ToString();
                this.groupNetworkAnalyzer.AddFriendsEdge(memberId, friendId); // if friendship exists, the new edge will be added
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

        // Group profiles file name
        private string generateGroupProfilesFileName(decimal groupId)
        {
            StringBuilder fileName = new StringBuilder(this.WorkingFolderTextBox.Text);

            fileName.Append("\\").Append(Math.Abs(groupId).ToString()).Append("-group-profiles");
            fileName.Append(".txt");

            return fileName.ToString();
        }

        private void printGroupProfilesHeader(StreamWriter writer)
        {
            writer.WriteLine("\"{0}\"\t\"{1}\"\t\"{2}\"\t\"{3}\"\t\"{4}\"\t\"{5}\"",
                    "id", "first_name", "last_name", "screen_name", "sex", "photo");
        }

        private void updateGroupProfilesFile(List<Profile> profiles, StreamWriter writer)
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

        private void updateGroupMembersFile(List<Member> members, StreamWriter writer)
        {
            foreach (Member m in members)
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

            fileName.Append("\\").Append(Math.Abs(groupId).ToString()).Append("-group-members-network");
            fileName.Append(".graphml");

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
            long timeToSleep = 333 - (getTimeNowMillis() - timeLastCall);
            if (timeToSleep > 0)
                Thread.Sleep((Int32)timeToSleep);

            return getTimeNowMillis();
        }

        // JObject utils
        private static String getStringField(String name, JObject o)
        {
            return o[name] != null ? o[name].ToString() : "";
        }

        private static String getStringTitleField(String name, JObject o)
        {
            if (o[name] != null)
            {
                if (o[name]["title"] != null)
                {
                   return o[name]["title"].ToString();
                }
            }
            return "";
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
    }
}
