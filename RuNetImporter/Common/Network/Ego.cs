using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace rcsir.net.common.Network
{
    public class Ego
    {
        public string ID { get; set; }
        public string Name { get; set; }

        public Ego(string ID, string Name)
        {
            this.ID = ID;
            this.Name = Name;
        }
    }
}
