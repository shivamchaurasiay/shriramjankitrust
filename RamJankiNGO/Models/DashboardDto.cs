using System.Data;

namespace RamJankiNGO.Models
{
    public class DashboardDto
    {
        public int TotalMembers { get; set; }
        public int ActiveMembers { get; set; }
        public decimal TotalDonationAmount { get; set; }
        public int TotalDonationCount { get; set; }
        public decimal CurrentMonthAmount { get; set; }
        public int PendingDonationsCount { get; set; }
        public int ActiveCampaigns { get; set; }
        public int NewInquiries { get; set; }

        public DataTable? RecentDonations { get; set; }
        public DataTable? RecentMembers { get; set; }
        public DataTable? CampaignSummary { get; set; }
    }
}
