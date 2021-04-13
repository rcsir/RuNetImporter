using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using FileHelpers;
using Newtonsoft.Json.Linq;
using rcsir.net.vk.finder.Dialogs;
using rcsir.net.vk.importer.api;
using rcsir.net.vk.importer.api.entity;
using rcsir.net.vk.importer.Dialogs;

namespace rcsir.net.vk.finder
{
    public partial class VkFinderForm : Form
    {
        private const string RequiredFields = "fields=photo,screen_name,sex,bdate,contacts,relation,status,city,country";

        private const int ItemsPerRequest = 1000;
        private long currentOffset;
        private long totalCount;

        private readonly VKLoginDialog vkLoginDialog;
        private readonly VkRestApi vkRestApi;
        private String userId;
        private String authToken;
        private static readonly AutoResetEvent ReadyEvent = new AutoResetEvent(false);
        private volatile bool run; // finder worker flag
        private bool withPhone = false;

        // special folder for application files
        private readonly string localApplicationDataPath = "";

        // places
        private List<Country> countries = new List<Country>(); 
        private List<Region> regions = new List<Region>();
        private List<City> cities = new List<City>();

        // document
        StreamWriter documentWriter;

        // error log
        StreamWriter errorLogWriter;

        private readonly FileHelperAsyncEngine<Country> countryEngine = new FileHelperAsyncEngine<Country>();
        private readonly FileHelperAsyncEngine<Region> regionEngine = new FileHelperAsyncEngine<Region>();
        private readonly FileHelperAsyncEngine<City> cityEngine = new FileHelperAsyncEngine<City>();

        // person record
        private class Person
        {
            public Person()
            {
                Id = "";
                FirstName = "";
                LastName = "";
                ScreenName = "";
                Sex = "";
                Bdate = "";
                Photo = "";
                MobilePhone = "";
                HomePhone = "";
                City = "";
                Country = "";
                Relation = "";
                Status = "";

            }
            public String Id { get; set; }
            public String FirstName { get; set; }
            public String LastName { get; set; }
            public String ScreenName { get; set; }
            public String Sex { get; set; }
            public String Bdate { get; set; }
            public String Photo { get; set; }
            public String MobilePhone { get; set; }
            public String HomePhone { get; set; }
            public String City { get; set; }
            public String Country { get; set; }
            public String Relation { get; set; }
            public String Status { get; set; }

        };

        public VkFinderForm()
        {
            InitializeComponent();

            userIdTextBox.Text = "Please authorize";
            FindProgressBar.Minimum = 0;
            FindProgressBar.Maximum = 10000;
            FindProgressBar.Step = 1;

            localApplicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            UpdateStatus(0, "Ready");

            vkLoginDialog = new VKLoginDialog();
            // subscribe for login events
            vkLoginDialog.OnUserLogin += UserLogin;

            vkRestApi = new VkRestApi();
            // set up data handler
            vkRestApi.OnData += OnData;
            // set up error handler
            vkRestApi.OnError += OnError;

            // folder for files
            folderBrowserDialog1.Description =
                "Select the directory that you want to use to store files.";
            // Default to the My Documents folder. 
            folderBrowserDialog1.RootFolder = Environment.SpecialFolder.Personal;
            WorkingFolderTextBox.Text = folderBrowserDialog1.SelectedPath;

            // set up background find worker handlers
            backgroundFinderWorker.DoWork += FindWork;
            backgroundFinderWorker.ProgressChanged += FindWorkProgressChanged;
            backgroundFinderWorker.RunWorkerCompleted += FindWorkCompleted;

            // set up background load worker handlers
            backgroundLoaderWorker.DoWork += LoadWork;        
        }

        private void VKFinderForm_Load(object sender, EventArgs e)
        {
            ActivateControls();
        }

        private void AuthorizeButton_Click(object sender, EventArgs e)
        {
            const bool reLogin = true; // if true - will delete cookies and re-login, use false for test
            vkLoginDialog.Login("friends", reLogin); // default permission - friends
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            backgroundFinderWorker.CancelAsync();
        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            // Show the FolderBrowserDialog.
            var result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                WorkingFolderTextBox.Text = folderBrowserDialog1.SelectedPath;

                // if error log exists - close it and create new one
                if (errorLogWriter != null)
                {
                    errorLogWriter.Close();
                    errorLogWriter = null;
                }
                var fileName = GenerateErrorLogFileName();
                errorLogWriter = File.CreateText(fileName);
                errorLogWriter.AutoFlush = true;
                printErrorLogHeader(errorLogWriter);

                // Load regions and cities from VK or file
                backgroundLoaderWorker.RunWorkerAsync(1); // parameter is not important
            }

