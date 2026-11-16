using Microsoft.AspNetCore.Mvc;
using RamJankiNGO.Data;
using RamJankiNGO.Models;

namespace RamJankiNGO.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SettingController : Controller
    {
        private readonly Datalayer _datalayer;

        public SettingController(Datalayer datalayer)
        {
            _datalayer = datalayer;
        }

        private bool CheckAuth()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("UserId"));
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });
            SettingsDto model = _datalayer.GetSettings();
            return View(model);
        }

        [HttpPost]
        public IActionResult Index(SettingsDto model)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });

            try
            {
                _datalayer.SaveSettings(model);
                TempData["SuccessMessage"] = "System settings updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to update settings: " + ex.Message;
            }

            return View(model);
        }
    }
}
