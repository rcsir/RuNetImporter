using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Smrf.AppLib;
using rcsir.net.common.NetworkAnalyzer;
using rcsir.net.common.Network;
using rcsir.net.ok.importer.GraphDataProvider;

namespace rcsir.net.ok.importer.NetworkAnalyzer
{
    public class OKNetworkAnalyzer : NetworkAnalyzerBase
    {
        public XmlDocument analyze(String userId, String authToken)
        {
            OKRestClient okRestClient = new OKRestClient();

            okRestClient.LoadUserInfo(userId, authToken);
            okRestClient.LoadFriends(userId);
            okRestClient.GetMutual(userId, authToken);

            VertexCollection vertices = okRestClient.GetVertices();
            EdgeCollection edges = okRestClient.GetEdges();

            AttributesDictionary<String> attributes = new AttributesDictionary<String>();
            return GenerateNetworkDocument(vertices, edges, attributes);
        }
    }
}
