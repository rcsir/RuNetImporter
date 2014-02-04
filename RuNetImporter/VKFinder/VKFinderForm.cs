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
        private static readonly string REQUIRED_FIELDS = "fields=photo,screen_name,sex,bdate,contacts,status";

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

            updateStatus("Ready");

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
            this.folderBrowserDialog1.RootFolder = Environment.SpecialFolder.Personal;
            // this.folderBrowserDialog1.RootFolder = Environment.SpecialFolder.MyComputer;
            this.WorkingFolderTextBox.Text = this.folderBrowserDialog1.SelectedPath;

        }

        private void AuthorizeButton_Click(object sender, EventArgs e)
        {
            bool reLogin = true; // if true - will delete cookies and relogin, use false for dev.
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
                SearchParameters searchParameters = searchDialog.searchParameters;
                string parameters = parseSearchParameters(searchParameters);

                decimal startYear = searchParameters.yearStart;
                decimal stopYear = searchParameters.yearEnd;
                decimal startMonth = searchParameters.monthStart;
                decimal stopMonth = searchParameters.monthEnd;

                if (stopYear < startYear)
                {
                    stopYear = startYear;
                }

                if (startYear > 1900)
                {
                    if (startMonth == 0)
                    {
                        startMonth = 1;
                    }

                    if (stopMonth == 0)
                    {
                        stopMonth = 12;
                    }
                }

                // create stream writer
                String fileName = generateFileName(searchParameters);
                writer = File.CreateText(fileName);
                printHeader(writer);

                VKRestContext context = new VKRestContext(this.userId, this.authToken);

                // loop by birth year and month to maximize number of matches (1000 at a time)
                StringBuilder sb = new StringBuilder();
                for (decimal y = startYear; y <= stopYear; y++)
                {
                    decimal realStartMonth = startMonth;
                    if (y > startYear)
                        realStartMonth = 1;

                    decimal realStopMonth = stopMonth;
                    if (y < stopYear)
                        realStopMonth = 12;

                    for (decimal m = realStartMonth; m <= realStopMonth; m++)
                    {
                        this.run = true;
                        this.totalCount = 0;
                        this.currentOffset = 0;

                        while (this.run)
                        {
                            updateStatus("Searching");

                            sb.Length = 0;
                            sb.Append("offset=").Append(currentOffset).Append("&");
                            sb.Append("count=").Append(ITEMS_PER_REQUEST).Append("&");
                            sb.Append(parameters);
                            // append year if any
                            if (y > 1900)
                            {
                                sb.Append("birth_year=").Append(y).Append("&");

                                if (m > 0 && m < 13)
                                {
                                    sb.Append("birth_month=").Append(m).Append("&");
                                }
                            }
                            // append required fields
                            sb.Append(REQUIRED_FIELDS);

                            context.parameters = sb.ToString();
                            Debug.WriteLine("Search parameters: " + context.parameters);

                            context.cookie = this.currentOffset.ToString();
                            vkRestApi.CallVKFunction(VKFunction.UsersSearch, context);

                            // wait for the user data
                            readyEvent.WaitOne();

                            // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                            // TODO: account for time spent in processing
                            Thread.Sleep(333);
                            this.currentOffset += ITEMS_PER_REQUEST;
                        }
                    }
                }

                updateStatus("Done");

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

            updateStatus("Processing " + count.ToString() + " records.");

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

                    // TODO: check phone with regex.
                    String tmp;
                    tmp = parsePhone(personObj["mobile_phone"] != null ? personObj["mobile_phone"].ToString() : "");
                    person.mobilePhone = tmp.Length >= 7 ? tmp : "";
                    tmp = parsePhone(personObj["home_phone"] != null ? personObj["home_phone"].ToString() : "");
                    person.homePhone = tmp.Length >= 7 ? tmp : "";

                    if (person.mobilePhone.Length == 0 &&
                        person.homePhone.Length == 0)
                    {
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

        private string parseSearchParameters(SearchParameters parameters)
        {
            StringBuilder builder = new StringBuilder();

            if (parameters.query.Length > 0)
            {
                builder.Append("q=").Append(parameters.query).Append("&");
            }

            if (parameters.city != null)
            {
                if (parameters.city.Value > 0)
                {
                    builder.Append("city=").Append(parameters.city.Value).Append("&");
                }
            }

            if (parameters.sex != null)
            {
                builder.Append("sex=").Append(parameters.sex.Value).Append("&");
            }

            // set dates in the search loop

            // set parameters
            return builder.ToString();
        }

        private string generateFileName(SearchParameters parameters)
        {
            StringBuilder fileName = new StringBuilder(this.WorkingFolderTextBox.Text);
            
            fileName.Append('\\');

            if (parameters.query.Length > 0)
                fileName.Append(parameters.query);

            if (parameters.city != null)
                fileName.Append(parameters.city.Name);
            else
                fileName.Append("anyCity");

            fileName.Append('-');

            if (parameters.sex != null)
                fileName.Append(parameters.sex.Sex);
            else
                fileName.Append("any");

            fileName.Append('-');
            fileName.Append(parameters.yearStart).Append('-');
            fileName.Append(parameters.monthStart).Append('-');
            fileName.Append(parameters.yearEnd).Append('-');
            fileName.Append(parameters.monthEnd);

            fileName.Append(".txt");

            return fileName.ToString();
        }

        private string parsePhone(string phone)
        {
            StringBuilder sb = new StringBuilder();

            // remove any non number characters from the phone
            for (int i = 0; i < phone.Length; i++)
            {
                if(char.IsDigit(phone[i]))
                {
                    sb.Append(phone[i]);
                }
            }

            return sb.ToString();
        }

        private void printHeader(StreamWriter writer)
        {
            writer.WriteLine("{0}\t\"{1}\"\t\"{2}\"\t\"{3}\"\t\"{4}\"\t\"{5}\"\t\"{6}\"\t\"{7}\"\t\"{8}\"\t\"{9}\"",
                    "uid", "Имя", "Фамилия", "Псевдоним", "пол", "д.рождения", "мобильный", "домашний", "фото", "статус");
        }

        private void UpdateFile(List<Person> persons)
        {
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

        private void updateStatus(String status)
        {
            this.toolStripStatusLabel1.Text = status;
        }
    }
}
