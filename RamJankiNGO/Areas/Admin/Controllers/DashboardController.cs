using Microsoft.AspNetCore.Mvc;
using System.Data;
using RamJankiNGO.Data;
using RamJankiNGO.Models;

namespace RamJankiNGO.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DashboardController : Controller
    {
        private readonly Datalayer _datalayer;

        public DashboardController(Datalayer datalayer)
        {
            _datalayer = datalayer;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "Admin" });
            }

            DashboardDto model = _datalayer.GetDashboardData();
            return View(model);
        }

        [HttpGet]
        public IActionResult Profile()
        {
            var sessionUserId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(sessionUserId) || !int.TryParse(sessionUserId, out int userId))
            {
                return RedirectToAction("Login", "Account", new { area = "Admin" });
            }

            DataTable dt = _datalayer.GetUserById(userId);
            UserProfileDto profile = new UserProfileDto();
            if (dt != null && dt.Rows.Count > 0)
            {
                var row = dt.Rows[0];
                profile.UserId = Convert.ToInt32(row["UserId"]);
                profile.UserName = row["UserName"].ToString() ?? "";
                profile.Email = row["Email"].ToString() ?? "";
                profile.MobileNu = row["Mobile"].ToString() ?? "";
            }
            return View(profile);
        }

        [HttpPost]
        public IActionResult Profile(UserProfileDto model)
        {
            try
            {
                _datalayer.UpdateUserDataById(model);
                HttpContext.Session.SetString("UserName", model.UserName);
                TempData["SuccessMessage"] = "Profile updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to update profile: " + ex.Message;
            }
            return RedirectToAction("Profile");
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            var sessionUserId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(sessionUserId) || !int.TryParse(sessionUserId, out int userId))
            {
                return RedirectToAction("Login", "Account", new { area = "Admin" });
            }

            DataTable dt = _datalayer.GetUserById(userId);
            UserProfileDto profile = new UserProfileDto();
            if (dt != null && dt.Rows.Count > 0)
            {
                profile.UserId = userId;
                profile.UserName = dt.Rows[0]["UserName"].ToString() ?? "";
            }
            return View(profile);
        }

        [HttpPost]
        public IActionResult ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            var sessionUserId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(sessionUserId))
            {
                return RedirectToAction("Login", "Account", new { area = "Admin" });
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "New passwords do not match or are empty!";
                return RedirectToAction("ChangePassword");
            }

            DataTable dt = _datalayer.ResetPassword(oldPassword, newPassword, sessionUserId);
            if (dt != null && dt.Rows.Count > 0 && Convert.ToInt32(dt.Rows[0]["Status"]) == 1)
            {
                TempData["SuccessMessage"] = "Password changed successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = dt?.Rows[0]["Message"]?.ToString() ?? "Incorrect old password!";
            }
            return RedirectToAction("ChangePassword");
        }
    }
}
