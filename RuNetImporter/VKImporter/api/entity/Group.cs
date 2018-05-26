using System;

namespace rcsir.net.vk.importer.api.entity
{
    public class Group : IEntity
    {
        public Group()
        {
            Id = 0;
            name = "";
            ScreenName = "";
            IsClosed = "";
            Type = "";
            MembersCount = "";
            City = "";
            Country = "";
            Photo = "";
            Description = "";
            Status = "";
        }

        public long Id { get; set; }

        public string name { get; set; }
        public string ScreenName { get; set; }
        public string IsClosed { get; set; }
        public string Type { get; set; }
        public string MembersCount { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string Photo { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }

        public string Name()
        {
            return "group";
        }

        public string FileHeader()
        {
            return String.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}",
                "id", "name", "screen_name", "is_closed", "type", "members_count", "city", "country", "photo", "description", "status");
        }

        public string ToFileLine()
        {
            return String.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}",
                    Id, name, ScreenName, IsClosed, Type, MembersCount, City, Country, Photo, Description, Status);
        }
    }
}
