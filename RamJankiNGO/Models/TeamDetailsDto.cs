using Microsoft.AspNetCore.Http;
using System;

namespace RamJankiNGO.Models
{
    public class TeamDetailsDto
    {
        public int TeamsId { get; set; }
        public int TeamId { get => TeamsId; set => TeamsId = value; }

        public string Designation { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Filename { get; set; }
        public string? ImagePath { get => Filename; set => Filename = value; }
        public IFormFile? FilePath { get; set; }
        public IFormFile? ImageFile { get => FilePath; set => FilePath = value; }
        public string? Action { get; set; }
        public int Status { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
