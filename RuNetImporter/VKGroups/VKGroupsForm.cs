using System;
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
        StreamWriter writer;

        // progress
        long totalCount;
        long currentOffset;
        int step;

        private class Post
        {
            public Post()
            {
                id = "";
                to_id = "";
                from_id = "";
                date = "";
                text = "";
            }

            public string id { get; set; }
            public string to_id { get; set; }
            public string from_id { get; set; }
            public string date { get; set; }
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

            this.groupsStripProgressBar.Minimum = 0;
            this.groupsStripProgressBar.Maximum = 100 * 100;
            this.groupsStripProgressBar.Step = 1;

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

            // create stream writer
            String fileName = generateGroupPostsFileName(groupId);
            writer = File.CreateText(fileName);
            printGroupPostsHeader(writer);

            VKRestContext context = new VKRestContext(this.userId, this.authToken);

            // get group posts 100 at a time and store them in the file
            isRunning = true;
            StringBuilder sb = new StringBuilder();

            this.totalCount = 0;
            this.currentOffset = 0;
            this.step = 1;

            while (this.isRunning)
            {
                if (bw.CancellationPending)
                    break;

                if(currentOffset > totalCount)
                {
                    // done
                    break;
                }

                bw.ReportProgress(step, "Downloading");

                sb.Length = 0;
                sb.Append("owner_id=").Append(groupId.ToString()).Append("&");
                sb.Append("offset=").Append(currentOffset).Append("&");
                sb.Append("count=").Append(POSTS_PER_REQUEST).Append("&");
                context.parameters = sb.ToString();
                Debug.WriteLine("Download parameters: " + context.parameters);

                context.cookie = currentOffset.ToString();
                
                //TODO:
                vkRestApi.CallVKFunction(VKFunction.WallGet, context);

                // wait for the user data
                readyEvent.WaitOne();

                // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                // TODO: account for time spent in processing
                Thread.Sleep(333);
                currentOffset += POSTS_PER_REQUEST;
            }

            writer.Close();

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
                this.groupsStripProgressBar.Increment(progress);
            } 
            else if (progress < 0)
            {
                // reset 
                this.groupsStripProgressBar.Value = 0;
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
            }

            if(this.totalCount == 0)
            {
                this.totalCount = data[VKRestApi.RESPONSE_BODY]["count"].ToObject<long>(); ;
                this.step = (int)(10000 * POSTS_PER_REQUEST / this.totalCount);
            }

            // now calc items in response
            int count = data[VKRestApi.RESPONSE_BODY]["items"].Count();

            List<Post> posts = new List<Post>();

            // process response body
            for (int i = 0; i < count; ++i)
            {
                JObject postObj = data[VKRestApi.RESPONSE_BODY]["items"][i].ToObject<JObject>();

                Post post = new Post();
                post.id = postObj["id"].ToString();
                post.to_id = postObj["to_id"] != null ? postObj["to_id"].ToString() : "";
                post.from_id = postObj["from_id"] != null ? postObj["from_id"].ToString() : "";
                post.date = postObj["date"] != null ? postObj["date"].ToString() : "";
                String t = postObj["text"] != null ? postObj["text"].ToString() : "";
                if(t.Length > 0)
                {
                    post.text = Regex.Replace(t, @"\r\n?|\n", "");
                }
                //post. = postObj[""] != null ? postObj[""].ToString() : "";
            
                posts.Add(post);
            }

            if (posts.Count > 0)
            {
                // save the posts list
                updateGroupPostsFile(posts);
            }
        }

        private string generateGroupPostsFileName(decimal groupId)
        {
            StringBuilder fileName = new StringBuilder(this.WorkingFolderTextBox.Text);

            fileName.Append("\\group-posts-").Append(groupId.ToString());
            fileName.Append(".txt");

            return fileName.ToString();
        }

        private void printGroupPostsHeader(StreamWriter writer)
        {
            writer.WriteLine("{0}\t\"{1}\"\t\"{2}\"\t\"{3}\"\t\"{4}\"",
                    "id", "to_id", "from_id", "date", "text");
        }

        private void updateGroupPostsFile(List<Post> posts)
        {
            foreach (Post p in posts)
            {
                writer.WriteLine("{0}\t\"{1}\"\t\"{2}\"\t\"{3}\"\t\"{4}\"",
                    p.id, p.to_id, p.from_id, p.date, p.text);
            }
        }
    }
}
