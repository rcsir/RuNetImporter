using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace rcsir.net.common.Network
{
    public class VertexCollection<T> : HashSet<Vertex<T>>
    {
        public VertexCollection()
            : base()
        {
            
        }

        public VertexCollection(IEnumerable<Vertex<T>> oVertices)
            : base(oVertices)
        {

        }

        public Vertex<T> this[T ID]
        {
            get { return this.FirstOrDefault(x => x.ID.Equals(ID)); }
            set { this.RemoveWhere(x => x.ID.Equals(ID)); this.Add(value); }           
        }

        //public Vertex this[int index]
        //{            
        //    get { return this.ElementAt(index); }
        //    set { this.Remove(this.ElementAt(index)); this.Add(value); }
        //}        
    }
}
