using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using rcsir.net.common.NetworkAnalyzer;
using rcsir.net.common.Network;
using rcsir.net.vk.importer.GraphDataProvider;

namespace rcsir.net.vk.importer.NetworkAnalyzer
{
    public class VKNetworkAnalyzer : NetworkAnalyzerBase
    {
        public XmlDocument analyze(String userId, String authToken)
        {
            VKRestClient vkRestClient = new VKRestClient();
            List<Vertex> vertices = new List<Vertex>();
            List<Edge> edges = new List<Edge>();

            vkRestClient.LoadUserInfo(userId, authToken);
            vkRestClient.LoadFriends(userId);
            vkRestClient.GetMutual(userId, authToken);

            return GenerateNetworkDocument(vertices, edges);
        }
    }
}
