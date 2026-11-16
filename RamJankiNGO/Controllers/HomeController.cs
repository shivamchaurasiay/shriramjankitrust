using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using RamJankiNGO.Data;
using RamJankiNGO.Models;
using RamJankiNGO.Services;

namespace RamJankiNGO.Controllers
{
    public class HomeController : Controller
    {
        private readonly Datalayer _datalayer;
        private readonly IEmailService _emailService;

        public HomeController(Datalayer datalayer, IEmailService emailService)
        {
            _datalayer = datalayer;
            _emailService = emailService;
        }

        public IActionResult Index()
        {
            ViewBag.Campaigns = _datalayer.GetActiveCampaigns();
            ViewBag.Projects = _datalayer.GetProjectsList();
            ViewBag.Team = _datalayer.GetTeamList();
            ViewBag.Blogs = _datalayer.GetBlogsList();
            ViewBag.Stats = _datalayer.GetDashboardData();
            ViewBag.Settings = _datalayer.GetSettings();
            return View();
        }

        public IActionResult AboutUs()
        {
            ViewBag.Team = _datalayer.GetTeamList();
            ViewBag.Settings = _datalayer.GetSettings();
            return View();
        }

        public IActionResult Causes()
        {
            DataTable dt = _datalayer.GetActiveCampaigns();
            return View(dt);
        }

        [HttpGet]
        public IActionResult Donation(int? campaignId)
        {
            ViewBag.Campaigns = _datalayer.GetActiveCampaigns();
            ViewBag.SelectedCampaignId = campaignId;
            ViewBag.Settings = _datalayer.GetSettings();

            DonationDto model = new DonationDto
            {
                Amount = 1000,
                CampaignId = campaignId
            };

            // Autofill if member is logged in
            string? memberUserId = HttpContext.Session.GetString("MemberUserId");
            int memberId = Convert.ToInt32(HttpContext.Session.GetString("MemberId") ?? "0");
            if (memberId > 0)
            {
                DataTable dt = _datalayer.GetMemberById(memberId);
                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow r = dt.Rows[0];
                    model.MemberId = memberId;
                    model.DonorName = r["FullName"].ToString() ?? "";
                    model.DonorPhone = r["Mobile"].ToString() ?? "";
                    model.DonorEmail = r["Email"]?.ToString();
                    model.DonorAddress = r["Address"]?.ToString();
                    ViewBag.IsAutofilled = true;
                    ViewBag.MemberCode = r["MemberCode"]?.ToString();
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult GetCurrentMember()
        {
            int memberId = Convert.ToInt32(HttpContext.Session.GetString("MemberId") ?? "0");
            if (memberId > 0)
            {
                DataTable dt = _datalayer.GetMemberById(memberId);
                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow r = dt.Rows[0];
                    return Json(new
                    {
                        loggedIn = true,
                        memberId = memberId,
                        memberCode = r["MemberCode"]?.ToString() ?? "",
                        fullName = r["FullName"].ToString() ?? "",
                        mobile = r["Mobile"].ToString() ?? "",
                        email = r["Email"]?.ToString() ?? "",
                        address = r["Address"]?.ToString() ?? ""
                    });
                }
            }
            return Json(new { loggedIn = false });
        }

        [HttpPost]
        public IActionResult Donation(DonationDto model)
        {
            // Strict Payment Gateway Enforcement:
            // Direct submission without verified payment gateway is strictly rejected.
            TempData["ErrorMessage"] = "Direct submission without payment gateway is not permitted. Please complete payment through Razorpay.";
            return RedirectToAction("Donation", new { campaignId = model.CampaignId });
        }

        [HttpPost]
        public async  Task<IActionResult>  ConfirmGatewayDonation([FromBody] GatewayDonationRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RazorpayPaymentId))
            {
                return BadRequest(new { success = false, message = "Payment Gateway transaction ID is missing. Receipt cannot be generated without payment." });
            }
            if (request.Amount <= 0)
            {
                return BadRequest(new { success = false, message = "Donation amount must be greater than zero." });
            }
            if (string.IsNullOrWhiteSpace(request.DonorName) || string.IsNullOrWhiteSpace(request.DonorPhone))
            {
                return BadRequest(new { success = false, message = "Donor Name and Mobile Number are required." });
            }

