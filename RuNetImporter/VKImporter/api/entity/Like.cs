using System;

namespace rcsir.net.vk.importer.api.entity
{
    public class Like : IEntity
    {
        public Like()
        {
            Type = "";
            OwnerId = 0;
            ItemId = 0;
            Count = 0;
        }

        public string Type { get; set; } // post, comment etc.
        public long OwnerId { get; set; }
        public long ItemId { get; set; }
        public long Count { get; set; }

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
