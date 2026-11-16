using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Data;
using System.IO;
using RamJankiNGO.Data;
using RamJankiNGO.Models;
using RamJankiNGO.Services;

namespace RamJankiNGO.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DonationController : Controller
    {
        private readonly Datalayer _datalayer;
        private readonly IEmailService _emailService;

        public DonationController(Datalayer datalayer, IEmailService emailService)
        {
            _datalayer = datalayer;
            _emailService = emailService;
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

        [HttpGet]
        public IActionResult AddOffline()
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });

            ViewBag.Campaigns = _datalayer.GetCampaignsList();
            ViewBag.Members = _datalayer.GetMembersList();
            return View(new DonationDto());
        }

        [HttpPost]
        public IActionResult AddOffline(DonationDto model)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });

            // Validate Donor Phone
            if (!AntiSpamValidator.IsValidPhone(model.DonorPhone, isRequired: true, out string cleanPhone, out string phoneErr))
            {
                ViewBag.Campaigns = _datalayer.GetCampaignsList();
                ViewBag.Members = _datalayer.GetMembersList();
                TempData["ErrorMessage"] = phoneErr;
                return View(model);
            }
            model.DonorPhone = cleanPhone;

            // Validate Donor Email if provided
            if (!string.IsNullOrWhiteSpace(model.DonorEmail))
            {
                if (!AntiSpamValidator.IsValidEmail(model.DonorEmail, out string cleanEmail, out string emailErr))
                {
                    ViewBag.Campaigns = _datalayer.GetCampaignsList();
                    ViewBag.Members = _datalayer.GetMembersList();
                    TempData["ErrorMessage"] = emailErr;
                    return View(model);
                }
                model.DonorEmail = cleanEmail;
            }

            model.CreatedBy = HttpContext.Session.GetString("UserName") ?? "Admin";

            try
            {
                string receiptNumber;
                int donId = _datalayer.RecordOfflineDonation(model, out receiptNumber);
                if (donId > 0)
                {
                    string campTitle = "General Seva Fund";
                    if (model.CampaignId.HasValue && model.CampaignId.Value > 0)
                    {
                        var campDt = _datalayer.GetCampaignById(model.CampaignId.Value);
                        if (campDt != null && campDt.Rows.Count > 0)
                        {
                            campTitle = campDt.Rows[0]["Title"]?.ToString() ?? "General Seva Fund";
                        }
                    }

                    string receiptUrl = $"{Request.Scheme}://{Request.Host}/Home/Receipt/{donId}";
                    _ = _emailService.SendDonationReceiptEmailAsync(model.DonorEmail ?? "", model.DonorName, receiptNumber, model.Amount, campTitle, receiptUrl);
                    _ = _emailService.SendAdminDonationAlertAsync(model.DonorName, model.DonorPhone, model.Amount, receiptNumber, campTitle);

                    TempData["SuccessMessage"] = $"Donation recorded successfully! Receipt Number: {receiptNumber}";
                    return RedirectToAction("Receipt", new { id = donId });
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to record donation.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error: " + ex.Message;
            }

            ViewBag.Campaigns = _datalayer.GetCampaignsList();
            ViewBag.Members = _datalayer.GetMembersList();
            return View(model);
        }

        public IActionResult Receipt(int id)
        {
            DataTable dt = _datalayer.GetDonationById(id);
            if (dt == null || dt.Rows.Count == 0) return NotFound();

            SettingsDto settings = _datalayer.GetSettings();
            ViewBag.Settings = settings;
            return View(dt.Rows[0]);
        }

        [HttpPost]
        public IActionResult Refund(int donationId, string reason)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["ErrorMessage"] = "Refund reason is mandatory.";
                return RedirectToAction("Index");
            }

            bool ok = _datalayer.RefundDonation(donationId, reason);
            if (ok)
            {
                TempData["SuccessMessage"] = "Donation marked as Refunded successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to refund donation.";
            }
            return RedirectToAction("Index");
        }

        public IActionResult ExportToExcel(DonationReportFilterDto filter)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });

            DataTable dt = _datalayer.GetDonationsList(filter);
            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Donations");
                ws.Cell(1, 1).Value = "Receipt No";
                ws.Cell(1, 2).Value = "Donation Code";
                ws.Cell(1, 3).Value = "Donor Name";
                ws.Cell(1, 4).Value = "Donor Mobile";
                ws.Cell(1, 5).Value = "Donor Email";
                ws.Cell(1, 6).Value = "Amount (INR)";
                ws.Cell(1, 7).Value = "Payment Mode";
                ws.Cell(1, 8).Value = "Status";
                ws.Cell(1, 9).Value = "Campaign";
                ws.Cell(1, 10).Value = "Date";

                int row = 2;
                foreach (DataRow r in dt.Rows)
                {
                    ws.Cell(row, 1).Value = r["ReceiptNumber"]?.ToString() ?? "";
                    ws.Cell(row, 2).Value = r["DonationCode"].ToString();
                    ws.Cell(row, 3).Value = r["DonorName"].ToString();
                    ws.Cell(row, 4).Value = r["DonorPhone"].ToString();
                    ws.Cell(row, 5).Value = r["DonorEmail"]?.ToString() ?? "";
                    ws.Cell(row, 6).Value = Convert.ToDecimal(r["Amount"]);
                    ws.Cell(row, 7).Value = r["PaymentMode"].ToString();
                    ws.Cell(row, 8).Value = r["PaymentStatus"].ToString();
                    ws.Cell(row, 9).Value = r["CampaignTitle"]?.ToString() ?? "General";
                    ws.Cell(row, 10).Value = Convert.ToDateTime(r["CreatedAt"]).ToString("dd/MM/yyyy HH:mm");
                    row++;
                }

                ws.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Donations_{DateTime.Now:yyyyMMdd}.xlsx");
                }
            }
        }
    }
}
