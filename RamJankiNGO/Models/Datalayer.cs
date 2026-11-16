using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using RamJankiNGO.Models;

namespace RamJankiNGO.Data
{
    public class Datalayer
    {
        private readonly dbHelper _dbHelper;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

        public Datalayer(dbHelper dbHelper, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _dbHelper = dbHelper;
            _configuration = configuration;
        }

        #region User & Authentication
        public DataTable GetUserByUsername(string mobileOrEmail)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Mobile", mobileOrEmail),
                new SqlParameter("@Action", "4")
            };
            return _dbHelper.ExecProcDataTable("ManageAdminLogin", parameters);
        }

        public DataTable GetUserById(int userId)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@Action", "7")
            };
            return _dbHelper.ExecProcDataTable("ManageAdminLogin", parameters);
        }

        public DataTable GetUserByUpdateUserLogin(int userId)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@Action", "5")
            };
            return _dbHelper.ExecProcDataTable("ManageAdminLogin", parameters);
        }

        public DataTable GetUserByUpdateUserLogout(int userId)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@Action", "6")
            };
            return _dbHelper.ExecProcDataTable("ManageAdminLogin", parameters);
        }

        public DataTable UpdateUserDataById(UserProfileDto user)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "2"),
                new SqlParameter("@UserId", user.UserId),
                new SqlParameter("@Mobile", user.MobileNu ?? string.Empty),
                new SqlParameter("@Email", user.Email ?? string.Empty),
                new SqlParameter("@UserName", user.UserName ?? string.Empty)
            };
            return _dbHelper.ExecProcDataTable("ManageAdminLogin", parameters);
        }

        public DataTable ResetPassword(string oldPassword, string newPassword, string employeeCode)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@oldPassword", oldPassword),
                new SqlParameter("@newPassword", newPassword),
                new SqlParameter("@employeeCode", employeeCode)
            };
            return _dbHelper.ExecProcDataTable("USP_ResetPassword", parameters);
        }

        public void SavePasswordResetOtp(string email, string otp)
        {
            string sql = "INSERT INTO PasswordResetOtps (Email, OtpCode, ExpiresAt, IsUsed, CreatedAt) VALUES (@Email, @Otp, DATEADD(minute, 2, GETDATE()), 0, GETDATE())";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Email", email),
                new SqlParameter("@Otp", otp)
            };
            _dbHelper.ExecuteNonQuery(sql, parameters);
        }

        public (bool isValid, string message) ValidatePasswordResetOtp(string email, string otp)
        {
            string checkSql = "SELECT TOP 1 Id, ExpiresAt, IsUsed, CASE WHEN GETDATE() > ExpiresAt THEN 1 ELSE 0 END AS IsExpired FROM PasswordResetOtps WHERE Email = @Email AND OtpCode = @Otp ORDER BY Id DESC";
            SqlParameter[] p = new SqlParameter[]
            {
                new SqlParameter("@Email", email),
                new SqlParameter("@Otp", otp)
            };
            DataTable dt = _dbHelper.ExecDataTable(checkSql, p);
            if (dt == null || dt.Rows.Count == 0)
            {
                return (false, "Aapka darj kiya gaya OTP galat hai. Kripya sahi OTP enter karein.");
            }

            if (Convert.ToBoolean(dt.Rows[0]["IsUsed"]))
            {
                return (false, "Yeh OTP pehle hi use ho chuka hai. Naya OTP generate karein.");
            }

            if (Convert.ToInt32(dt.Rows[0]["IsExpired"]) == 1)
            {
                return (false, "OTP expire ho chuka hai (2 minutes exceeded). Kripya naya OTP generate karein.");
            }

            return (true, "Valid");
        }

        public bool ResetUserPasswordWithOtp(string email, string otp, string newPassword)
        {
            var validation = ValidatePasswordResetOtp(email, otp);
            if (!validation.isValid) return false;

            string updateOtpSql = "UPDATE PasswordResetOtps SET IsUsed = 1 WHERE Email = @Email AND OtpCode = @Otp";
            SqlParameter[] pOtp = new SqlParameter[]
            {
                new SqlParameter("@Email", email),
                new SqlParameter("@Otp", otp)
            };
            _dbHelper.ExecuteNonQuery(updateOtpSql, pOtp);

            string updatePassSql = "UPDATE Users SET Password = @NewPassword, UpdatedAt = GETDATE() WHERE Email = @Email OR Mobile = @Email";
            SqlParameter[] pPass = new SqlParameter[]
            {
                new SqlParameter("@NewPassword", newPassword),
                new SqlParameter("@Email", email)
            };
            int rows = _dbHelper.ExecuteNonQuery(updatePassSql, pPass);
            return rows > 0;
        }
        #endregion

        #region Dashboard
        public DashboardDto GetDashboardData()
        {
            DashboardDto dto = new DashboardDto();
            try
            {
                DataSet ds = _dbHelper.ExecProcDataSet("GetDashboardStats", null);
                if (ds != null && ds.Tables.Count > 0)
                {
                    DataTable dtStats = ds.Tables[0];
                    if (dtStats.Rows.Count > 0)
                    {
                        DataRow row = dtStats.Rows[0];
                        dto.TotalMembers = Convert.ToInt32(row["TotalMembers"]);
                        dto.ActiveMembers = Convert.ToInt32(row["ActiveMembers"]);
                        dto.TotalDonationAmount = Convert.ToDecimal(row["TotalDonationAmount"]);
                        dto.TotalDonationCount = Convert.ToInt32(row["TotalDonationCount"]);
                        dto.CurrentMonthAmount = Convert.ToDecimal(row["CurrentMonthAmount"]);
                        dto.PendingDonationsCount = Convert.ToInt32(row["PendingDonationsCount"]);
                        dto.ActiveCampaigns = Convert.ToInt32(row["ActiveCampaigns"]);
                        dto.NewInquiries = Convert.ToInt32(row["NewInquiries"]);
                    }

                    if (ds.Tables.Count > 1) dto.RecentDonations = ds.Tables[1];
                    if (ds.Tables.Count > 2) dto.RecentMembers = ds.Tables[2];
                    if (ds.Tables.Count > 3) dto.CampaignSummary = ds.Tables[3];
                }
            }
            catch (Exception)
            {
                // Fallback default
            }
            return dto;
        }
        #endregion

        #region Members
        public DataTable GetMembersList(string? search = null)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "4"),
                new SqlParameter("@Search", (object?)search ?? DBNull.Value)
            };
            return _dbHelper.ExecProcDataTable("ManageMembers", parameters);
        }

        public DataTable GetMemberById(int memberId)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "5"),
                new SqlParameter("@MemberId", memberId)
            };
            return _dbHelper.ExecProcDataTable("ManageMembers", parameters);
        }

        public DataTable GetMemberByUserId(int userId)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "6"),
                new SqlParameter("@UserId", userId)
            };
            return _dbHelper.ExecProcDataTable("ManageMembers", parameters);
        }

        public DataTable CheckMemberDuplicate(string mobile, string? email = null, string? memberCode = null)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "7"),
                new SqlParameter("@Mobile", mobile),
                new SqlParameter("@Email", (object?)email ?? DBNull.Value),
                new SqlParameter("@MemberCode", (object?)memberCode ?? DBNull.Value)
            };
            return _dbHelper.ExecProcDataTable("ManageMembers", parameters);
        }

        public int InsertUpdateMember(MemberDto model)
        {
            string action = model.MemberId > 0 ? "2" : "1";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", action),
                new SqlParameter("@MemberId", model.MemberId > 0 ? (object)model.MemberId : DBNull.Value),
                new SqlParameter("@MemberCode", (object?)model.MemberCode ?? DBNull.Value),
                new SqlParameter("@FullName", model.FullName),
                new SqlParameter("@Email", (object?)model.Email ?? DBNull.Value),
                new SqlParameter("@Mobile", model.Mobile),
                new SqlParameter("@Address", (object?)model.Address ?? DBNull.Value),
                new SqlParameter("@City", (object?)model.City ?? DBNull.Value),
                new SqlParameter("@State", (object?)model.State ?? DBNull.Value),
                new SqlParameter("@PinCode", (object?)model.PinCode ?? DBNull.Value),
                new SqlParameter("@DateOfBirth", model.DateOfBirth.HasValue ? (object)model.DateOfBirth.Value : DBNull.Value),
                new SqlParameter("@MemberType", model.MemberType ?? "Regular"),
                new SqlParameter("@Status", model.Status),
                new SqlParameter("@ProfilePhoto", (object?)model.ProfilePhoto ?? DBNull.Value)
            };

            DataTable dt = _dbHelper.ExecProcDataTable("ManageMembers", parameters);
            if (dt != null && dt.Rows.Count > 0)
            {
                return Convert.ToInt32(dt.Rows[0]["MemberId"]);
            }
            return 0;
        }

        public bool ToggleMemberStatus(int memberId)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "3"),
                new SqlParameter("@MemberId", memberId)
            };
            DataTable dt = _dbHelper.ExecProcDataTable("ManageMembers", parameters);
            return dt != null && dt.Rows.Count > 0;
        }

        public (bool success, string message, int memberId, string memberCode) RegisterMember(RegisterDto model)
        {
            DataTable dtDup = CheckMemberDuplicate(model.Mobile, model.Email);
            if (dtDup != null && dtDup.Rows.Count > 0)
            {
                return (false, "An account with this Mobile Number or Email already exists. Please login instead.", 0, "");
            }

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "1"),
                new SqlParameter("@FullName", model.FullName),
                new SqlParameter("@Mobile", model.Mobile),
                new SqlParameter("@Email", (object?)model.Email ?? DBNull.Value),
                new SqlParameter("@Password", model.Password),
                new SqlParameter("@City", (object?)model.City ?? DBNull.Value),
                new SqlParameter("@State", (object?)model.State ?? DBNull.Value),
                new SqlParameter("@Address", (object?)model.Address ?? DBNull.Value),
                new SqlParameter("@PinCode", (object?)model.PinCode ?? DBNull.Value),
                new SqlParameter("@MemberType", "Registered Member"),
                new SqlParameter("@Status", 1)
            };

            DataTable dt = _dbHelper.ExecProcDataTable("ManageMembers", parameters);
            if (dt != null && dt.Rows.Count > 0)
            {
                int newMemId = Convert.ToInt32(dt.Rows[0]["MemberId"]);
                string memCode = dt.Rows[0]["MemberCode"]?.ToString() ?? "";
                return (true, "Registration successful!", newMemId, memCode);
            }

            return (false, "Unable to register at this moment. Please try again.", 0, "");
        }
        #endregion

        #region Donations & Receipts
        public DataTable GetDonationsList(DonationReportFilterDto filter)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "4"),
                new SqlParameter("@StartDate", filter.StartDate.HasValue ? (object)filter.StartDate.Value : DBNull.Value),
                new SqlParameter("@EndDate", filter.EndDate.HasValue ? (object)filter.EndDate.Value : DBNull.Value),
                new SqlParameter("@PaymentStatus", (object?)filter.PaymentStatus ?? DBNull.Value),
                new SqlParameter("@CampaignId", filter.CampaignId.HasValue ? (object)filter.CampaignId.Value : DBNull.Value),
                new SqlParameter("@Search", (object?)filter.Search ?? DBNull.Value)
            };
            return _dbHelper.ExecProcDataTable("ManageDonations", parameters);
        }

        public DataTable GetDonationById(int donationId)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "5"),
                new SqlParameter("@DonationId", donationId)
            };
            return _dbHelper.ExecProcDataTable("ManageDonations", parameters);
        }

        public DataTable GetDonationsByMemberId(int memberId)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "6"),
                new SqlParameter("@MemberId", memberId)
            };
            return _dbHelper.ExecProcDataTable("ManageDonations", parameters);
        }

        public int CreateOnlineDonationPending(DonationDto model)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "1"),
                new SqlParameter("@MemberId", model.MemberId.HasValue ? (object)model.MemberId.Value : DBNull.Value),
                new SqlParameter("@CampaignId", model.CampaignId.HasValue ? (object)model.CampaignId.Value : DBNull.Value),
                new SqlParameter("@DonorName", model.DonorName),
                new SqlParameter("@DonorEmail", (object?)model.DonorEmail ?? DBNull.Value),
                new SqlParameter("@DonorPhone", model.DonorPhone),
                new SqlParameter("@DonorAddress", (object?)model.DonorAddress ?? DBNull.Value),
                new SqlParameter("@DonorPan", (object?)model.DonorPan ?? DBNull.Value),
                new SqlParameter("@Amount", model.Amount),
                new SqlParameter("@PaymentMode", "Online"),
                new SqlParameter("@CreatedBy", (object?)model.CreatedBy ?? "Public Donor")
            };

            DataTable dt = _dbHelper.ExecProcDataTable("ManageDonations", parameters);
            if (dt != null && dt.Rows.Count > 0)
            {
                model.DonationCode = dt.Rows[0]["DonationCode"].ToString();
                if (dt.Columns.Contains("MemberId") && dt.Rows[0]["MemberId"] != DBNull.Value)
                {
                    model.MemberId = Convert.ToInt32(dt.Rows[0]["MemberId"]);
                }
                return Convert.ToInt32(dt.Rows[0]["DonationId"]);
            }
            return 0;
        }

        public bool ConfirmDonationSuccess(int donationId, string gatewayRef, out string receiptNumber)
        {
            receiptNumber = string.Empty;
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "2"),
                new SqlParameter("@DonationId", donationId),
                new SqlParameter("@PaymentGatewayReference", gatewayRef)
            };

            DataTable dt = _dbHelper.ExecProcDataTable("ManageDonations", parameters);
            if (dt != null && dt.Rows.Count > 0)
            {
                receiptNumber = dt.Rows[0]["ReceiptNumber"].ToString();
                return true;
            }
            return false;
        }

        public int RecordOfflineDonation(DonationDto model, out string receiptNumber)
        {
            receiptNumber = string.Empty;
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "3"),
                new SqlParameter("@MemberId", model.MemberId.HasValue ? (object)model.MemberId.Value : DBNull.Value),
                new SqlParameter("@CampaignId", model.CampaignId.HasValue ? (object)model.CampaignId.Value : DBNull.Value),
                new SqlParameter("@DonorName", model.DonorName),
                new SqlParameter("@DonorEmail", (object?)model.DonorEmail ?? DBNull.Value),
                new SqlParameter("@DonorPhone", model.DonorPhone),
                new SqlParameter("@DonorAddress", (object?)model.DonorAddress ?? DBNull.Value),
                new SqlParameter("@DonorPan", (object?)model.DonorPan ?? DBNull.Value),
                new SqlParameter("@Amount", model.Amount),
                new SqlParameter("@PaymentMode", model.PaymentMode),
                new SqlParameter("@PaymentGatewayReference", (object?)model.PaymentGatewayReference ?? DBNull.Value),
                new SqlParameter("@Notes", (object?)model.Notes ?? DBNull.Value),
                new SqlParameter("@CreatedBy", (object?)model.CreatedBy ?? "Admin")
            };

            DataTable dt = _dbHelper.ExecProcDataTable("ManageDonations", parameters);
            if (dt != null && dt.Rows.Count > 0)
            {
                receiptNumber = dt.Rows[0]["ReceiptNumber"].ToString();
                return Convert.ToInt32(dt.Rows[0]["DonationId"]);
            }
            return 0;
        }

        public bool RefundDonation(int donationId, string reason)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "7"),
                new SqlParameter("@DonationId", donationId),
                new SqlParameter("@Notes", reason)
            };
            DataTable dt = _dbHelper.ExecProcDataTable("ManageDonations", parameters);
            return dt != null && dt.Rows.Count > 0;
        }

        public DataTable GetDonationByGatewayRef(string gatewayRef)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "8"),
                new SqlParameter("@PaymentGatewayReference", gatewayRef)
            };
            return _dbHelper.ExecProcDataTable("ManageDonations", parameters);
        }

        public DataTable GetDonationsByPhoneOrEmail(string phone, string? email = null)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "9"),
                new SqlParameter("@DonorPhone", phone),
                new SqlParameter("@DonorEmail", (object?)email ?? DBNull.Value)
            };
            return _dbHelper.ExecProcDataTable("ManageDonations", parameters);
        }
        #endregion

        #region Campaigns
        public DataTable GetCampaignsList()
        {
            SqlParameter[] parameters = new SqlParameter[] { new SqlParameter("@Action", "4") };
            return _dbHelper.ExecProcDataTable("ManageCampaigns", parameters);
        }

        public DataTable GetActiveCampaigns()
        {
            SqlParameter[] parameters = new SqlParameter[] { new SqlParameter("@Action", "6") };
            return _dbHelper.ExecProcDataTable("ManageCampaigns", parameters);
        }

        public DataTable GetCampaignById(int id)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "5"),
                new SqlParameter("@CampaignId", id)
            };
            return _dbHelper.ExecProcDataTable("ManageCampaigns", parameters);
        }

        public DataTable GetProjectsList()
        {
            return _dbHelper.ExecAdaptorDataTable("SELECT ProjectId, Title, Slug, Category, Description, ImagePath, Status, CreatedAt FROM Projects WHERE Status = 1 ORDER BY ProjectId ASC");
        }

        public int InsertUpdateCampaign(CampaignDto model)
        {
            string action = model.CampaignId > 0 ? "2" : "1";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", action),
                new SqlParameter("@CampaignId", model.CampaignId > 0 ? (object)model.CampaignId : DBNull.Value),
                new SqlParameter("@Title", model.Title),
                new SqlParameter("@Category", (object?)model.Category ?? DBNull.Value),
                new SqlParameter("@ShortDescription", (object?)model.ShortDescription ?? DBNull.Value),
                new SqlParameter("@Description", (object?)model.Description ?? DBNull.Value),
                new SqlParameter("@TargetAmount", model.TargetAmount),
                new SqlParameter("@RaisedAmount", model.RaisedAmount),
                new SqlParameter("@StartDate", model.StartDate.HasValue ? (object)model.StartDate.Value : DBNull.Value),
                new SqlParameter("@EndDate", model.EndDate.HasValue ? (object)model.EndDate.Value : DBNull.Value),
                new SqlParameter("@ImagePath", (object?)model.ImagePath ?? DBNull.Value),
                new SqlParameter("@IsFeatured", model.IsFeatured),
                new SqlParameter("@Status", model.Status)
            };
            DataTable dt = _dbHelper.ExecProcDataTable("ManageCampaigns", parameters);
            if (dt != null && dt.Rows.Count > 0) return Convert.ToInt32(dt.Rows[0]["CampaignId"]);
            return 0;
        }

        public bool DeleteCampaign(int id)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "3"),
                new SqlParameter("@CampaignId", id)
            };
            DataTable dt = _dbHelper.ExecProcDataTable("ManageCampaigns", parameters);
            return dt != null && dt.Rows.Count > 0;
        }
        #endregion

        #region Blogs
        public DataTable GetBlogsList()
        {
            SqlParameter[] parameters = new SqlParameter[] { new SqlParameter("@Action", "4") };
            return _dbHelper.ExecProcDataTable("ManageBlogs", parameters);
        }

        public DataTable GetBlogById(int id)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "5"),
                new SqlParameter("@BlogId", id)
            };
            return _dbHelper.ExecProcDataTable("ManageBlogs", parameters);
        }

        public int InsertUpdateBlog(BlogDto model)
        {
            string action = model.BlogId > 0 ? "2" : "1";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", action),
                new SqlParameter("@BlogId", model.BlogId > 0 ? (object)model.BlogId : DBNull.Value),
                new SqlParameter("@Title", model.Title),
                new SqlParameter("@Category", (object?)model.Category ?? DBNull.Value),
                new SqlParameter("@Author", (object?)model.Author ?? "Admin"),
                new SqlParameter("@ShortDescription", (object?)model.ShortDescription ?? DBNull.Value),
                new SqlParameter("@Description", (object?)model.Description ?? DBNull.Value),
                new SqlParameter("@ImagePath", (object?)model.ImagePath ?? DBNull.Value),
                new SqlParameter("@Status", model.Status)
            };
            DataTable dt = _dbHelper.ExecProcDataTable("ManageBlogs", parameters);
            if (dt != null && dt.Rows.Count > 0) return Convert.ToInt32(dt.Rows[0]["BlogId"]);
            return 0;
        }

        public bool DeleteBlog(int id)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "3"),
                new SqlParameter("@BlogId", id)
            };
            DataTable dt = _dbHelper.ExecProcDataTable("ManageBlogs", parameters);
            return dt != null && dt.Rows.Count > 0;
        }
        #endregion

        #region Our Team
        public DataTable GetTeamList()
        {
            SqlParameter[] parameters = new SqlParameter[] { new SqlParameter("@Action", "4") };
            return _dbHelper.ExecProcDataTable("ManageOurTeam", parameters);
        }

        public DataTable GetTeamById(int id)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "5"),
                new SqlParameter("@TeamId", id)
            };
            return _dbHelper.ExecProcDataTable("ManageOurTeam", parameters);
        }

        public int InsertUpdateTeam(TeamDetailsDto model)
        {
            string action = model.TeamId > 0 ? "2" : "1";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", action),
                new SqlParameter("@TeamId", model.TeamId > 0 ? (object)model.TeamId : DBNull.Value),
                new SqlParameter("@Name", model.Name),
                new SqlParameter("@Designation", model.Designation),
                new SqlParameter("@Description", (object?)model.Description ?? DBNull.Value),
                new SqlParameter("@ImagePath", (object?)model.ImagePath ?? DBNull.Value),
                new SqlParameter("@Status", model.Status)
            };
            DataTable dt = _dbHelper.ExecProcDataTable("ManageOurTeam", parameters);
            if (dt != null && dt.Rows.Count > 0) return Convert.ToInt32(dt.Rows[0]["TeamId"]);
            return 0;
        }

        public bool DeleteTeam(int id)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "3"),
                new SqlParameter("@TeamId", id)
            };
            DataTable dt = _dbHelper.ExecProcDataTable("ManageOurTeam", parameters);
            return dt != null && dt.Rows.Count > 0;
        }
        #endregion

        #region Gallery
        public DataTable GetGalleryMasterList()
        {
            SqlParameter[] parameters = new SqlParameter[] { new SqlParameter("@Action", "4") };
            return _dbHelper.ExecProcDataTable("ManageGallery", parameters);
        }

        public int InsertUpdateGalleryMaster(GalleryMasterDto model)
        {
            string action = model.GalleryId > 0 ? "2" : "1";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", action),
                new SqlParameter("@GalleryId", model.GalleryId > 0 ? (object)model.GalleryId : DBNull.Value),
                new SqlParameter("@GalleryName", model.GalleryName),
                new SqlParameter("@Status", model.Status)
            };
            DataTable dt = _dbHelper.ExecProcDataTable("ManageGallery", parameters);
            if (dt != null && dt.Rows.Count > 0) return Convert.ToInt32(dt.Rows[0]["GalleryId"]);
            return 0;
        }

        public bool DeleteGalleryMaster(int id)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "3"),
                new SqlParameter("@GalleryId", id)
            };
            DataTable dt = _dbHelper.ExecProcDataTable("ManageGallery", parameters);
            return dt != null && dt.Rows.Count > 0;
        }

        public DataTable GetGalleryMasterById(int id)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "10"),
                new SqlParameter("@GalleryId", id)
            };
            return _dbHelper.ExecProcDataTable("ManageGallery", parameters);
        }

        public DataTable GetGalleryDetailsList(int? galleryId = null)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "7"),
                new SqlParameter("@GalleryId", galleryId.HasValue ? (object)galleryId.Value : DBNull.Value)
            };
            return _dbHelper.ExecProcDataTable("ManageGallery", parameters);
        }

        public DataTable GetGalleryDetailById(int id)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "9"),
                new SqlParameter("@GalleryDetailsId", id)
            };
            return _dbHelper.ExecProcDataTable("ManageGallery", parameters);
        }

        public int InsertGalleryDetail(GalleryDetailsDto model)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "5"),
                new SqlParameter("@GalleryId", model.GalleryId),
                new SqlParameter("@ImageTitle", (object?)model.ImageTitle ?? DBNull.Value),
                new SqlParameter("@ImagePath", model.ImagePath ?? string.Empty),
                new SqlParameter("@Status", model.Status)
            };
            DataTable dt = _dbHelper.ExecProcDataTable("ManageGallery", parameters);
            if (dt != null && dt.Rows.Count > 0) return Convert.ToInt32(dt.Rows[0]["GalleryDetailsId"]);
            return 0;
        }

        public int UpdateGalleryDetail(GalleryDetailsDto model)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "8"),
                new SqlParameter("@GalleryDetailsId", model.GalleryDId),
                new SqlParameter("@GalleryId", model.GalleryMId),
                new SqlParameter("@ImageTitle", (object?)model.ImageTitle ?? DBNull.Value),
                new SqlParameter("@ImagePath", model.ImagePath ?? string.Empty),
                new SqlParameter("@Status", model.Status)
            };
            DataTable dt = _dbHelper.ExecProcDataTable("ManageGallery", parameters);
            if (dt != null && dt.Rows.Count > 0) return Convert.ToInt32(dt.Rows[0]["GalleryDetailsId"]);
            return 0;
        }

        public bool DeleteGalleryDetail(int id)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Action", "6"),
                new SqlParameter("@GalleryDetailsId", id)
            };
            DataTable dt = _dbHelper.ExecProcDataTable("ManageGallery", parameters);
            return dt != null && dt.Rows.Count > 0;
        }
        #endregion

        #region Inquiries & Volunteer
        public bool InsertContactInquiry(Contactusdto model)
        {
            string query = "INSERT INTO dbo.ContactInquiries (Name, Email, Phone, Subject, Message, Status, CreatedAt) VALUES (@Name, @Email, @Phone, @Subject, @Message, 'New', GETDATE())";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Name", model.Name ?? ""),
                new SqlParameter("@Email", model.Email ?? ""),
                new SqlParameter("@Phone", (object?)model.Phone ?? DBNull.Value),
                new SqlParameter("@Subject", (object?)model.Subject ?? "General Inquiry"),
                new SqlParameter("@Message", model.Message ?? "")
            };
            return _dbHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        public bool HasRecentInquiry(string email, string? phone, int hoursWindow, out DateTime? lastInquiryTime)
        {
            lastInquiryTime = null;
            if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
                return false;

            string cleanEmail = (email ?? "").Trim().ToLowerInvariant();
            string cleanPhone = RamJankiNGO.Services.AntiSpamValidator.SanitizePhone(phone ?? "");

            string query = @"
                SELECT TOP 1 CreatedAt 
                FROM dbo.ContactInquiries 
                WHERE (
                    LOWER(LTRIM(RTRIM(Email))) = @Email 
                    OR (@CleanPhone <> '' AND REPLACE(REPLACE(REPLACE(ISNULL(Phone, ''), ' ', ''), '-', ''), '+91', '') = @CleanPhone)
                )
                AND CreatedAt >= DATEADD(HOUR, -@Hours, GETDATE())
                ORDER BY CreatedAt DESC";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Email", cleanEmail),
                new SqlParameter("@CleanPhone", cleanPhone),
                new SqlParameter("@Hours", hoursWindow)
            };

            DataTable dt = _dbHelper.ExecDataTable(query, parameters);
            if (dt != null && dt.Rows.Count > 0 && dt.Rows[0]["CreatedAt"] != DBNull.Value)
            {
                lastInquiryTime = Convert.ToDateTime(dt.Rows[0]["CreatedAt"]);
                return true;
            }

            return false;
        }

        public DataTable GetContactInquiries()
        {
            return _dbHelper.ExecAdaptorDataTable("SELECT InquiryId, Name, Email, Phone, Subject, Message, Status, CreatedAt FROM dbo.ContactInquiries ORDER BY InquiryId DESC");
        }

        public bool DeleteContactInquiry(int inquiryId)
        {
            string query = "DELETE FROM dbo.ContactInquiries WHERE InquiryId = @InquiryId";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@InquiryId", inquiryId)
            };
            return _dbHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        public bool InsertVolunteer(VolunteerDto model)
        {
            string query = "INSERT INTO dbo.VolunteerRegistrations (Name, Email, Phone, Address, Occupation, AreaOfInterest, Message, Status, CreatedAt) VALUES (@Name, @Email, @Phone, @Address, @Occupation, @AreaOfInterest, @Message, 'Pending', GETDATE())";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Name", model.Name ?? ""),
                new SqlParameter("@Email", (object?)model.Email ?? DBNull.Value),
                new SqlParameter("@Phone", model.Phone ?? ""),
                new SqlParameter("@Address", (object?)model.Address ?? DBNull.Value),
                new SqlParameter("@Occupation", (object?)model.Occupation ?? DBNull.Value),
                new SqlParameter("@AreaOfInterest", (object?)model.AreaOfInterest ?? "General Seva"),
                new SqlParameter("@Message", (object?)model.Message ?? DBNull.Value)
            };
            return _dbHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        public bool HasRecentVolunteer(string email, string? phone, int hoursWindow, out DateTime? lastVolunteerTime)
        {
            lastVolunteerTime = null;
            if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
                return false;

            string cleanEmail = (email ?? "").Trim().ToLowerInvariant();
            string cleanPhone = RamJankiNGO.Services.AntiSpamValidator.SanitizePhone(phone ?? "");

            string query = @"
                SELECT TOP 1 CreatedAt 
                FROM dbo.VolunteerRegistrations 
                WHERE (
                    LOWER(LTRIM(RTRIM(Email))) = @Email 
                    OR (@CleanPhone <> '' AND REPLACE(REPLACE(REPLACE(ISNULL(Phone, ''), ' ', ''), '-', ''), '+91', '') = @CleanPhone)
                )
                AND CreatedAt >= DATEADD(HOUR, -@Hours, GETDATE())
                ORDER BY CreatedAt DESC";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Email", cleanEmail),
                new SqlParameter("@CleanPhone", cleanPhone),
                new SqlParameter("@Hours", hoursWindow)
            };

            DataTable dt = _dbHelper.ExecDataTable(query, parameters);
            if (dt != null && dt.Rows.Count > 0 && dt.Rows[0]["CreatedAt"] != DBNull.Value)
            {
                lastVolunteerTime = Convert.ToDateTime(dt.Rows[0]["CreatedAt"]);
                return true;
            }

            return false;
        }

        public DataTable GetVolunteers()
        {
            return _dbHelper.ExecAdaptorDataTable("SELECT VolunteerId, Name, Email, Phone, Address, Occupation, AreaOfInterest, Message, Status, CreatedAt FROM dbo.VolunteerRegistrations ORDER BY VolunteerId DESC");
        }
        #endregion

        #region Blogs Comments
        public DataTable GetBlogCommentDetailsList()
        {
            return _dbHelper.ExecAdaptorDataTable("SELECT c.CommentId, c.BlogId, c.UserName AS Name, c.Email, c.Comment AS Message, CAST(CASE WHEN c.IsApproved = 1 THEN 0 ELSE 1 END AS BIT) AS IsDeleted, ISNULL(b.Title, 'General') AS BlogTittle FROM BlogComments c LEFT JOIN Blogs b ON c.BlogId = b.BlogId ORDER BY c.CommentId DESC");
        }

        public bool DeleteComments(int id)
        {
            return _dbHelper.ExecuteNonQuery($"DELETE FROM BlogComments WHERE CommentId = {id}") > 0;
        }

        public bool BlogCommentHideshow(int commentId, bool isChecked)
        {
            int val = isChecked ? 0 : 1;
            return _dbHelper.ExecuteNonQuery($"UPDATE BlogComments SET IsApproved = {val} WHERE CommentId = {commentId}") > 0;
        }
        #endregion

        #region Settings
        public SettingsDto GetSettings()
        {
            SettingsDto settings = new SettingsDto();
            DataTable dt = _dbHelper.ExecAdaptorDataTable("SELECT SettingKey, SettingValue FROM Settings");
            foreach (DataRow row in dt.Rows)
            {
                string key = row["SettingKey"].ToString();
                string val = row["SettingValue"]?.ToString() ?? "";

                if (key == "OrgName") settings.OrgName = val;
                else if (key == "TrustHindiName") settings.TrustHindiName = val;
                else if (key == "TrustLegalName") settings.TrustLegalName = val;
                else if (key == "OrgTagline") settings.OrgTagline = val;
                else if (key == "OrgEmail") settings.OrgEmail = val;
                else if (key == "OrgPhone") settings.OrgPhone = val;
                else if (key == "OrgPhoneSecondary") settings.OrgPhoneSecondary = val;
                else if (key == "OrgAddress") settings.OrgAddress = val;
                else if (key == "OrgAddressHindi") settings.OrgAddressHindi = val;
                else if (key == "DarpanId") settings.DarpanId = val;
                else if (key == "TrustRegNo") settings.TrustRegNo = val;
                else if (key == "PanNumber") settings.PanNumber = val;
                else if (key == "Section80G") settings.Section80G = val;
                else if (key == "Section12A") settings.Section12A = val;
                else if (key == "CSRNumber") settings.CSRNumber = val;
                else if (key == "GstNumber") settings.GstNumber = val;
                else if (key == "BankName") settings.BankName = val;
                else if (key == "AccountName") settings.AccountName = val;
                else if (key == "AccountNumber") settings.AccountNumber = val;
                else if (key == "IfscCode") settings.IfscCode = val;
                else if (key == "UpiId") settings.UpiId = val;
                else if (key == "ChairmanName") settings.ChairmanName = val;
                else if (key == "ChairmanPhone") settings.ChairmanPhone = val;
                else if (key == "ReceiptPrefix") settings.ReceiptPrefix = val;
                else if (key == "ReceiptNextNumber") settings.ReceiptNextNumber = val;
                else if (key == "MemberPrefix") settings.MemberPrefix = val;
                else if (key == "MemberNextNumber") settings.MemberNextNumber = val;
                else if (key == "Currency") settings.Currency = val;
                else if (key == "RazorpayKeyId") settings.RazorpayKeyId = val;
                else if (key == "RazorpayKeySecret") settings.RazorpayKeySecret = val;
                else if (key == "EnableLiveGateway") settings.EnableLiveGateway = val;
            }
            // Override from appsettings.json if defined
            string? appKeyId = _configuration["RazorpaySettings:KeyId"];
            if (!string.IsNullOrEmpty(appKeyId)) settings.RazorpayKeyId = appKeyId;

            string? appKeySecret = _configuration["RazorpaySettings:KeySecret"];
            if (!string.IsNullOrEmpty(appKeySecret)) settings.RazorpayKeySecret = appKeySecret;

            return settings;
        }

        public void SaveSettings(SettingsDto model)
        {
            UpdateSetting("OrgName", model.OrgName);
            UpdateSetting("OrgTagline", model.OrgTagline);
            UpdateSetting("OrgEmail", model.OrgEmail);
            UpdateSetting("OrgPhone", model.OrgPhone);
            UpdateSetting("OrgAddress", model.OrgAddress);
            UpdateSetting("ReceiptPrefix", model.ReceiptPrefix);
            UpdateSetting("ReceiptNextNumber", model.ReceiptNextNumber);
            UpdateSetting("MemberPrefix", model.MemberPrefix);
            UpdateSetting("MemberNextNumber", model.MemberNextNumber);
            UpdateSetting("Currency", model.Currency);
            UpdateSetting("RazorpayKeyId", model.RazorpayKeyId);
            UpdateSetting("RazorpayKeySecret", model.RazorpayKeySecret);
            UpdateSetting("EnableLiveGateway", model.EnableLiveGateway);
        }

        private void UpdateSetting(string key, string val)
        {
            _dbHelper.ExecuteNonQuery($"UPDATE Settings SET SettingValue = N'{(val ?? "").Replace("'", "''")}' WHERE SettingKey = '{key}'");
        }
        #endregion
    }
}
