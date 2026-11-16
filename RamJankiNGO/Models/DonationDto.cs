using System;
using System.ComponentModel.DataAnnotations;

namespace RamJankiNGO.Models
{
    public class DonationDto
    {
        public int DonationId { get; set; }
        public string DonationCode { get; set; } = string.Empty;
        public string? ReceiptNumber { get; set; }
        public int? MemberId { get; set; }
        public string? MemberCode { get; set; }
        public int? CampaignId { get; set; }
        public string? CampaignTitle { get; set; }

        [Required(ErrorMessage = "Donor Name is required.")]
        public string DonorName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        public string? DonorEmail { get; set; }

        [Required(ErrorMessage = "Donor Mobile is required.")]
        public string DonorPhone { get; set; } = string.Empty;

        public string? DonorAddress { get; set; }
        public string? DonorPan { get; set; }

        [Required(ErrorMessage = "Donation Amount is required.")]
        [Range(1, 10000000, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        public string Currency { get; set; } = "INR";
        public string PaymentMode { get; set; } = "Online"; // Online, Cash, Cheque, Bank Transfer, UPI
        public string PaymentStatus { get; set; } = "Pending"; // Pending, Successful, Failed, Refunded
        public string? PaymentGatewayReference { get; set; }
        public string? GatewayOrderId { get; set; }
        public string? GatewayPaymentId { get; set; }
        public string? GatewaySignature { get; set; }
        public DateTime TransactionDate { get; set; } = DateTime.Now;
        public DateTime? PaidAt { get; set; }
        public string? Notes { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class DonationReportFilterDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? PaymentStatus { get; set; }
        public int? CampaignId { get; set; }
        public string? Search { get; set; }
        public string? ExportFormat { get; set; } // Excel or PDF
    }
}

    public class GatewayDonationRequestDto
    {
        public int? CampaignId { get; set; }
        public int? MemberId { get; set; }
        public decimal Amount { get; set; }
        public string DonorName { get; set; } = string.Empty;
        public string DonorPhone { get; set; } = string.Empty;
        public string? DonorEmail { get; set; }
        public string? DonorAddress { get; set; }
        public string? DonorPan { get; set; }
        public string RazorpayPaymentId { get; set; } = string.Empty;
        public string? PaymentMode { get; set; }
    }

    public class PaymentRecoveryRequestDto
    {
        public string? RazorpayPaymentId { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }
