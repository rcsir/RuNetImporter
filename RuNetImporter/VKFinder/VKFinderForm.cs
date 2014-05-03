using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        private static readonly string REQUIRED_FIELDS = "fields=photo,screen_name,sex,bdate,contacts,relation,status";

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
        private bool withPhone = false;

        // document
        StreamWriter writer;

        private class Person
        {
            public Person()
            {
                id = "";
                firstName = "";
                lastName = "";
                screenName = "";
                sex = "";
                bdate = "";
                photo = "";
                mobilePhone = "";
                homePhone = "";
                relation = "";
                status = "";

            }
            public String id { get; set; }
            public String firstName { get; set; }
            public String lastName { get; set; }
            public String screenName { get; set; }
            public String sex { get; set; }
            public String bdate { get; set; }
            public String photo { get; set; }
            public String mobilePhone { get; set; }
            public String homePhone { get; set; }
            public String relation { get; set; }
            public String status { get; set; }

        };

        public VKFinderForm()
        {
            InitializeComponent();

            this.userIdTextBox.Text = "Please authorize";
            this.FindProgressBar.Minimum = 0;
            this.FindProgressBar.Maximum = 10000;
            this.FindProgressBar.Step = 1;

            updateStatus(0, "Ready");

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

            // set up background worker handlers
            this.backgroundFinderWorker.DoWork 
                += new DoWorkEventHandler(findWork);

            this.backgroundFinderWorker.ProgressChanged 
                += new ProgressChangedEventHandler(findWorkProgressChanged);

            this.backgroundFinderWorker.RunWorkerCompleted 
                += new RunWorkerCompletedEventHandler(findWorkCompleted);
        }

        private void findWork(Object sender, DoWorkEventArgs args)
        {
            // Do not access the form's BackgroundWorker reference directly. 
            // Instead, use the reference provided by the sender parameter.
            BackgroundWorker bw = sender as BackgroundWorker;

            // Extract the argument. 
            SearchParameters searchParameters = args.Argument as SearchParameters;

            string parameters = parseSearchParameters(searchParameters);

            Int32 startYear = (Int32)searchParameters.yearStart;
            Int32 stopYear = (Int32)searchParameters.yearEnd;
            Int32 startMonth = (Int32)searchParameters.monthStart;
            Int32 stopMonth = (Int32)searchParameters.monthEnd;

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
            else
            {
                startYear = stopYear = 1900;
                startMonth = stopMonth = 1;
                searchParameters.useSlowSearch = false;
            }

            DateTime startDate = new DateTime(startYear, startMonth, 1);
            DateTime stopDate = new DateTime(stopYear, stopMonth, 1);

            if(searchParameters.useSlowSearch)
            {
                // set stop date to the last day of the stop month
                stopDate = stopDate.AddMonths(1).AddDays(-1); 
            }

            // figure out step
            int step = 10000;
                
            if( startYear > 1900)
            {
                if(searchParameters.useSlowSearch)
                {
                    // calculate number of days
                    step = (int)(10000 / ((stopDate - startDate).TotalDays) + 0.5);
                }
                else
                {
                    // calculate number of months 
                    step = (int)(10000 / ((stopDate.Year - startDate.Year) * 12  + stopDate.Month - startDate.Month + 1));
                }
            }

            // create stream writer
            String fileName = generateFileName(searchParameters);
            writer = File.CreateText(fileName);
            printHeader(writer);

            VKRestContext context = new VKRestContext(this.userId, this.authToken);

            // loop by birth year and month to maximize number of matches (1000 at a time)
            StringBuilder sb = new StringBuilder();
            while(startDate <= stopDate)
            {
                if (bw.CancellationPending)
                    break;

                bw.ReportProgress(step, "Searching"); 

                this.run = true;
                this.totalCount = 0;
                this.currentOffset = 0;

                while (this.run &&
                    this.currentOffset <= this.totalCount)
                {
                    if (bw.CancellationPending)
                        break;

                    sb.Length = 0;
                    sb.Append("offset=").Append(currentOffset).Append("&");
                    sb.Append("count=").Append(ITEMS_PER_REQUEST).Append("&");
                    sb.Append(parameters);
                    // append bdate 
                    if (startYear > 1900)
                    {
                        sb.Append("birth_year=").Append(startDate.Year).Append("&");
                        sb.Append("birth_month=").Append(startDate.Month).Append("&");
                        if(searchParameters.useSlowSearch)
                        {
                            sb.Append("birth_day=").Append(startDate.Day).Append("&");
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

                // increment date
                startDate = searchParameters.useSlowSearch ? startDate.AddDays(1) : startDate.AddMonths(1);
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

        private void findWorkProgressChanged(Object sender, ProgressChangedEventArgs args)
        {
            String status = args.UserState as String;
            int progress = args.ProgressPercentage;
            updateStatus(progress, status);
        }

        private void findWorkCompleted(object sender, RunWorkerCompletedEventArgs args)
        {
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
            this.backgroundFinderWorker.CancelAsync();
            // indicate that we can continue
            readyEvent.Set();
        }

        private void ActivateControls()
        {
            bool isAuthorized = (this.userId != null && this.userId.Length > 0);

            if (isAuthorized)
            {
                // enable user controls
                this.FindUsersButton.Enabled = true;
                this.CancelFindButton.Enabled = true;
            }
            else
            {
                // disable user controls
                this.FindUsersButton.Enabled = false;
                this.CancelFindButton.Enabled = false;
            }
        }

        private void FindUsersButton_Click(object sender, EventArgs e)
        {
            UserSearchDialog searchDialog = new UserSearchDialog();

            if (searchDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SearchParameters searchParameters = searchDialog.searchParameters;
                this.withPhone = searchParameters.withPhone;

                updateStatus(-1, "Start");
                this.backgroundFinderWorker.RunWorkerAsync(searchParameters);
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
            if (data[VKRestApi.RESPONSE_BODY] == null)
            {
                this.run = false;
                return;
            }

            if (totalCount <= 0)
            {
                totalCount = data[VKRestApi.RESPONSE_BODY]["count"].ToObject<long>(); ;
            }

            int count = data[VKRestApi.RESPONSE_BODY]["items"].Count();

            if (count <= 0)
            {
                this.run = false;
                return;
            }

            this.backgroundFinderWorker.ReportProgress(0, "Processing " + count + " records out of " + totalCount);

            List<Person> persons = new List<Person>();
            // process response body
            for (int i = 0; i < count; ++i)
            {
                JObject personObj = data[VKRestApi.RESPONSE_BODY]["items"][i].ToObject<JObject>();


                // TODO: check phone with regex.
                String t1, t2;
                t1 = personObj["mobile_phone"] != null ? parsePhone(personObj["mobile_phone"].ToString()) : "";
                t2 = personObj["home_phone"] != null ? parsePhone(personObj["home_phone"].ToString()) : "";

                if (withPhone &&
                    t1.Length < 7 &&
                    t2.Length < 7)
                {
                    continue; // invalid phone number
                }

                Person person = new Person();
                person.mobilePhone = t1;
                person.homePhone = t2;
                person.id = personObj["id"].ToString();
                person.firstName = personObj["first_name"].ToString();
                person.lastName = personObj["last_name"].ToString();
                person.screenName = personObj["screen_name"] != null ? personObj["screen_name"].ToString() : "";
                person.sex = personObj["sex"] != null ? personObj["sex"].ToString() : "";
                person.bdate = personObj["bdate"] != null ? personObj["bdate"].ToString() : "";
                person.photo = personObj["photo"] != null ? personObj["photo"].ToString() : "";
                person.relation = personObj["relation"] != null ? personObj["relation"].ToString() : "";
                string t = personObj["status"] != null ? personObj["status"].ToString() : "";
                if (t.Length > 0)
                {
                    person.status = Regex.Replace(t, @"\r\n?|\n", "");
                }
            
                persons.Add(person);
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
            writer.WriteLine("{0}\t\"{1}\"\t\"{2}\"\t\"{3}\"\t\"{4}\"\t\"{5}\"\t\"{6}\"\t\"{7}\"\t\"{8}\"\t\"{9}\"\t\"{10}\"",
                    "id", "Имя", "Фамилия", "Псевдоним", "пол", "д.рождения", "мобильный", "домашний", "фото", "отношения", "статус");
        }

        private void UpdateFile(List<Person> persons)
        {
            foreach (Person p in persons)
            {
                writer.WriteLine("{0}\t\"{1}\"\t\"{2}\"\t\"{3}\"\t\"{4}\"\t\"{5}\"\t\"{6}\"\t\"{7}\"\t\"{8}\"\t\"{9}\"\t\"{10}\"",
                    p.id, p.firstName, p.lastName, p.screenName, p.sex, p.bdate, p.mobilePhone, p.homePhone, p.photo, p.relation, p.status);
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

        private void updateStatus(int progress, String status)
        {
            if (progress > 0)
            {
                this.FindProgressBar.Increment(progress);

            } 
            else if (progress < 0)
            {
                // reset 
                this.FindProgressBar.Value = 0;
            }

            this.toolStripStatusLabel1.Text = status;
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.backgroundFinderWorker.CancelAsync();
        }

        private void VKFinderForm_Load(object sender, EventArgs e)
        {

        }

        private void userIdTextBox_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
