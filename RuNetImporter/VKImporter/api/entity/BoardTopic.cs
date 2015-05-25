using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rcsir.net.vk.importer.api.entity
{
    public class BoardTopic : IEntity
    {
        public BoardTopic()
        {
            id = 0;
            title = "";
            created = "";
            created_by = 0;
            updated = "";
            updated_by = 0;
            is_closed = false;
            is_fixed = false;
            comments = 0;
        }

        public long id { get; set; }
        public string title { get; set; }
        public string created { get; set; }
        public long created_by { get; set; }
        public string updated { get; set; }
        public long updated_by { get; set; }
        public bool is_closed { get; set; }
        public bool is_fixed { get; set; }
        public long comments { get; set; }

        public string Name()
        {
            return "board-topic";
        }

        public string FileHeader()
        {
            return String.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}",
                "id", "title", "created", "created_by", "updated", "updated_by", "is_closed", "is_fixed", "comments");
        }

        public string ToFileLine()
        {
            return String.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}",
                id, title, created, created_by, updated, updated_by, is_closed, is_fixed, comments);
        }
    }
}
