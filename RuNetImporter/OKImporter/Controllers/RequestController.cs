using Newtonsoft.Json.Linq;
using rcsir.net.ok.importer.Api;
using rcsir.net.ok.importer.Storages;

namespace rcsir.net.ok.importer.Controllers
{
    class RequestController
    {
        private readonly RequestParametersStorage parametersStorage;
        private readonly Authorization auth;

        private readonly PostRequests postRequests;

        internal PostRequests RequestsHelper { get { return postRequests; } }

        private string requiredFields;
        internal string RequiredFields { set { requiredFields = parametersStorage.HiddenFields + value; } }

        internal string AuthUri { get { return parametersStorage.AuthUri; } }

        internal RequestController()
        {
            parametersStorage = new RequestParametersStorage();
            auth = new Authorization(parametersStorage);
            postRequests = new PostRequests(parametersStorage);
            requiredFields = parametersStorage.AllUserFields;
        }

        internal void DeleteCookies()
        {
            auth.DeleteCookies();
        }

        internal bool TryGetAccessToken(string stringUrl)
        {
            if (!auth.IsCodeValid(stringUrl))
                return false;
            getToken();
            return true;
        }

        internal JArray GetFriends()
        {
            return JArray.Parse(postRequests.MakeApiRequest("method=friends.get")); // fid=160539089447&fid=561967133371&fid=561692396161&
        }

        internal JObject GetEgoInfo()
        {
            return JObject.Parse(postRequests.MakeApiRequest("method=users.getCurrentUser"));
        }

        internal JArray GetUsersInfo(string usersIds)
        {
            var response = postRequests.MakeApiRequest("fields=" + requiredFields + "&method=users.getInfo&uids=" + usersIds);
            return response != null ? JArray.Parse(response) : null;
        }

        internal JArray GetAreFriends(string uids1, string uids2)
        {
            return JArray.Parse(postRequests.MakeApiRequest("method=friends.areFriends&uids1=" + uids1 + "&uids2=" + uids2));
        }

        internal JArray GetMutualFriends(string friendId)
        {
            return JArray.Parse(postRequests.MakeApiRequest("method=friends.getMutualFriends&target_id=" + friendId));  //  &source_id=160539089447
        }

        private void getToken()
        {
            string postedData = parametersStorage.TokenParameters;
            // Valid response string, f.e.: "{\"token_type\":\"session\",\"refresh_token\":\"e19530afe4f7c094d20f966078e2d0a16896a5_561692396161_138785\",\"access_token\":\"63ipa.949evrsa14g4i039194d1f3bh3kd\"}"
            string responseString = postRequests.MakeRequest(postedData, false);
            var token = JObject.Parse(responseString);
            parametersStorage.UpdateAuthTokens(token["access_token"], token["refresh_token"]);
        }

    }
}
