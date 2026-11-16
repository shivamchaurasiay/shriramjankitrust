using ClosedXML.Excel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using RamJankiNGO.Data;
using RamJankiNGO.Models;
using RamJankiNGO.Services;

namespace RamJankiNGO.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class MemberController : Controller
    {
        private readonly Datalayer _datalayer;
        private readonly IWebHostEnvironment _env;
        private readonly IEmailService _emailService;

        public MemberController(Datalayer datalayer, IWebHostEnvironment env, IEmailService emailService)
        {
            _datalayer = datalayer;
            _env = env;
            _emailService = emailService;
        }

        private bool CheckAuth()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("UserId"));
        }

        public IActionResult Index(string? search)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });
            DataTable dt = _datalayer.GetMembersList(search);
            ViewBag.Search = search;
            return View(dt);
        }

        [HttpGet]
        public IActionResult AddEditMember(int? id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });
            MemberDto model = new MemberDto();

            if (id.HasValue && id.Value > 0)
            {
                DataTable dt = _datalayer.GetMemberById(id.Value);
                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow r = dt.Rows[0];
                    model.MemberId = Convert.ToInt32(r["MemberId"]);
                    model.MemberCode = r["MemberCode"].ToString() ?? "";
                    model.FullName = r["FullName"].ToString() ?? "";
                    model.Email = r["Email"]?.ToString();
                    model.Mobile = r["Mobile"].ToString() ?? "";
                    model.Address = r["Address"]?.ToString();
                    model.City = r["City"]?.ToString();
                    model.State = r["State"]?.ToString();
                    model.PinCode = r["PinCode"]?.ToString();
                    if (r["DateOfBirth"] != DBNull.Value) model.DateOfBirth = Convert.ToDateTime(r["DateOfBirth"]);
                    model.MemberType = r["MemberType"]?.ToString() ?? "Regular";
                    model.Status = Convert.ToInt32(r["Status"]);
                    model.ProfilePhoto = r["ProfilePhoto"]?.ToString();
                }
            }
            return View(model);
        }

        [HttpPost]
        public IActionResult AddEditMember(MemberDto model)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });

            if (model.PhotoFile != null && model.PhotoFile.Length > 0)
            {
                string uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "members");
                if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.PhotoFile.FileName);
                string filePath = Path.Combine(uploadFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    model.PhotoFile.CopyTo(stream);
                }
                model.ProfilePhoto = "/uploads/members/" + fileName;
            }

            try
            {
                bool isNew = model.MemberId == 0;
                int res = _datalayer.InsertUpdateMember(model);
                if (res > 0)
                {
                    if (isNew && !string.IsNullOrWhiteSpace(model.Email))
                    {
                        var mDt = _datalayer.GetMemberById(res);
                        string memCode = mDt != null && mDt.Rows.Count > 0 ? mDt.Rows[0]["MemberCode"]?.ToString() ?? "MEM" : "MEM";
                        string loginUrl = $"{Request.Scheme}://{Request.Host}/MemberPortal/Login";
                        _ = _emailService.SendMemberWelcomeEmailAsync(model.Email, model.FullName, memCode, loginUrl);
                    }

                    TempData["SuccessMessage"] = model.MemberId > 0 ? "Member updated successfully!" : "Member added successfully! Welcome email sent.";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to save member record.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error: " + ex.Message;
            }

            return View(model);
        }

        public IActionResult ToggleStatus(int id)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });
            _datalayer.ToggleMemberStatus(id);
            TempData["SuccessMessage"] = "Member status changed successfully!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Import()
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });
            return View(new MemberImportResult());
        }

        [HttpPost]
        public IActionResult Import(IFormFile excelFile)
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });

            MemberImportResult result = new MemberImportResult();

            if (excelFile == null || excelFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a valid Excel (.xlsx) file.";
                return View(result);
            }

            try
            {
                using (var stream = new MemoryStream())
                {
                    excelFile.CopyTo(stream);
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rows = worksheet.RangeUsed().RowsUsed();

                        int rowIndex = 0;
                        foreach (var row in rows)
                        {
                            rowIndex++;
                            if (rowIndex == 1) continue; // Skip header

                            result.TotalRows++;
                            string fullName = row.Cell(1).GetString().Trim();
                            string mobile = row.Cell(2).GetString().Trim();
                            string email = row.Cell(3).GetString().Trim();
                            string memberType = row.Cell(4).GetString().Trim();
                            string address = row.Cell(5).GetString().Trim();
                            string city = row.Cell(6).GetString().Trim();
                            string state = row.Cell(7).GetString().Trim();
                            string pincode = row.Cell(8).GetString().Trim();

                            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(mobile))
                            {
                                result.FailedCount++;
                                result.Errors.Add($"Row {rowIndex}: Full Name and Mobile are required.");
                                continue;
                            }

                            // Check duplicate
                            DataTable dtDup = _datalayer.CheckMemberDuplicate(mobile, email);
                            if (dtDup != null && dtDup.Rows.Count > 0)
                            {
                                result.SkippedCount++;
                                result.Errors.Add($"Row {rowIndex}: Mobile {mobile} already registered (Skipped).");
                                continue;
                            }

                            MemberDto member = new MemberDto
                            {
                                FullName = fullName,
                                Mobile = mobile,
                                Email = string.IsNullOrWhiteSpace(email) ? null : email,
                                MemberType = string.IsNullOrWhiteSpace(memberType) ? "Regular" : memberType,
                                Address = address,
                                City = city,
                                State = state,
                                PinCode = pincode,
                                Status = 1
                            };

                            int newId = _datalayer.InsertUpdateMember(member);
                            if (newId > 0)
                            {
                                result.SuccessCount++;
                            }
                            else
                            {
                                result.FailedCount++;
                                result.Errors.Add($"Row {rowIndex}: Database insert failed for {fullName}.");
                            }
                        }
                    }
                }

                TempData["SuccessMessage"] = $"Import completed! {result.SuccessCount} added, {result.SkippedCount} skipped duplicates, {result.FailedCount} failed.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to process Excel file: " + ex.Message;
            }

            return View(result);
        }

        public IActionResult DownloadSampleTemplate()
        {
            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Members");
                ws.Cell(1, 1).Value = "FullName*";
                ws.Cell(1, 2).Value = "Mobile*";
                ws.Cell(1, 3).Value = "Email";
                ws.Cell(1, 4).Value = "MemberType";
                ws.Cell(1, 5).Value = "Address";
                ws.Cell(1, 6).Value = "City";
                ws.Cell(1, 7).Value = "State";
                ws.Cell(1, 8).Value = "PinCode";

                // Sample row
                ws.Cell(2, 1).Value = "Ramesh Kumar";
                ws.Cell(2, 2).Value = "9876543211";
                ws.Cell(2, 3).Value = "ramesh@example.com";
                ws.Cell(2, 4).Value = "Regular";
                ws.Cell(2, 5).Value = "Station Road";
                ws.Cell(2, 6).Value = "Ayodhya";
                ws.Cell(2, 7).Value = "Uttar Pradesh";
                ws.Cell(2, 8).Value = "224123";

                ws.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Member_Import_Sample.xlsx");
                }
            }
        }

        public IActionResult ExportToExcel()
        {
            if (!CheckAuth()) return RedirectToAction("Login", "Account", new { area = "Admin" });

            DataTable dt = _datalayer.GetMembersList();
            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Members List");
                ws.Cell(1, 1).Value = "Member Code";
                ws.Cell(1, 2).Value = "Full Name";
                ws.Cell(1, 3).Value = "Mobile";
                ws.Cell(1, 4).Value = "Email";
                ws.Cell(1, 5).Value = "Member Type";
                ws.Cell(1, 6).Value = "City";
                ws.Cell(1, 7).Value = "State";
                ws.Cell(1, 8).Value = "Status";
                ws.Cell(1, 9).Value = "Total Donated (INR)";
                ws.Cell(1, 10).Value = "Joined Date";

                int row = 2;
                foreach (DataRow r in dt.Rows)
                {
                    ws.Cell(row, 1).Value = r["MemberCode"].ToString();
                    ws.Cell(row, 2).Value = r["FullName"].ToString();
                    ws.Cell(row, 3).Value = r["Mobile"].ToString();
                    ws.Cell(row, 4).Value = r["Email"]?.ToString() ?? "";
                    ws.Cell(row, 5).Value = r["MemberType"]?.ToString() ?? "";
                    ws.Cell(row, 6).Value = r["City"]?.ToString() ?? "";
                    ws.Cell(row, 7).Value = r["State"]?.ToString() ?? "";
                    ws.Cell(row, 8).Value = Convert.ToInt32(r["Status"]) == 1 ? "Active" : "Inactive";
                    ws.Cell(row, 9).Value = Convert.ToDecimal(r["TotalDonated"]);
                    ws.Cell(row, 10).Value = Convert.ToDateTime(r["JoinedAt"]).ToString("dd/MM/yyyy");
                    row++;
                }

                ws.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Members_{DateTime.Now:yyyyMMdd}.xlsx");
                }
            }
        }
    }
}
