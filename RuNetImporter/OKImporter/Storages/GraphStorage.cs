using System;
using System.Collections.Generic;
using System.Linq;
using rcsir.net.common.Network;
using Smrf.AppLib;
using rcsir.net.common.Utilities;

namespace rcsir.net.ok.importer.Storages
{
    class GraphStorage
    {
        private Vertex<String> egoVertex;
        private bool includeEgo; // include ego vertex and edges, should be controled by UI

        private readonly VertexCollection<String> vertices = new VertexCollection<String>();
        internal VertexCollection<String> Vertices { get { return vertices; } }

        private readonly EdgeCollection<String> edges = new EdgeCollection<String>();
        internal EdgeCollection<String> Edges { get { return edges; } }

        private readonly List<string> friendIds = new List<string>();
        internal List<string> FriendIds { get { return friendIds; } }

        internal void AddFriendId(string id)
        {
            friendIds.Add(id);
        }

        internal void MakeEgoVertex(JSONObject ego, AttributesDictionary<String> attributes)
        {
            egoVertex = makeVertex(ego, attributes, "Ego");
            includeEgo = true;
        }

        internal void AddFriendVertex(JSONObject friend, AttributesDictionary<String> attributes)
        {
            vertices.Add(makeVertex(friend, attributes, "Friend"));
        }

        internal void AddEdge(string vertex1Id, string vertex2Id)
        {
            Vertex<String> vertex1 = vertices.FirstOrDefault(x => x.ID == vertex1Id);
            Vertex<String> vertex2 = vertices.FirstOrDefault(x => x.ID == vertex2Id);
            if (vertex1 != null && vertex2 != null)
                edges.Add(new Edge<String>(vertex1, vertex2, "", "Friend", "", 1));
        }

        internal void AddIncludeMeEdgesIfNeeded()
        {
            if (!includeEgo)
                return;

            foreach (Vertex<String> friend in Vertices)
                edges.Add(new Edge<String>(egoVertex, friend, "", "Friend", "", 1));

            vertices.Add(egoVertex);
        }

        internal void ClearEdges()
        {
            edges.Clear();
        }

        internal void ClearVertices()
        {
            FriendIds.Clear();
            vertices.Clear();
        }

        private Vertex<String> makeVertex(JSONObject token, AttributesDictionary<String> attributes, string type)
        {
            return new Vertex<String>(token.Dictionary["uid"].String, token.Dictionary["name"].String, type, attributes);
        }
    }
}
