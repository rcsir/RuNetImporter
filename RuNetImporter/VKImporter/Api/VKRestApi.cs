using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using rcsir.net.vk.importer.api.entity;

namespace rcsir.net.vk.importer.api
{
    // VK enumeration for sex field
    public enum VkSexField
    {
        Any = 0,
        Female = 1,
        Male = 2
    };

    // VK enumeration for status (relationship)
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
        GroupsGetInvitedUsers,
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
            public OnErrorEventArgs(VkFunction function, VkRestContext context, long code, String error) :
                this(function, context, code, error, "")
            {
            }

            public OnErrorEventArgs(VkFunction function, VkRestContext context, long code, String error, String details)
            {
                Function = function;
                Context = context;
                Code = code;
                Error = error;
                Details = details;
            }

            public VkFunction Function { get; private set; }
            public long Code { get; private set; }
            public String Error { get; private set; }
            public String Details { get; private set; }
            public VkRestContext Context { get; private set; }

            public string Name()
            {
                return "error";
            }

            public string FileHeader()
            {
                return String.Format("{0}\t{1}\t{2}\t{3}",
                        "function", "code", "error", "details");
            }

            public string ToFileLine()
            {
                return String.Format("{0}\t{1}\t{2}\t{3}",
                        Function, Code, Error, Details);
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
            public long Offset { get; set; } // commonly used in lookup API
            public long Count { get; set; } // commonly used in lookup API 
            public String Cookie { get; set; }
            public String Lang { get; set; } // language - en, ru
        }

        // parameters: function id, name, user_id name, version, isOpen
        // if isOpen is true - method does not require an access token
        private static readonly Method GetProfiles = new Method(VkFunction.GetProfiles, "getProfiles", "uid", "5.21", false);
        private static readonly Method LoadFriends = new Method(VkFunction.LoadFriends, "friends.get", "user_id", "5.21", false);
        private static readonly Method FriendsGet = new Method(VkFunction.FriendsGet, "friends.get", "", "5.21", false); // w/o user id - current user 
        private static readonly Method FriendsGetMutual = new Method(VkFunction.FriendsGetMutual, "friends.getMutual", "source_uid", "5.21", false);
        private static readonly Method UsersSearch = new Method(VkFunction.UsersSearch, "users.search", "", "5.21", false);
        private static readonly Method WallGet = new Method(VkFunction.WallGet, "wall.get", "", "5.14", false);
        private static readonly Method WallGetComments = new Method(VkFunction.WallGetComments, "wall.getComments", "", "5.14", false);
        private static readonly Method StatsGet = new Method(VkFunction.StatsGet, "stats.get", "", "5.25", false);
        private static readonly Method GroupsGetMembers = new Method(VkFunction.GroupsGetMembers, "groups.getMembers", "", "5.21", false);
        private static readonly Method GroupsGetInvitedUsers = new Method(VkFunction.GroupsGetInvitedUsers, "groups.getInvitedUsers", "", "5.58", false);
        private static readonly Method GroupsGetById = new Method(VkFunction.GroupsGetById, "groups.getById", "", "5.21", false);
        private static readonly Method LikesGetList = new Method(VkFunction.LikesGetList, "likes.getList", "", "5.21", false);
        private static readonly Method UsersGet = new Method(VkFunction.UsersGet, "users.get", "", "5.21", false);
        private static readonly Method DatabaseGetCountries = new Method(VkFunction.DatabaseGetCountries, "database.getCountries", "", "5.21", true);
        private static readonly Method DatabaseGetRegions = new Method(VkFunction.DatabaseGetRegions, "database.getRegions", "", "5.21", true);
        private static readonly Method DatabaseGetCities = new Method(VkFunction.DatabaseGetCities, "database.getCities", "", "5.21", true);
        private static readonly Method BoardGetTopics = new Method(VkFunction.BoardGetTopics, "board.getTopics", "", "5.32", false);
        private static readonly Method BoardGetComments = new Method(VkFunction.BoardGetComments, "board.getComments", "", "5.32", false);
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

        // API URL
        private const String ApiUrl = "https://api.vk.com/method/";