            ActivateControls();
        }

        private void FindUsersButton_Click(object sender, EventArgs e)
        {
            var searchDialog = new UserSearchDialog(regions, cities);

            if (searchDialog.ShowDialog() == DialogResult.OK)
            {
                var searchParameters = searchDialog.searchParameters;
                withPhone = searchParameters.withPhone;

                UpdateStatus(-1, "Start");
                backgroundFinderWorker.RunWorkerAsync(searchParameters);
            }
            else
            {
                Debug.WriteLine("Search canceled");
            }
        }

        private void ActivateControls()
        {
            var isAuthorized = (!string.IsNullOrEmpty(userId) &&
                                WorkingFolderTextBox.Text.Any());

            if (isAuthorized)
            {
                // enable user controls
                FindUsersButton.Enabled = true;
                CancelFindButton.Enabled = true;
            }
            else
            {
                // disable user controls
                FindUsersButton.Enabled = false;
                CancelFindButton.Enabled = false;
            }
        }

        // Async Workers
        
        // Load Countries, Regions and Cities from VK Database or a file
        private void LoadWork(Object sender, DoWorkEventArgs args)
        {
            var bw = sender as BackgroundWorker;
            var sb = new StringBuilder {Length = 0};

            sb.Append(localApplicationDataPath).Append("\\").Append("vk_countries.csv");
            var countriesFile = sb.ToString();

            sb.Length = 0;
            sb.Append(localApplicationDataPath).Append("\\").Append("vk_regions.csv");
            var regionsFile = sb.ToString();

            sb.Length = 0;
            sb.Append(localApplicationDataPath).Append("\\").Append("vk_cities.csv");
            var citiesFile = sb.ToString();

            try
            {
                var engine1 = new FileHelperEngine<Country>();
                countries = new List<Country>(engine1.ReadFileAsList(countriesFile));
                
                var engine2 = new FileHelperEngine<Region>();
                regions = engine2.ReadFileAsList(regionsFile);

                var engine3 = new FileHelperEngine<City>();
                cities = engine3.ReadFileAsList(citiesFile);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Files do not exist, will generate them from VK");
            }

            if (countries.Any() && regions.Any() && cities.Any())
            {
                // loaded from file
                return;
            }

            // Extract the argument. 
            // SearchParameters searchParameters = args.Argument as SearchParameters;
            var context = new VkRestApi.VkRestContext(userId, authToken);

            // process countries
            // right now we care only about Russia - 1
            countries.Add(new Country(1, "Россия"));
            countries.Add(new Country(2, "Украина"));
            
            foreach (var country in countries)
            {
                // reset counters
                run = true;
                totalCount = 0;
                currentOffset = 0;
                long timeLastCall = 0;

                // add region for major cities
                regions.Add(new Region(0, country.Id, "Большие города"));

                while (run &&
                    currentOffset <= totalCount)
                {
                    sb.Length = 0;
                    sb.Append("country_id=").Append(country.Id).Append("&");
                    sb.Append("offset=").Append(currentOffset).Append("&");
                    sb.Append("count=").Append(ItemsPerRequest).Append("&");
                    context.Parameters = sb.ToString();
                    context.Cookie = country.Id.ToString(); // send country ID as a cooky
                    Debug.WriteLine("request parameters: " + context.Parameters);

                    // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                    timeLastCall = Utils.SleepTime(timeLastCall);
                    vkRestApi.CallVkFunction(VkFunction.DatabaseGetRegions, context);

                    // wait for the data
                    ReadyEvent.WaitOne();
                    currentOffset += ItemsPerRequest;
                }

                // process important cities
                // reset counters
                run = true;
                totalCount = 0;
                currentOffset = 0;
                timeLastCall = 0;

                while (run &&
                    currentOffset <= totalCount)
                {
                    sb.Length = 0;
                    sb.Append("country_id=").Append(country.Id).Append("&"); // Russia
                    sb.Append("need_all=").Append(0).Append("&"); // need only important one
                    sb.Append("offset=").Append(currentOffset).Append("&");
                    sb.Append("count=").Append(ItemsPerRequest).Append("&");
                    context.Parameters = sb.ToString();
                    context.Cookie = 0.ToString(); // set region for important cities to 0
                    Debug.WriteLine("request parameters: " + context.Parameters);

                    // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                    timeLastCall = Utils.SleepTime(timeLastCall);
                    vkRestApi.CallVkFunction(VkFunction.DatabaseGetCities, context);

                    // wait for the user data
                    ReadyEvent.WaitOne();
                    currentOffset += ItemsPerRequest;
                }

                // process cities for each region
                foreach (var region in regions)
                {
                    // reset counters
                    run = true;
                    totalCount = 0;
                    currentOffset = 0;
                    timeLastCall = 0;

                    while (run &&
                           currentOffset <= totalCount)
                    {
                        sb.Length = 0;
                        sb.Append("country_id=").Append(country.Id).Append("&"); 
                        sb.Append("region_id=").Append(region.Id).Append("&"); // set region id
                        sb.Append("need_all=").Append(0).Append("&"); // need only important one
                        sb.Append("offset=").Append(currentOffset).Append("&");
                        sb.Append("count=").Append(ItemsPerRequest).Append("&");
                        context.Parameters = sb.ToString();
                        context.Cookie = region.Id.ToString(); // set region id as a cooky
                        Debug.WriteLine("request parameters: " + context.Parameters);

                        // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                        timeLastCall = Utils.SleepTime(timeLastCall);
                        vkRestApi.CallVkFunction(VkFunction.DatabaseGetCities, context);

                        // wait for the user data
                        ReadyEvent.WaitOne();
                        currentOffset += ItemsPerRequest;
                    }
                }
            }

            // save countries
            using (countryEngine.BeginWriteFile(countriesFile))
            {
                countryEngine.WriteNexts(countries);
            }

            using (regionEngine.BeginWriteFile(regionsFile))
            {
                regionEngine.WriteNexts(regions);
            }

            using (cityEngine.BeginWriteFile(citiesFile))
            {
                cityEngine.WriteNexts(cities);
            }

            //args.Result = TimeConsumingOperation(bw, arg);
        }

