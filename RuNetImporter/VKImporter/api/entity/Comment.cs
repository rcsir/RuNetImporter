using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rcsir.net.vk.importer.api.entity
{
    public class Comment : IEntity
    {
        public Comment()
        {
            id = 0;
            post_id = 0;
            from_id = 0;
            date = "";
            reply_to_uid = 0;
            reply_to_cid = 0;
            likes = 0;
            attachments = 0;
            text = "";
        }

        public long id { get; set; }
        public long post_id { get; set; }
        public long from_id { get; set; }
        public string date { get; set; }
        public long reply_to_uid { get; set; } // user id
        public long reply_to_cid { get; set; } // comment id 
        public long likes { get; set; }
        public long attachments { get; set; }
        public string text { get; set; }

        public string Name()
        {
            return "comment";
        }

        public string FileHeader()
        {
            return String.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}",
                    "id", "post_id", "from", "date", "reply_to_user", "reply_to_comment", "likes", "attachments", "text");
        }

        public string ToFileLine()
        {
            return String.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}",
                    id, post_id, from_id, date, reply_to_uid, reply_to_cid, likes, attachments, text);
        }
    }
}
