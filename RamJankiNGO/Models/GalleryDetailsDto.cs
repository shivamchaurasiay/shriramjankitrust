using Microsoft.AspNetCore.Http;
using System;

namespace RamJankiNGO.Models
{
    public class GalleryDetailsDto
    {
        public int GalleryDId { get; set; }
        public int GalleryDetailsId { get => GalleryDId; set => GalleryDId = value; }

        public int GalleryMId { get; set; }
        public int GalleryId { get => GalleryMId; set => GalleryMId = value; }

        public string? GalleryName { get; set; }
        public string? ImageTitle { get; set; }
        public string? Filename { get; set; }
        public string? ImagePath { get => Filename; set => Filename = value; }
        public IFormFile? FilePath { get; set; }
        public IFormFile? ImageFile { get => FilePath; set => FilePath = value; }
        public string? Action { get; set; }
        public int Status { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
