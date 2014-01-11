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
        private Vertex egoVertex;
        private bool includeEgo; // include ego vertex and edges, should be controled by UI

        private readonly VertexCollection vertices = new VertexCollection();
        internal VertexCollection Vertices { get { return vertices; } }

        private readonly EdgeCollection edges = new EdgeCollection();
        internal EdgeCollection Edges { get { return edges; } }

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
            Vertex vertex1 = vertices.FirstOrDefault(x => x.ID == vertex1Id);
            Vertex vertex2 = vertices.FirstOrDefault(x => x.ID == vertex2Id);
            if (vertex1 != null && vertex2 != null)
                edges.Add(new Edge(vertex1, vertex2, "", "Friend", "", 1));
        }

        internal void AddIncludeMeEdgesIfNeeded()
        {
            if (!includeEgo)
                return;

            foreach (Vertex friend in Vertices)
                edges.Add(new Edge(egoVertex, friend, "", "Friend", "", 1));

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

        private Vertex makeVertex(JSONObject token, AttributesDictionary<String> attributes, string type)
        {
            return new Vertex(token.Dictionary["uid"].String, token.Dictionary["name"].String, type, attributes);
        }
    }
}
