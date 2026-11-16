using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using RamJankiNGO.Data;
using RamJankiNGO.Models;

namespace RamJankiNGO.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class MasterController : Controller
    {
        private readonly Datalayer _datalayer;
        private readonly IWebHostEnvironment _env;

        public MasterController(Datalayer datalayer, IWebHostEnvironment env)
        {
            _datalayer = datalayer;
            _env = env;
        }

        private bool CheckAuth()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("UserId"));
        }

        #region Campaigns / Causes
        [HttpGet]
        public IActionResult AddCampaign(int? id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });
            CampaignDto model = new CampaignDto();
            if (id.HasValue && id.Value > 0)
            {
                DataTable dt = _datalayer.GetCampaignById(id.Value);
                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow r = dt.Rows[0];
                    model.CampaignId = Convert.ToInt32(r["CampaignId"]);
                    model.Title = r["Title"].ToString() ?? "";
                    model.Category = r["Category"]?.ToString();
                    model.ShortDescription = r["ShortDescription"]?.ToString();
                    model.Description = r["Description"]?.ToString();
                    model.TargetAmount = Convert.ToDecimal(r["TargetAmount"]);
                    model.RaisedAmount = Convert.ToDecimal(r["RaisedAmount"]);
                    if (r["StartDate"] != DBNull.Value) model.StartDate = Convert.ToDateTime(r["StartDate"]);
                    if (r["EndDate"] != DBNull.Value) model.EndDate = Convert.ToDateTime(r["EndDate"]);
                    model.ImagePath = r["ImagePath"]?.ToString();
                    model.IsFeatured = Convert.ToBoolean(r["IsFeatured"]);
                    model.Status = Convert.ToInt32(r["Status"]);
                }
            }
            return View(model);
        }

        [HttpPost]
        public IActionResult AddCampaign(CampaignDto model)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                string uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "campaigns");
                if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ImageFile.FileName);
                string filePath = Path.Combine(uploadFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    model.ImageFile.CopyTo(stream);
                }
                model.ImagePath = "/uploads/campaigns/" + fileName;
            }

            int res = _datalayer.InsertUpdateCampaign(model);
            if (res > 0)
            {
                TempData["SuccessMessage"] = model.CampaignId > 0 ? "Campaign updated successfully!" : "Campaign added successfully!";
                return RedirectToAction("CampaignList");
            }
            TempData["ErrorMessage"] = "Failed to save campaign.";
            return View(model);
        }

        public IActionResult CampaignList()
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });
            DataTable dt = _datalayer.GetCampaignsList();
            return View(dt);
        }

        public IActionResult DeleteCampaign(int id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });
            _datalayer.DeleteCampaign(id);
            TempData["SuccessMessage"] = "Campaign deleted successfully!";
            return RedirectToAction("CampaignList");
        }
        #endregion

        #region Blogs / News
        [HttpGet]
        public IActionResult AddBlogDetails(int? id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });
            BlogDetailsDto model = new BlogDetailsDto();
            if (id.HasValue && id.Value > 0)
            {
                DataTable dt = _datalayer.GetBlogById(id.Value);
                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow r = dt.Rows[0];
                    model.BlogId = Convert.ToInt32(r["BlogId"]);
                    model.BlogHeading = r["Title"]?.ToString() ?? "";
                    model.BlogTopic = r.Table.Columns.Contains("Category") ? (r["Category"]?.ToString() ?? "News") : "News";
                    model.BlogTittle = r.Table.Columns.Contains("ShortDescription") && !string.IsNullOrEmpty(r["ShortDescription"]?.ToString()) ? r["ShortDescription"].ToString()! : (r["Title"]?.ToString() ?? "");
                    model.BlogDescription = r["Description"]?.ToString() ?? "";
                    model.Filename = r["ImagePath"]?.ToString() ?? "";
                }
            }
            return View(model);
        }

        [HttpPost]
        public IActionResult AddBlogDetails(BlogDetailsDto model)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });

            string imgPath = model.Filename ?? "";
            if (model.FilePath != null && model.FilePath.Length > 0)
            {
                string uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "blogs");
                if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.FilePath.FileName);
                string filePath = Path.Combine(uploadFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    model.FilePath.CopyTo(stream);
                }
                imgPath = "/uploads/blogs/" + fileName;
            }

            BlogDto dto = new BlogDto
            {
                BlogId = model.BlogId,
                Title = model.BlogHeading ?? "",
                Category = model.BlogTopic ?? "News",
                Author = "Admin",
                ShortDescription = model.BlogTittle ?? "",
                Description = model.BlogDescription ?? "",
                ImagePath = imgPath,
                Status = 1
            };

            int res = _datalayer.InsertUpdateBlog(dto);
            if (res > 0)
            {
                TempData["SuccessMessage"] = model.BlogId > 0 ? "Blog updated successfully!" : "Blog added successfully!";
                return RedirectToAction("BlogDetailsList");
            }
            TempData["ErrorMessage"] = "Failed to save blog post.";
            return View(model);
        }

        public IActionResult BlogDetailsList()
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });
            DataTable dt = _datalayer.GetBlogsList();
            var list = new List<BlogDetailsDto>();
            if (dt != null)
            {
                foreach (DataRow r in dt.Rows)
                {
                    string img = r.Table.Columns.Contains("ImagePath") ? (r["ImagePath"]?.ToString() ?? "") : "";
                    list.Add(new BlogDetailsDto
                    {
                        BlogId = Convert.ToInt32(r["BlogId"]),
                        BlogTopic = r.Table.Columns.Contains("Category") ? (r["Category"]?.ToString() ?? "News") : "News",
                        BlogHeading = r["Title"]?.ToString() ?? "",
                        BlogTittle = r.Table.Columns.Contains("ShortDescription") && !string.IsNullOrEmpty(r["ShortDescription"]?.ToString()) ? r["ShortDescription"].ToString()! : (r["Title"]?.ToString() ?? ""),
                        BlogDescription = r.Table.Columns.Contains("Description") ? (r["Description"]?.ToString() ?? "") : "",
                        PostDate = r.Table.Columns.Contains("CreatedAt") && r["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(r["CreatedAt"]).ToString("dd-MMM-yyyy") : "",
                        Filename = img
                    });
                }
            }
            return View(list);
        }

        public IActionResult DeleteBlogDetails(int id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });
            _datalayer.DeleteBlog(id);
            TempData["SuccessMessage"] = "Blog deleted successfully!";
            return RedirectToAction("BlogDetailsList");
        }
        #endregion

        #region Our Team
        [HttpGet]
        public IActionResult AddOurTeamDetails(int? id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });
            TeamDetailsDto model = new TeamDetailsDto();
            if (id.HasValue && id.Value > 0)
            {
                DataTable dt = _datalayer.GetTeamById(id.Value);
                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow r = dt.Rows[0];
                    model.TeamsId = Convert.ToInt32(r["TeamId"]);
                    model.Name = r["Name"]?.ToString() ?? "";
                    model.Designation = r["Designation"]?.ToString() ?? "";
                    model.Description = r["Description"]?.ToString();
                    model.Filename = r["ImagePath"]?.ToString();
                    model.Status = r.Table.Columns.Contains("Status") && r["Status"] != DBNull.Value ? Convert.ToInt32(r["Status"]) : 1;
                }
            }
            return View(model);
        }

        [HttpPost]
        public IActionResult AddOurTeamDetails(TeamDetailsDto model)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });

            if (model.FilePath != null && model.FilePath.Length > 0)
            {
                string uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "team");
                if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.FilePath.FileName);
                string filePath = Path.Combine(uploadFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    model.FilePath.CopyTo(stream);
                }
                model.Filename = "/uploads/team/" + fileName;
            }

            int res = _datalayer.InsertUpdateTeam(model);
            if (res > 0)
            {
                TempData["SuccessMessage"] = model.TeamsId > 0 ? "Team member updated successfully!" : "Team member added successfully!";
                return RedirectToAction("OurTeamDetailsList");
            }
            TempData["ErrorMessage"] = "Failed to save team member.";
            return View(model);
        }

        public IActionResult OurTeamDetailsList()
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });
            DataTable dt = _datalayer.GetTeamList();
            var list = new List<TeamDetailsDto>();
            if (dt != null)
            {
                foreach (DataRow r in dt.Rows)
                {
                    string img = r.Table.Columns.Contains("ImagePath") ? (r["ImagePath"]?.ToString() ?? "") : "";
                    list.Add(new TeamDetailsDto
                    {
                        TeamsId = Convert.ToInt32(r["TeamId"]),
                        Name = r["Name"]?.ToString() ?? "",
                        Designation = r["Designation"]?.ToString() ?? "",
                        Description = r.Table.Columns.Contains("Description") ? (r["Description"]?.ToString() ?? "") : "",
                        Filename = img,
                        Status = r.Table.Columns.Contains("Status") && r["Status"] != DBNull.Value ? Convert.ToInt32(r["Status"]) : 1,
                        CreatedAt = r.Table.Columns.Contains("CreatedAt") && r["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(r["CreatedAt"]) : DateTime.Now
                    });
                }
            }
            return View(list);
        }

        public IActionResult DeleteOurTeamDetails(int id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });
            _datalayer.DeleteTeam(id);
            TempData["SuccessMessage"] = "Team member deleted successfully!";
            return RedirectToAction("OurTeamDetailsList");
        }
        #endregion

        #region Gallery
        [HttpGet]
        public IActionResult AddGalleryMaster(int? id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });
            GalleryMasterDto model = new GalleryMasterDto();
            if (id.HasValue && id.Value > 0)
            {
                DataTable dt = _datalayer.GetGalleryMasterById(id.Value);
                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow r = dt.Rows[0];
                    model.GalleryMId = Convert.ToInt32(r["GalleryId"]);
                    model.GalleryName = r["GalleryName"]?.ToString() ?? "";
                    model.Status = r.Table.Columns.Contains("Status") && r["Status"] != DBNull.Value ? Convert.ToInt32(r["Status"]) : 1;
                }
            }
            return View(model);
        }

        [HttpPost]
        public IActionResult AddGalleryMaster(GalleryMasterDto model)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });
            int res = _datalayer.InsertUpdateGalleryMaster(model);
            if (res > 0)
            {
                TempData["SuccessMessage"] = model.GalleryMId > 0 ? "Gallery album updated successfully!" : "Gallery album added successfully!";
                return RedirectToAction("GalleryMasterList");
            }
            TempData["ErrorMessage"] = "Failed to save album.";
            return View(model);
        }

        public IActionResult GalleryMasterList()
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });
            DataTable dt = _datalayer.GetGalleryMasterList();
            var list = new List<GalleryMasterDto>();
            if (dt != null)
            {
                foreach (DataRow r in dt.Rows)
                {
                    list.Add(new GalleryMasterDto
                    {
                        GalleryMId = r.Table.Columns.Contains("GalleryId") && r["GalleryId"] != DBNull.Value ? Convert.ToInt32(r["GalleryId"]) : 0,
                        GalleryName = r["GalleryName"]?.ToString() ?? "",
                        Status = r.Table.Columns.Contains("Status") && r["Status"] != DBNull.Value ? Convert.ToInt32(r["Status"]) : 1,
                        CreatedAt = r.Table.Columns.Contains("CreatedAt") && r["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(r["CreatedAt"]) : DateTime.Now
                    });
                }
            }
            return View(list);
        }

        public IActionResult DeleteGalleryMaster(int id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });
            _datalayer.DeleteGalleryMaster(id);
            TempData["SuccessMessage"] = "Gallery album deleted successfully!";
            return RedirectToAction("GalleryMasterList");
        }

        private List<SelectListItem> GetAlbumsDropdown()
        {
            DataTable dt = _datalayer.GetGalleryMasterList();
            List<SelectListItem> list = new List<SelectListItem>();
            if (dt != null)
            {
                foreach (DataRow r in dt.Rows)
                {
                    list.Add(new SelectListItem
                    {
                        Value = r["GalleryId"].ToString(),
                        Text = r["GalleryName"].ToString()
                    });
                }
            }
            return list;
        }

        [HttpGet]
        public IActionResult AddGalleryDetails(int? id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });
            ViewBag.service = GetAlbumsDropdown();

            GalleryDetailsDto model = new GalleryDetailsDto();
            if (id.HasValue && id.Value > 0)
            {
                DataTable dt = _datalayer.GetGalleryDetailById(id.Value);
                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow r = dt.Rows[0];
                    model.GalleryDId = Convert.ToInt32(r["GalleryDetailsId"]);
                    model.GalleryMId = Convert.ToInt32(r["GalleryId"]);
                    model.ImageTitle = r["ImageTitle"]?.ToString();
                    model.Filename = r["ImagePath"]?.ToString();
                    model.Status = r.Table.Columns.Contains("Status") && r["Status"] != DBNull.Value ? Convert.ToInt32(r["Status"]) : 1;
                }
            }
            return View(model);
        }

        [HttpPost]
        public IActionResult AddGalleryDetails(GalleryDetailsDto model)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });

            if (model.FilePath != null && model.FilePath.Length > 0)
            {
                string uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "gallery");
                if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.FilePath.FileName);
                string filePath = Path.Combine(uploadFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    model.FilePath.CopyTo(stream);
                }
                model.Filename = "/uploads/gallery/" + fileName;
            }

            int res;
            if (model.GalleryDId > 0)
            {
                res = _datalayer.UpdateGalleryDetail(model);
            }
            else
            {
                res = _datalayer.InsertGalleryDetail(model);
            }

            if (res > 0)
            {
                TempData["SuccessMessage"] = model.GalleryDId > 0 ? "Gallery photo updated successfully!" : "Photo uploaded to gallery successfully!";
                return RedirectToAction("GalleryDetailsList");
            }
            TempData["ErrorMessage"] = "Failed to upload photo.";
            ViewBag.service = GetAlbumsDropdown();
            return View(model);
        }

        public IActionResult GalleryDetailsList(int? galleryId)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });
            DataTable dt = _datalayer.GetGalleryDetailsList(galleryId);
            var list = new List<GalleryDetailsDto>();
            if (dt != null)
            {
                foreach (DataRow r in dt.Rows)
                {
                    string img = r.Table.Columns.Contains("ImagePath") ? (r["ImagePath"]?.ToString() ?? "") : "";
                    string gName = r.Table.Columns.Contains("GalleryName") ? (r["GalleryName"]?.ToString() ?? "") : "";
                    list.Add(new GalleryDetailsDto
                    {
                        GalleryDId = r.Table.Columns.Contains("GalleryDetailsId") && r["GalleryDetailsId"] != DBNull.Value ? Convert.ToInt32(r["GalleryDetailsId"]) : 0,
                        GalleryMId = r.Table.Columns.Contains("GalleryId") && r["GalleryId"] != DBNull.Value ? Convert.ToInt32(r["GalleryId"]) : 0,
                        GalleryName = gName,
                        Action = gName,
                        ImageTitle = r.Table.Columns.Contains("ImageTitle") ? (r["ImageTitle"]?.ToString() ?? "") : "",
                        Filename = img,
                        Status = r.Table.Columns.Contains("Status") && r["Status"] != DBNull.Value ? Convert.ToInt32(r["Status"]) : 1,
                        CreatedAt = r.Table.Columns.Contains("CreatedAt") && r["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(r["CreatedAt"]) : DateTime.Now
                    });
                }
            }
            return View(list);
        }

        public IActionResult DeleteGalleryDetails(int id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });
            _datalayer.DeleteGalleryDetail(id);
            TempData["SuccessMessage"] = "Gallery image deleted successfully!";
            return RedirectToAction("GalleryDetailsList");
        }
        #endregion

        #region Inquiries & Volunteers
        public IActionResult Inquiries()
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });
            DataTable dt = _datalayer.GetContactInquiries();
            return View(dt);
        }

        [HttpPost]
        public IActionResult DeleteInquiry(int id)
        {
            if (!CheckAuth()) return Json(new { success = false, message = "Unauthorized" });
            bool ok = _datalayer.DeleteContactInquiry(id);
            return Json(new { success = ok, message = ok ? "Inquiry deleted successfully." : "Failed to delete inquiry." });
        }

        public IActionResult Volunteers()
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });
            DataTable dt = _datalayer.GetVolunteers();
            return View(dt);
        }
        #endregion
    }
}
