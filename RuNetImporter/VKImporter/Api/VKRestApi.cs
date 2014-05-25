﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using Smrf.AppLib;
using rcsir.net.common.Network;

namespace rcsir.net.vk.importer.api
{
    // Define implemented VK rest API
    public enum VKFunction
    {
        LoadUserInfo,
        LoadFriends,
        GetFriends,
        GetMutual,
        UsersSearch,
        WallGet,
        WallGetComments,
        GroupsGetMembers,
        GroupsGetById,
        LikesGetList,
        UsersGet
    };

    // VK enum for sex field
    public enum VKSexField
    {
        any = 0,
        female = 1,
        male = 2
    };

    // VK enum for status (relationship)
    public enum VKRelationshipStatus
    {
        NotMarried = 1, 
        InRelationship = 2, 
        Engaged = 3, 
        Married = 4,
        ItsComplicated = 5, 
        ActivelySearching = 6,
        InLove = 7 
    };

    // VK city
    public class VKCity
    {
        public string Name { get; set; }
        public int Value { get; set; }

        public VKCity(String name, int value)
        {
            this.Name = name;
            this.Value = value;
        }
        
        public override string ToString()
        {
            return Name;
        }
    };

    // VK sex
    public class VKSex
    {
        public string Sex { get; set; }
        public int Value { get; set; }

        public VKSex(String sex, int value)
        {
            this.Sex = sex;
            this.Value = value;
        }

        public override string ToString()
        {
            return Sex;
        }
    };

    // onData event arguments
    public class OnDataEventArgs : EventArgs
    {
        public OnDataEventArgs(VKFunction function, JObject data, String cookie)
        {
            this.function = function;
            this.data = data;
            this.cookie = cookie;
        }

        public readonly VKFunction function;
        public readonly JObject data;
        public readonly String cookie; // parameters from the request
    }

    // onError event arguments
    public class OnErrorEventArgs : EventArgs
    {
        public OnErrorEventArgs(VKFunction function, String error) :
            this(function, 0, error)
        {
        }

        public OnErrorEventArgs(VKFunction function, long code, String error) :
            this(function, code, error,  "")
        {
        }

        public OnErrorEventArgs(VKFunction function, long code, String error, String details)
        {
            this.function = function;
            this.code = code;
            this.error = error;
            this.details = details;
        }

        public readonly VKFunction function;
        public readonly long code;
        public readonly String error;
        public readonly String details;
    }

    public class VKRestContext 
    {
        public VKRestContext(String userId, String authToken)
        {
            this.userId = userId;
            this.authToken = authToken;
            this.parameters = null;
            this.offset = 0;
            this.count = 1000;
            this.cookie = null;
        }

        public VKRestContext(String userId, String authToken, String parameters) : 
            this(userId, authToken)
        {
            this.parameters = parameters;
        }

        public VKRestContext(String userId, String authToken, String parameters, long offset, long count) :
            this(userId, authToken, parameters)
        {
            this.offset = offset;
            this.count = count;
        }

        public VKRestContext(String userId, String authToken, String parameters, long offset, long count, String cookie) :
            this(userId, authToken, parameters, offset, count)
        {
            this.cookie = cookie;
        }

        public readonly String userId;
        public readonly String authToken;
        public String parameters {get; set;}
        public long offset { get; set; } // commonly used in lookup api
        public long count { get; set; } // commonly used in lookup api 
        public String cookie { get; set; }
    }

    public class VKRestApi
    {

        // define OnData delegate
        public delegate void DataHandler
        (
            object VKRestApi,
            OnDataEventArgs onDataArgs
        );

        public DataHandler OnData;

        // define OnError delegate
        public delegate void ErrorHandler
        (
            object VKRestApi,
            OnErrorEventArgs onErrorArgs
        );

        public ErrorHandler OnError;

        // API usrl
        private readonly String api_url = "https://api.vk.com";

        // Request parameters
        public static readonly String GET_METHOD = "Get";
        public static readonly String CONTENT_TYPE = "application/json; charset=utf-8";
        public static readonly String CONTENT_ACCEPT = "application/json"; // Determines the response type as XML or JSON etc
        public static readonly String RESPONSE_BODY = "response";
        public static readonly String ERROR_BODY = "error";

        public VKRestApi()
        {
        }

