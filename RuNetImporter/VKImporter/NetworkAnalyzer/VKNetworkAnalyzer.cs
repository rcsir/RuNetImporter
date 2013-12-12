using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Smrf.AppLib;
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

            vkRestClient.LoadUserInfo(userId, authToken);
            vkRestClient.LoadFriends(userId);
            vkRestClient.GetMutual(userId, authToken);

            VertexCollection vertices = vkRestClient.GetVertices();
            EdgeCollection edges = vkRestClient.GetEdges();
            CreateIncludeMeEdges(edges, vertices);

            // create default attributes (values will be empty)
            AttributesDictionary<String> attributes = new AttributesDictionary<String>();

            return GenerateNetworkDocument(vertices, edges, attributes);
        }

        private void CreateIncludeMeEdges(EdgeCollection edges, VertexCollection vertices)
        {
            List<Vertex> friends = vertices.Where(x => x.Type == "Friend").ToList();
            Vertex ego = vertices.FirstOrDefault(x => x.Type == "Ego");

            if (ego != null)
            {
                foreach (Vertex oFriend in friends)
                {
                    edges.Add(new Edge(ego, oFriend, "", "Friend", "", 1));
                }
            }
        }

    }
}
