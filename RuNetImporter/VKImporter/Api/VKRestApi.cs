using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using rcsir.net.vk.importer.api.entity;

namespace rcsir.net.vk.importer.api
{
    // VK enum for sex field
    public enum VkSexField
    {
        Any = 0,
        Female = 1,
        Male = 2
    };

    // VK enum for status (relationship)
    public enum VkRelationshipStatus
    {
        NotMarried = 1, 
        InRelationship = 2, 
        Engaged = 3, 
        Married = 4,
        ItsComplicated = 5, 
        ActivelySearching = 6,
        InLove = 7 
    };

    // VK Country
    public class VkCountry
    {
        public int Id { get; private set; }
        public string Title { get; private set; }

        public VkCountry(int id, string title)
        {
            Id = id;
            Title = title;
        }

        public override string ToString()
        {
            return Title;
        }
    };

    // VK Region
    public class VkRegion
    {
        public int Id { get; private set; }
        public string Title { get; private set; }

        public VkRegion(int id, string title)
        {
            Id = id;
            Title = title;
        }

        public override string ToString()
        {
            return Title;
        }
    };

    // VK city
    public class VkCity
    {
        public int Id { get; private set; }
        public string Title { get; private set; }
        public bool Important { get; private set; }
        public string RegionTitle { get; private set; }
        public string AreaTitle { get; private set; }

        public VkCity(int id, String title)
        {
            Title = title;
            Id = id;
            Important = false;
            RegionTitle = "";
            AreaTitle = "";
        }

        public VkCity(int id, String title, bool important)
            : this(id, title)
        {
            Important = important;
        }

        public VkCity(int id, String title, bool important, string region)
            : this(id, title, important)
        {
            RegionTitle = region;
        }

        public VkCity(int id, String title, bool important, string region, string area)
            : this(id, title, important, region)
        {
            AreaTitle = area;
        }

        public override string ToString()
        {
            return String.Format("{0}, {1}, {2}",Title, AreaTitle, RegionTitle);
        }
    };

    // VK sex
    public class VkSex
    {
        public string Sex { get; set; }
        public int Value { get; set; }

        public VkSex(String sex, int value)
        {
            Sex = sex;
            Value = value;
        }

        public override string ToString()
        {
            return Sex;
        }
    };

    // onData event arguments
    public class OnDataEventArgs : EventArgs
    {
        public OnDataEventArgs(VkFunction function, JObject data, String cookie)
        {
            Function = function;
            Data = data;
            Cookie = cookie;
        }

        public VkFunction Function { get; private set; }
        public JObject Data { get; private set; }
        public String Cookie { get; private set; } // parameters from the request
    }

    // Define implemented VK rest API
    public enum VkFunction
    {
        GetProfiles,
        LoadFriends,
        FriendsGet,
        FriendsGetMutual,
        UsersSearch,
        WallGet,
        WallGetComments,
        GroupsGetMembers,
        GroupsGetById,
        LikesGetList,
        UsersGet,
        DatabaseGetCountries,
        DatabaseGetRegions,
        DatabaseGetCities,
        StatsGet,
        BoardGetTopics,
        BoardGetComments,
        PhotosSearch
    };


    public class VkRestApi
    {
        private class Method
        {
            public Method(VkFunction fid, string name, string userParam, string version, bool isOpen)
            {
                Fid = fid;
                Name = name;
                UserParam = userParam;
                Version = version;
                IsOpen = isOpen;
            }

            public VkFunction Fid { get; private set; }
            public string Name { get; private set; }
            public string UserParam { get; private set; }
            public string Version { get; private set; }
            public bool IsOpen { get; private set; }

        };

        // onError event arguments
        public class OnErrorEventArgs : EventArgs, IEntity
        {
            public OnErrorEventArgs() :
                this(VkFunction.FriendsGet, "")
            {
            }

            public OnErrorEventArgs(VkFunction function, String error) :
                this(function, 0, error)
            {
            }

            public OnErrorEventArgs(VkFunction function, long code, String error) :
                this(function, code, error, "")
            {
            }

            public OnErrorEventArgs(VkFunction function, long code, String error, String details)
            {
                Function = function;
                Code = code;
                Error = error;
                Details = details;
            }

            public VkFunction Function { get; private set; }
            public long Code { get; private set; }
            public String Error { get; private set; }
            public String Details { get; private set; }

            public string Name()
            {
                return "error";
            }

            public string FileHeader()
            {
                return String.Format("{0}\t{1}\t{2}",
                        "function", "code", "error");
            }

            public string ToFileLine()
            {
                return String.Format("{0}\t{1}\t{2}",
                        Function, Code, Error);
            }
        }

        // VK Rest API call context
        public class VkRestContext
        {
            public VkRestContext(String userId, String authToken)
            {
                UserId = userId;
                AuthToken = authToken;
                Parameters = null;
                Offset = 0;
                Count = 1000;
                Cookie = null;
                Lang = "ru"; // en, ru
            }

            public VkRestContext(String userId, String authToken, String parameters) :
                this(userId, authToken)
            {
                Parameters = parameters;
            }

