using System;
using Smrf.AppLib;

namespace rcsir.net.common.Network
{
    public class Vertex
    {
        public string ID { get; set; }
        public string Name {get; set; }
        public string Type { get; set; }
        public string ToolTip { get; set; }
        public AttributesDictionary<String> Attributes { get; set; }

        public Vertex(string ID, string Name, string Type)
        {
            this.ID = ID;
            this.Name = Name;
            this.Type = Type;
            Attributes = new AttributesDictionary<String>();            
        }

        public Vertex(string ID, string Name, string Type, AttributesDictionary<String> Attributes)
        {
            this.ID = ID;
            this.Name = Name;            
            this.Type = Type;
            this.Attributes = Attributes;
        }

        public override int GetHashCode()
        {
            return (ID.GetHashCode());
        }
        public override bool Equals(object obj)
        {
            return Equals(obj as Vertex);
        }
        private bool Equals(Vertex obj)
        {
            return (obj != null &&                    
                    obj.ID.Equals(this.ID));
        }
    }
}
