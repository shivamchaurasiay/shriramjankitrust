using RamJankiNGO.Data;
using RamJankiNGO.Models;
using RamJankiNGO.Services;

namespace RamJankiNGO
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Register dbHelper, Datalayer and EmailService matching AuraWeb architecture
            builder.Services.AddTransient<dbHelper>();
            builder.Services.AddTransient<Datalayer>();
            builder.Services.AddTransient<IEmailService, EmailService>();

            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromDays(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.MaxAge = TimeSpan.FromDays(30);
            });

            builder.Services.AddControllersWithViews()
                .AddRazorRuntimeCompilation();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseSession();
            app.UseAuthorization();

            // Persistent Login Auto-Hydration Middleware (Admin: 1 Month, Member: 1 Week)
            app.Use(async (context, next) =>
            {
                // 1. Admin Session Restoration from 1-Month Persistent Cookie
                if (string.IsNullOrEmpty(context.Session.GetString("UserId")))
                {
                    var adminCookie = context.Request.Cookies[AuthTokenHelper.AdminCookieName];
                    if (AuthTokenHelper.ValidateAdminCookie(adminCookie, out string aUserId, out string aRoleId, out string aUserName))
                    {
                        context.Session.SetString("UserId", aUserId);
                        context.Session.SetString("RoleId", aRoleId);
                        context.Session.SetString("UserName", aUserName);
                    }
                }

                // 2. Member/User Session Restoration from 1-Week Persistent Cookie
                if (string.IsNullOrEmpty(context.Session.GetString("MemberUserId")))
                {
                    var memberCookie = context.Request.Cookies[AuthTokenHelper.MemberCookieName];
                    if (AuthTokenHelper.ValidateMemberCookie(memberCookie, out string mUserId, out string mId, out string mName, out string mCode))
                    {
                        context.Session.SetString("MemberUserId", mUserId);
                        context.Session.SetString("MemberId", mId);
                        context.Session.SetString("MemberName", mName);
                        context.Session.SetString("MemberCode", mCode);
                    }
                }

                await next();
            });

            // Security Guard: Prevent any direct unauthorized access to Admin pages
            app.Use(async (context, next) =>
            {
                var path = context.Request.Path.Value ?? "";
                if (path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase))
                {
                    // Allow public admin endpoints: Login, ForgotPassword, and static/content if any
                    bool isPublicAdminRoute = path.Equals("/Admin/Account/Login", StringComparison.OrdinalIgnoreCase) ||
                                             path.Equals("/Admin/Account/ForgotPassword", StringComparison.OrdinalIgnoreCase) ||
                                             path.Equals("/Admin/Account/ResetPassword", StringComparison.OrdinalIgnoreCase);

                    var userId = context.Session.GetString("UserId");
                    if (!isPublicAdminRoute && string.IsNullOrEmpty(userId))
                    {
                        context.Response.Redirect("/Admin/Account/Login");
                        return;
                    }
                }
                await next();
            });

            app.UseStatusCodePages();

            // Area route for Admin
            app.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

            // Default route for Frontend
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
