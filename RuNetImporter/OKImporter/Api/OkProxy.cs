using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace rcsir.net.ok.importer.Api
{
    class OkProxy
    {

        public string GetToken(string code)
        {
            string postedData = "client_id=" + Authorization.ClientId + "&grant_type=authorization_code&client_secret=" + PostRequests.client_secret +
                                "&code=" + code + "&redirect_uri=" + Authorization.RedirectUrl + "&type=user_agent";
            return PostRequests.MakeRequest(postedData, false);
        }

        public string GetFriends()
        {
            return PostRequests.MakeApiRequest("method=friends.get"); // fid=160539089447&fid=561967133371&fid=561692396161&
        }

        public string GetEgoInfo()
        {
            return PostRequests.MakeApiRequest("method=users.getCurrentUser");
        }

        public string GetUsersInfo(string usersIds)
        {
            return PostRequests.MakeApiRequest("fields=uid,name,first_name,last_name,age,gender,locale&method=users.getInfo&uids=" + usersIds);
        }

        public string GetAreFriends(string uids1, string uids2)
        {
            return PostRequests.MakeApiRequest("method=friends.areFriends&uids1=" + uids1 + "&uids2=" + uids2);
        }

        public string GetMutualFriends(string friendId)
        {
            return PostRequests.MakeApiRequest("method=friends.getMutualFriends&target_id=" + friendId);  //  &source_id=160539089447
        }
    }
}
