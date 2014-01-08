using System.Linq;
using System.Threading;
using rcsir.net.ok.importer.Dialogs;
using rcsir.net.ok.importer.Events;
using Smrf.AppLib;

namespace rcsir.net.ok.importer.Controllers
{
    public class OkController
    {
        private const int areFriendsPerStep = 100;
        private const int sleepTimeout = 100;

        private readonly GraphDataManager graphDataManager;
        private readonly RequestController requestController;

        private string egoId;

        public OkController(ICommandEventDispatcher main)
        {
            graphDataManager = new GraphDataManager();
            requestController = new RequestController();
            requestController.DeleteCookies();
            updateMainForm(main);
            configureListeners(main);
        }

        internal void generateGraph(bool isTest = false, bool isMutual = true)
        {
            loadFriends();
            if (!isTest)
                isMutual = graphDataManager.FriendsCount > areFriendsPerStep / 2;
            if (isMutual)
                getMutualGraph();
            else
                getAreGraph();
            graphDataManager.AddMeIfNeeded();
        }

        private void updateMainForm(ICommandEventDispatcher main)
        {
            main.LoginDialog.AuthUri = requestController.AuthUri;
            main.DialogAttributes = graphDataManager.OkDialogAttributes;
        }

        private void configureListeners(ICommandEventDispatcher main)
        {
            main.CommandEventHandler += commandHandler;
            main.LoginDialog.CommandEventHandler += commandHandler;
            graphDataManager.OnData += main.OnData;
        }

        private void commandHandler(object sender, CommandEventArgs e)
        {
            switch (e.CommandName) {
                case CommandEventArgs.Commands.GetAccessToken:
                    getAccessToken(e.Parameter);
                    break;
                case CommandEventArgs.Commands.LoadFriends:
                    loadFriends();
                    break;
                case CommandEventArgs.Commands.GetGraphByAreFriends:
                    getAreGraph();
                    break;
                case CommandEventArgs.Commands.GetGraphByMutualFriends:
                    getMutualGraph();
                    break;
                case CommandEventArgs.Commands.GenerateGraphByAreFriends:
                    generateGraph(true, false);
                    break;
                case CommandEventArgs.Commands.GenerateGraphByMutualFriends:
                    generateGraph(true);
                    break;
                case CommandEventArgs.Commands.UpdateAllAttributes:
                    graphDataManager.UpdateAllAttributes(e.Rows);
                    makeEgoIfNeeded(e.IsMeIncluding);
                    break;
            }
        }

        private void getAccessToken(string stringUrl)
        {
            if (!requestController.TryGetAccessToken(stringUrl))
                return;
            loadEgoInfo();
        }

        private void loadFriends()
        {
            graphDataManager.ClearVertices();
            var friends = requestController.GetFriends(); // fid=160539089447&fid=561967133371&fid=561692396161&
            string friendUids = ""; // userId;
            foreach (var friend in friends) {
//  JObject friendDict = JObject.Parse(MakeRequest("method=friends.getMutualFriends&target_id=" + friend));  //  &source_id=160539089447
                friendUids += "," + friend;
                graphDataManager.AddFriendId(friend.ToString());
            }
            graphDataManager.ResumeFriendsList();
            loadFriendsInfo(friendUids);
        }

        private void getAreGraph()
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
            graphDataManager.ResumeGetGraph(false);
        }

        private void getMutualGraph()
        {
            graphDataManager.ClearEdges();
            foreach (var friendId in graphDataManager.FriendIds) {
                var friendsDict = requestController.GetMutualFriends(friendId); //  &source_id=160539089447
                graphDataManager.AddFriends(friendId, friendsDict);
                Thread.Sleep(sleepTimeout);
            }
            graphDataManager.ResumeGetGraph();
/*
            if (friend.Integer > subFriend.Integer)
                addEdge(friend.String, subFriend.String);*/
        }

        private void loadEgoInfo()
        {
            var ego = requestController.GetEgoInfo();
            egoId = ego["uid"].ToString();
            graphDataManager.SendEgo(ego);
       }

        private void loadFriendsInfo(string uids)
        {
            updateRequiredFields();
            var friends = requestController.GetUsersInfo(uids);
            graphDataManager.AddFriends(friends);
        }

        private void makeEgoIfNeeded(bool isMeIncluding)
        {
            if (!isMeIncluding)
                return;
            updateRequiredFields();
            var ego = requestController.GetUsersInfo(egoId);
            graphDataManager.MakeEgo(ego[0]);
        }

        private void updateRequiredFields()
        {
            requestController.RequiredFields = graphDataManager.CreateRequiredFieldsString();
        }
    }
}
