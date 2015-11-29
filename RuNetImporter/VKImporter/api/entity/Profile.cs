using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rcsir.net.vk.importer.api.entity
{
    public class Profile : IEntity
    {
        public Profile()
        {
            id = 0;
            first_name = "";
            last_name = "";
            screen_name = "";
            deactivated = "";
            bdate = "";
            city = "";
            country = "";
            photo = "";
            sex = "";
            relation = "";
            education = "";
            status = "";
        }

        public long id { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string screen_name { get; set; }
        public string deactivated { get; set; }
        public string bdate { get; set; }
        public string city { get; set; }
        public string country { get; set; }
        public string photo { get; set; }
        public string sex { get; set; }
        public string relation { get; set; }
        public string education { get; set; }
        public string status { get; set; }

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
                id, first_name, last_name, screen_name, deactivated, bdate, city, country, photo, sex, relation, education, status);
        }
    }
}
