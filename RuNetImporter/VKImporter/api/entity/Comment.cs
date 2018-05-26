using System;

namespace rcsir.net.vk.importer.api.entity
{
    public class Comment : IEntity
    {
        public Comment()
        {
            Id = 0;
            PostId = 0;
            FromId = 0;
            Date = "";
            ReplyToUid = 0;
            ReplyToCid = 0;
            Likes = 0;
            Attachments = 0;
            Text = "";
        }

        public long Id { get; set; }
        public long PostId { get; set; }
        public long FromId { get; set; }
        public string Date { get; set; }
        public long ReplyToUid { get; set; } // user id
        public long ReplyToCid { get; set; } // comment id 
        public long Likes { get; set; }
        public long Attachments { get; set; }
        public string Text { get; set; }

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
                    Id, PostId, FromId, Date, ReplyToUid, ReplyToCid, Likes, Attachments, Text);
        }
    }
}