        // Request parameters
        public const String GetMethod = "Get";
        public const String ContentType = "application/json; charset=utf-8";
        public const String ContentAccept = "application/json"; // Determines the response type as XML or JSON etc
        public const String ResponseBody = "response";
        public const String ErrorBody = "error";

        // error constants
        public const long CriticalErrorCode = -1;
        public const String CriticalErrorText = "Critical Error";

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
                case VkFunction.GroupsGetInvitedUsers:
                    MakeVkCall(GroupsGetInvitedUsers, context);
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
            var sb = new StringBuilder(ApiUrl);
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
            MakeRestCall(method.Fid, sb.ToString(), context);
            
        }

        // make REST call to VK services
        private void MakeRestCall(VkFunction function, String uri, VkRestContext context)
        {
            if (OnData == null || OnError == null) 
                throw new ArgumentException("OnData and OnError handlers must be provided");

            try
            {
                // Create URI 
                var address = new Uri(uri);
                Debug.WriteLine("REST call: " + address);

                // Create the web request 
                var request = WebRequest.Create(address) as HttpWebRequest;

                if (request == null)
                {
                    var args = new OnErrorEventArgs(function, context, CriticalErrorCode, CriticalErrorText, "Request object is null");
                    OnError(this, args);
                    return;
                }

                // Set type to Get 
                request.Method = GetMethod;
                request.ContentType = ContentType;
                request.Accept = ContentAccept;

                // Get response 
                using (var response = request.GetResponse() as HttpWebResponse)
                {
                    if (response == null)
                    {
                        var args = new OnErrorEventArgs(function, context, CriticalErrorCode, CriticalErrorText, "Response object is null");
                        OnError(this, args);
                        return;
                    }

                    var responseStream = response.GetResponseStream();
                    if (responseStream == null)
                    {
                        var args = new OnErrorEventArgs(function, context, CriticalErrorCode, CriticalErrorText, "Response stream is null");
                        OnError(this, args);
                        return;
                    }

                    var responseCode = (int)response.StatusCode;
                    if (responseCode < 300)
                    {

                        string responseBody;

                        try
                        {
                            responseBody = ((new StreamReader(responseStream)).ReadToEnd());
                        }
                        catch (IOException e)
                        {
                            var args = new OnErrorEventArgs(function, context, CriticalErrorCode, 
                                CriticalErrorText, e.Message);
                            OnError(this, args);
                            return;                            
                        }

                        //var contentType = response.ContentType;
                        var o = JObject.Parse(responseBody);
                        if (o[ResponseBody] != null)
                        {
                            var args = new OnDataEventArgs(function, o, context.Cookie);
                            OnData(this, args);
                        }
                        else if (o[ErrorBody] != null)
                        {
                            long code = 0;
                            var error = "";
                            if (o[ErrorBody]["error_code"] != null)
                            {
                                code = o[ErrorBody]["error_code"].ToObject<long>();
                            }
                            if (o[ErrorBody]["error_msg"] != null)
                            {
                                error = o[ErrorBody]["error_msg"].ToString();
                            }

                            var args = new OnErrorEventArgs(function, context, code, error, o[ErrorBody].ToString());
                            OnError(this, args);
                        }
                    }
                    else
                    {
                        var args = new OnErrorEventArgs(function, context, CriticalErrorCode, CriticalErrorText, "Unexpected response code: " + responseCode);
                        OnError(this, args);
                    }
                }
            }
            catch (WebException exception)
            {
                HandleWebException(function, exception, context);
            }
        }

        private void HandleWebException(VkFunction function, WebException ex, VkRestContext context)
        {
            if (ex.Status == WebExceptionStatus.ProtocolError)
            {
                var statusCode = (int)((HttpWebResponse)ex.Response).StatusCode;
                var responseStream = ex.Response.GetResponseStream();
                var responseText = responseStream!= null ? (new StreamReader(responseStream)).ReadToEnd() : "";
                var args = new OnErrorEventArgs(function, context, statusCode, responseText);
                OnError(this, args);
            }
            else
            {
                var args = new OnErrorEventArgs(function, context, CriticalErrorCode, CriticalErrorText, ex.Message);
                OnError(this, args);
            }
        }
    }
}
