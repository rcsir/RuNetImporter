using System;

namespace rcsir.net.vk.importer.api.entity
{
    public class PhotoInfo : IEntity
    {
        public PhotoInfo()
        {
            Id = 0;
            AlbumId = 0;
            OwnerId = 0;
            Photo130 = "";
            Width = 0;
            Height = 0;
            Text = "";
            Date = "";
            Latit = "";
            Longit = "";
        }

        public long Id { get; set; }
        public long AlbumId { get; set; }
        public long OwnerId { get; set; }
        public string Photo130 { get; set; }
        public long Width { get; set; }
        public long Height { get; set; }
        public string Text { get; set; }
        public string Date { get; set; }
        public string Latit { get; set; }
        public string Longit { get; set; }

        public string Name()
        {
            return "photo";
        }

        public string FileHeader()
        {
            return String.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}",
                    "id", "album_id", "owner", "photo", "width", "height", "text", "date", "lat", "long");
        }

        public string ToFileLine()
        {
            return String.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}",
                    Id, AlbumId, OwnerId, Photo130, Width, Height, Text, Date, Latit, Longit);
        }
    }
}
