using System;
using FileHelpers;
using rcsir.net.vk.importer.Annotations;

namespace rcsir.net.vk.importer.api.entity
{
    [DelimitedRecord(",")]
    public class City
    {
        public int Id;
        public int RegionId;
        public string Title;
        public bool Important;
        public string RegionTitle;
        public string AreaTitle;

        [UsedImplicitly]
        private City() : this(0, "")
        {
            
        } 

        public City(int id, String title)
        {
            Id = id;
            RegionId = 0;
            Title = title;
            Important = false;
            RegionTitle = "";
            AreaTitle = "";
        }

        public City(int id, String title, bool important)
            : this(id, title)
        {
            Important = important;
        }

        public City(int id, String title, bool important, int regionId, string region)
            : this(id, title, important)
        {
            RegionId = regionId;
            RegionTitle = region;
        }

        public City(int id, String title, bool important, int regionId, string region, string area)
            : this(id, title, important, regionId, region)
        {
            AreaTitle = area;
        }

        public override string ToString()
        {
            return String.Format("{0}, {1}, {2}", Title, RegionTitle, AreaTitle);
        }
    }
}
