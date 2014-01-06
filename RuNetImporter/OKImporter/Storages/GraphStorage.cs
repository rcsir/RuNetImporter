using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json.Linq;
using rcsir.net.common.Network;
using Smrf.AppLib;

namespace rcsir.net.ok.importer.Storages
{
    public class GraphStorage : INotifyPropertyChanged
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

//        public string EgoId { get { return egoVertex.ID; } }

        public void AddFriendId(string id)
        {
            friendIds.Add(id);
        }
/*
        public void AddEgoVertexIfNeeded(JObject ego, AttributesDictionary<String> attributes)
        {
            egoVertex = new Vertex(ego["uid"].ToString(), ego["name"].ToString(), "Ego", attributes);
            if (includeEgo)
                vertices.Add(egoVertex);
        }
*/
        public void AddFriendVertex(JObject friend, AttributesDictionary<String> attributes)
        {
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
            if (!includeEgo)
                return;
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

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(info));
        }
    }
}