        // VK API
        public void CallVKFunction(VKFunction function, VKRestContext context)
        {
            switch (function)
            {
                case VKFunction.LoadUserInfo:
                    LoadUserInfo(function, context.userId, context.authToken);
                    break;
                case VKFunction.LoadFriends:
                    LoadFriends(function, context.userId, context.parameters);
                    break;
                case VKFunction.GetFriends:
                    GetFriends(function, context.parameters, context.cookie);
                    break;
                case VKFunction.GetMutual:
                    GetMutual(function, context.userId, context.authToken, context.parameters, context.cookie);
                    break;
                case VKFunction.UsersSearch:
                    UsersSearch(function, context.authToken, context.parameters);
                    break;
                case VKFunction.WallGet:
                    WallGet(function, context.authToken, context.parameters);
                    break;
                case VKFunction.WallGetComments:
                    WallGetComments(function, context.authToken, context.parameters, context.cookie);
                    break;
                case VKFunction.GroupsGetMembers:
                    GroupsGetMembers(function, context.authToken, context.parameters, context.cookie);
                    break;
                case VKFunction.GroupsGetById:
                    GroupsGetById(function, context.authToken, context.parameters, context.cookie);
                    break;
                case VKFunction.LikesGetList:
                    LikesGetList(function, context.authToken, context.parameters);
                    break;
                case VKFunction.UsersGet:
                    UsersGet(function, context.authToken, context.parameters);
                    break;
                default:
                    break;
            }
        }


        private void LoadUserInfo(VKFunction function, String userId, String authToken)
        {
            StringBuilder sb = new StringBuilder(api_url);
            sb.Append("/method/getProfiles");
            sb.Append('?');
            sb.Append("uid=").Append(userId).Append('&');
            sb.Append("access_token=").Append(authToken);

            makeRestCall(function, sb.ToString());
        }

        private void LoadFriends(VKFunction function, String userId, String parameters)
        {
            StringBuilder sb = new StringBuilder(api_url);
            sb.Append("/method/friends.get");
            sb.Append('?');
            sb.Append("user_id=").Append(userId).Append('&');
            sb.Append(parameters);
            sb.Append('&').Append("v=5.21");

            makeRestCall(function, sb.ToString());
        }

        // just like LoadFriends, but for any user, provided via parameters's [user_id] field
        private void GetFriends(VKFunction function, String parameters, String cookie)
        {
            StringBuilder sb = new StringBuilder(api_url);
            sb.Append("/method/friends.get");
            sb.Append('?');
            sb.Append(parameters);

            makeRestCall(function, sb.ToString(), cookie);
        }

        private void GetMutual(VKFunction function, String userId, String authToken, String parameters, String cookie)
        {
            StringBuilder sb = new StringBuilder(api_url);
            sb.Append("/method/friends.getMutual");
            sb.Append('?');
            sb.Append("source_uid=").Append(userId).Append('&');
            sb.Append("access_token=").Append(authToken).Append('&');
            sb.Append(parameters);
            sb.Append('&').Append("v=5.21");

            makeRestCall(function, sb.ToString(), cookie);
        }

        private void UsersSearch(VKFunction function, String authToken, String parameters)
        {
            StringBuilder sb = new StringBuilder(api_url);
            sb.Append("/method/users.search");
            sb.Append('?');
            sb.Append("access_token=").Append(authToken).Append('&');
            sb.Append(parameters);
            sb.Append('&').Append("v=5.14");

            makeRestCall(function, sb.ToString());
        }

        // WALL GET v 5.14
        private void WallGet(VKFunction function, String authToken, String parameters)
        {
            StringBuilder sb = new StringBuilder(api_url);
            sb.Append("/method/wall.get");
            sb.Append('?');
            sb.Append("access_token=").Append(authToken).Append('&');
            sb.Append(parameters);
            sb.Append('&').Append("v=5.14");

            makeRestCall(function, sb.ToString());
        }

        // WALL GET Comments v 5.14
        private void WallGetComments(VKFunction function, String authToken, String parameters, String cookie)
        {
            StringBuilder sb = new StringBuilder(api_url);
            sb.Append("/method/wall.getComments");
            sb.Append('?');
            sb.Append("access_token=").Append(authToken).Append('&');
            sb.Append(parameters);
            sb.Append('&').Append("v=5.14");

            makeRestCall(function, sb.ToString(), cookie);
        }