            public VkRestContext(String userId, String authToken, String parameters, long offset, long count) :
                this(userId, authToken, parameters)
            {
                Offset = offset;
                Count = count;
            }

            public VkRestContext(String userId, String authToken, String parameters, long offset, long count, String cookie) :
                this(userId, authToken, parameters, offset, count)
            {
                Cookie = cookie;
            }

            public readonly String UserId;
            public readonly String AuthToken;
            public String Parameters { get; set; }
            public long Offset { get; set; } // commonly used in lookup api
            public long Count { get; set; } // commonly used in lookup api 
            public String Cookie { get; set; }
            public String Lang { get; set; } // language - en, ru
        }

        // params: fid, name, user_id name, version, isOpen
        // if isOpen is true - method does not require an access token
        private static readonly Method GetProfiles = new Method(VkFunction.GetProfiles, "getProfiles", "uid", "5.21", false);
        private static readonly Method LoadFriends = new Method(VkFunction.LoadFriends, "friends.get", "user_id", "5.21", true);
        private static readonly Method FriendsGet = new Method(VkFunction.FriendsGet, "friends.get", "", "5.21", true); // w/o user id - current user 
        private static readonly Method FriendsGetMutual = new Method(VkFunction.FriendsGetMutual, "friends.getMutual", "source_uid", "5.21", false);
        private static readonly Method UsersSearch = new Method(VkFunction.UsersSearch, "users.search", "", "5.21", false);
        private static readonly Method WallGet = new Method(VkFunction.WallGet, "wall.get", "", "5.14", true);
        private static readonly Method WallGetComments = new Method(VkFunction.WallGetComments, "wall.getComments", "", "5.14", true);
        private static readonly Method StatsGet = new Method(VkFunction.StatsGet, "stats.get", "", "5.25", false);
        private static readonly Method GroupsGetMembers = new Method(VkFunction.GroupsGetMembers, "groups.getMembers", "", "5.21", true);
        private static readonly Method GroupsGetById = new Method(VkFunction.GroupsGetById, "groups.getById", "", "5.21", true);
        private static readonly Method LikesGetList = new Method(VkFunction.LikesGetList, "likes.getList", "", "5.21", true);
        private static readonly Method UsersGet = new Method(VkFunction.UsersGet, "users.get", "", "5.21", true);
        private static readonly Method DatabaseGetCountries = new Method(VkFunction.DatabaseGetCountries, "database.getCountries", "", "5.21", true);
        private static readonly Method DatabaseGetRegions = new Method(VkFunction.DatabaseGetRegions, "database.getRegions", "", "5.21", true);
        private static readonly Method DatabaseGetCities = new Method(VkFunction.DatabaseGetCities, "database.getCities", "", "5.21", true);
        private static readonly Method BoardGetTopics = new Method(VkFunction.BoardGetTopics, "board.getTopics", "", "5.32", true);
        private static readonly Method BoardGetComments = new Method(VkFunction.BoardGetComments, "board.getComments", "", "5.32", true);
        private static readonly Method PhotosSearch = new Method(VkFunction.PhotosSearch, "photos.search", "", "5.33", true);

        // define OnData delegate
        public delegate void DataHandler
        (
            object vkRestApi,
            OnDataEventArgs onDataArgs
        );

        public DataHandler OnData;

        // define OnError delegate
        public delegate void ErrorHandler
        (
            object vkRestApi,
            OnErrorEventArgs onErrorArgs
        );

        public ErrorHandler OnError;

        // API usrl
        private readonly String api_url = "https://api.vk.com/method/";

        // Request parameters
        public static readonly String GET_METHOD = "Get";
        public static readonly String CONTENT_TYPE = "application/json; charset=utf-8";
        public static readonly String CONTENT_ACCEPT = "application/json"; // Determines the response type as XML or JSON etc
        public static readonly String RESPONSE_BODY = "response";
        public static readonly String ERROR_BODY = "error";

        // functions switch
        public void CallVkFunction(VkFunction function, VkRestContext context)
        {
            switch (function)
            {
                case VkFunction.GetProfiles:
                    MakeVkCall(GetProfiles, context);
                    break;
                case VkFunction.LoadFriends:
                    MakeVkCall(LoadFriends, context);
                    break;
                case VkFunction.FriendsGet:
                    MakeVkCall(FriendsGet, context);
                    break;
                case VkFunction.FriendsGetMutual:
                    MakeVkCall(FriendsGetMutual, context);
                    break;
                case VkFunction.UsersSearch:
                    MakeVkCall(UsersSearch, context);
                    break;
                case VkFunction.WallGet:
                    MakeVkCall(WallGet, context);
                    break;
                case VkFunction.WallGetComments:
                    MakeVkCall(WallGetComments, context);
                    break;
                case VkFunction.StatsGet:
                    MakeVkCall(StatsGet, context);
                    break;
                case VkFunction.GroupsGetMembers:
                    MakeVkCall(GroupsGetMembers, context);
                    break;
                case VkFunction.GroupsGetById:
                    MakeVkCall(GroupsGetById, context);
                    break;
                case VkFunction.LikesGetList:
                    MakeVkCall(LikesGetList, context);
                    break;
                case VkFunction.UsersGet:
                    MakeVkCall(UsersGet, context);
                    break;
                case VkFunction.DatabaseGetCountries:
                    MakeVkCall(DatabaseGetCountries, context);
                    break;
                case VkFunction.DatabaseGetRegions:
                    MakeVkCall(DatabaseGetRegions, context);
                    break;
                case VkFunction.DatabaseGetCities:
                    MakeVkCall(DatabaseGetCities, context);
                    break;
                case VkFunction.BoardGetTopics:
                    MakeVkCall(BoardGetTopics, context);
                    break;
                case VkFunction.BoardGetComments:
                    MakeVkCall(BoardGetComments, context);
                    break;
                case VkFunction.PhotosSearch:
                    MakeVkCall(PhotosSearch, context);
                    break;
            }
        }

