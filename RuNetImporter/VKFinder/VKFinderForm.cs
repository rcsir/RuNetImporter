using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.IO;
using rcsir.net.vk.importer.Dialogs;
using rcsir.net.vk.importer.api;
using rcsir.net.vk.finder.Dialogs;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace VKFinder
{
    public partial class VKFinderForm : Form
    {
        private static readonly int ITEMS_PER_REQUEST = 1000;
        private long currentOffset;
        private long totalCount;

        private VKLoginDialog vkLoginDialog;
        private VKRestApi vkRestApi;
        private String userId;
        private String authToken;
        private long expiresAt;
        private static AutoResetEvent readyEvent = new AutoResetEvent(false);
        private volatile bool run;

        // document
        StreamWriter writer;

        private class Person
        {
            public Person()
            {
                uid = "";
                firstName = "";
                lastName = "";
                screenName = "";
                sex = "";
                bdate = "";
                photo = "";
                mobilePhone = "";
                homePhone = "";
                status = "";

            }
            public String uid { get; set; }
            public String firstName { get; set; }
            public String lastName { get; set; }
            public String screenName { get; set; }
            public String sex { get; set; }
            public String bdate { get; set; }
            public String photo { get; set; }
            public String mobilePhone { get; set; }
            public String homePhone { get; set; }
            public String status { get; set; }

        };

        public VKFinderForm()
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

            // folder for files
            this.folderBrowserDialog1.Description =
                "Select the directory that you want to use to store files.";
            // this.folderBrowserDialog1.ShowNewFolderButton = false;
            // Default to the My Documents folder. 
            // this.folderBrowserDialog1.RootFolder = Environment.SpecialFolder.Personal;
            this.folderBrowserDialog1.RootFolder = Environment.SpecialFolder.MyComputer;
            this.WorkingFolderTextBox.Text = this.folderBrowserDialog1.RootFolder.ToString();

        }

        private void AuthorizeButton_Click(object sender, EventArgs e)
        {
            bool reLogin = false; // if trueur - will delete cookies and relogin, use false for dev.
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

        private void OnData(object vkRestApi, OnDataEventArgs onDataArgs)
        {
            switch (onDataArgs.function)
            {
                case VKFunction.LoadUserInfo:
                    //OnLoadUserInfo(onDataArgs.data);
                    break;
                case VKFunction.UsersSearch:
                    OnUsersSearch(onDataArgs.data);
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

        private void ActivateControls()
        {
            bool isAuthorized = (this.userId != null && this.userId.Length > 0);

            if (isAuthorized)
            {
                // enable user controls
                this.FindUsersButton.Enabled = true;
            }
            else
            {
                // disable user controls
                this.FindUsersButton.Enabled = false;
            }

        }

        private void FindUsersButton_Click(object sender, EventArgs e)
        {
            UserSearchDialog searchDialog = new UserSearchDialog();

            if (searchDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                String parameters = searchDialog.parameters;
                // append required fields
                parameters += "fields=photo,screen_name,sex,bdate,contacts,status";

                // create stream writer
                String fileName = this.WorkingFolderTextBox.Text;
                fileName += "\\";
                fileName += "blah-blah";
                fileName += ".txt";
                writer = File.CreateText(fileName);

                VKRestContext context = new VKRestContext(this.userId, this.authToken);
                StringBuilder sb = new StringBuilder();

                this.run = true;
                this.totalCount = 0;
                this.currentOffset = 0;

                while (this.run)
                {
                    sb.Length = 0;
                    sb.Append("offset=").Append(currentOffset).Append("&");
                    sb.Append("count=").Append(ITEMS_PER_REQUEST).Append("&");
                    sb.Append(parameters);

                    context.parameters = sb.ToString();
                    Debug.WriteLine("Search parameters: " + context.parameters);

                    context.cookie = this.currentOffset.ToString();
                    vkRestApi.CallVKFunction(VKFunction.UsersSearch, context);

                    // wait for the user data
                    readyEvent.WaitOne();

                    // play nice, sleep for 1/3 sec to stay within 3 requests/second limit
                    // TODO: account for time spent in processing
                    Thread.Sleep(333);
                    this.currentOffset += ITEMS_PER_REQUEST;
                }

                writer.Close();
            }
            else
            {
                Debug.WriteLine("Search canceled");
            }
        }

        //================================
        // data handlers

        // process users search response
        private void OnUsersSearch(JObject data)
        {
            int count = data[VKRestApi.RESPONSE_BODY].Count();

            if (count <= 1)
            {
                this.run = false;
            }

            if (totalCount <= 0)
            {
                totalCount = data[VKRestApi.RESPONSE_BODY][0].ToObject<long>();
            }

            List<Person> persons = new List<Person>();
            // process response body
            for (int i = 1; i < count; ++i)
            {
                JObject personObj = data[VKRestApi.RESPONSE_BODY][i].ToObject<JObject>();

                if (personObj["mobile_phone"] != null ||
                    personObj["home_phone"] != null)
                {
                    Person person = new Person();

                    person.mobilePhone = personObj["mobile_phone"] != null ? personObj["mobile_phone"].ToString() : "";
                    person.homePhone = personObj["home_phone"] != null ? personObj["home_phone"].ToString() : "";

                    if (person.mobilePhone.Length == 0 &&
                        person.homePhone.Length == 0)
                    {
                        // TODO: check phone with regex.
                        continue; // invalid phone number
                    }

                    person.uid = personObj["uid"].ToString();
                    person.firstName = personObj["first_name"].ToString();
                    person.lastName = personObj["last_name"].ToString();
                    person.screenName = personObj["screen_name"] != null ? personObj["screen_name"].ToString() : "";
                    person.sex = personObj["sex"] != null ? personObj["sex"].ToString() : "";
                    person.bdate = personObj["bdate"] != null ? personObj["bdate"].ToString() : "";
                    person.photo = personObj["photo"] != null ? personObj["photo"].ToString() : "";
                    person.status = personObj["status"] != null ? personObj["status"].ToString() : "";
                    persons.Add(person);
                }
            }

            if (persons.Count > 0)
            {
                // save the list
                UpdateFile(persons);
            }
        }

        private void UpdateFile(List<Person> persons)
        {
            // TODO: serialize and update file
            foreach (Person p in persons)
            {
                writer.WriteLine("{0}\t\"{1}\"\t\"{2}\"\t\"{3}\"\t\"{4}\"\t\"{5}\"\t\"{6}\"\t\"{7}\"\t\"{8}\"\t\"{9}\"",
                    p.uid, p.firstName, p.lastName, p.screenName, p.sex, p.bdate, p.mobilePhone, p.homePhone, p.photo, p.status);
            }
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
    }
}
