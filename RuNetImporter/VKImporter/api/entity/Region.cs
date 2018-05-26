using FileHelpers;
using rcsir.net.vk.importer.Annotations;

namespace rcsir.net.vk.importer.api.entity
{
    [DelimitedRecord(",")]
    public class Region
    {
        public int Id;
        public int CountryId;
        public string Title;

        [UsedImplicitly]
        private Region() :
            this(0,0,"")
        {
            
        }

        public Region(int id, int countryId, string title)
        {
            Id = id;
            CountryId = countryId;
            Title = title;
        }

        public override string ToString()
        {
            return Title;
        }
    }
}
