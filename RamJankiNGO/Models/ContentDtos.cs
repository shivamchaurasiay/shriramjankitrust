using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace RamJankiNGO.Models
{
    public class BlogDto
    {
        public int BlogId { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        public string Title { get; set; } = string.Empty;

        public string? Category { get; set; }
        public string Author { get; set; } = "Admin";
        public string? ShortDescription { get; set; }
        public string? Description { get; set; }
        public string? ImagePath { get; set; }
        public IFormFile? ImageFile { get; set; }
        public int Status { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class Contactusdto
    {
        public int InquiryId { get; set; }

        [Required(ErrorMessage = "Your Name is required.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Your Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        public string Email { get; set; } = string.Empty;

        public string? Phone { get; set; }
        public string? Subject { get; set; }

        [Required(ErrorMessage = "Message is required.")]
        public string Message { get; set; } = string.Empty;

        public string Status { get; set; } = "New";
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Anti-bot & security fields
        public string? Honeypot { get; set; }
        public string? FormToken { get; set; }
    }

    public class VolunteerDto
    {
        public int VolunteerId { get; set; }

        [Required(ErrorMessage = "Your Name is required.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email Address is required.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone Number is required.")]
        public string Phone { get; set; } = string.Empty;

        public string? Address { get; set; }
        public string? Occupation { get; set; }
        public string? AreaOfInterest { get; set; }
        public string? Message { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Anti-bot & security fields
        public string? Honeypot { get; set; }
        public string? FormToken { get; set; }
    }

    public class SettingsDto
    {
        public string OrgName { get; set; } = "SHRI RAM JANKI MANDIR SEWA TRUST";
        public string TrustHindiName { get; set; } = "श्रीराम जानकी मंदिर सेवा ट्रस्ट";
        public string TrustLegalName { get; set; } = "SHRI RAM JANKI MANDIR SEWA TRUST";
        public string OrgTagline { get; set; } = "आयकर अधिनियम की धारा 80G और 12A में पंजीकृत संस्था";
        public string OrgEmail { get; set; } = "itdeskmindforgetechnology@gmail.com";
        public string OrgPhone { get; set; } = "+91 9918953452, +91 9250931408";
        public string OrgPhonePrimary { get; set; } = "+91 9918953452";
        public string OrgPhoneSecondary { get; set; } = "+91 9250931408";
        public string OrgAddress { get; set; } = "Karanpur, Station Road, Babhnan, Gonda - 271313, Near Ram Mandir, Ayodhya Dham, UP";
        public string OrgAddressHindi { get; set; } = "करनपुर, स्टेशन रोड, बभनान, गोण्डा - 271313 (निकट राम मंदिर, अयोध्या धाम, उ.प्र.)";
        public string DarpanId { get; set; } = "UP/2026/0956133";
        public string TrustRegNo { get; set; } = "15/B4/G20/2025";
        public string PanNumber { get; set; } = "ABLTS7164D";
        public string Section80G { get; set; } = "ABLTS7164DF20261";
        public string Section12A { get; set; } = "ABLTS7164DE20251";
        public string CSRNumber { get; set; } = "CSR00110689";
        public string GstNumber { get; set; } = "09ABLTS7164D1Z1";
        public string BankName { get; set; } = "INDIAN BANK";
        public string AccountName { get; set; } = "SHRI RAM JANKI MANDIR SEWA TRUST";
        public string AccountNumber { get; set; } = "8242427234";
        public string IfscCode { get; set; } = "IDIB000B510";
        public string UpiId { get; set; } = "8242427234@indianbk";
        public string ChairmanName { get; set; } = "पवन कुमार वर्मा (Pawan Kumar Verma)";
        public string ChairmanPhone { get; set; } = "+91 9918953452";
        public string ReceiptPrefix { get; set; } = "SJT";
        public string ReceiptNextNumber { get; set; } = "1001";
        public string MemberPrefix { get; set; } = "MEM";
        public string MemberNextNumber { get; set; } = "101";
        public string Currency { get; set; } = "INR";
        public string RazorpayKeyId { get; set; } = "";
        public string RazorpayKeySecret { get; set; } = "";
        public string EnableLiveGateway { get; set; } = "0";
    }
}