            try
            {
                // 1. Idempotency Check: if this payment ID was already processed, return existing receipt immediately
                DataTable dtEx = _datalayer.GetDonationByGatewayRef(request.RazorpayPaymentId.Trim());
                if (dtEx != null && dtEx.Rows.Count > 0)
                {
                    DataRow exRow = dtEx.Rows[0];
                    int exDonId = Convert.ToInt32(exRow["DonationId"]);
                    string exRecNo = exRow["ReceiptNumber"]?.ToString() ?? "";
                    string exStatus = exRow["PaymentStatus"]?.ToString() ?? "";

                    if (exStatus.Equals("Successful", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(exRecNo))
                    {
                        return Ok(new
                        {
                            success = true,
                            receiptNumber = exRecNo,
                            donationId = exDonId,
                            amount = Convert.ToDecimal(exRow["Amount"]),
                            redirectUrl = $"/Home/Receipt/{exDonId}",
                            alreadyProcessed = true
                        });
                    }
                    else
                    {
                        // Pending record exists, confirm it now!
                        _datalayer.ConfirmDonationSuccess(exDonId, request.RazorpayPaymentId.Trim(), out exRecNo);
                        return Ok(new
                        {
                            success = true,
                            receiptNumber = exRecNo,
                            donationId = exDonId,
                            amount = Convert.ToDecimal(exRow["Amount"]),
                            redirectUrl = $"/Home/Receipt/{exDonId}",
                            recovered = true
                        });
                    }
                }

                var model = new DonationDto
                {
                    MemberId = request.MemberId,
                    CampaignId = request.CampaignId,
                    DonorName = request.DonorName.Trim(),
                    DonorPhone = request.DonorPhone.Trim(),
                    DonorEmail = !string.IsNullOrWhiteSpace(request.DonorEmail) ? request.DonorEmail.Trim() : null,
                    DonorAddress = !string.IsNullOrWhiteSpace(request.DonorAddress) ? request.DonorAddress.Trim() : null,
                    DonorPan = !string.IsNullOrWhiteSpace(request.DonorPan) ? request.DonorPan.Trim().ToUpper() : null,
                    Amount = request.Amount,
                    PaymentMode = !string.IsNullOrWhiteSpace(request.PaymentMode) ? request.PaymentMode : "Razorpay Online",
                    PaymentGatewayReference = request.RazorpayPaymentId
                };

                // Create pending record
                int donId = _datalayer.CreateOnlineDonationPending(model);
                if (donId <= 0)
                {
                    return StatusCode(500, new { success = false, message = "Failed to initiate donation record." });
                }

                // Confirm ONLY with the verified Razorpay payment ID!
                string receiptNumber;
                bool confirmed = _datalayer.ConfirmDonationSuccess(donId, request.RazorpayPaymentId, out receiptNumber);
                if (!confirmed || string.IsNullOrEmpty(receiptNumber))
                {
                    return StatusCode(500, new { success = false, message = "Failed to confirm donation and issue receipt." });
                }

                // Fetch campaign title
                string campTitle = "General Seva Fund";
                if (request.CampaignId.HasValue && request.CampaignId.Value > 0)
                {
                    var dtCamp = _datalayer.GetCampaignById(request.CampaignId.Value);
                    if (dtCamp != null && dtCamp.Rows.Count > 0)
                    {
                        campTitle = dtCamp.Rows[0]["Title"]?.ToString() ?? campTitle;
                    }
                }

                // Send 80G Receipt Email & Admin Notification
                string receiptUrl = $"{Request.Scheme}://{Request.Host}/Home/Receipt/{donId}";
                if (!string.IsNullOrEmpty(model.DonorEmail))
                {
                    _ = _emailService.SendDonationReceiptEmailAsync(model.DonorEmail, model.DonorName, receiptNumber, model.Amount, campTitle, receiptUrl);
                }
                _ = _emailService.SendAdminDonationAlertAsync(model.DonorName, model.DonorPhone, model.Amount, receiptNumber, campTitle);

                return Ok(new
                {
                    success = true,
                    receiptNumber = receiptNumber,
                    donationId = donId,
                    amount = model.Amount,
                    redirectUrl = $"/Home/Receipt/{donId}"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error processing payment: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult RecoverPayment([FromBody] PaymentRecoveryRequestDto request)
        {
            if (request == null || (string.IsNullOrWhiteSpace(request.RazorpayPaymentId) && string.IsNullOrWhiteSpace(request.Phone)))
            {
                return BadRequest(new { success = false, message = "Please enter either a Payment ID (pay_...) or Mobile Number." });
            }

            try
            {
                // 1. Try search by Payment Gateway Reference
                if (!string.IsNullOrWhiteSpace(request.RazorpayPaymentId))
                {
                    DataTable dt = _datalayer.GetDonationByGatewayRef(request.RazorpayPaymentId.Trim());
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        DataRow r = dt.Rows[0];
                        int dId = Convert.ToInt32(r["DonationId"]);
                        string status = r["PaymentStatus"]?.ToString() ?? "";
                        string recNo = r["ReceiptNumber"]?.ToString() ?? "";

                        if (!status.Equals("Successful", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(recNo))
                        {
                            _datalayer.ConfirmDonationSuccess(dId, request.RazorpayPaymentId.Trim(), out recNo);
                        }

                        return Ok(new
                        {
                            success = true,
                            found = true,
                            receiptNumber = recNo,
                            donationId = dId,
                            amount = Convert.ToDecimal(r["Amount"]),
                            donorName = r["DonorName"]?.ToString(),
                            date = Convert.ToDateTime(r["CreatedAt"]).ToString("dd MMM yyyy, hh:mm tt"),
                            redirectUrl = $"/Home/Receipt/{dId}"
                        });
                    }
                }

                // 2. Try search by Donor Phone
                if (!string.IsNullOrWhiteSpace(request.Phone))
                {
                    DataTable dtPhone = _datalayer.GetDonationsByPhoneOrEmail(request.Phone.Trim(), request.Email?.Trim());
                    if (dtPhone != null && dtPhone.Rows.Count > 0)
                    {
                        var list = new List<object>();
                        foreach (DataRow r in dtPhone.Rows)
                        {
                            list.Add(new
                            {
                                donationId = Convert.ToInt32(r["DonationId"]),
                                receiptNumber = r["ReceiptNumber"]?.ToString() ?? "",
                                amount = Convert.ToDecimal(r["Amount"]),
                                donorName = r["DonorName"]?.ToString() ?? "",
                                status = r["PaymentStatus"]?.ToString() ?? "",
                                date = Convert.ToDateTime(r["CreatedAt"]).ToString("dd MMM yyyy, hh:mm tt"),
                                redirectUrl = $"/Home/Receipt/{r["DonationId"]}"
                            });
                        }
                        return Ok(new { success = true, found = true, results = list });
                    }
                }

                return Ok(new { success = false, found = false, message = "No matching donation record found with provided details. If money was debited, please contact trust helpline with your bank transaction UTR." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error querying payment: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult TrackPayment()
        {
            ViewBag.Settings = _datalayer.GetSettings();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SubmitInquiry(Contactusdto model)
        {
            // 1. Honeypot bot trap (hidden field that bots fill blindly)
            string? hp = model.Honeypot ?? Request.Form["inquiry_fax_hp"].ToString();
            if (!string.IsNullOrWhiteSpace(hp))
            {
                // Silently trap bot without DB insert or email notification
                TempData["SuccessMessage"] = "Thank you! Your inquiry has been received.";
                return RedirectToAction("Index");
            }

            // 2. Submission timing check (blocks instant automated bot submissions < 2s)
            string? token = model.FormToken ?? Request.Form["form_token"].ToString();
            if (!AntiSpamValidator.ValidateSubmissionTiming(token, out string timingError))
            {
                TempData["SuccessMessage"] = "Thank you! Your inquiry has been received.";
                return RedirectToAction("Index");
            }

            // 3. Sender Name validation (letters/spaces only, no URLs, no spam words)
            if (!AntiSpamValidator.IsValidName(model.Name, out string cleanName, out string nameError))
            {
                TempData["ErrorMessage"] = nameError;
                return RedirectToAction("Index");
            }

            // 4. Strict Email format validation (RFC-compliant regex + disposable domains blacklist)
            if (!AntiSpamValidator.IsValidEmail(model.Email, out string cleanEmail, out string emailError))
            {
                TempData["ErrorMessage"] = emailError;
                return RedirectToAction("Index");
            }

            // 5. Phone validation (10-digit Indian mobile number)
            if (!AntiSpamValidator.IsValidPhone(model.Phone, isRequired: false, out string cleanPhone, out string phoneError))
            {
                TempData["ErrorMessage"] = phoneError;
                return RedirectToAction("Index");
            }

            // 6. Message content validation (min length 8, max 2000, no link bombing)
            if (!AntiSpamValidator.IsValidMessage(model.Message, out string cleanMessage, out string msgError))
            {
                TempData["ErrorMessage"] = msgError;
                return RedirectToAction("Index");
            }

            // 7. Rate Limiting: 6-Hour limit per email / phone number
            if (_datalayer.HasRecentInquiry(cleanEmail, cleanPhone, hoursWindow: 6, out DateTime? lastInquiryTime))
            {
                var nextAllowed = (lastInquiryTime ?? DateTime.Now).AddHours(6);
                var remaining = nextAllowed - DateTime.Now;
                int hoursLeft = Math.Max(1, (int)Math.Ceiling(remaining.TotalHours));

                TempData["ErrorMessage"] = $"Aapne haal hi me inquiry darj ki hai. Duplicate entry rokkne ke liye ek email se 6 ghante me kewal 1 inquiry bhej sakte hain. Kripya {hoursLeft} ghante baad dobara koshish karein ya helpline (+91 9918953452) par call karein.";
                return RedirectToAction("Index");
            }

            try
            {
                model.Name = cleanName;
                model.Email = cleanEmail;
                model.Phone = cleanPhone;
                model.Message = cleanMessage;
                string cleanSubject = AntiSpamValidator.SanitizeSubject(model.Subject);

                bool ok = _datalayer.InsertContactInquiry(model);
                if (ok)
                {
                    _ = _emailService.SendAdminInquiryAlertAsync(model.Name, model.Email, model.Phone, model.Message, cleanSubject);
                    TempData["SuccessMessage"] = "Aapki inquiry safaltapoorvak darj ho gayi hai. Shriram Janki Trust team aapse shighra hi sampark karegi.";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error sending message: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        public IActionResult Receipt(int id)
        {
            DataTable dt = _datalayer.GetDonationById(id);
            if (dt == null || dt.Rows.Count == 0) return NotFound();

            ViewBag.Settings = _datalayer.GetSettings();
            return View("~/Areas/Admin/Views/Donation/Receipt.cshtml", dt.Rows[0]);
        }

        public IActionResult Event()
        {
            DataTable dt = _datalayer.GetBlogsList();
            ViewBag.Settings = _datalayer.GetSettings();
            return View(dt);
        }

        public IActionResult Blog()
        {
            DataTable dt = _datalayer.GetBlogsList();
            ViewBag.Settings = _datalayer.GetSettings();
            return View(dt);
        }

        public IActionResult BlogDetails(int id)
        {
            DataTable dt = _datalayer.GetBlogById(id);
            if (dt == null || dt.Rows.Count == 0)
            {
                return RedirectToAction("Blog");
            }

            ViewBag.RecentBlogs = _datalayer.GetBlogsList();
            ViewBag.ActiveCauses = _datalayer.GetActiveCampaigns();
            ViewBag.Settings = _datalayer.GetSettings();
            return View(dt.Rows[0]);
        }

        public IActionResult EventDetails(int id)
        {
            DataTable dt = _datalayer.GetBlogById(id);
            if (dt == null || dt.Rows.Count == 0)
            {
                return RedirectToAction("Event");
            }

            ViewBag.RecentBlogs = _datalayer.GetBlogsList();
            ViewBag.ActiveCauses = _datalayer.GetActiveCampaigns();
            ViewBag.Settings = _datalayer.GetSettings();
            return View("~/Views/Home/BlogDetails.cshtml", dt.Rows[0]);
        }

        public IActionResult Gallery()
        {
            ViewBag.Albums = _datalayer.GetGalleryMasterList();
            DataTable photos = _datalayer.GetGalleryDetailsList();
            return View(photos);
        }

        [HttpGet]
        public IActionResult Volunteer()
        {
            return View(new VolunteerDto());
        }

        [HttpPost]
        public IActionResult Volunteer(VolunteerDto model)
        {
            ViewBag.Settings = _datalayer.GetSettings();

            // 1. Honeypot bot trap
            string? hp = model.Honeypot ?? Request.Form["inquiry_fax_hp"].ToString();
            if (!string.IsNullOrWhiteSpace(hp))
            {
                TempData["SuccessMessage"] = "Thank you for volunteering with Shriram Janki Trust! Our team will get in touch with you shortly.";
                return RedirectToAction("Volunteer");
            }

            // 2. Submission timing check
            string? token = model.FormToken ?? Request.Form["form_token"].ToString();
            if (!AntiSpamValidator.ValidateSubmissionTiming(token, out string timingError))
            {
                TempData["SuccessMessage"] = "Thank you for volunteering with Shriram Janki Trust! Our team will get in touch with you shortly.";
                return RedirectToAction("Volunteer");
            }

            // 3. Name validation
            if (!AntiSpamValidator.IsValidName(model.Name, out string cleanName, out string nameError))
            {
                TempData["ErrorMessage"] = nameError;
                return View(model);
            }

            // 4. Strict Email validation
            if (!AntiSpamValidator.IsValidEmail(model.Email, out string cleanEmail, out string emailError))
            {
                TempData["ErrorMessage"] = emailError;
                return View(model);
            }

            // 5. Phone validation (strictly required for volunteers)
            if (!AntiSpamValidator.IsValidPhone(model.Phone, isRequired: true, out string cleanPhone, out string phoneError))
            {
                TempData["ErrorMessage"] = phoneError;
                return View(model);
            }

            // 6. Rate Limiting: 6-Hour limit for volunteer application
            if (_datalayer.HasRecentVolunteer(cleanEmail, cleanPhone, hoursWindow: 6, out DateTime? lastVolTime))
            {
                var nextAllowed = (lastVolTime ?? DateTime.Now).AddHours(6);
                var remaining = nextAllowed - DateTime.Now;
                int hoursLeft = Math.Max(1, (int)Math.Ceiling(remaining.TotalHours));

                TempData["ErrorMessage"] = $"Aapne haal hi me volunteer aavedan darj kiya hai. Kripya {hoursLeft} ghante baad dobara koshish karein ya seedhe trust helpline (+91 9918953452) par call karein.";
                return View(model);
            }

            try
            {
                model.Name = cleanName;
                model.Email = cleanEmail;
                model.Phone = cleanPhone;

                bool ok = _datalayer.InsertVolunteer(model);
                if (ok)
                {
                    _ = _emailService.SendAdminVolunteerAlertAsync(model.Name, model.Email, model.Phone, model.AreaOfInterest ?? "General Seva");
                    TempData["SuccessMessage"] = "Thank you for volunteering with Shriram Janki Trust! Our team will get in touch with you shortly.";
                    return RedirectToAction("Volunteer");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error submitting application: " + ex.Message;
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ContactUs()
        {
            ViewBag.Settings = _datalayer.GetSettings();
            return View(new Contactusdto());
        }

        [HttpPost]
        public IActionResult ContactUs(Contactusdto model)
        {
            ViewBag.Settings = _datalayer.GetSettings();

            // 1. Honeypot bot trap
            string? hp = model.Honeypot ?? Request.Form["inquiry_fax_hp"].ToString();
            if (!string.IsNullOrWhiteSpace(hp))
            {
                TempData["SuccessMessage"] = "Thank you! Your message has been sent to Shriram Janki Trust administration.";
                return RedirectToAction("ContactUs");
            }

            // 2. Submission timing check
            string? token = model.FormToken ?? Request.Form["form_token"].ToString();
            if (!AntiSpamValidator.ValidateSubmissionTiming(token, out string timingError))
            {
                TempData["SuccessMessage"] = "Thank you! Your message has been sent to Shriram Janki Trust administration.";
                return RedirectToAction("ContactUs");
            }

            // 3. Sender Name validation
            if (!AntiSpamValidator.IsValidName(model.Name, out string cleanName, out string nameError))
            {
                TempData["ErrorMessage"] = nameError;
                return View(model);
            }

            // 4. Strict Email validation
            if (!AntiSpamValidator.IsValidEmail(model.Email, out string cleanEmail, out string emailError))
            {
                TempData["ErrorMessage"] = emailError;
                return View(model);
            }

            // 5. Phone validation (10-digit Indian mobile number)
            if (!AntiSpamValidator.IsValidPhone(model.Phone, isRequired: false, out string cleanPhone, out string phoneError))
            {
                TempData["ErrorMessage"] = phoneError;
                return View(model);
            }

            // 6. Message content validation
            if (!AntiSpamValidator.IsValidMessage(model.Message, out string cleanMessage, out string msgError))
            {
                TempData["ErrorMessage"] = msgError;
                return View(model);
            }

            // 7. Rate Limiting: 6-Hour limit per email / phone number
            if (_datalayer.HasRecentInquiry(cleanEmail, cleanPhone, hoursWindow: 6, out DateTime? lastInquiryTime))
            {
                var nextAllowed = (lastInquiryTime ?? DateTime.Now).AddHours(6);
                var remaining = nextAllowed - DateTime.Now;
                int hoursLeft = Math.Max(1, (int)Math.Ceiling(remaining.TotalHours));

                TempData["ErrorMessage"] = $"Aapne haal hi me inquiry darj ki hai. Duplicate entry rokkne ke liye ek email se 6 ghante me kewal 1 inquiry bhej sakte hain. Kripya {hoursLeft} ghante baad dobara koshish karein ya helpline (+91 9918953452) par call karein.";
                return View(model);
            }

            try
            {
                model.Name = cleanName;
                model.Email = cleanEmail;
                model.Phone = cleanPhone;
                model.Message = cleanMessage;
                string cleanSubject = AntiSpamValidator.SanitizeSubject(model.Subject);

                bool ok = _datalayer.InsertContactInquiry(model);
                if (ok)
                {
                    _ = _emailService.SendAdminInquiryAlertAsync(model.Name, model.Email, model.Phone, model.Message, cleanSubject);
                    TempData["SuccessMessage"] = "Thank you! Your message has been sent to Shriram Janki Trust administration.";
                    return RedirectToAction("ContactUs");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error sending message: " + ex.Message;
            }

            return View(model);
        }

        public IActionResult Privacy()
        {
            ViewBag.Settings = _datalayer.GetSettings();
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
