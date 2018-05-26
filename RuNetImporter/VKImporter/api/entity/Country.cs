using FileHelpers;
using rcsir.net.vk.importer.Annotations;

namespace rcsir.net.vk.importer.api.entity
{
    [DelimitedRecord(",")]
    public class Country
    {
        public int Id;
        public string Title;

        [UsedImplicitly]
        private Country() :
            this(0,"")
        {
                
        }
        
        public Country(int id, string title)
        {
            Id = id;
            Title = title;
        }

        public override string ToString()
        {
            return Title;
        }
    }
}
