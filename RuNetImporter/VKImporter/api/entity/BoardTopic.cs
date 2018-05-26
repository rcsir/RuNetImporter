using System;

namespace rcsir.net.vk.importer.api.entity
{
    public class BoardTopic : IEntity
    {
        public BoardTopic()
        {
            Id = 0;
            Title = "";
            Created = "";
            CreatedBy = 0;
            Updated = "";
            UpdatedBy = 0;
            IsClosed = false;
            IsFixed = false;
            Comments = 0;
        }

        public long Id { get; set; }
        public string Title { get; set; }
        public string Created { get; set; }
        public long CreatedBy { get; set; }
        public string Updated { get; set; }
        public long UpdatedBy { get; set; }
        public bool IsClosed { get; set; }
        public bool IsFixed { get; set; }
        public long Comments { get; set; }

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
                Id, Title, Created, CreatedBy, Updated, UpdatedBy, IsClosed, IsFixed, Comments);
        }
    }
}
