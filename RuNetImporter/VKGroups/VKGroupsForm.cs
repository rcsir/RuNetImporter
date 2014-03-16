﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
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

namespace VKGroups
{
    public partial class VKGroupsForm : Form
    {
        private static readonly int POSTS_PER_REQUEST = 100;

        private VKLoginDialog vkLoginDialog;
        private VKRestApi vkRestApi;
        private String userId;
        private String authToken;
        private long expiresAt;
        private static AutoResetEvent readyEvent = new AutoResetEvent(false);
        private volatile bool isRunning;

        // document
        StreamWriter groupPostsWriter;
        StreamWriter groupProfilesWriter;
        StreamWriter groupCommentsWriter;

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

        public VKGroupsForm()
        {
            InitializeComponent();
            this.userIdTextBox.Text = "Please authorize";
        
            vkLoginDialog = new VKLoginDialog();
            // subscribe for login events
            vkLoginDialog.OnUserLogin += new VKLoginDialog.UserLoginHandler(UserLogin);

            vkRestApi = new VKRestApi();
            // set up data handler
            vkRestApi.OnData += new VKRestApi.DataHandler(OnData);
            // set up error handler
            vkRestApi.OnError += new VKRestApi.ErrorHandler(OnError);

            // set up background worker handlers
            this.backgroundGroupsWorker.DoWork
                += new DoWorkEventHandler(groupsWork);

            this.backgroundGroupsWorker.ProgressChanged
                += new ProgressChangedEventHandler(groupsWorkProgressChanged);

            this.backgroundGroupsWorker.RunWorkerCompleted
                += new RunWorkerCompletedEventHandler(groupsWorkCompleted);

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

        private void AuthorizeButton_Click(object sender, EventArgs e)
        {
            bool reLogin = false; // TODO: if true - will delete cookies and relogin, use false for dev.
            vkLoginDialog.Login("friends", reLogin); // default permission - friends
        }

        private void UserLogin(object loginDialog, UserLoginEventArgs loginArgs)
        {
            Debug.WriteLine("User Logged In: " + loginArgs.ToString());

            this.userId = loginArgs.userId;
            this.authToken = loginArgs.authToken;
            this.expiresAt = loginArgs.expiersIn; // todo: calc expiration time

            this.userIdTextBox.Clear();
            this.userIdTextBox.Text = "Authorized " + loginArgs.userId;
            this.ActivateControls();
        }

        private void ActivateControls()
        {
            bool isAuthorized = (this.userId != null && this.userId.Length > 0);

            if (isAuthorized)
            {
                // enable user controls
                this.FindGroupsButton.Enabled = false; //TODO: disable for now
                this.DownloadGroupPosts.Enabled = true;
                this.CancelJobBurron.Enabled = true;
            }
            else
            {
                // disable user controls
                this.FindGroupsButton.Enabled = false;
                this.DownloadGroupPosts.Enabled = false;
                this.CancelJobBurron.Enabled = false;
            }
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
            Debug.WriteLine("Function " + onErrorArgs.function + ", returned error: " + onErrorArgs.error);
            // indicate that data is ready and we can continue
            readyEvent.Set();
        }


        private void groupsWork(Object sender, DoWorkEventArgs args)
        {
            // Do not access the form's BackgroundWorker reference directly. 
            // Instead, use the reference provided by the sender parameter.
            BackgroundWorker bw = sender as BackgroundWorker;

            // Extract the argument. 
            decimal groupId = (decimal)args.Argument; 

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
            isRunning = true;
            StringBuilder sb = new StringBuilder();

            this.postsWithComments.Clear(); // reser comments reference list

            this.totalCount = 0;
            this.currentOffset = 0;
            this.step = 1;

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

                // call VK REST API
                vkRestApi.CallVKFunction(VKFunction.WallGet, context);

                // wait for the user data
                readyEvent.WaitOne();

                // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                // TODO: account for time spent in processing
                Thread.Sleep(333);
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

                        // call VK REST API
                        vkRestApi.CallVKFunction(VKFunction.WallGetComments, context);

                        // wait for the user data
                        readyEvent.WaitOne();

                        // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                        // TODO: account for time spent in processing
                        Thread.Sleep(333);
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

        private void button1_Click(object sender, EventArgs e)
        {
            // Show the FolderBrowserDialog.
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                this.WorkingFolderTextBox.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void DownloadGroupPosts_Click(object sender, EventArgs e)
        {
            DownloadGroupPostsDialog postsDialog = new DownloadGroupPostsDialog();

            if (postsDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                updateStatus(-1, "Start");
                decimal groupId = postsDialog.groupId;
                this.backgroundGroupsWorker.RunWorkerAsync(groupId);
            }
            else
            {
                Debug.WriteLine("Download canceled");
            }

        }

        private void FindGroupsButton_Click(object sender, EventArgs e)
        {
            FindGroupsDialog groupsDialog = new FindGroupsDialog();

            if (groupsDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                //SearchParameters searchParameters = groupsDialog.searchParameters;
                //this.backgroundFinderWorker.RunWorkerAsync(searchParameters);
            }
            else
            {
                Debug.WriteLine("Search canceled");
            }
        }

        private void CancelJobBurron_Click(object sender, EventArgs e)
        {
            if(isRunning)
                this.backgroundGroupsWorker.CancelAsync();
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
                updateGroupPostsFile(posts);
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
                    updateGroupProfilesFile(profiles);
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
                updateGroupCommentsFile(comments);
            }
        }


        // Group posts file name
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

        private void updateGroupPostsFile(List<Post> posts)
        {
            foreach (Post p in posts)
            {
                groupPostsWriter.WriteLine("{0}\t{1}\t{2}\t{3}\t\"{4}\"\t\"{5}\"\t{6}\t{7}\t{8}\t{9}\t\"{10}\"",
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

        private void updateGroupProfilesFile(List<Profile> profiles)
        {
            foreach (Profile p in profiles)
            {
                groupProfilesWriter.WriteLine("{0}\t\"{1}\"\t\"{2}\"\t\"{3}\"\t{4}\t\"{5}\"",
                    p.id, p.first_name, p.last_name, p.screen_name, p.sex, p.photo);
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

        private void updateGroupCommentsFile(List<Comment> comments)
        {
            foreach (Comment c in comments)
            {
                groupCommentsWriter.WriteLine("{0}\t{1}\t{2}\t\"{3}\"\t{4}\t{5}\t{6}\t{7}\t\"{8}\"",
                    c.id, c.post_id, c.from_id, c.date, c.reply_to_uid, c.reply_to_cid, c.likes, c.attachments, c.text);
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
    }
}
