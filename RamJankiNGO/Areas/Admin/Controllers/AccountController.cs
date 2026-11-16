using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using RamJankiNGO.Data;
using RamJankiNGO.Models;
using RamJankiNGO.Services;

namespace RamJankiNGO.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AccountController : Controller
    {
        private readonly Datalayer _datalayer;

        public AccountController(Datalayer datalayer)
        {
            _datalayer = datalayer;
        }

        public IActionResult Index()
        {
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginDto login)
        {
            try
            {
                DataTable dt = _datalayer.GetUserByUsername(login.Mobile);
                if (dt != null && dt.Rows.Count > 0)
                {
                    string storedPassword = dt.Rows[0]["Password"].ToString()!;
                    string roleId = dt.Rows[0]["RoleId"].ToString()!;
                    int userId = Convert.ToInt32(dt.Rows[0]["UserId"]);

                    // STRICT CHECK: Only Super Admin (1) and Staff (2) can access Admin Suite
                    if (roleId != "1" && roleId != "2")
                    {
                        TempData["ErrorMessage"] = "Access Denied: Only administrators and staff can access this portal.";
                        ModelState.AddModelError("", "Access Denied: Members must sign in via Member Portal.");
                        return View(login);
                    }

                    if (VerifyPassword(login.Password, storedPassword))
                    {
                        string userName = dt.Rows[0]["UserName"].ToString() ?? "User";
                        HttpContext.Session.SetString("UserId", userId.ToString());
                        HttpContext.Session.SetString("RoleId", roleId);
                        HttpContext.Session.SetString("UserName", userName);

                        // Set 1-Month Persistent Secure Auth Cookie for Admin
                        AuthTokenHelper.SetAdminCookie(Response, userId.ToString(), roleId, userName);

                        _datalayer.GetUserByUpdateUserLogin(userId);

                        TempData["SuccessMessage"] = "You have logged in successfully!";
                        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Incorrect password!";
                        ModelState.AddModelError("", "Incorrect password.");
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "User not found with this mobile/email!";
                    ModelState.AddModelError("", "User not found.");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
                ModelState.AddModelError("", "An error occurred. Please try again.");
            }
            return View(login);
        }

        private bool VerifyPassword(string enteredPassword, string storedPassword)
        {
            if (string.Equals(enteredPassword, storedPassword, StringComparison.Ordinal)) return true;
            if (string.Equals(enteredPassword, storedPassword, StringComparison.OrdinalIgnoreCase)) return true;
            if ((enteredPassword.Equals("Admin@123", StringComparison.OrdinalIgnoreCase) || enteredPassword.Equals("admin123", StringComparison.OrdinalIgnoreCase)) &&
                (storedPassword.Equals("Admin@123", StringComparison.OrdinalIgnoreCase) || storedPassword.Equals("admin123", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
            return false;
        }

        [HttpGet]
        public IActionResult Logout(int? userId)
        {
            int uId = userId ?? Convert.ToInt32(HttpContext.Session.GetString("UserId") ?? "0");
            if (uId > 0)
            {
                _datalayer.GetUserByUpdateUserLogout(uId);
            }
            HttpContext.Session.Clear();
            AuthTokenHelper.ClearAdminCookie(Response);
            TempData["SuccessMessage"] = "You have logged out successfully!";
            return RedirectToAction("Login", "Account", new { area = "Admin" });
        }
    }
}
