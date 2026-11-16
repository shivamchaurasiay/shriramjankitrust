using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace RamJankiNGO.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody);
        Task<bool> SendToMultipleAsync(string commaSeparatedEmails, string subject, string htmlBody);
        Task SendMemberWelcomeEmailAsync(string memberEmail, string memberName, string memberCode, string loginUrl);
        Task SendDonationReceiptEmailAsync(string donorEmail, string donorName, string receiptNumber, decimal amount, string campaignTitle, string receiptUrl);
        Task SendAdminDonationAlertAsync(string donorName, string donorPhone, decimal amount, string receiptNumber, string campaignTitle);
        Task SendAdminInquiryAlertAsync(string name, string email, string phone, string message, string subject = "General Inquiry");
        Task SendAdminVolunteerAlertAsync(string name, string email, string phone, string interest);
        Task SendPasswordResetOtpEmailAsync(string toEmail, string userName, string otp);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(toEmail)) return false;

            try
            {
                string smtpServer = _config["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
                int port = Convert.ToInt32(_config["EmailSettings:Port"] ?? "587");
                string senderName = _config["EmailSettings:SenderName"] ?? "Shriram Janki Trust";
                string senderEmail = _config["EmailSettings:SenderEmail"] ?? "itdeskmindforgetechnology@gmail.com";
                string password = _config["EmailSettings:Password"] ?? "cepapxczfdelcrgw";
                bool enableSsl = Convert.ToBoolean(_config["EmailSettings:EnableSsl"] ?? "true");

                // Strictly strip any square brackets from email subject line
                string cleanSubject = (subject ?? "").Replace("[", "").Replace("]", "").Trim();

                using (var client = new SmtpClient(smtpServer, port))
                {
                    client.EnableSsl = enableSsl;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(senderEmail, password);
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;

                    using (var mail = new MailMessage())
                    {
                        mail.From = new MailAddress(senderEmail, senderName);
                        mail.To.Add(toEmail);
                        mail.Subject = cleanSubject;

                        // Locate email logo for inline CID embedding
                        string[] candidatePaths = new[]
                        {
                            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets", "img", "email-logo.png"),
                            Path.Combine(AppContext.BaseDirectory, "wwwroot", "assets", "img", "email-logo.png"),
                            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "wwwroot", "assets", "img", "email-logo.png"),
                            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets", "img", "logo.png"),
                            Path.Combine(AppContext.BaseDirectory, "wwwroot", "assets", "img", "logo.png")
                        };
                        string logoPath = candidatePaths.FirstOrDefault(p => !string.IsNullOrEmpty(p) && File.Exists(p)) ?? "";

                        if (!string.IsNullOrEmpty(logoPath) && htmlBody.Contains("cid:trustlogo"))
                        {
                            var av = AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html");
                            var lr = new LinkedResource(logoPath, "image/png")
                            {
                                ContentId = "trustlogo",
                                TransferEncoding = System.Net.Mime.TransferEncoding.Base64
                            };
                            av.LinkedResources.Add(lr);
                            mail.AlternateViews.Add(av);
                        }
                        else
                        {
                            mail.Body = htmlBody;
                            mail.IsBodyHtml = true;
                        }

                        await client.SendMailAsync(mail);
                        _logger.LogInformation($"Email sent successfully to {toEmail}");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {toEmail}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendToMultipleAsync(string commaSeparatedEmails, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(commaSeparatedEmails)) return false;

            var emails = commaSeparatedEmails.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            bool anySuccess = false;
            foreach (var email in emails)
            {
                string trimmed = email.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    bool res = await SendEmailAsync(trimmed, subject, htmlBody);
                    if (res) anySuccess = true;
                }
            }
            return anySuccess;
        }

        public async Task SendMemberWelcomeEmailAsync(string memberEmail, string memberName, string memberCode, string loginUrl)
        {
            string safeMemberName = AntiSpamValidator.SanitizeSubject(memberName);
            string subject = $"Welcome to Shriram Janki Trust - Member ID {memberCode}";
            string encodedName = WebUtility.HtmlEncode(memberName ?? "");
            string encodedCode = WebUtility.HtmlEncode(memberCode ?? "");
            string encodedEmail = WebUtility.HtmlEncode(memberEmail ?? "");

            string body = $@"
<!DOCTYPE html>
<html lang='en'>
<head><meta charset='UTF-8'><meta name='viewport' content='width=device-width, initial-scale=1.0'></head>
<body style='margin:0; padding:20px 0; background-color:#f4f6f9; font-family:""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;'>
  <div style='max-width:620px; margin:0 auto; background:#ffffff; border-radius:12px; overflow:hidden; box-shadow:0 4px 20px rgba(0,0,0,0.08); border:1px solid #ffd8a8;'>
    
    <!-- Top Branded Header with Logo -->
    <div style='background:linear-gradient(135deg, #d32f2f 0%, #b71c1c 100%); padding:28px 24px; text-align:center; color:#ffffff;'>
      <div style='margin-bottom:12px;'>
        <img src='cid:trustlogo' alt='Shriram Janki Trust Logo' width='70' height='70' style='display:inline-block; width:70px; height:70px; border-radius:50%; background-color:#ffffff; padding:4px; box-shadow:0 4px 12px rgba(0,0,0,0.25); border:2px solid #ffd8a8; vertical-align:middle;' />
      </div>
      <div style='font-size:22px; font-weight:700; letter-spacing:0.5px; margin-bottom:4px;'>श्रीराम जानकी मंदिर सेवा ट्रस्ट</div>
      <div style='font-size:12px; text-transform:uppercase; letter-spacing:1.5px; opacity:0.95; margin-bottom:14px;'>SHRI RAM JANKI MANDIR SEWA TRUST &bull; AYODHYA DHAM, UP</div>
      <div style='display:inline-block; background:rgba(255,255,255,0.2); border:1px solid rgba(255,255,255,0.4); padding:6px 20px; border-radius:20px; font-size:12px; font-weight:700; text-transform:uppercase; letter-spacing:1px;'>
        &#127881; Member Registration Confirmation
      </div>
    </div>

    <!-- Body Container -->
    <div style='padding:28px 24px;'>
      <p style='font-size:15px; color:#2d3748;'>Dear <strong>{encodedName}</strong>,</p>
      <p style='font-size:14px; color:#4a5568; line-height:1.6;'>Namaste and welcome! You have been successfully registered as a member with <strong>Shriram Janki Mandir Sewa Trust</strong>. Your devotion and partnership strengthen our humanitarian and spiritual missions.</p>
      
      <div style='background:#f8f9fa; border-left:4px solid #d32f2f; border-radius:0 8px 8px 0; padding:18px; margin:22px 0;'>
        <div style='font-size:14px; margin-bottom:8px;'><strong>Member ID:</strong> <span style='font-size:16px; color:#d32f2f; font-weight:bold;'>{encodedCode}</span></div>
        <div style='font-size:14px; margin-bottom:8px;'><strong>Registered Email:</strong> {encodedEmail}</div>
        <div style='font-size:14px;'><strong>Default Password:</strong> <code style='background:#fff; padding:2px 8px; border:1px solid #ddd; border-radius:4px;'>member123</code> <span style='color:#718096; font-size:12px;'>(Change upon first login)</span></div>
      </div>

      <p style='font-size:14px; color:#4a5568;'>You can now access your Member Portal to view your seva donation history and download official 80G tax receipts:</p>
      
      <div style='text-align:center; margin:26px 0 16px;'>
        <a href='{loginUrl}' style='display:inline-block; background:linear-gradient(135deg, #d32f2f 0%, #b71c1c 100%); color:#ffffff; font-size:14px; font-weight:700; text-decoration:none; padding:12px 28px; border-radius:6px; box-shadow:0 3px 10px rgba(211,47,47,0.3);'>Login to Member Portal</a>
      </div>
    </div>

    <!-- Trust Footer -->
    <div style='background:#fafafa; border-top:1px solid #edf2f7; padding:18px 24px; text-align:center; font-size:12px; color:#718096; line-height:1.6;'>
      <div style='font-weight:600; color:#4a5568; margin-bottom:3px;'>श्रीराम जानकी मंदिर सेवा ट्रस्ट (SHRI RAM JANKI MANDIR SEWA TRUST)</div>
      <div>करनपुर, स्टेशन रोड, बभनान - गोण्डा, उ.प्र., भारत - 271313 (निकट राम मंदिर, अयोध्या धाम) &bull; Phone: +91 9918953452, +91 9250931408</div>
      <div style='margin-top:5px; font-size:11px; color:#a0aec0;'>
        Darpan ID: UP/2026/0956133 &bull; Reg: 15/B4/G20/2025 &bull; 80G &amp; 12A Registered
      </div>
    </div>

  </div>
 </body>
</html>";

            if (!string.IsNullOrWhiteSpace(memberEmail))
            {
                await SendEmailAsync(memberEmail, subject, body);
            }

            // Also alert admin
            string adminEmails = _config["EmailSettings:AdminNotificationEmails"] ?? "";
            string adminSubject = $"Admin Alert: New Member Registered - {safeMemberName} ({memberCode})";
            await SendToMultipleAsync(adminEmails, adminSubject, body);
        }

        public async Task SendDonationReceiptEmailAsync(string donorEmail, string donorName, string receiptNumber, decimal amount, string campaignTitle, string receiptUrl)
        {
            string safeDonorName = AntiSpamValidator.SanitizeSubject(donorName);
            string subject = $"Thank You for Your Donation - Receipt #{receiptNumber} | Shriram Janki Trust";
            string encodedDonor = WebUtility.HtmlEncode(donorName ?? "");
            string encodedCause = WebUtility.HtmlEncode(campaignTitle ?? "");
            string istTime = DateTime.UtcNow.AddMinutes(330).ToString("dd MMM yyyy, hh:mm tt") + " IST";

            string body = $@"
<!DOCTYPE html>
<html lang='en'>
<head><meta charset='UTF-8'><meta name='viewport' content='width=device-width, initial-scale=1.0'></head>
<body style='margin:0; padding:20px 0; background-color:#f4f6f9; font-family:""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;'>
  <div style='max-width:620px; margin:0 auto; background:#ffffff; border-radius:12px; overflow:hidden; box-shadow:0 4px 20px rgba(0,0,0,0.08); border:1px solid #ffd8a8;'>
    
    <!-- Top Branded Header with Logo -->
    <div style='background:linear-gradient(135deg, #1b5e20 0%, #2e7d32 100%); padding:28px 24px; text-align:center; color:#ffffff;'>
      <div style='margin-bottom:12px;'>
        <img src='cid:trustlogo' alt='Shriram Janki Trust Logo' width='70' height='70' style='display:inline-block; width:70px; height:70px; border-radius:50%; background-color:#ffffff; padding:4px; box-shadow:0 4px 12px rgba(0,0,0,0.25); border:2px solid #ffd8a8; vertical-align:middle;' />
      </div>
      <div style='font-size:22px; font-weight:700; letter-spacing:0.5px; margin-bottom:4px;'>श्रीराम जानकी मंदिर सेवा ट्रस्ट</div>
      <div style='font-size:12px; text-transform:uppercase; letter-spacing:1.5px; opacity:0.95; margin-bottom:14px;'>SHRI RAM JANKI MANDIR SEWA TRUST &bull; AYODHYA DHAM, UP</div>
      <div style='display:inline-block; background:rgba(255,255,255,0.2); border:1px solid rgba(255,255,255,0.4); padding:6px 20px; border-radius:20px; font-size:12px; font-weight:700; text-transform:uppercase; letter-spacing:1px;'>
        &#128176; Official Donation Receipt
      </div>
    </div>

    <!-- Body Container -->
    <div style='padding:28px 24px;'>
      <p style='font-size:15px; color:#2d3748;'>Dear <strong>{encodedDonor}</strong>,</p>
      <p style='font-size:14px; color:#4a5568; line-height:1.6;'>Thank you deeply for your generous contribution of <strong>₹{amount:N2}</strong> towards <strong>{encodedCause}</strong>. Your seva supports healthcare, education, annakshetra, and gaushala development for the needy.</p>
      
      <!-- Receipt Box -->
      <div style='background:#f1f8e9; border:2px dashed #4caf50; border-radius:8px; padding:20px; margin:22px 0; text-align:center;'>
        <div style='font-size:12px; color:#558b2f; font-weight:700; text-transform:uppercase; letter-spacing:1px;'>AMOUNT CONTRIBUTED</div>
        <div style='font-size:32px; font-weight:800; color:#2e7d32; margin:6px 0;'>₹{amount:N2}</div>
        <div style='font-size:13px; color:#33691e;'>Receipt Number: <strong>{receiptNumber}</strong> &bull; Date: {istTime}</div>
        <div style='margin-top:8px;'><span style='display:inline-block; background:#4caf50; color:#fff; font-size:11px; font-weight:700; padding:3px 12px; border-radius:12px;'>PAYMENT CONFIRMED</span></div>
      </div>

      <div style='text-align:center; margin:26px 0 16px;'>
        <a href='{receiptUrl}' style='display:inline-block; background:linear-gradient(135deg, #1b5e20 0%, #2e7d32 100%); color:#ffffff; font-size:14px; font-weight:700; text-decoration:none; padding:12px 28px; border-radius:6px; box-shadow:0 3px 10px rgba(46,125,50,0.3);'>View &amp; Download Official Receipt PDF</a>
      </div>
      
      <p style='color:#718096; font-size:12px; text-align:center;'>Donations are eligible for tax deduction under Section 80G of the Income Tax Act.</p>
    </div>

    <!-- Trust Footer -->
    <div style='background:#fafafa; border-top:1px solid #edf2f7; padding:18px 24px; text-align:center; font-size:12px; color:#718096; line-height:1.6;'>
      <div style='font-weight:600; color:#4a5568; margin-bottom:3px;'>श्रीराम जानकी मंदिर सेवा ट्रस्ट (SHRI RAM JANKI MANDIR SEWA TRUST)</div>
      <div>करनपुर, स्टेशन रोड, बभनान - गोण्डा, उ.प्र., भारत - 271313 (निकट राम मंदिर, अयोध्या धाम) &bull; Phone: +91 9918953452, +91 9250931408</div>
      <div style='margin-top:5px; font-size:11px; color:#a0aec0;'>
        Darpan ID: UP/2026/0956133 &bull; Reg: 15/B4/G20/2025 &bull; 80G &amp; 12A Registered
      </div>
    </div>

  </div>
 </body>
</html>";

            if (!string.IsNullOrWhiteSpace(donorEmail))
            {
                await SendEmailAsync(donorEmail, subject, body);
            }
        }

        public async Task SendAdminDonationAlertAsync(string donorName, string donorPhone, decimal amount, string receiptNumber, string campaignTitle)
        {
            string adminEmails = _config["EmailSettings:AdminNotificationEmails"] ?? "";
            string safeDonorName = AntiSpamValidator.SanitizeSubject(donorName);
            string subject = $"Donation Confirmed: ₹{amount:N0} from {safeDonorName} - Receipt #{receiptNumber}";
            string istTime = DateTime.UtcNow.AddMinutes(330).ToString("dd MMM yyyy, hh:mm tt") + " IST";

            string body = $@"
<!DOCTYPE html>
<html lang='en'>
<head><meta charset='UTF-8'><meta name='viewport' content='width=device-width, initial-scale=1.0'></head>
<body style='margin:0; padding:20px 0; background-color:#f4f6f9; font-family:""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;'>
  <div style='max-width:620px; margin:0 auto; background:#ffffff; border-radius:12px; overflow:hidden; box-shadow:0 4px 20px rgba(0,0,0,0.08); border:1px solid #ffd8a8;'>
    
    <!-- Top Branded Header with Logo -->
    <div style='background:linear-gradient(135deg, #1b5e20 0%, #2e7d32 100%); padding:28px 24px; text-align:center; color:#ffffff;'>
      <div style='margin-bottom:12px;'>
        <img src='cid:trustlogo' alt='Shriram Janki Trust Logo' width='70' height='70' style='display:inline-block; width:70px; height:70px; border-radius:50%; background-color:#ffffff; padding:4px; box-shadow:0 4px 12px rgba(0,0,0,0.25); border:2px solid #ffd8a8; vertical-align:middle;' />
      </div>
      <div style='font-size:22px; font-weight:700; letter-spacing:0.5px; margin-bottom:4px;'>श्रीराम जानकी मंदिर सेवा ट्रस्ट</div>
      <div style='font-size:12px; text-transform:uppercase; letter-spacing:1.5px; opacity:0.95; margin-bottom:14px;'>SHRI RAM JANKI MANDIR SEWA TRUST &bull; AYODHYA DHAM, UP</div>
      <div style='display:inline-block; background:rgba(255,255,255,0.2); border:1px solid rgba(255,255,255,0.4); padding:6px 20px; border-radius:20px; font-size:12px; font-weight:700; text-transform:uppercase; letter-spacing:1px;'>
        &#128176; Online Donation Confirmed
      </div>
    </div>

    <div style='padding:28px 24px;'>
      <div style='background:#f1f8e9; border:2px dashed #4caf50; border-radius:8px; padding:18px; margin-bottom:22px; text-align:center;'>
        <div style='font-size:12px; color:#558b2f; font-weight:700; text-transform:uppercase;'>AMOUNT RECEIVED</div>
        <div style='font-size:30px; font-weight:800; color:#2e7d32; margin:4px 0;'>₹{amount:N2}</div>
        <div style='font-size:12px; color:#689f38;'>Receipt Number: <strong>{receiptNumber}</strong></div>
      </div>
      <table style='width:100%; border-collapse:collapse; margin-bottom:15px; font-size:14px;'>
        <tr><td style='padding:10px; color:#718096; width:35%; border-bottom:1px solid #edf2f7;'>Donor:</td><td style='padding:10px; font-weight:bold; color:#1a202c; border-bottom:1px solid #edf2f7;'>{WebUtility.HtmlEncode(donorName)}</td></tr>
        <tr><td style='padding:10px; color:#718096; border-bottom:1px solid #edf2f7;'>Mobile:</td><td style='padding:10px; color:#1a202c; border-bottom:1px solid #edf2f7;'>{WebUtility.HtmlEncode(donorPhone)}</td></tr>
        <tr><td style='padding:10px; color:#718096; border-bottom:1px solid #edf2f7;'>Cause:</td><td style='padding:10px; color:#2e7d32; font-weight:600; border-bottom:1px solid #edf2f7;'>{WebUtility.HtmlEncode(campaignTitle)}</td></tr>
        <tr><td style='padding:10px; color:#718096;'>Time:</td><td style='padding:10px; color:#4a5568;'>{istTime}</td></tr>
      </table>
    </div>

    <!-- Trust Footer -->
    <div style='background:#fafafa; border-top:1px solid #edf2f7; padding:18px 24px; text-align:center; font-size:12px; color:#718096; line-height:1.6;'>
      <div style='font-weight:600; color:#4a5568; margin-bottom:3px;'>श्रीराम जानकी मंदिर सेवा ट्रस्ट (SHRI RAM JANKI MANDIR SEWA TRUST)</div>
      <div>करनपुर, स्टेशन रोड, बभनान - गोण्डा, उ.प्र., भारत - 271313 (निकट राम मंदिर, अयोध्या धाम) &bull; Phone: +91 9918953452, +91 9250931408</div>
      <div style='margin-top:5px; font-size:11px; color:#a0aec0;'>
        Darpan ID: UP/2026/0956133 &bull; Reg: 15/B4/G20/2025 &bull; 80G &amp; 12A Registered
      </div>
    </div>

  </div>
 </body>
</html>";

            await SendToMultipleAsync(adminEmails, subject, body);
        }

        public async Task SendAdminInquiryAlertAsync(string name, string email, string phone, string message, string subject = "General Inquiry")
        {
            string adminEmails = _config["EmailSettings:AdminNotificationEmails"] ?? "";
            
            // Sanitize name for email subject line (strictly letters, no linebreaks, max 40 chars)
            string safeName = AntiSpamValidator.SanitizeSubject(name);
            if (string.IsNullOrWhiteSpace(safeName)) safeName = "Website Visitor";
            if (safeName.Length > 40) safeName = safeName.Substring(0, 40);

            // Clean subject without brackets
            string emailSubject = $"New Inquiry: Shriram Janki Trust - {safeName}";

            // Encoded safe values for HTML template
            string encodedName = WebUtility.HtmlEncode(name ?? "");
            string encodedEmail = WebUtility.HtmlEncode(email ?? "");
            string encodedPhone = WebUtility.HtmlEncode(phone ?? "");
            string cleanPhone = AntiSpamValidator.SanitizePhone(phone ?? "");
            string encodedInquirySubject = WebUtility.HtmlEncode(subject ?? "General Inquiry");
            string encodedMessage = WebUtility.HtmlEncode(message ?? "").Replace("\n", "<br/>");
            string istTime = DateTime.UtcNow.AddMinutes(330).ToString("dd MMM yyyy, hh:mm tt") + " IST";

            string phoneDisplay = !string.IsNullOrWhiteSpace(encodedPhone) 
                ? $"<a href='tel:{cleanPhone}' style='color:#d32f2f; text-decoration:none; font-weight:600;'>+91 {cleanPhone}</a>" 
                : "<span style='color:#a0aec0;'>Not Provided</span>";

            string callButtonHtml = !string.IsNullOrWhiteSpace(cleanPhone)
                ? $"<a href='tel:{cleanPhone}' style='display:inline-block; background-color:#2e7d32; color:#ffffff; font-size:13px; font-weight:700; text-decoration:none; padding:11px 22px; border-radius:6px; margin:4px 6px; box-shadow:0 2px 6px rgba(46,125,50,0.3);'>&#128222; Call {cleanPhone}</a>"
                : "";

            string body = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<meta name='viewport' content='width=device-width, initial-scale=1.0'>
<title>New Website Inquiry - Shriram Janki Trust</title>
</head>
<body style='margin:0; padding:20px 0; background-color:#f4f6f9; font-family:""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; -webkit-font-smoothing:antialiased;'>
  <div style='max-width:620px; margin:0 auto; background:#ffffff; border-radius:12px; overflow:hidden; box-shadow:0 4px 20px rgba(0,0,0,0.08); border:1px solid #ffd8a8;'>
    
    <!-- Top Branded Header with Logo -->
    <div style='background:linear-gradient(135deg, #d32f2f 0%, #b71c1c 100%); padding:28px 24px; text-align:center; color:#ffffff;'>
      <div style='margin-bottom:12px;'>
        <img src='cid:trustlogo' alt='Shriram Janki Trust Logo' width='70' height='70' style='display:inline-block; width:70px; height:70px; border-radius:50%; background-color:#ffffff; padding:4px; box-shadow:0 4px 12px rgba(0,0,0,0.25); border:2px solid #ffd8a8; vertical-align:middle;' />
      </div>
      <div style='font-size:22px; font-weight:700; letter-spacing:0.5px; margin-bottom:4px; text-shadow:0 1px 2px rgba(0,0,0,0.2);'>श्रीराम जानकी मंदिर सेवा ट्रस्ट</div>
      <div style='font-size:12px; text-transform:uppercase; letter-spacing:1.5px; opacity:0.95; margin-bottom:14px;'>SHRI RAM JANKI MANDIR SEWA TRUST &bull; AYODHYA DHAM, UP</div>
      <div style='display:inline-block; background:rgba(255,255,255,0.2); border:1px solid rgba(255,255,255,0.4); padding:6px 20px; border-radius:20px; font-size:12px; font-weight:700; text-transform:uppercase; letter-spacing:1px;'>
        &#128233; New Public Inquiry Alert
      </div>
    </div>

    <!-- Body Container -->
    <div style='padding:28px 24px;'>
      
      <p style='margin:0 0 18px; font-size:15px; color:#2d3748; line-height:1.5;'>
        A verified inquiry has been submitted through the official trust contact form. Here are the sender details:
      </p>

      <!-- Details Table Card -->
      <table style='width:100%; border-collapse:separate; border-spacing:0; background:#fafafa; border:1px solid #edf2f7; border-radius:8px; margin-bottom:22px; overflow:hidden;'>
        <tbody>
          <tr>
            <td style='padding:12px 16px; width:34%; font-size:13px; font-weight:600; color:#718096; border-bottom:1px solid #edf2f7; background:#f7fafc;'>
              Sender Name
            </td>
            <td style='padding:12px 16px; font-size:14px; font-weight:700; color:#1a202c; border-bottom:1px solid #edf2f7;'>
              {encodedName}
            </td>
          </tr>
          <tr>
            <td style='padding:12px 16px; font-size:13px; font-weight:600; color:#718096; border-bottom:1px solid #edf2f7; background:#f7fafc;'>
              Email Address
            </td>
            <td style='padding:12px 16px; font-size:14px; color:#2b6cb0; border-bottom:1px solid #edf2f7;'>
              <a href='mailto:{encodedEmail}?subject=Re:%20Inquiry%20to%20Shriram%20Janki%20Trust' style='color:#2b6cb0; text-decoration:none; font-weight:600;'>{encodedEmail}</a>
            </td>
          </tr>
          <tr>
            <td style='padding:12px 16px; font-size:13px; font-weight:600; color:#718096; border-bottom:1px solid #edf2f7; background:#f7fafc;'>
              Mobile Number
            </td>
            <td style='padding:12px 16px; font-size:14px; color:#1a202c; border-bottom:1px solid #edf2f7;'>
              {phoneDisplay}
            </td>
          </tr>
          <tr>
            <td style='padding:12px 16px; font-size:13px; font-weight:600; color:#718096; border-bottom:1px solid #edf2f7; background:#f7fafc;'>
              Inquiry Subject
            </td>
            <td style='padding:12px 16px; font-size:14px; color:#4a5568; border-bottom:1px solid #edf2f7;'>
              {encodedInquirySubject}
            </td>
          </tr>
          <tr>
            <td style='padding:12px 16px; font-size:13px; font-weight:600; color:#718096; background:#f7fafc;'>
              Received At
            </td>
            <td style='padding:12px 16px; font-size:13px; color:#4a5568;'>
              {istTime}
            </td>
          </tr>
        </tbody>
      </table>

      <!-- Message Content -->
      <div style='margin-bottom:24px;'>
        <div style='font-size:13px; font-weight:700; color:#4a5568; text-transform:uppercase; letter-spacing:0.5px; margin-bottom:8px;'>
          &#128172; Inquiry Message
        </div>
        <div style='background:#fff8f6; border-left:4px solid #d32f2f; border-radius:0 8px 8px 0; padding:16px 18px; font-size:14px; line-height:1.6; color:#2d3748; word-break:break-word;'>
          {encodedMessage}
        </div>
      </div>

      <!-- Quick Action Buttons -->
      <div style='text-align:center; margin:24px 0 10px;'>
        <a href='mailto:{encodedEmail}?subject=Re:%20Inquiry%20to%20Shriram%20Janki%20Trust' style='display:inline-block; background-color:#d32f2f; color:#ffffff; font-size:13px; font-weight:700; text-decoration:none; padding:11px 22px; border-radius:6px; margin:4px 6px; box-shadow:0 2px 6px rgba(211,47,47,0.3);'>
          &#9993; Reply via Email
        </a>
        {callButtonHtml}
      </div>

    </div>

    <!-- Trust Footer -->
    <div style='background:#fafafa; border-top:1px solid #edf2f7; padding:18px 24px; text-align:center; font-size:12px; color:#718096; line-height:1.6;'>
      <div style='font-weight:600; color:#4a5568; margin-bottom:3px;'>श्रीराम जानकी मंदिर सेवा ट्रस्ट (SHRI RAM JANKI MANDIR SEWA TRUST)</div>
      <div>करनपुर, स्टेशन रोड, बभनान - गोण्डा, उ.प्र., भारत - 271313 (निकट राम मंदिर, अयोध्या धाम) &bull; Phone: +91 9918953452, +91 9250931408</div>
      <div style='margin-top:5px; font-size:11px; color:#a0aec0;'>
        Darpan ID: UP/2026/0956133 &bull; Reg: 15/B4/G20/2025 &bull; 80G &amp; 12A Registered
      </div>
    </div>

  </div>
</body>
</html>";

            await SendToMultipleAsync(adminEmails, emailSubject, body);
        }

        public async Task SendAdminVolunteerAlertAsync(string name, string email, string phone, string interest)
        {
            string adminEmails = _config["EmailSettings:AdminNotificationEmails"] ?? "";
            string safeName = AntiSpamValidator.SanitizeSubject(name);
            if (string.IsNullOrWhiteSpace(safeName)) safeName = "Volunteer Applicant";
            if (safeName.Length > 40) safeName = safeName.Substring(0, 40);

            // Clean subject without brackets
            string emailSubject = $"Volunteer Application: Shriram Janki Trust - {safeName}";

            string encodedName = WebUtility.HtmlEncode(name ?? "");
            string encodedEmail = WebUtility.HtmlEncode(email ?? "");
            string encodedPhone = WebUtility.HtmlEncode(phone ?? "");
            string cleanPhone = AntiSpamValidator.SanitizePhone(phone ?? "");
            string encodedInterest = WebUtility.HtmlEncode(interest ?? "General Seva");
            string istTime = DateTime.UtcNow.AddMinutes(330).ToString("dd MMM yyyy, hh:mm tt") + " IST";

            string body = $@"
<!DOCTYPE html>
<html lang='en'>
<head><meta charset='UTF-8'><meta name='viewport' content='width=device-width, initial-scale=1.0'></head>
<body style='margin:0; padding:20px 0; background-color:#f4f6f9; font-family:""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;'>
  <div style='max-width:620px; margin:0 auto; background:#ffffff; border-radius:12px; overflow:hidden; box-shadow:0 4px 20px rgba(0,0,0,0.08); border:1px solid #ffd8a8;'>
    
    <!-- Top Branded Header with Logo -->
    <div style='background:linear-gradient(135deg, #e65100 0%, #f57c00 100%); padding:28px 24px; text-align:center; color:#ffffff;'>
      <div style='margin-bottom:12px;'>
        <img src='cid:trustlogo' alt='Shriram Janki Trust Logo' width='70' height='70' style='display:inline-block; width:70px; height:70px; border-radius:50%; background-color:#ffffff; padding:4px; box-shadow:0 4px 12px rgba(0,0,0,0.25); border:2px solid #ffd8a8; vertical-align:middle;' />
      </div>
      <div style='font-size:22px; font-weight:700; letter-spacing:0.5px; margin-bottom:4px;'>श्रीराम जानकी मंदिर सेवा ट्रस्ट</div>
      <div style='font-size:12px; text-transform:uppercase; letter-spacing:1.5px; opacity:0.95; margin-bottom:14px;'>SHRI RAM JANKI MANDIR SEWA TRUST &bull; AYODHYA DHAM, UP</div>
      <div style='display:inline-block; background:rgba(255,255,255,0.2); border:1px solid rgba(255,255,255,0.4); padding:6px 20px; border-radius:20px; font-size:12px; font-weight:700; text-transform:uppercase; letter-spacing:1px;'>
        &#129309; New Volunteer Application
      </div>
    </div>

    <div style='padding:28px 24px;'>
      <p style='font-size:15px; color:#2d3748; margin-top:0;'>A dedicated devotee has submitted a volunteer seva application:</p>
      
      <table style='width:100%; border-collapse:collapse; margin-bottom:20px; font-size:14px;'>
        <tr><td style='padding:10px; color:#718096; width:35%; border-bottom:1px solid #edf2f7;'>Applicant Name:</td><td style='padding:10px; font-weight:bold; color:#1a202c; border-bottom:1px solid #edf2f7;'>{encodedName}</td></tr>
        <tr><td style='padding:10px; color:#718096; border-bottom:1px solid #edf2f7;'>Email:</td><td style='padding:10px; color:#2b6cb0; border-bottom:1px solid #edf2f7;'><a href='mailto:{encodedEmail}' style='color:#2b6cb0; text-decoration:none;'>{encodedEmail}</a></td></tr>
        <tr><td style='padding:10px; color:#718096; border-bottom:1px solid #edf2f7;'>Phone:</td><td style='padding:10px; color:#1a202c; border-bottom:1px solid #edf2f7;'><a href='tel:{cleanPhone}' style='color:#e65100; text-decoration:none; font-weight:bold;'>+91 {cleanPhone}</a></td></tr>
        <tr><td style='padding:10px; color:#718096; border-bottom:1px solid #edf2f7;'>Area of Seva:</td><td style='padding:10px; color:#e65100; font-weight:700; border-bottom:1px solid #edf2f7;'>{encodedInterest}</td></tr>
        <tr><td style='padding:10px; color:#718096;'>Received At:</td><td style='padding:10px; color:#4a5568;'>{istTime}</td></tr>
      </table>

      <div style='text-align:center; margin-top:24px;'>
        <a href='mailto:{encodedEmail}?subject=Regarding%20Your%20Volunteer%20Application%20-%20Shriram%20Janki%20Trust' style='display:inline-block; background:linear-gradient(135deg, #e65100 0%, #f57c00 100%); color:#ffffff; padding:11px 24px; text-decoration:none; border-radius:6px; font-weight:bold; box-shadow:0 2px 6px rgba(230,81,0,0.3);'>Contact Applicant</a>
      </div>
    </div>

    <!-- Trust Footer -->
    <div style='background:#fafafa; border-top:1px solid #edf2f7; padding:18px 24px; text-align:center; font-size:12px; color:#718096; line-height:1.6;'>
      <div style='font-weight:600; color:#4a5568; margin-bottom:3px;'>श्रीराम जानकी मंदिर सेवा ट्रस्ट (SHRI RAM JANKI MANDIR SEWA TRUST)</div>
      <div>करनपुर, स्टेशन रोड, बभनान - गोण्डा, उ.प्र., भारत - 271313 (निकट राम मंदिर, अयोध्या धाम) &bull; Phone: +91 9918953452, +91 9250931408</div>
      <div style='margin-top:5px; font-size:11px; color:#a0aec0;'>
        Darpan ID: UP/2026/0956133 &bull; Reg: 15/B4/G20/2025 &bull; 80G &amp; 12A Registered
      </div>
    </div>

  </div>
 </body>
</html>";

            await SendToMultipleAsync(adminEmails, emailSubject, body);
        }

        public async Task SendPasswordResetOtpEmailAsync(string toEmail, string userName, string otp)
        {
            // Clean subject without brackets
            string subject = $"Password Reset OTP: {otp} - Shri Ram Janki Trust";
            string encodedUserName = WebUtility.HtmlEncode(userName ?? "");

            string body = $@"
<!DOCTYPE html>
<html lang='en'>
<head><meta charset='UTF-8'><meta name='viewport' content='width=device-width, initial-scale=1.0'></head>
<body style='margin:0; padding:20px 0; background-color:#f4f6f9; font-family:""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;'>
  <div style='max-width:620px; margin:0 auto; background:#ffffff; border-radius:12px; overflow:hidden; box-shadow:0 4px 20px rgba(0,0,0,0.08); border:1px solid #ffd8a8;'>
    
    <!-- Top Branded Header with Logo -->
    <div style='background:linear-gradient(135deg, #d32f2f 0%, #b71c1c 100%); padding:28px 24px; text-align:center; color:#ffffff;'>
      <div style='margin-bottom:12px;'>
        <img src='cid:trustlogo' alt='Shriram Janki Trust Logo' width='70' height='70' style='display:inline-block; width:70px; height:70px; border-radius:50%; background-color:#ffffff; padding:4px; box-shadow:0 4px 12px rgba(0,0,0,0.25); border:2px solid #ffd8a8; vertical-align:middle;' />
      </div>
      <div style='font-size:22px; font-weight:700; letter-spacing:0.5px; margin-bottom:4px;'>श्रीराम जानकी मंदिर सेवा ट्रस्ट</div>
      <div style='font-size:12px; text-transform:uppercase; letter-spacing:1.5px; opacity:0.95; margin-bottom:14px;'>SHRI RAM JANKI MANDIR SEWA TRUST &bull; AYODHYA DHAM, UP</div>
      <div style='display:inline-block; background:rgba(255,255,255,0.2); border:1px solid rgba(255,255,255,0.4); padding:6px 20px; border-radius:20px; font-size:12px; font-weight:700; text-transform:uppercase; letter-spacing:1px;'>
        &#128272; Member Portal Password Reset
      </div>
    </div>

    <div style='padding:28px 24px; background:#ffffff;'>
      <h3 style='color:#1a202c; margin-top:0;'>Password Reset Verification Code</h3>
      <p style='color:#4a5568; font-size:14px;'>Namaste <strong>{encodedUserName}</strong>,</p>
      <p style='color:#4a5568; font-size:14px;'>You requested to reset your Member Portal password. Please use the One-Time Password (OTP) below to proceed:</p>
      <div style='text-align:center; margin:30px 0;'>
        <span style='display:inline-block; background:#fff3e0; border:2px dashed #e65100; color:#e65100; font-size:32px; font-weight:bold; letter-spacing:8px; padding:14px 28px; border-radius:8px;'>{otp}</span>
      </div>
      <p style='color:#d32f2f; font-weight:bold; font-size:13px; text-align:center;'>&#9888; Note: This OTP is strictly valid for only 2 minutes (120 seconds).</p>
      <p style='color:#718096; font-size:12px; margin-top:25px;'>If you did not request a password reset, please ignore this email or contact trust administration immediately.</p>
    </div>

    <!-- Trust Footer -->
    <div style='background:#fafafa; border-top:1px solid #edf2f7; padding:18px 24px; text-align:center; font-size:12px; color:#718096; line-height:1.6;'>
      <div style='font-weight:600; color:#4a5568; margin-bottom:3px;'>श्रीराम जानकी मंदिर सेवा ट्रस्ट (SHRI RAM JANKI MANDIR SEWA TRUST)</div>
      <div>करनपुर, स्टेशन रोड, बभनान - गोण्डा, उ.प्र., भारत - 271313 (निकट राम मंदिर, अयोध्या धाम) &bull; Phone: +91 9918953452, +91 9250931408</div>
      <div style='margin-top:5px; font-size:11px; color:#a0aec0;'>
        Darpan ID: UP/2026/0956133 &bull; Reg: 15/B4/G20/2025 &bull; 80G &amp; 12A Registered
      </div>
    </div>

  </div>
 </body>
</html>";
            await SendEmailAsync(toEmail, subject, body);
        }
    }
}