        // Groups GET Members v 5.21
        private void GroupsGetMembers(VKFunction function, String authToken, String parameters, String cookie)
        {
            StringBuilder sb = new StringBuilder(api_url);
            sb.Append("/method/groups.getMembers");
            sb.Append('?');
            sb.Append("access_token=").Append(authToken).Append('&');
            sb.Append(parameters);
            sb.Append('&').Append("v=5.21");

            makeRestCall(function, sb.ToString(), cookie);
        }

        // Groups GET By ID v 5.21
        private void GroupsGetById(VKFunction function, String authToken, String parameters, String cookie)
        {
            StringBuilder sb = new StringBuilder(api_url);
            sb.Append("/method/groups.getById");
            sb.Append('?');
            sb.Append("access_token=").Append(authToken).Append('&');
            sb.Append(parameters);
            sb.Append('&').Append("v=5.21");

            makeRestCall(function, sb.ToString(), cookie);
        }

        // Likes GET List v 5.21 (get list of ids who has liked the item)
        private void LikesGetList(VKFunction function, String authToken, String parameters)
        {
            StringBuilder sb = new StringBuilder(api_url);
            sb.Append("/method/likes.getList");
            sb.Append('?');
            sb.Append("access_token=").Append(authToken).Append('&');
            sb.Append(parameters);
            sb.Append('&').Append("v=5.21");

            makeRestCall(function, sb.ToString());
        }

        // Users GET v 5.21 (get users info)
        private void UsersGet(VKFunction function, String authToken, String parameters)
        {
            StringBuilder sb = new StringBuilder(api_url);
            sb.Append("/method/users.get");
            sb.Append('?');
            sb.Append("access_token=").Append(authToken).Append('&');
            sb.Append(parameters);
            sb.Append('&').Append("v=5.21");

            makeRestCall(function, sb.ToString());
        }

        private void makeRestCall(VKFunction function, String uri, String cookie = null)
        {
            try
            {
                // Create URI 
                Uri address = new Uri(uri);
                Debug.WriteLine("REST call: " + address.ToString());

                // Create the web request 
                HttpWebRequest request = WebRequest.Create(address) as HttpWebRequest;

                // Set type to Get 
                request.Method = GET_METHOD;
                request.ContentType = CONTENT_TYPE;
                request.Accept = CONTENT_ACCEPT;

                // Get response 
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    Stream ResponseStream = null;
                    ResponseStream = response.GetResponseStream();
                    int responseCode = (int)response.StatusCode;
                    if (responseCode < 300)
                    {
                        string responseBody = ((new StreamReader(ResponseStream)).ReadToEnd());
                        string contentType = response.ContentType;
                        JObject o = JObject.Parse(responseBody);
                        if (o[RESPONSE_BODY] != null)
                        {
                            // Debug.WriteLine("REST response: " + o[RESPONSE_BODY].ToString());
                            // OK - notify listeners
                            if (OnData != null)
                            {
                                OnDataEventArgs args = new OnDataEventArgs(function, o, cookie);
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

                                OnErrorEventArgs args = new OnErrorEventArgs(function, code, error, o[ERROR_BODY].ToString());
                                OnError(this, args);
                            }
                        }
                    }
                }
            }
            catch (WebException Ex)
            {
                handleWebException(function, Ex);
            }

        }

        private void handleWebException(VKFunction function, WebException Ex)
        {
            if (Ex.Status == WebExceptionStatus.ProtocolError)
            {
                int StatusCode = (int)((HttpWebResponse)Ex.Response).StatusCode;
                Stream ResponseStream = null;
                ResponseStream = ((HttpWebResponse)Ex.Response).GetResponseStream();
                string responseText = (new StreamReader(ResponseStream)).ReadToEnd();

                if (StatusCode == 500)
                {
                    Debug.WriteLine("Error 500 - " + responseText);
                }
                else
                {
                    // Do Something for other status codes
                    Debug.WriteLine("Error " + StatusCode);
                }

                // Error - notify listeners
                if (OnError != null)
                {
                    StringBuilder errorsb = new StringBuilder();
                    errorsb.Append("StatusCode: ").Append(StatusCode).Append(',');
                    errorsb.Append("Error: \'").Append(responseText).Append("\'");
                    OnErrorEventArgs args = new OnErrorEventArgs(function, errorsb.ToString());
                    OnError(this, args);
                }
            }
            else
            {
                throw (Ex); // Or check for other WebExceptionStatus
            }
        }
    }
}
