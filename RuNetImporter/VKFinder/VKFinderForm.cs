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
        private static readonly string REQUIRED_FIELDS = "fields=photo,screen_name,sex,bdate,contacts,relation,status,city,country";

        private static readonly int ITEMS_PER_REQUEST = 1000;
        private long currentOffset;
        private long totalCount;

        private readonly VKLoginDialog vkLoginDialog;
        private readonly VkRestApi vkRestApi;
        private String userId;
        private String authToken;
        private long expiresAt;
        private static readonly AutoResetEvent readyEvent = new AutoResetEvent(false);
        private volatile bool run; // finder worker flag
        //private volatile bool load; // loader worker flag
        private bool withPhone = false;

        // places
        private List<VkRegion> regions = new List<VkRegion>();
        private VkCity SaintPetersburg = new VkCity(2, "Санкт-Петербург", true, "Санкт-Петербург");
        private VkCity Moscow = new VkCity(1, "Москва", true, "Москва");
        private VkCity Ekaterinburg = new VkCity(49, "Екатеринбург", true, "Екатеринбург");
        private readonly Dictionary<string, List<VkCity>> citiesByDistrict = new Dictionary<string, List<VkCity>>();

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
                city = "";
                country = "";
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
            public String city { get; set; }
            public String country { get; set; }
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

            vkRestApi = new VkRestApi();
            // set up data handler
            vkRestApi.OnData += new VkRestApi.DataHandler(OnData);
            // set up error handler
            vkRestApi.OnError += new VkRestApi.ErrorHandler(OnError);

            // folder for files
            this.folderBrowserDialog1.Description =
                "Select the directory that you want to use to store files.";
            // this.folderBrowserDialog1.ShowNewFolderButton = false;
            // Default to the My Documents folder. 
            this.folderBrowserDialog1.RootFolder = Environment.SpecialFolder.Personal;
            // this.folderBrowserDialog1.RootFolder = Environment.SpecialFolder.MyComputer;
            this.WorkingFolderTextBox.Text = this.folderBrowserDialog1.SelectedPath;

            // set up background find worker handlers
            this.backgroundFinderWorker.DoWork 
                += new DoWorkEventHandler(findWork);

            this.backgroundFinderWorker.ProgressChanged 
                += new ProgressChangedEventHandler(findWorkProgressChanged);

            this.backgroundFinderWorker.RunWorkerCompleted 
                += new RunWorkerCompletedEventHandler(findWorkCompleted);

            // set up background load worker handlers
            this.backgroundLoaderWorker.DoWork
                += new DoWorkEventHandler(loadWork);        
        }

        private void VKFinderForm_Load(object sender, EventArgs e)
        {
            this.ActivateControls();
        }

        private void AuthorizeButton_Click(object sender, EventArgs e)
        {
            bool reLogin = true; // if true - will delete cookies and relogin, use false for dev.
            vkLoginDialog.Login("friends", reLogin); // default permission - friends
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.backgroundFinderWorker.CancelAsync();
        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            // Show the FolderBrowserDialog.
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                this.WorkingFolderTextBox.Text = folderBrowserDialog1.SelectedPath;
                if (this.citiesByDistrict.Count() == 0)
                {
                    // add spb
                    var spb = new List<VkCity> {SaintPetersburg};
                    this.citiesByDistrict.Add("Санкт-Петербург город", spb);

                    // add moscow
                    var moscow = new List<VkCity> {Moscow};
                    this.citiesByDistrict.Add("Москва город", moscow);

                    // add eburg
                    var eburg = new List<VkCity> {Ekaterinburg};
                    this.citiesByDistrict.Add("Екатеринбург город", eburg);

                    this.backgroundLoaderWorker.RunWorkerAsync(1); // param is not important
                }
            }

            this.ActivateControls();
        }

        private void FindUsersButton_Click(object sender, EventArgs e)
        {
            UserSearchDialog searchDialog = new UserSearchDialog(this.citiesByDistrict);

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

        private void ActivateControls()
        {
            bool isAuthorized = (this.userId != null && 
                                this.userId.Length > 0 &&
                                this.WorkingFolderTextBox.Text.Count() > 0);

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

        // Async Workers

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

            DateTime saveStartDate = new DateTime(startYear, startMonth, 1);
            DateTime saveStopDate = new DateTime(stopYear, stopMonth, 1);

            if(searchParameters.useSlowSearch)
            {
                // set stop date to the last day of the stop month
                saveStopDate = saveStopDate.AddMonths(1).AddDays(-1); 
            }

            // figure out step
            int step = 10000;
                
            if( startYear > 1900)
            {
                if(searchParameters.useSlowSearch)
                {
                    // calculate number of days
                    step = (int)(10000 / ((saveStopDate - saveStartDate).TotalDays) + 0.5);
                }
                else
                {
                    // calculate number of months 
                    step = (int)(10000 / ((saveStopDate.Year - saveStartDate.Year) * 12  + saveStopDate.Month - saveStartDate.Month + 1));
                }
            }

            step /= searchParameters.cities.Count();

            // create stream writer
            String fileName = generateFileName(searchParameters);
            writer = File.CreateText(fileName);
            printHeader(writer);

            var context = new VkRestApi.VkRestContext(this.userId, this.authToken);

            // loop by birth year and month to maximize number of matches (1000 at a time)
            StringBuilder sb = new StringBuilder();
            foreach (var city in searchParameters.cities)
            {
                long timeLastCall = 0;
                DateTime startDate = saveStartDate;
                DateTime stopDate = saveStopDate;

                while (startDate <= stopDate)
                {
                    if (bw.CancellationPending)
                        break;

                    bw.ReportProgress(step, "Searching in " + city.Title);

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
                        if (city.Id > 0)
                        {
                            sb.Append("city=").Append(city.Id).Append("&");
                        }

                        // append bdate 
                        if (startYear > 1900)
                        {
                            sb.Append("birth_year=").Append(startDate.Year).Append("&");
                            sb.Append("birth_month=").Append(startDate.Month).Append("&");
                            if (searchParameters.useSlowSearch)
                            {
                                sb.Append("birth_day=").Append(startDate.Day).Append("&");
                            }
                        }
                        // append required fields
                        sb.Append(REQUIRED_FIELDS);

                        context.Parameters = sb.ToString();
                        Debug.WriteLine("Search parameters: " + context.Parameters);

                        context.Cookie = this.currentOffset.ToString();

                        // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                        timeLastCall = sleepTime(timeLastCall);
                        vkRestApi.CallVkFunction(VkFunction.UsersSearch, context);

                        // wait for the user data
                        readyEvent.WaitOne();

                        this.currentOffset += ITEMS_PER_REQUEST;
                    }

                    // increment date
                    startDate = searchParameters.useSlowSearch ? startDate.AddDays(1) : startDate.AddMonths(1);
                }
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

        private void loadWork(Object sender, DoWorkEventArgs args)
        {
            // Do not access the form's BackgroundWorker reference directly. 
            // Instead, use the reference provided by the sender parameter.
            BackgroundWorker bw = sender as BackgroundWorker;

            // Extract the argument. 
            // SearchParameters searchParameters = args.Argument as SearchParameters;
            var context = new VkRestApi.VkRestContext(this.userId, this.authToken);


            // loop by birth year and month to maximize number of matches (1000 at a time)
            StringBuilder sb = new StringBuilder();
            this.run = true;
            this.totalCount = 0;
            this.currentOffset = 0;
            long timeLastCall = 0;

            // process regions
            while (this.run &&
                this.currentOffset <= this.totalCount)
            {
                sb.Length = 0;
                sb.Append("country_id=").Append(1).Append("&"); // Russia
                sb.Append("offset=").Append(currentOffset).Append("&");
                sb.Append("count=").Append(ITEMS_PER_REQUEST).Append("&");
                context.Parameters = sb.ToString();
                Debug.WriteLine("request params: " + context.Parameters);

                // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                timeLastCall = sleepTime(timeLastCall);
                vkRestApi.CallVkFunction(VkFunction.DatabaseGetRegions, context);

                // wait for the user data
                readyEvent.WaitOne();
                this.currentOffset += ITEMS_PER_REQUEST;
            }

            this.run = true;
            this.totalCount = 0;
            this.currentOffset = 0;
            timeLastCall = 0;
            // process cities
            while (this.run &&
                this.currentOffset <= this.totalCount)
            {
                sb.Length = 0;
                sb.Append("country_id=").Append(1).Append("&"); // Russia
                sb.Append("region_id=").Append(1045244).Append("&"); // Leningradskaya obl.
                sb.Append("need_all=").Append(1).Append("&"); // all
                sb.Append("offset=").Append(currentOffset).Append("&");
                sb.Append("count=").Append(ITEMS_PER_REQUEST).Append("&");
                context.Parameters = sb.ToString();
                Debug.WriteLine("request params: " + context.Parameters);

                // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                timeLastCall = sleepTime(timeLastCall);
                vkRestApi.CallVkFunction(VkFunction.DatabaseGetCities, context);

                // wait for the user data
                readyEvent.WaitOne();
                this.currentOffset += ITEMS_PER_REQUEST;
            }

            //args.Result = TimeConsumingOperation(bw, arg);

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
            switch (onDataArgs.Function)
            {
                case VkFunction.UsersSearch:
                    OnUsersSearch(onDataArgs.Data);
                    break;
                case VkFunction.DatabaseGetCountries:
                    OnGetCountries(onDataArgs.Data);
                    break;
                case VkFunction.DatabaseGetRegions:
                    OnGetRegions(onDataArgs.Data);
                    break;
                case VkFunction.DatabaseGetCities:
                    OnGetCities(onDataArgs.Data);
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
            // notify user about the error
            Debug.WriteLine("Function " + onErrorArgs.Function + ", returned error: " + onErrorArgs.Error);
            
            // keep on going ... this.backgroundFinderWorker.CancelAsync();
            
            // indicate that we can continue
            readyEvent.Set();
        }

        //================================
        // data handlers

        // process users search response
        private void OnUsersSearch(JObject data)
        {
            if (data[VkRestApi.ResponseBody] == null)
            {
                this.run = false;
                return;
            }

            if (totalCount <= 0)
            {
                totalCount = data[VkRestApi.ResponseBody]["count"].ToObject<long>(); ;
            }

            int count = data[VkRestApi.ResponseBody]["items"].Count();

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
                JObject personObj = data[VkRestApi.ResponseBody]["items"][i].ToObject<JObject>();


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
                person.city = getStringField("city", "title", personObj);
                person.country = getStringField("country", "title", personObj);
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

        // process countries response
        private void OnGetCountries(JObject data)
        {
            if (data[VkRestApi.ResponseBody] == null)
            {
                this.run = false;
                return;
            }

            if (totalCount <= 0)
            {
                totalCount = data[VkRestApi.ResponseBody]["count"].ToObject<long>(); ;
            }

            int count = data[VkRestApi.ResponseBody]["items"].Count();

            if (count <= 0)
            {
                this.run = false;
                return;
            }

            List<VkCountry> countries = new List<VkCountry>();
            // process response body
            for (int i = 0; i < count; ++i)
            {
                JObject obj = data[VkRestApi.ResponseBody]["items"][i].ToObject<JObject>();
                int id = getIntField("id", obj);
                string title = getStringField("title", obj);
                if(id > 0)
                {
                    countries.Add(new VkCountry(id, title));
                }
            }

            if (countries.Count > 0)
            {
                //UpdateFile(countries);
            }
        }

        // process regions response
        private void OnGetRegions(JObject data)
        {
            if (data[VkRestApi.ResponseBody] == null)
            {
                this.run = false;
                return;
            }

            if (totalCount <= 0)
            {
                totalCount = data[VkRestApi.ResponseBody]["count"].ToObject<long>(); ;
            }

            int count = data[VkRestApi.ResponseBody]["items"].Count();

            if (count <= 0)
            {
                this.run = false;
                return;
            }

            // process response body
            for (int i = 0; i < count; ++i)
            {
                JObject obj = data[VkRestApi.ResponseBody]["items"][i].ToObject<JObject>();
                int id = getIntField("id", obj);
                string title = getStringField("title", obj);
                if (id > 0)
                {
                    regions.Add(new VkRegion(id, title));
                }
            }

            //if (regions.Count > 0)
            //{
                //UpdateFile(regions);
            //}
        }

        // process cities response
        private void OnGetCities(JObject data)
        {
            if (data[VkRestApi.ResponseBody] == null)
            {
                this.run = false;
                return;
            }

            if (totalCount <= 0)
            {
                totalCount = data[VkRestApi.ResponseBody]["count"].ToObject<long>(); ;
            }

            int count = data[VkRestApi.ResponseBody]["items"].Count();

            if (count <= 0)
            {
                this.run = false;
                return;
            }

            // process response body
            for (int i = 0; i < count; ++i)
            {
                JObject obj = data[VkRestApi.ResponseBody]["items"][i].ToObject<JObject>();
                int id = getIntField("id", obj);
                string title = getStringField("title", obj);
                int important = getIntField("important", obj, 0);
                string region = getStringField("region", obj);
                string area = getStringField("area", obj);
                if (id > 0)
                {
                    String location = "";

                    if (area.Count() > 0)
                    {
                        location = area;
                    }
                    else if (region.Count() > 0)
                    {
                        location = region;
                    }

                    List<VkCity> cs;
                    if (!citiesByDistrict.TryGetValue(location, out cs))
                    {
                        cs = new List<VkCity>();
                        citiesByDistrict.Add(location, cs);
                    }

                    cs.Add(new VkCity(id, title, important > 0, region, area));
                }
            }

            //if (cities.Count > 0)
            //{
                //UpdateFile(cities);
            //}
        }

        private string parseSearchParameters(SearchParameters parameters)
        {
            StringBuilder builder = new StringBuilder();

            if (parameters.query.Length > 0)
            {
                builder.Append("q=").Append(parameters.query).Append("&");
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

            if (parameters.cities.Count() == 1)
                fileName.Append(parameters.cities[0].Title);
            else
                fileName.Append(parameters.cities[0].Title + "-and-more");

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
            writer.WriteLine("{0}\t\"{1}\"\t\"{2}\"\t\"{3}\"\t\"{4}\"\t\"{5}\"\t\"{6}\"\t\"{7}\"\t\"{8}\"\t\"{9}\"\t\"{10}\"\t\"{11}\"\t\"{12}\"",
                    "id", "Имя", "Фамилия", "Псевдоним", "пол", "д.рождения", "мобильный", "домашний", "город", "страна", "фото", "отношения", "статус");
        }

        private void UpdateFile(List<Person> persons)
        {
            foreach (Person p in persons)
            {
                writer.WriteLine("{0}\t\"{1}\"\t\"{2}\"\t\"{3}\"\t\"{4}\"\t\"{5}\"\t\"{6}\"\t\"{7}\"\t\"{8}\"\t\"{9}\"\t\"{10}\"\t\"{11}\"\t\"{12}\"",
                    p.id, p.firstName, p.lastName, p.screenName, p.sex, p.bdate, p.mobilePhone, p.homePhone, p.city, p.country, p.photo, p.relation, p.status);
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
            long timeToSleep = 400 - (getTimeNowMillis() - timeLastCall);
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

        private static int getIntField(String name, JObject o, int def = 0)
        {
            int result = def;

            if (o[name] != null)
            {
                string value = o[name].ToString();

                try
                {
                    result = Convert.ToInt32(value);
                }
                catch (OverflowException)
                {
                    Debug.WriteLine("The value is outside the range of the Int32 type: " + value);
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
    }
}
