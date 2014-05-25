using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Smrf.SocialNetworkLib;
using Smrf.AppLib;

namespace rcsir.net.common.Network
{
    public class Edge<T>
    {
        public Vertex<T> Vertex1 { get; set; }        
        public Vertex<T> Vertex2 { get; set; }
        public string Type { get; set; }
        public string Relationship { get; set; }
        public string Comment { get; set; }        
        public int Weight { get; set; }
        public string FeedOfOrigin { get; set; }
        public DateTime Timestamp { get; set; }
        public EdgeDirection Direction { get; set; }        


        public Edge(Vertex<T> Vertex1, Vertex<T> Vertex2, string Type,
                    string Relationship, string Comment, int Weight)
        {
            this.Vertex1 = Vertex1;
            this.Vertex2 = Vertex2;
            this.Type = Type;
            this.Relationship = Relationship;
            this.Comment = Comment;            
            this.Weight = Weight;
            this.FeedOfOrigin = "";
            this.Direction = EdgeDirection.Undirected;
        }

        public Edge(Vertex<T> Vertex1, Vertex<T> Vertex2, string Type,
                    string Relationship, string Comment, int Weight,
                    int Timestamp, EdgeDirection eDirection)
        {
            this.Vertex1 = Vertex1;
            this.Vertex2 = Vertex2;
            this.Type = Type;
            this.Relationship = Relationship;
            this.Comment = Comment;
            this.Weight = Weight;
            this.FeedOfOrigin = "";
            if(Timestamp>0)
                this.Timestamp = DateUtil.ConvertToDateTime(Timestamp);
            this.Direction = eDirection;
        }

        public Edge(Vertex<T> Vertex1, Vertex<T> Vertex2, string Type,
                    string Relationship, string Comment, int Weight,
                    int Timestamp, string FeedOfOrigin, EdgeDirection eDirection)
        {
            this.Vertex1 = Vertex1;
            this.Vertex2 = Vertex2;
            this.Type = Type;
            this.Relationship = Relationship;
            this.Comment = Comment;
            this.Weight = Weight;
            this.FeedOfOrigin = FeedOfOrigin;
            if (Timestamp > 0)
                this.Timestamp = DateUtil.ConvertToDateTime(Timestamp);
            this.Direction = eDirection;
        }



        public override int GetHashCode()
        {
            return (Vertex1.GetHashCode() + Vertex2.GetHashCode() + Type.GetHashCode() + Relationship.GetHashCode());
        }
        public override bool Equals(object obj)
        {
            return Equals(obj as Edge<T>);
        }
        private bool Equals(Edge<T> obj)
        {
            if (Direction == EdgeDirection.Undirected)
            {
                return (obj != null && 
                            (
                                (
                                obj.Vertex1.Equals(this.Vertex1) &&
                                obj.Vertex2.Equals(this.Vertex2)
                                ) ||
                                (
                                obj.Vertex1.Equals(this.Vertex2) &&
                                obj.Vertex2.Equals(this.Vertex1)
                                )
                            ) &&
                    obj.Type.Equals(this.Type) &&
                    obj.Relationship.Equals(this.Relationship));
            }
            else
            {
                return (obj != null &&
                        obj.Vertex1.Equals(this.Vertex1) &&
                        obj.Vertex2.Equals(this.Vertex2) &&
                        obj.Type.Equals(this.Type) &&
                        obj.Relationship.Equals(this.Relationship));
            }
        }



    }
}
