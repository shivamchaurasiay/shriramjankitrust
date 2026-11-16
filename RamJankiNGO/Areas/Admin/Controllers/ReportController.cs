using Microsoft.AspNetCore.Mvc;
using System.Data;
using RamJankiNGO.Data;
using RamJankiNGO.Models;

namespace RamJankiNGO.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ReportController : Controller
    {
        private readonly Datalayer _datalayer;

        public ReportController(Datalayer datalayer)
        {
            _datalayer = datalayer;
        }

        private bool CheckAuth()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("UserId"));
        }

        public IActionResult Index(DonationReportFilterDto filter)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });

            DataTable dt = _datalayer.GetDonationsList(filter);
            ViewBag.Campaigns = _datalayer.GetCampaignsList();
            ViewBag.Filter = filter;
            return View(dt);
        }

        public IActionResult MemberReport(string? search)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });

            DataTable dt = _datalayer.GetMembersList(search);
            ViewBag.Search = search;
            return View(dt);
        }
    }
}
