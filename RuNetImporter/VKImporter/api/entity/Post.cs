using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rcsir.net.vk.importer.api.entity
{
    public class Post : IEntity
    {
        public Post()
        {
            id = 0;
            owner_id = 0;
            from_id = 0;
            signer_id = 0;
            date = "";
            post_type = "";
            comments = 0;
            likes = 0;
            reposts = 0;
            attachments = 0;
            text = "";
        }

        public long id { get; set; }
        public long owner_id { get; set; }
        public long from_id { get; set; }
        public long signer_id { get; set; }
        public string date { get; set; }
        public string post_type { get; set; }
        public long comments { get; set; }
        public long likes { get; set; }
        public long reposts { get; set; }
        public long attachments { get; set; }
        public string text { get; set; }
        
        public string Name()
        {
            return "post";
        }

        public string FileHeader()
        {
            return String.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}",
                    "id", "owner", "from", "signer", "date", "post_type", "comments", "likes", "reposts", "attachments", "text");
        }

        public string ToFileLine()
        {
            return String.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t\"{10}\"",
                    id, owner_id, from_id, signer_id, date, post_type, comments, likes, reposts, attachments, text);
        }
    }
}
