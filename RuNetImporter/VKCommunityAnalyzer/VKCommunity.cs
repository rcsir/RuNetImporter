using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;
using System.Text.RegularExpressions;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using rcsir.net.vk.importer.Dialogs;
using rcsir.net.vk.importer.api;

using rcsir.net.vk.community.Dialogs;
using rcsir.net.vk.community.NetworkAnalyzer;

namespace VKCommunityAnalyzer
{
    public partial class VKCommunity : Form
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
        private volatile bool isMembersRunning;
        private volatile bool isEgoNetWorkRunning;

        HashSet<long> memberIds = new HashSet<long>();
        HashSet<long> friendIds = new HashSet<long>();
        Dictionary<long, Profile> profiles = new Dictionary<long, Profile>();

        HashSet<String> homeTown = new HashSet<String>(StringComparer.OrdinalIgnoreCase);

        // document
        StreamWriter groupMembersWriter;

        // error log
        private StreamWriter errorLogWriter;

        // commumity analyzer document
        private CommunityAnalyzer communityAnalyzer;
        private EgoNetworkAnalyzer egoNetAnalyzer = new EgoNetworkAnalyzer();

        // progress
        long totalCount;
        long currentOffset;
        int step;

        public VKCommunity()
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

            // this.folderBrowserDialog1.ShowNewFolderButton = false;
            // Default to the My Documents folder. 
            this.folderBrowserDialog1.RootFolder = Environment.SpecialFolder.Personal;
            // this.folderBrowserDialog1.RootFolder = Environment.SpecialFolder.MyComputer;
            this.WorkingFolderTextBox.Text = this.folderBrowserDialog1.SelectedPath;

            // setup background group members worker handlers
            this.backgroundMembersWorker.DoWork
                += new DoWorkEventHandler(membersWork);

            this.backgroundMembersWorker.ProgressChanged
                += new ProgressChangedEventHandler(membersWorkProgressChanged);

            this.backgroundMembersWorker.RunWorkerCompleted
                += new RunWorkerCompletedEventHandler(membersWorkCompleted);

            // setup background Ego net workder handlers
            this.backgroundEgoNetWorker.DoWork
                += new DoWorkEventHandler(egoNetWork);

            this.backgroundEgoNetWorker.ProgressChanged
                += new ProgressChangedEventHandler(egoNetWorkProgressChanged);

            this.backgroundEgoNetWorker.RunWorkerCompleted
                += new RunWorkerCompletedEventHandler(egoNetWorkCompleted);

            this.GroupsProgressBar.Minimum = 0;
            this.GroupsProgressBar.Maximum = 100 * POSTS_PER_REQUEST;
            this.GroupsProgressBar.Step = 1;

