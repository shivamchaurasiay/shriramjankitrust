using System;

namespace RamJankiNGO.Models
{
    public class GalleryMasterDto
    {
        public int GalleryMId { get; set; }
        public int GalleryId { get => GalleryMId; set => GalleryMId = value; }

        public string GalleryName { get; set; } = string.Empty;
        public string? Action { get; set; }
        public int Status { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
