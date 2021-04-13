using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace rcsir.net.common.Network
{
    public class EdgeCollection<T> : HashSet<Edge<T>>
    {
        public EdgeCollection()
            : base()
        {
            
        }

        public Edge<T> this[T Vertex1ID, T Vertex2ID]
        {
            get { return this.FirstOrDefault(x => x.Vertex1.ID.Equals(Vertex1ID) && x.Vertex2.ID.Equals(Vertex2ID)); }
            set { this.RemoveWhere(x => x.Vertex1.ID.Equals(Vertex1ID) && x.Vertex2.ID.Equals(Vertex2ID)); this.Add(value); }           
        }

        public Edge<T> this[Vertex<T> Vertex1, Vertex<T> Vertex2]
        {
            get { return this.FirstOrDefault(x => x.Vertex1.Equals(Vertex1) && x.Vertex2.Equals(Vertex2)); }
            set { this.RemoveWhere(x => x.Vertex1.Equals(Vertex1) && x.Vertex2.Equals(Vertex2)); this.Add(value); }
        }
        
    }
}
