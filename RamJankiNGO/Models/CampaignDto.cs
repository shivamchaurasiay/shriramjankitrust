using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace RamJankiNGO.Models
{
    public class CampaignDto
    {
        public int CampaignId { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        public string Title { get; set; } = string.Empty;

        public string? Category { get; set; }
        public string? ShortDescription { get; set; }
        public string? Description { get; set; }

        [Range(0, 100000000, ErrorMessage = "Target Amount must be positive.")]
        public decimal TargetAmount { get; set; }

        public decimal RaisedAmount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? ImagePath { get; set; }
        public IFormFile? ImageFile { get; set; }
        public bool IsFeatured { get; set; }
        public int Status { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int ProgressPercent => TargetAmount > 0 ? (int)Math.Min(100, (RaisedAmount / TargetAmount) * 100) : 0;
    }
}
