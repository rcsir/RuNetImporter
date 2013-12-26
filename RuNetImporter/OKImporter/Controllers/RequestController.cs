using Newtonsoft.Json.Linq;
using rcsir.net.ok.importer.Api;

namespace rcsir.net.ok.importer.Controllers
{
    class RequestController
    {

        internal void GetToken()
        {
            string postedData = "client_id=" + Authorization.ClientId + "&grant_type=authorization_code&client_secret=" + PostRequests.client_secret +
                                "&code=" + Authorization.Code + "&redirect_uri=" + Authorization.RedirectUrl + "&type=user_agent";
// Valid response string, f.e.: "{\"token_type\":\"session\",\"refresh_token\":\"e19530afe4f7c094d20f966078e2d0a16896a5_561692396161_138785\",\"access_token\":\"63ipa.949evrsa14g4i039194d1f3bh3kd\"}"
            string responseString = PostRequests.MakeRequest(postedData, false);
            var token = JObject.Parse(responseString);
            PostRequests.AuthToken = token["access_token"].ToString();
        }

        internal JArray GetFriends()
        {
            return JArray.Parse(PostRequests.MakeApiRequest("method=friends.get")); // fid=160539089447&fid=561967133371&fid=561692396161&
        }

        internal JObject GetEgoInfo()
        {
            return JObject.Parse(PostRequests.MakeApiRequest("method=users.getCurrentUser"));
        }

        internal JArray GetUsersInfo(string usersIds)
        {
            var response = PostRequests.MakeApiRequest("fields=uid,name,first_name,last_name,age,gender,locale&method=users.getInfo&uids=" + usersIds);
            return response != null ? JArray.Parse(response) : null;  //   PostRequests.MakeApiRequest("fields=uid,name,first_name,last_name,age,gender,locale&method=users.getInfo&uids=" + usersIds);
        }

        internal JArray GetAreFriends(string uids1, string uids2)
        {
            return JArray.Parse(PostRequests.MakeApiRequest("method=friends.areFriends&uids1=" + uids1 + "&uids2=" + uids2));
        }

        public JArray GetMutualFriends(string friendId)
        {
            return JArray.Parse(PostRequests.MakeApiRequest("method=friends.getMutualFriends&target_id=" + friendId));  //  &source_id=160539089447
        }
    }
}
