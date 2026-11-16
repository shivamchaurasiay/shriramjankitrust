using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Data;
using System.Threading.Tasks;
using RamJankiNGO.Data;
using RamJankiNGO.Models;
using RamJankiNGO.Services;

namespace RamJankiNGO.Controllers
{
    public class MemberPortalController : Controller
    {
        private readonly Datalayer _datalayer;
        private readonly IEmailService _emailService;

        public MemberPortalController(Datalayer datalayer, IEmailService emailService)
        {
            _datalayer = datalayer;
            _emailService = emailService;
        }

        private bool IsMemberLoggedIn()
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("MemberUserId"))) return true;
            var memberToken = HttpContext.Request.Cookies[AuthTokenHelper.MemberCookieName];
            if (AuthTokenHelper.ValidateMemberCookie(memberToken, out string mUserId, out string mId, out string mName, out string mCode))
            {
                HttpContext.Session.SetString("MemberUserId", mUserId);
                HttpContext.Session.SetString("MemberId", mId);
                HttpContext.Session.SetString("MemberName", mName);
                HttpContext.Session.SetString("MemberCode", mCode);
                return true;
            }
            return false;
        }

        private int GetMemberUserId()
        {
            return Convert.ToInt32(HttpContext.Session.GetString("MemberUserId") ?? "0");
        }

        private int GetMemberId()
        {
            return Convert.ToInt32(HttpContext.Session.GetString("MemberId") ?? "0");
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (IsMemberLoggedIn())
            {
                return RedirectToAction("Dashboard");
            }
            return View(new LoginDto());
        }

        [HttpPost]
        public IActionResult Login(LoginDto login)
        {
            if (string.IsNullOrWhiteSpace(login.Mobile) || string.IsNullOrWhiteSpace(login.Password))
            {
                ViewBag.ErrorMessage = "Please enter Mobile Number / Email / Member ID and Password.";
                return View(login);
            }

            DataTable dt = _datalayer.GetUserByUsername(login.Mobile.Trim());
            if (dt != null && dt.Rows.Count > 0)
            {
                var userRow = dt.Rows[0];
                int roleId = Convert.ToInt32(userRow["RoleId"]);

                // STRICT CHECK: Admin (RoleId = 1) and Staff (RoleId = 2) CANNOT login to Member Portal!
                if (roleId == 1 || roleId == 2)
                {
                    ViewBag.ErrorMessage = "Invalid credentials. Admin users cannot sign in here; please use the Admin Portal.";
                    return View(login);
                }

                string storedPass = userRow["Password"].ToString()!;
                if (storedPass == login.Password)
                {
                    int userId = Convert.ToInt32(userRow["UserId"]);
                    DataTable dtMember = _datalayer.GetMemberByUserId(userId);

                    if (dtMember != null && dtMember.Rows.Count > 0)
                    {
                        var mRow = dtMember.Rows[0];
                        HttpContext.Session.SetString("MemberUserId", userId.ToString());
                        HttpContext.Session.SetString("MemberId", mRow["MemberId"].ToString()!);
                        HttpContext.Session.SetString("MemberName", mRow["FullName"].ToString()!);
                        HttpContext.Session.SetString("MemberCode", mRow["MemberCode"].ToString()!);

                        // Set 1-Week Persistent Secure Auth Cookie for Member
                        AuthTokenHelper.SetMemberCookie(Response, userId.ToString(), mRow["MemberId"].ToString()!, mRow["FullName"].ToString()!, mRow["MemberCode"].ToString()!);

                        TempData["AuthSuccess"] = $"Welcome back, {mRow["FullName"]}!";
                        return RedirectToAction("Dashboard");
                    }
                    else
                    {
                        string mobile = userRow["Mobile"].ToString()!;
                        var dtDup = _datalayer.CheckMemberDuplicate(mobile);
                        if (dtDup != null && dtDup.Rows.Count > 0)
                        {
                            var mRow = dtDup.Rows[0];
                            HttpContext.Session.SetString("MemberUserId", userId.ToString());
                            HttpContext.Session.SetString("MemberId", mRow["MemberId"].ToString()!);
                            HttpContext.Session.SetString("MemberName", mRow["FullName"].ToString()!);
                            HttpContext.Session.SetString("MemberCode", mRow["MemberCode"].ToString()!);

                            // Set 1-Week Persistent Secure Auth Cookie for Member
                            AuthTokenHelper.SetMemberCookie(Response, userId.ToString(), mRow["MemberId"].ToString()!, mRow["FullName"].ToString()!, mRow["MemberCode"].ToString()!);

                            TempData["AuthSuccess"] = $"Welcome back, {mRow["FullName"]}!";
                            return RedirectToAction("Dashboard");
                        }

                        ViewBag.ErrorMessage = "Member profile not found. Please register as a member.";
                        return View(login);
                    }
                }
            }

            ViewBag.ErrorMessage = "Invalid credentials. Please verify your Mobile / Email / Member ID and Password.";
            return View(login);
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (IsMemberLoggedIn())
            {
                return RedirectToAction("Dashboard");
            }
            return View(new RegisterDto { State = "Uttar Pradesh" });
        }

        [HttpPost]
        public IActionResult Register(RegisterDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(model.Password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#^()_+\-=\[\]{}|;:,.<>\/]).{8,50}$"))
            {
                ViewBag.ErrorMessage = "Password must be at least 8 characters long and include at least one uppercase letter (A-Z), one lowercase letter (a-z), one numeric digit (0-9), and one special character (e.g. @, #, $, !).";
                return View(model);
            }

            var result = _datalayer.RegisterMember(model);
            if (!result.success)
            {
                ViewBag.ErrorMessage = result.message;
                return View(model);
            }

            // Auto-login the new member
            DataTable dtUser = _datalayer.GetUserByUsername(model.Mobile);
            if (dtUser != null && dtUser.Rows.Count > 0)
            {
                int userId = Convert.ToInt32(dtUser.Rows[0]["UserId"]);
                HttpContext.Session.SetString("MemberUserId", userId.ToString());
                HttpContext.Session.SetString("MemberId", result.memberId.ToString());
                HttpContext.Session.SetString("MemberName", model.FullName);
                HttpContext.Session.SetString("MemberCode", result.memberCode);

                TempData["AuthSuccess"] = $"Namaste {model.FullName}! Registration successful. Your Member ID is {result.memberCode}.";
                return RedirectToAction("Dashboard");
            }

            TempData["AuthSuccess"] = $"Registration successful! Your Member ID is {result.memberCode}. Please sign in.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            if (IsMemberLoggedIn()) return RedirectToAction("Dashboard");
            return View(new ForgotPasswordRequestDto());
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequestDto model)
        {
            if (!ModelState.IsValid) return View(model);

            string email = model.Email.Trim();
            DataTable dt = _datalayer.GetUserByUsername(email);
            if (dt == null || dt.Rows.Count == 0)
            {
                ViewBag.ErrorMessage = "Yeh Email database me register nahi mila. Kripya apna registered email enter karein.";
                return View(model);
            }

            string userName = dt.Rows[0]["UserName"]?.ToString() ?? "Member";
            string otp = Random.Shared.Next(100000, 999999).ToString();

            // Save OTP with 2 minutes expiry in database
            _datalayer.SavePasswordResetOtp(email, otp);

            // Send Email OTP via Gmail SMTP
            _ = _emailService.SendPasswordResetOtpEmailAsync(email, userName, otp);

            TempData["ResetEmail"] = email;
            TempData["AuthSuccess"] = "Aapke email par 6-digit OTP bhej diya gaya hai. Yeh OTP 2 minute ke liye valid hai.";
            return RedirectToAction("ResetPassword", new { email = email });
        }

        [HttpGet]
        public IActionResult ResetPassword(string? email)
        {
            if (IsMemberLoggedIn()) return RedirectToAction("Dashboard");

            string targetEmail = email ?? TempData["ResetEmail"]?.ToString() ?? "";
            if (string.IsNullOrEmpty(targetEmail))
            {
                return RedirectToAction("ForgotPassword");
            }

            var model = new ResetPasswordWithOtpDto { Email = targetEmail };
            return View(model);
        }

        [HttpPost]
        public IActionResult ResetPassword(ResetPasswordWithOtpDto model)
        {
            if (!ModelState.IsValid) return View(model);

            var validation = _datalayer.ValidatePasswordResetOtp(model.Email, model.Otp.Trim());
            if (!validation.isValid)
            {
                ViewBag.ErrorMessage = validation.message;
                return View(model);
            }

            bool success = _datalayer.ResetUserPasswordWithOtp(model.Email, model.Otp.Trim(), model.NewPassword);
            if (success)
            {
                TempData["AuthSuccess"] = "Aapka password safaltapoorvak badal diya gaya hai! Naye password se login karein.";
                return RedirectToAction("Login");
            }

            ViewBag.ErrorMessage = "Password update nahi ho saka. Kripya dobara koshish karein.";
            return View(model);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove("MemberUserId");
            HttpContext.Session.Remove("MemberId");
            HttpContext.Session.Remove("MemberName");
            HttpContext.Session.Remove("MemberCode");
            AuthTokenHelper.ClearMemberCookie(Response);
            TempData["AuthSuccess"] = "You have been logged out successfully.";
            return RedirectToAction("Login");
        }

        public IActionResult Dashboard()
        {
            if (!IsMemberLoggedIn()) return RedirectToAction("Login");

            int memberId = GetMemberId();
            DataTable dtMember = _datalayer.GetMemberById(memberId);
            DataTable dtDonations = _datalayer.GetDonationsByMemberId(memberId);

            ViewBag.Member = dtMember != null && dtMember.Rows.Count > 0 ? dtMember.Rows[0] : null;
            ViewBag.Donations = dtDonations;
            return View();
        }

        [HttpGet]
        public IActionResult Profile()
        {
            if (!IsMemberLoggedIn()) return RedirectToAction("Login");

            int memberId = GetMemberId();
            DataTable dt = _datalayer.GetMemberById(memberId);
            MemberDto model = new MemberDto();

            if (dt != null && dt.Rows.Count > 0)
            {
                var r = dt.Rows[0];
                model.MemberId = Convert.ToInt32(r["MemberId"]);
                model.MemberCode = r["MemberCode"].ToString()!;
                model.FullName = r["FullName"].ToString()!;
                model.Mobile = r["Mobile"].ToString()!;
                model.Email = r["Email"]?.ToString();
                model.Address = r["Address"]?.ToString();
                model.City = r["City"]?.ToString();
                model.State = r["State"]?.ToString() ?? "Uttar Pradesh";
                model.PinCode = r["PinCode"]?.ToString();
                model.MemberType = r["MemberType"]?.ToString() ?? "Regular";
                if (r["DateOfBirth"] != DBNull.Value) model.DateOfBirth = Convert.ToDateTime(r["DateOfBirth"]);
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult Profile(MemberDto model)
        {
            if (!IsMemberLoggedIn()) return RedirectToAction("Login");

            model.MemberId = GetMemberId();
            _datalayer.InsertUpdateMember(model);
            HttpContext.Session.SetString("MemberName", model.FullName);
            TempData["AuthSuccess"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }

        public IActionResult Donations()
        {
            if (!IsMemberLoggedIn()) return RedirectToAction("Login");

            int memberId = GetMemberId();
            DataTable dtDonations = _datalayer.GetDonationsByMemberId(memberId);
            return View(dtDonations);
        }

        public IActionResult Receipt(int id)
        {
            if (!IsMemberLoggedIn()) return RedirectToAction("Login");

            int memberId = GetMemberId();
            DataTable dt = _datalayer.GetDonationById(id);
            if (dt == null || dt.Rows.Count == 0) return NotFound();

            var row = dt.Rows[0];
            bool isOwner = false;
            if (row["MemberId"] != DBNull.Value && Convert.ToInt32(row["MemberId"]) == memberId)
            {
                isOwner = true;
            }
            else
            {
                DataTable dtMember = _datalayer.GetMemberById(memberId);
                if (dtMember != null && dtMember.Rows.Count > 0)
                {
                    string mPhone = dtMember.Rows[0]["Mobile"]?.ToString() ?? "";
                    string mEmail = dtMember.Rows[0]["Email"]?.ToString() ?? "";
                    string dPhone = row["DonorPhone"]?.ToString() ?? "";
                    string dEmail = row["DonorEmail"]?.ToString() ?? "";
                    if ((!string.IsNullOrEmpty(mPhone) && mPhone == dPhone) ||
                        (!string.IsNullOrEmpty(mEmail) && mEmail.Equals(dEmail, StringComparison.OrdinalIgnoreCase)))
                    {
                        isOwner = true;
                    }
                }
            }

            if (!isOwner) return Forbid();

            ViewBag.Settings = _datalayer.GetSettings();
            return View("~/Areas/Admin/Views/Donation/Receipt.cshtml", row);
        }
    }
}
