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
        private OKRestClient _okRestClient;
        public OKRestClient okRestClient { set { _okRestClient = value; }}

        public XmlDocument analyze(bool isMutual = true)
        {
//            _okRestClient.LoadUserInfo(userId, authToken);
            _okRestClient.LoadFriends();
            if (isMutual)
                _okRestClient.GetMutualFriends();
            else
                _okRestClient.GetAreFriends();

            VertexCollection vertices = _okRestClient.GetVertices();
            EdgeCollection edges = _okRestClient.GetEdges();

            AttributesDictionary<String> attributes = new AttributesDictionary<String>();
            return GenerateNetworkDocument(vertices, edges, attributes);
        }
    }
}
