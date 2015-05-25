using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rcsir.net.vk.importer.api.entity
{
    public class Group : IEntity
    {
        public Group()
        {
            id = 0;
            name = "";
            screen_name = "";
            is_closed = "";
            type = "";
            members_count = "";
            city = "";
            country = "";
            photo = "";
            description = "";
            status = "";
        }

        public long id { get; set; }
        public string name { get; set; }
        public string screen_name { get; set; }
        public string is_closed { get; set; }
        public string type { get; set; }
        public string members_count { get; set; }
        public string city { get; set; }
        public string country { get; set; }
        public string photo { get; set; }
        public string description { get; set; }
        public string status { get; set; }

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
                    id, name, screen_name, is_closed, type, members_count, city, country, photo, description, status);
        }
    }
}
