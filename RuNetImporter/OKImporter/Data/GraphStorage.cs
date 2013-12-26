using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using rcsir.net.common.Network;
using Smrf.AppLib;

namespace rcsir.net.ok.importer.Data
{
    public class GraphStorage
    {
        private Vertex egoVertex;

        private readonly VertexCollection vertices = new VertexCollection();
        public VertexCollection Vertices { get { return vertices; } }

        private readonly EdgeCollection edges = new EdgeCollection();
        public EdgeCollection Edges { get { return edges; } }

        private readonly List<string> friendIds = new List<string>();
        public List<string> FriendIds { get { return friendIds; } }

        private bool includeEgo; // include ego vertex and edges, should be controled by UI
        public bool IncludeEgo { set { includeEgo = value;  } }

        public string EgoId { get { return egoVertex.ID; } }

        public void AddFriendId(string id)
        {
            friendIds.Add(id);
        }

        public void AddEgoVertexIfNeeded(JObject ego)
        {
            AttributesDictionary<String> attributes = createAttributes(ego);
            egoVertex = new Vertex(ego["uid"].ToString(), ego["name"].ToString(), "Ego", attributes);
            if (includeEgo)
                vertices.Add(egoVertex);
        }

        public void AddFriendVertex(JObject friend)
        {
            AttributesDictionary<String> attributes = createAttributes(friend);
            vertices.Add(new Vertex(friend["uid"].ToString(), friend["name"].ToString(), "Friend", attributes)); ;
        }

        public void AddEdge(string vertex1Id, string vertex2Id)
        {
            Vertex vertex1 = vertices.FirstOrDefault(x => x.ID == vertex1Id);
            Vertex vertex2 = vertices.FirstOrDefault(x => x.ID == vertex2Id);
            if (vertex1 != null && vertex2 != null)
                edges.Add(new Edge(vertex1, vertex2, "", "Friend", "", 1));
        }

        public void AddIncludeMeEdgesIfNeeded()
        {
            List<Vertex> friends = vertices.Where(x => x.Type == "Friend").ToList();
            foreach (Vertex friend in friends)
                edges.Add(new Edge(egoVertex, friend, "", "Friend", "", 1));
        }

        public void ClearEdges()
        {
            edges.Clear();
        }

        public void ClearVertices()
        {
            vertices.Clear();
        }

        private AttributesDictionary<String> createAttributes(JObject obj)
        {
            AttributesDictionary<String> attributes = new AttributesDictionary<String>();
            List<AttributeUtils.Attribute> keys = new List<AttributeUtils.Attribute>(attributes.Keys);
            foreach (AttributeUtils.Attribute key in keys) {
                string name = key.value;
                if (obj[name] != null) {
                    // assert it is null?
                    string value = obj[name].ToString();
                    attributes[key] = value;
                }
            }
            return attributes;
        }
    }
}
