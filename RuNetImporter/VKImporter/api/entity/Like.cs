using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rcsir.net.vk.importer.api.entity
{
    public class Like : IEntity
    {
        public Like()
        {
            type = "";
            owner_id = 0;
            item_id = 0;
        }

        public string type { get; set; } // post, comment etc.
        public long owner_id { get; set; }
        public long item_id { get; set; }

        public string Name()
        {
            throw new NotImplementedException();
        }

        public string FileHeader()
        {
            throw new NotImplementedException();
        }

        public string ToFileLine()
        {
            throw new NotImplementedException();
        }
    }
}
