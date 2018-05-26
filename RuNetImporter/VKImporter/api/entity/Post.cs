using System;

namespace rcsir.net.vk.importer.api.entity
{
    public class Post : IEntity
    {
        public Post()
        {
            Id = 0;
            OwnerId = 0;
            FromId = 0;
            SignerId = 0;
            Date = "";
            PostType = "";
            Comments = 0;
            Likes = 0;
            Reposts = 0;
            Attachments = 0;
            Text = "";
        }

        public long Id { get; set; }
        public long OwnerId { get; set; }
        public long FromId { get; set; }
        public long SignerId { get; set; }
        public string Date { get; set; }
        public string PostType { get; set; }
        public long Comments { get; set; }
        public long Likes { get; set; }
        public long Reposts { get; set; }
        public long Attachments { get; set; }
        public string Text { get; set; }
        
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
                    Id, OwnerId, FromId, SignerId, Date, PostType, Comments, Likes, Reposts, Attachments, Text);
        }
    }

    public class PostCopyHistory : IEntity
    {
        public PostCopyHistory(long postId, Post post)
        {
            Postid = postId;
            Post = post;
        }

        public long Postid { get; private set; }
        public Post Post { get; private set; }

        public string Name()
        {
            return "post-copy-history";
        }

        public string FileHeader()
        {
            return String.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}",
                    "post_id", "id", "owner", "from", "signer", "date", "post_type", "comments", "likes", "reposts", "attachments", "text");
        }

        public string ToFileLine()
        {
            return String.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t\"{11}\"",
                    Postid, Post.Id, Post.OwnerId, Post.FromId, Post.SignerId, Post.Date, Post.PostType,
                    Post.Comments, Post.Likes, Post.Reposts, Post.Attachments, Post.Text);
        }
    }
}
