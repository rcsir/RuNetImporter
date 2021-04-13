using Smrf.AppLib;
using rcsir.net.common.Utilities;
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

        internal JSONObject[] GetFriends()
        {
            return postRequests.MakeApiRequest("method=friends.get").Array; // fid=160539089447&fid=561967133371&fid=561692396161&
        }

        internal JSONObject GetEgoInfo()
        {
            return postRequests.MakeApiRequest("method=users.getCurrentUser");
        }

        internal JSONObject[] GetUsersInfo(string usersIds)
        {
            var response = postRequests.MakeApiRequest("fields=" + requiredFields + "&method=users.getInfo&uids=" + usersIds);
            return response != null ? response.Array : null;
        }

        internal JSONObject[] GetAreFriends(string uids1, string uids2)
        {
            return postRequests.MakeApiRequest("method=friends.areFriends&uids1=" + uids1 + "&uids2=" + uids2).Array;
        }

        internal JSONObject[] GetMutualFriends(string friendId)
        {
            return postRequests.MakeApiRequest("method=friends.getMutualFriends&target_id=" + friendId).Array;  //  &source_id=160539089447
        }

        private void getToken()
        {
            string postedData = parametersStorage.TokenParameters;
            // Valid response string, f.e.: "{\"token_type\":\"session\",\"refresh_token\":\"e19530afe4f7c094d20f966078e2d0a16896a5_561692396161_138785\",\"access_token\":\"63ipa.949evrsa14g4i039194d1f3bh3kd\"}"
            var token = postRequests.MakeRequest(postedData, false);
            parametersStorage.UpdateAuthTokens(token.Dictionary["access_token"].String, token.Dictionary["refresh_token"].String);
        }

    }
}