            ActivateControls();
        }

        private void AuthorizeButton_Click(object sender, EventArgs e)
        {
            bool reLogin = false; // TODO: if true - will delete cookies and relogin, use false for dev.
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

        private void FindCommunityButton_Click(object sender, EventArgs e)
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

        private void DownloadGroupMembers_Click(object sender, EventArgs e)
        {
            DownloadGroupMembersDialog membersDialog = new DownloadGroupMembersDialog();
            membersDialog.groupId = this.groupId; // pass saved groupId
            membersDialog.isGroup = this.isGroup;

            if (membersDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                updateStatus(-1, "Start");
                this.homeTown.Clear();

                decimal gid = this.isGroup ? decimal.Negate(this.groupId) : this.groupId;
                
                string[] separators = { ",", ";", ":", "\t" };
                string[] towns = membersDialog.homeTown.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                foreach (var t in towns)
                {
                    this.homeTown.Add(t);
                }

                isMembersRunning = true;
                this.backgroundMembersWorker.RunWorkerAsync(gid);
                ActivateControls();
            }
            else
            {
                Debug.WriteLine("Download members canceled");
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
            if (isMembersRunning)
                this.backgroundMembersWorker.CancelAsync();

            if (isEgoNetWorkRunning)
                this.backgroundEgoNetWorker.CancelAsync();
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
                    //OnWallGet(onDataArgs.data);
                    break;
                case VKFunction.WallGetComments:
                    //OnWallGetComments(onDataArgs.data, onDataArgs.cookie);
                    break;
                case VKFunction.GroupsGetMembers:
                    OnGroupsGetMembers(onDataArgs.data, onDataArgs.cookie);
                    break;
                case VKFunction.GetFriends:
                    //OnGetFriends(onDataArgs.data, onDataArgs.cookie);
                    break;
                case VKFunction.GroupsGetById:
                    OnGroupsGetById(onDataArgs.data, onDataArgs.cookie);
                    break;
                case VKFunction.LoadUserInfo:
                    //OnLoadUserInfo(onDataArgs.data);
                    break;
                case VKFunction.LikesGetList:
                    //OnLikesGetList(onDataArgs.data);
                    break;
                case VKFunction.UsersGet:
                    //OnUsersGet(onDataArgs.data);
                    break;
                case VKFunction.LoadFriends:
                    OnLoadFriends(onDataArgs.data, onDataArgs.cookie);
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
            Debug.WriteLine("Function " + onErrorArgs.function + ", returned error: " + onErrorArgs.details);

            if (errorLogWriter != null)
            {
                updateErrorLogFile(onErrorArgs, errorLogWriter);
            }

            // indicate that data is ready and we can continue
            readyEvent.Set();
        }

        private void ActivateControls()
        {
            if (isAuthorized)
            {
                // enable user controls
                if (isWorkingFolderSet)
                {
                    bool shouldActivate = this.groupId != 0;
                    bool isBusy = this.isMembersRunning || this.isEgoNetWorkRunning;

                    this.FindCommunityButton.Enabled = true && !isBusy;

                    // activate group buttons
                    this.DownloadGroupMembers.Enabled = shouldActivate && !isBusy;
                    this.CancelJobBurron.Enabled = isBusy; // todo: activate only when running
                }
            }
            else
            {
                // disable user controls
                this.FindCommunityButton.Enabled = false;
                this.DownloadGroupMembers.Enabled = false;
                this.CancelJobBurron.Enabled = false;
            }
        }

        // data handlers
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
                this.communityAnalyzer = new CommunityAnalyzer();

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


        // Members work async handlers
        private void membersWork(Object sender, DoWorkEventArgs args)
        {
            // Do not access the form's BackgroundWorker reference directly. 
            // Instead, use the reference provided by the sender parameter.
            BackgroundWorker bw = sender as BackgroundWorker;

            isMembersRunning = true;

            // Extract the argument. 
            decimal groupId = (decimal)args.Argument;

            VKRestContext context = new VKRestContext(this.userId, this.authToken);
            StringBuilder sb = new StringBuilder();

            this.memberIds.Clear(); // clear member ids
            this.profiles.Clear(); // clear profiles

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

            context = null;

            isEgoNetWorkRunning = true;

            // gather the list of all users friends to build the group network
            this.totalCount = 0;
            this.currentOffset = 0;
            this.step = 1;

            // request group comments
            bw.ReportProgress(-1, "Getting members Ego network");
            this.step = (int)(10000 / memberIds.Count);

            timeLastCall = 0;
            long l = 0;
            foreach (long mId in memberIds)
            {
                if (bw.CancellationPending || !isEgoNetWorkRunning)
                    break;

                bw.ReportProgress(step, "Getting friends: " + (++l) + " out of " + memberIds.Count);

                // reset friends
                this.friendIds.Clear();

                // reset ego net analyzer
                //this.egoNetAnalyzer.Clear();

                // for each member get his friends
                context = new VKRestContext(mId.ToString(), this.authToken);

                sb.Length = 0;
                sb.Append("fields=").Append(PROFILE_FIELDS);
                context.parameters = sb.ToString();
                context.cookie = mId.ToString(); // pass member id in the cookie context field
                vkRestApi.CallVKFunction(VKFunction.LoadFriends, context);

                // wait for the friends data
                readyEvent.WaitOne();

                //foreach (long targetId in this.friendIds)
                //{
                //    sb.Length = 0;
                //    sb.Append("target_uid=").Append(targetId); // Append target friend ids
                //    context.parameters = sb.ToString();
                //    context.cookie = targetId.ToString(); // pass target id in the cookie context field

                //    // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                //    timeLastCall = sleepTime(timeLastCall);
                //    vkRestApi.CallVKFunction(VKFunction.GetMutual, context);

                //    // wait for the friends data
                //    readyEvent.WaitOne();
                //}

                // save ego net document
                //XmlDocument egoNet = this.egoNetAnalyzer.GenerateEgoNetwork();
                //egoNet.Save(generateEgoNetworkFileName(groupId, mId));

            }


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

            // process group members
            String fileName = generateGroupMembersFileName(groupId);
            groupMembersWriter = File.CreateText(fileName);
            printGroupMembersHeader(groupMembersWriter);

            if (profiles.Count > 0)
            {
                // save the profiles
                updateGroupMembersFile(profiles, groupMembersWriter);
                
            }

            // todo: write data to the file
            groupMembersWriter.Close();

            ActivateControls();
        }

        // Ego Net work async handlers
        private void egoNetWork(Object sender, DoWorkEventArgs args)
        {
            // Do not access the form's BackgroundWorker reference directly. 
            // Instead, use the reference provided by the sender parameter.
            BackgroundWorker bw = sender as BackgroundWorker;


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

            this.stripStatusLabel.Text = status;
        }


        // process comunity members
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

                    profiles[profile.id] = profile;

                    // update moved status
                    profile.moved = "no";
                    if (profile.city.Count() > 0 && this.homeTown.Count() > 0)
                    {
                        if (!this.homeTown.Contains(profile.city))
                        {
                            profile.moved = "yes";
                        }
                    }

                    // add graph member vertex
                    this.memberIds.Add(profile.id);

                    this.communityAnalyzer.addMemberVertex(profileObj);
                }
            }
        }

        // for user's EGO nets
        // process load user friends response
        private void OnLoadFriends(JObject data, String cookie)
        {
            if (data[VKRestApi.RESPONSE_BODY] == null)
            {
                this.isEgoNetWorkRunning = false;
                return;
            }

            long mId = Convert.ToInt64(cookie); // members id passed as a cookie

            // now calc items in response
            int count = data[VKRestApi.RESPONSE_BODY]["items"].Count();

            if (profiles.ContainsKey(mId))
            {
                profiles[mId].friends = count;
            }

            // process response body
            for (int i = 0; i < count; ++i)
            {
                JObject friend = data[VKRestApi.RESPONSE_BODY]["items"][i].ToObject<JObject>();

                long id = friend["id"].ToObject<long>();
                // add user id to the friends list
                this.friendIds.Add(id);

                string city = getStringField("city", "title", friend);

                if (profiles.ContainsKey(mId))
                {
                    if (city.Count() > 0 && homeTown.Count() > 0)
                    {
                        if (homeTown.Contains(city))
                        {
                            profiles[mId].home_friends++;
                        }
                    }
                }


                // add friend vertex
                // this.egoNetAnalyzer.AddFriendVertex(friend);
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
            writer.WriteLine("\"{0}\"\t\"{1}\"\t\"{2}\"\t\"{3}\"\t\"{4}\"\t\"{5}\"\t\"{6}\"\t\"{7}\"\t\"{8}\"\t\"{9}\"\t\"{10}\"\t\"{11}\"\t\"{12}\"\t\"{13}\"\t\"{14}\"",
                    "id", "first_name", "last_name", "screen_name", "bdate", "city", "country", "photo", "sex", "relation", "education","moved","friends","home_friends","status");
        }

        private void updateGroupMembersFile(Dictionary<long, Profile> profiles, StreamWriter writer)
        {
            foreach(KeyValuePair<long,Profile> kv in profiles) 
            {
                Profile m = kv.Value;
                writer.WriteLine("{0}\t\"{1}\"\t\"{2}\"\t\"{3}\"\t\"{4}\"\t\"{5}\"\t\"{6}\"\t\"{7}\"\t\"{8}\"\t\"{9}\"\t\"{10}\"\t\"{11}\"\t\"{12}\"\t\"{13}\"\t\"{14}\"",
                    m.id, m.first_name, m.last_name, m.screen_name, m.bdate, m.city, m.country, m.photo, m.sex, m.relation, m.education, m.moved,m.friends, m.home_friends, m.status);
            }
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

            if (o[name] != null)
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

        // data
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
                moved = "";
                friends = 0;
                home_friends = 0;
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
            public string moved { get; set; }
            public int friends { get; set; }
            public int home_friends { get; set; }
            public string status { get; set; }
        };

    }
}
