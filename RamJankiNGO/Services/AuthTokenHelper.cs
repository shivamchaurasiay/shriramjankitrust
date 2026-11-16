using Microsoft.AspNetCore.Http;
using System;
using System.Security.Cryptography;
using System.Text;

namespace RamJankiNGO.Services
{
    public static class AuthTokenHelper
    {
        private const string SecretKey = "RamJanki_NGO_Ayodhya_Secret_Key_Auth_2026_Live_Persistence";
        public const string AdminCookieName = "SJT_Admin_Auth";
        public const string MemberCookieName = "SJT_Member_Auth";

        private static string ComputeHmac(string data)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SecretKey));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash);
        }

        // ==========================================
        // ADMIN AUTHENTICATION (30 Days = 1 Month)
        // ==========================================
        public static void SetAdminCookie(HttpResponse response, string userId, string roleId, string userName)
        {
            long expiresEpoch = DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeSeconds();
            string payload = $"{userId}|{roleId}|{userName}|{expiresEpoch}";
            string sig = ComputeHmac(payload);
            string token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{payload}::{sig}"));

            var options = new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = false
            };
            response.Cookies.Append(AdminCookieName, token, options);
        }

        public static bool ValidateAdminCookie(string? token, out string userId, out string roleId, out string userName)
        {
            userId = "";
            roleId = "";
            userName = "";
            if (string.IsNullOrWhiteSpace(token)) return false;

            try
            {
                string raw = Encoding.UTF8.GetString(Convert.FromBase64String(token));
                string[] parts = raw.Split(new[] { "::" }, StringSplitOptions.None);
                if (parts.Length != 2) return false;

                string payload = parts[0];
                string sig = parts[1];
                if (ComputeHmac(payload) != sig) return false;

                string[] fields = payload.Split('|');
                if (fields.Length != 4) return false;

                userId = fields[0];
                roleId = fields[1];
                userName = fields[2];
                long expiresEpoch = Convert.ToInt64(fields[3]);

                if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiresEpoch) return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void ClearAdminCookie(HttpResponse response)
        {
            response.Cookies.Delete(AdminCookieName);
        }

        // ==========================================
        // MEMBER / USER AUTHENTICATION (7 Days = 1 Week)
        // ==========================================
        public static void SetMemberCookie(HttpResponse response, string memberUserId, string memberId, string memberName, string memberCode)
        {
            long expiresEpoch = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeSeconds();
            string payload = $"{memberUserId}|{memberId}|{memberName}|{memberCode}|{expiresEpoch}";
            string sig = ComputeHmac(payload);
            string token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{payload}::{sig}"));

            var options = new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(7),
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = false
            };
            response.Cookies.Append(MemberCookieName, token, options);
        }

        public static bool ValidateMemberCookie(string? token, out string memberUserId, out string memberId, out string memberName, out string memberCode)
        {
            memberUserId = "";
            memberId = "";
            memberName = "";
            memberCode = "";
            if (string.IsNullOrWhiteSpace(token)) return false;

            try
            {
                string raw = Encoding.UTF8.GetString(Convert.FromBase64String(token));
                string[] parts = raw.Split(new[] { "::" }, StringSplitOptions.None);
                if (parts.Length != 2) return false;

                string payload = parts[0];
                string sig = parts[1];
                if (ComputeHmac(payload) != sig) return false;

                string[] fields = payload.Split('|');
                if (fields.Length != 5) return false;

                memberUserId = fields[0];
                memberId = fields[1];
                memberName = fields[2];
                memberCode = fields[3];
                long expiresEpoch = Convert.ToInt64(fields[4]);

                if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiresEpoch) return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void ClearMemberCookie(HttpResponse response)
        {
            response.Cookies.Delete(MemberCookieName);
        }
    }
}