        private void FindWork(Object sender, DoWorkEventArgs args)
        {
            // Do not access the form's BackgroundWorker reference directly. 
            // Instead, use the reference provided by the sender parameter.
            var bw = sender as BackgroundWorker;
            if(bw == null)
                throw new ArgumentNullException("sender");

            // Extract the argument. 
            var searchParameters = args.Argument as SearchParameters;
            if (searchParameters == null)
                throw new ArgumentException("args.Argument");

            var parameters = parseSearchParameters(searchParameters);

            var startYear = (Int32)searchParameters.yearStart;
            var stopYear = (Int32)searchParameters.yearEnd;
            var startMonth = (Int32)searchParameters.monthStart;
            var stopMonth = (Int32)searchParameters.monthEnd;

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

            var saveStartDate = new DateTime(startYear, startMonth, 1);
            var saveStopDate = new DateTime(stopYear, stopMonth, 1);

            if(searchParameters.useSlowSearch)
            {
                // set stop date to the last day of the stop month
                saveStopDate = saveStopDate.AddMonths(1).AddDays(-1); 
            }

            // figure out step
            var step = 10000;
                
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
                    step = 10000 / ((saveStopDate.Year - saveStartDate.Year) * 12  + saveStopDate.Month - saveStartDate.Month + 1);
                }
            }

            step /= searchParameters.cities.Count();

            // create stream documentWriter
            var fileName = GenerateFileName(searchParameters);
            documentWriter = File.CreateText(fileName);
            printHeader(documentWriter);

            var context = new VkRestApi.VkRestContext(this.userId, this.authToken);

            // loop by birth year and month to maximize number of matches (1000 at a time)
            var sb = new StringBuilder();
            foreach (var city in searchParameters.cities)
            {
                long timeLastCall = 0;
                var startDate = saveStartDate;
                var stopDate = saveStopDate;

                while (startDate <= stopDate)
                {
                    if (bw.CancellationPending)
                        break;

                    bw.ReportProgress(step, "Searching in " + city.Title);

                    run = true;
                    totalCount = 0;
                    currentOffset = 0;

                    while (run &&
                        currentOffset <= totalCount)
                    {
                        if (bw.CancellationPending)
                            break;

                        sb.Length = 0;
                        sb.Append("offset=").Append(currentOffset).Append("&");
                        sb.Append("count=").Append(ItemsPerRequest).Append("&");
                        sb.Append(parameters);
                        if (city.Id > 0)
                        {
                            sb.Append("city=").Append(city.Id).Append("&");
                        }

                        // append birth date 
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
                        sb.Append(RequiredFields);

                        context.Parameters = sb.ToString();
                        Debug.WriteLine("Search parameters: " + context.Parameters);

                        context.Cookie = currentOffset.ToString();

                        // play nice, sleep for 1/3 sec to stay within 3 requests/second limits
                        timeLastCall = Utils.SleepTime(timeLastCall);
                        vkRestApi.CallVkFunction(VkFunction.UsersSearch, context);

                        // wait for the user data
                        ReadyEvent.WaitOne();

                        currentOffset += ItemsPerRequest;
                    }

                    // increment date
                    startDate = searchParameters.useSlowSearch ? startDate.AddDays(1) : startDate.AddMonths(1);
                }
            }

            documentWriter.Close();

            //args.Result = TimeConsumingOperation(bw, arg);

            // If the operation was canceled by the user,  
            // set the DoWorkEventArgs.Cancel property to true. 
            if (bw.CancellationPending)
            {
                args.Cancel = true;
            }

        }

        private void FindWorkProgressChanged(Object sender, ProgressChangedEventArgs args)
        {
            var status = args.UserState as String;
            var progress = args.ProgressPercentage;
            UpdateStatus(progress, status);
        }

        private void FindWorkCompleted(object sender, RunWorkerCompletedEventArgs args)
        {
            if (args.Error != null)
            {
                MessageBox.Show(args.Error.Message);
                UpdateStatus(0, "Error");
            }
            else if (args.Cancelled)
            {
                MessageBox.Show("Search canceled!");
                UpdateStatus(0, "Canceled");
            }
            else
            {
                //MessageBox.Show("Search complete!");
                UpdateStatus(10000, "Done");
            }
        }

        private void UserLogin(object loginDialog, UserLoginEventArgs loginArgs)
        {
            Debug.WriteLine("User Logged In: " + loginArgs);
            
            userId = loginArgs.userId;
            authToken = loginArgs.authToken;

            userIdTextBox.Clear();
            userIdTextBox.Text = "Authorized " + loginArgs.userId;
            ActivateControls();
        }

        private void OnData(object vkApi, OnDataEventArgs onDataArgs)
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
                    OnGetRegions(onDataArgs.Data, onDataArgs.Cookie);
                    break;
                case VkFunction.DatabaseGetCities:
                    OnGetCities(onDataArgs.Data, onDataArgs.Cookie);
                    break;
                default:
                    Debug.WriteLine("Error, unknown function.");
                    break;
            }

            // indicate that data is ready and we can continue
            ReadyEvent.Set();
        }

        // main error handler
        private void OnError(object vkApi, VkRestApi.OnErrorEventArgs onErrorArgs)
        {
            Debug.WriteLine("Function " + onErrorArgs.Function + ", returned error: " + onErrorArgs.Details);

            if (errorLogWriter != null)
            {
                updateErrorLogFile(onErrorArgs, errorLogWriter);
            }

            if (onErrorArgs.Code == VkRestApi.CriticalErrorCode)
            {
                var result = MessageBox.Show(
                    onErrorArgs.Details + "\n Please fix connection problem or just wait.\n Press \'Yes\' to continue with the current request" +
                    "\nPress \'No\' to continue with the next request.",
                    onErrorArgs.Error,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button1);

                if (result == DialogResult.Yes)
                {
                    vkRestApi.CallVkFunction(onErrorArgs.Function, onErrorArgs.Context);
                    return;
                }
            }
            else
            {
                switch (onErrorArgs.Code)
                {
                    case 6:
                        // this is too many requests error - repeat last API call
                        Utils.SleepTime(0);
                        vkRestApi.CallVkFunction(onErrorArgs.Function, onErrorArgs.Context);
                        return;
                    case 15:
                        // user is not found - continue
                        break;
                    default:
                        var result = MessageBox.Show(
                            onErrorArgs.Details + "\n Please fix connection problem or just wait.\n Press \'Yes\' to continue with the current request" +
                            "\nPress \'No\' to continue with the next request.",
                            onErrorArgs.Error,
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning,
                            MessageBoxDefaultButton.Button1);

                        if (result == DialogResult.Yes)
                        {
                            vkRestApi.CallVkFunction(onErrorArgs.Function, onErrorArgs.Context);
                            return;
                        }
                        break;
                }
            }

            // indicate that data is ready and we can continue
            ReadyEvent.Set();
        }

        //================================
        // data handlers

        // process users search response
        private void OnUsersSearch(JObject data)
        {
            if (data[VkRestApi.ResponseBody] == null)
            {
                run = false;
                return;
            }

            if (totalCount <= 0)
            {
                totalCount = data[VkRestApi.ResponseBody]["count"].ToObject<long>();
            }

            var count = data[VkRestApi.ResponseBody]["items"].Count();

            if (count <= 0)
            {
                run = false;
                return;
            }

            backgroundFinderWorker.ReportProgress(0, "Processing " + count + " records out of " + totalCount);

            var persons = new List<Person>();
            // process response body
            for (var i = 0; i < count; ++i)
            {
                var personObj = data[VkRestApi.ResponseBody]["items"][i].ToObject<JObject>();

                // TODO: check phone with regex.
                var t1 = personObj["mobile_phone"] != null ? parsePhone(personObj["mobile_phone"].ToString()) : "";
                var t2 = personObj["home_phone"] != null ? parsePhone(personObj["home_phone"].ToString()) : "";

                if (withPhone &&
                    t1.Length < 7 &&
                    t2.Length < 7)
                {
                    continue; // invalid phone number
                }

                var person = new Person();
                person.MobilePhone = t1;
                person.HomePhone = t2;
                person.Id = personObj["id"].ToString();
                person.FirstName = personObj["first_name"].ToString();
                person.LastName = personObj["last_name"].ToString();
                person.ScreenName = personObj["screen_name"] != null ? personObj["screen_name"].ToString() : "";
                person.Sex = personObj["sex"] != null ? personObj["sex"].ToString() : "";
                person.Bdate = personObj["bdate"] != null ? personObj["bdate"].ToString() : "";
                person.City = Utils.GetStringField("city", "title", personObj);
                person.Country = Utils.GetStringField("country", "title", personObj);
                person.Photo = personObj["photo"] != null ? personObj["photo"].ToString() : "";
                person.Relation = personObj["relation"] != null ? personObj["relation"].ToString() : "";
                var t = personObj["status"] != null ? personObj["status"].ToString() : "";
                if (t.Length > 0)
                {
                    person.Status = Regex.Replace(t, @"\r\n?|\n", "");
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
                run = false;
                return;
            }

            if (totalCount <= 0)
            {
                totalCount = data[VkRestApi.ResponseBody]["count"].ToObject<long>();
            }

            var count = data[VkRestApi.ResponseBody]["items"].Count();

            if (count <= 0)
            {
                run = false;
                return;
            }

            var cs = new List<Country>();
            // process response body
            for (var i = 0; i < count; ++i)
            {
                var obj = data[VkRestApi.ResponseBody]["items"][i].ToObject<JObject>();
                var id = Utils.GetIntField("id", obj);
                var title = Utils.GetStringField("title", obj);
                if(id > 0)
                {
                    cs.Add(new Country(id, title));
                }
            }
        }

        // process regions response
        private void OnGetRegions(JObject data, string cookie)
        {
            if (data[VkRestApi.ResponseBody] == null)
            {
                run = false;
                return;
            }

            if (totalCount <= 0)
            {
                totalCount = data[VkRestApi.ResponseBody]["count"].ToObject<long>(); ;
            }

            var count = data[VkRestApi.ResponseBody]["items"].Count();

            if (count <= 0)
            {
                run = false;
                return;
            }

            var countryId = Convert.ToInt32(cookie); // country id is passed as a cookie

            // process response body
            for (var i = 0; i < count; ++i)
            {
                var obj = data[VkRestApi.ResponseBody]["items"][i].ToObject<JObject>();
                var id = Utils.GetIntField("id", obj);
                var title = Utils.GetStringField("title", obj);
                if (id > 0)
                {
                    regions.Add(new Region(id, countryId, title));
                }
            }
        }

        // process cities response
        private void OnGetCities(JObject data, string cookie)
        {
            if (data[VkRestApi.ResponseBody] == null)
            {
                run = false;
                return;
            }

            if (totalCount <= 0)
            {
                totalCount = data[VkRestApi.ResponseBody]["count"].ToObject<long>(); ;
            }

            var count = data[VkRestApi.ResponseBody]["items"].Count();

            if (count <= 0)
            {
                run = false;
                return;
            }

            var regionId = Convert.ToInt32(cookie); // region id is passed as a cookie

            // process response body
            for (var i = 0; i < count; ++i)
            {
                var obj = data[VkRestApi.ResponseBody]["items"][i].ToObject<JObject>();
                var id = Utils.GetIntField("id", obj);
                var title = Utils.GetStringField("title", obj);
                var important = Utils.GetIntField("important", obj);
                var region = Utils.GetStringField("region", obj);
                var area = Utils.GetStringField("area", obj);
                if (id <= 0)
                    continue;

                if (region.Length == 0 &&
                    area.Length == 0)
                {
                    region = title; // major city - set title as a region
                }

                var city = new City(id, title, important > 0, regionId, region, area);
                cities.Add(city);
            }
        }

        private string parseSearchParameters(SearchParameters parameters)
        {
            var builder = new StringBuilder();

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

        private string GenerateFileName(SearchParameters parameters)
        {
            var fileName = new StringBuilder(this.WorkingFolderTextBox.Text);
            
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
            var sb = new StringBuilder();

            // remove any non number characters from the phone
            foreach (var ch in phone)
            {
                if(char.IsDigit(ch))
                {
                    sb.Append(ch);
                }
            }

            return sb.ToString();
        }

        private void printHeader(StreamWriter writer)
        {
            writer.WriteLine("{0}\t\"{1}\"\t\"{2}\"\t\"{3}\"\t\"{4}\"\t\"{5}\"\t\"{6}\"\t\"{7}\"\t\"{8}\"\t\"{9}\"\t\"{10}\"\t\"{11}\"\t\"{12}\"",
                    "id", "Имя", "Фамилия", "Псевдоним", "пол", "д.рождения", "мобильный", "домашний", "город", "страна", "фото", "отношения", "статус");
        }

        private void UpdateFile(IEnumerable<Person> persons)
        {
            foreach (var p in persons)
            {
                documentWriter.WriteLine("{0}\t\"{1}\"\t\"{2}\"\t\"{3}\"\t\"{4}\"\t\"{5}\"\t\"{6}\"\t\"{7}\"\t\"{8}\"\t\"{9}\"\t\"{10}\"\t\"{11}\"\t\"{12}\"",
                    p.Id, p.FirstName, p.LastName, p.ScreenName, p.Sex, p.Bdate, p.MobilePhone, p.HomePhone, p.City, p.Country, p.Photo, p.Relation, p.Status);
            }
        }

        private void UpdateStatus(int progress, String status)
        {
            if (progress > 0)
            {
                FindProgressBar.Increment(progress);

            } 
            else if (progress < 0)
            {
                // reset 
                FindProgressBar.Value = 0;
            }

            toolStripStatusLabel1.Text = status;
        }

        // Error log file
        private string GenerateErrorLogFileName()
        {
            var fileName = new StringBuilder(WorkingFolderTextBox.Text);
            return fileName.Append("\\").Append("error").Append(".log").ToString();
        }

        private void printErrorLogHeader(StreamWriter writer)
        {
            writer.WriteLine("{0}\t{1}\t{2}\t{3}",
                    "function", "error_code", "error", "details");
        }

        private void updateErrorLogFile(VkRestApi.OnErrorEventArgs error, StreamWriter writer)
        {
            writer.WriteLine("{0}\t{1}\t{2}\t{3}",
                error.Function, error.Code, error.Error, error.Details);
        }

        private void VKFinderForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (errorLogWriter != null)
            {
                errorLogWriter.Flush();
                errorLogWriter.Close();
            }
        }
    }
}
