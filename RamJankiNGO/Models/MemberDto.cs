using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace RamJankiNGO.Models
{
    public class MemberDto
    {
        public int MemberId { get; set; }
        public int? UserId { get; set; }
        public string MemberCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full Name is required.")]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Mobile Number is required.")]
        [Phone(ErrorMessage = "Invalid Mobile Number.")]
        public string Mobile { get; set; } = string.Empty;

        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PinCode { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string MemberType { get; set; } = "Regular";
        public int Status { get; set; } = 1;
        public string? ProfilePhoto { get; set; }
        public IFormFile? PhotoFile { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.Now;

        // Aggregates
        public decimal TotalDonated { get; set; }
        public int TotalDonationsCount { get; set; }
    }

    public class MemberImportResult
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int SkippedCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}
