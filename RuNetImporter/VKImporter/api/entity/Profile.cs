using System;

namespace rcsir.net.vk.importer.api.entity
{
    public class Profile : IEntity
    {
        public Profile()
        {
            Id = 0;
            FirstName = "";
            LastName = "";
            ScreenName = "";
            Deactivated = "";
            Bdate = "";
            City = "";
            Country = "";
            Photo = "";
            Sex = "";
            Relation = "";
            Education = "";
            Status = "";
        }

        public long Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ScreenName { get; set; }
        public string Deactivated { get; set; }
        public string Bdate { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string Photo { get; set; }
        public string Sex { get; set; }
        public string Relation { get; set; }
        public string Education { get; set; }
        public string Status { get; set; }

        public string Name()
        {
            return "profile";
        }

        public string FileHeader()
        {
            return String.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}",
                "id", "first_name", "last_name", "screen_name", "deactivated", "bdate", "city", "country", "photo", "sex", "relation", "education", "status");
        }

        public string ToFileLine()
        {
            return String.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}",
                Id, FirstName, LastName, ScreenName, Deactivated, Bdate, City, Country, Photo, Sex, Relation, Education, Status);
        }
    }
}