        private void MakeVkCall( Method method, VkRestContext context)
        {
            var sb = new StringBuilder(api_url);
            sb.Append(method.Name).Append('?');
            
            if (method.UserParam.Length > 0)
            {
                // if method uses user id - get id name from method and user id from context
                sb.Append(method.UserParam).Append("=").Append(context.UserId).Append('&');
            }
            
            if (!method.IsOpen)
            {
                // if method is secure - apply access token from context
                sb.Append("access_token=").Append(context.AuthToken).Append('&');                
            }
            
            if (context.Parameters.Length > 0)
            {
                // apply additional parameters from the context
                sb.Append(context.Parameters).Append('&');
            }

            sb.Append("v=").Append(method.Version).Append('&');
            sb.Append("lang=").Append(context.Lang);

            // make rest call
            MakeRestCall(method.Fid, sb.ToString(), context.Cookie);
            
        }

        // make REST call to VK services
        private void MakeRestCall(VkFunction function, String uri, String cookie = null)
        {
            try
            {
                // Create URI 
                var address = new Uri(uri);
                Debug.WriteLine("REST call: " + address);

                // Create the web request 
                var request = WebRequest.Create(address) as HttpWebRequest;

                // Set type to Get 
                request.Method = GET_METHOD;
                request.ContentType = CONTENT_TYPE;
                request.Accept = CONTENT_ACCEPT;

                // Get response 
                using (var response = request.GetResponse() as HttpWebResponse)
                {
                    Stream responseStream = null;
                    responseStream = response.GetResponseStream();
                    var responseCode = (int)response.StatusCode;
                    if (responseCode < 300)
                    {
                        string responseBody = ((new StreamReader(responseStream)).ReadToEnd());
                        string contentType = response.ContentType;
                        JObject o = JObject.Parse(responseBody);
                        if (o[RESPONSE_BODY] != null)
                        {
                            // Debug.WriteLine("REST response: " + o[RESPONSE_BODY].ToString());
                            // OK - notify listeners
                            if (OnData != null)
                            {
                                var args = new OnDataEventArgs(function, o, cookie);
                                OnData(this, args);
                            }
                        }
                        else if (o[ERROR_BODY] != null)
                        {
                            // Debug.WriteLine("REST error: " + o[ERROR_BODY].ToString());
                            // Error - notify listeners
                            if (OnError != null)
                            {
                                long code = 0;
                                String error = "";
                                if (o[ERROR_BODY]["error_code"] != null)
                                {
                                    code = o[ERROR_BODY]["error_code"].ToObject<long>();
                                }
                                if (o[ERROR_BODY]["error_msg"] != null)
                                {
                                    error = o[ERROR_BODY]["error_msg"].ToString();
                                }

                                var args = new OnErrorEventArgs(function, code, error, o[ERROR_BODY].ToString());
                                OnError(this, args);
                            }
                        }
                    }
                }
            }
            catch (WebException exception)
            {
                HandleWebException(function, exception);
            }
        }

        private void HandleWebException(VkFunction function, WebException ex)
        {
            if (ex.Status == WebExceptionStatus.ProtocolError)
            {
                var statusCode = (int)((HttpWebResponse)ex.Response).StatusCode;
                Stream responseStream = ex.Response.GetResponseStream();
                string responseText = (new StreamReader(responseStream)).ReadToEnd();

                if (statusCode == 500)
                {
                    Debug.WriteLine("Error 500 - " + responseText);
                }
                else
                {
                    // Do Something for other status codes
                    Debug.WriteLine("Error " + statusCode);
                }

                // Error - notify listeners
                if (OnError != null)
                {
                    var errorsb = new StringBuilder();
                    errorsb.Append("StatusCode: ").Append(statusCode).Append(',');
                    errorsb.Append("Error: \'").Append(responseText).Append("\'");
                    var args = new OnErrorEventArgs(function, errorsb.ToString());
                    OnError(this, args);
                }
            }
            else
            {
                throw (ex); // Or check for other WebExceptionStatus
            }
        }
    }
}
