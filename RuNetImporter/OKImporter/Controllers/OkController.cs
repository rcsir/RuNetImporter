using System.Linq;
using System.Threading;
using rcsir.net.common.Network;
using rcsir.net.ok.importer.Api;
using Smrf.AppLib;

namespace rcsir.net.ok.importer.Controllers
{
    public class OkController
    {
        private const int areFriendsPerStep = 100;
        private const int sleepTimeout = 100;

        private readonly GraphDataManager graphDataManager;
        private readonly RequestController requestController;

        public string EgoId { get { return graphDataManager.EgoId; } }
        public VertexCollection Vertices { get { return graphDataManager.Vertices; } }
        public EdgeCollection Edges { get { return graphDataManager.Edges; } }

        public DataHandler OnData;
        public ErrorHandler OnError;
        // define OnData delegate
        public delegate void DataHandler(object obj, OnDataEventArgs onDataArgs);
        // define OnError delegate
        public delegate void ErrorHandler(object obj, OnErrorEventArgs onErrorArgs);

        public OkController()
        {
            graphDataManager = new GraphDataManager();
            requestController = new RequestController();
        }

        public void CallOkFunction(OKFunction function)
        {
            switch (function) {
                case OKFunction.GetAccessToken:
                    GetAccessToken();
                    break;
                case OKFunction.LoadFriends:
                    LoadFriends();
                    break;
                case OKFunction.GetAreGraph:
                    GetAreGraph();
                    break;
                case OKFunction.GetMutualGraph:
                    GetMutualGraph();
                    break;
                case OKFunction.GenerateAreGraph:
                    GenerateGraph(true, false);
                    break;
                case OKFunction.GenerateMutualGraph:
                    GenerateGraph(true);
                    break;
                case OKFunction.GenerateGraph:
                    GenerateGraph();
                    break;
            }
            if (OnData != null) {
                OnDataEventArgs args = new OnDataEventArgs(function);
                OnData(this, args);
            }
        }

        private void GetAccessToken()
        {
            requestController.GetToken();
            LoadEgoInfo();
        }

        private void LoadFriends()
        {
            var friends = requestController.GetFriends(); // fid=160539089447&fid=561967133371&fid=561692396161&
            string friendUids = ""; // userId;
            foreach (var friend in friends) {
//  JObject friendDict = JObject.Parse(MakeRequest("method=friends.getMutualFriends&target_id=" + friend));  //  &source_id=160539089447
                friendUids += "," + friend;
                graphDataManager.AddFriendId(friend.ToString());
            }
            LoadFriendsInfo(friendUids);
        }

        private void GetAreGraph()
        {
            graphDataManager.ClearEdges();
            var pares = MathUtil.GeneratePares(graphDataManager.FriendIds.ToArray());
            string[] uidsArr1 = pares[0].Split(',');
            string[] uidsArr2 = pares[1].Split(',');
            for (var i = 0; i < uidsArr1.Length; i += areFriendsPerStep) {
                string uids1 = string.Join(",", uidsArr1.Skip(i).Take(areFriendsPerStep).ToArray());
                string uids2 = string.Join(",", uidsArr2.Skip(i).Take(areFriendsPerStep).ToArray());
                var friendsDict = requestController.GetAreFriends(uids1, uids2);
                graphDataManager.AddAreFriends(friendsDict);
                Thread.Sleep(sleepTimeout);
            }
        }

        private void GetMutualGraph()
        {
            graphDataManager.ClearEdges();
            foreach (var friendId in graphDataManager.FriendIds) {
                var friendsDict = requestController.GetMutualFriends(friendId); //  &source_id=160539089447
                graphDataManager.AddFriends(friendId, friendsDict);
                Thread.Sleep(sleepTimeout);
            }
/*
            if (friend.Integer > subFriend.Integer)
                addEdge(friend.String, subFriend.String);*/
        }

        private void GenerateGraph(bool isTest = false, bool isMutual = true)
        {
            LoadFriends();
            if (!isTest)
                isMutual = graphDataManager.FriendsCount > areFriendsPerStep / 2;
            if (isMutual)
                GetMutualGraph();
            else
                GetAreGraph();
            graphDataManager.AddMeIfNeeded();
        }

        private void LoadEgoInfo()
        {
            var ego = requestController.GetEgoInfo();
            if (OnData != null) {
                OnDataEventArgs args = new OnDataEventArgs(OKFunction.LoadUserInfo, ego);
                OnData(this, args);
            }
            graphDataManager.AddEgo(ego);
        }

        private void LoadFriendsInfo(string uids)
        {
            var friends = requestController.GetUsersInfo(uids);
            graphDataManager.AddFriends(friends);
        }
    }
}
