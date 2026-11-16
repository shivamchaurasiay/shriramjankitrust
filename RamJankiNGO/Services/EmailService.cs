using System;
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
                        mail.Subject = subject;
                        mail.Body = htmlBody;
                        mail.IsBodyHtml = true;

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
            string subject = $"Welcome to Shriram Janki Trust - Your Member ID: {memberCode}";
            string body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;'>
                <div style='background-color: #ea5455; color: white; padding: 25px; text-align: center;'>
                    <h2 style='margin: 0;'>Shriram Janki Trust</h2>
                    <p style='margin: 5px 0 0;'>Member Registration Confirmation</p>
                </div>
                <div style='padding: 25px;'>
                    <p>Dear <strong>{memberName}</strong>,</p>
                    <p>Namaste and welcome! You have been successfully registered as a member with Shriram Janki Trust.</p>
                    <div style='background-color: #f8f9fa; border-left: 4px solid #ea5455; padding: 15px; margin: 20px 0;'>
                        <p style='margin: 0;'><strong>Member ID:</strong> <span style='font-size: 16px; color: #ea5455;'>{memberCode}</span></p>
                        <p style='margin: 5px 0 0;'><strong>Registered Email:</strong> {memberEmail}</p>
                        <p style='margin: 5px 0 0;'><strong>Default Password:</strong> <code>member123</code> (Change upon first login)</p>
                    </div>
                    <p>You can now access your Member Portal to view your seva donation history and download official tax receipts:</p>
                    <div style='text-align: center; margin: 25px 0;'>
                        <a href='{loginUrl}' style='background-color: #ea5455; color: white; padding: 12px 25px; text-decoration: none; border-radius: 4px; font-weight: bold;'>Login to Member Portal</a>
                    </div>
                    <p style='color: #666; font-size: 12px;'>Thank you for your devotion and partnership in social seva.<br/>Shriram Janki Trust, Ayodhya, UP</p>
                </div>
            </div>";

            if (!string.IsNullOrWhiteSpace(memberEmail))
            {
                await SendEmailAsync(memberEmail, subject, body);
            }

            // Also alert admin
            string adminEmails = _config["EmailSettings:AdminNotificationEmails"] ?? "";
            string adminSubject = $"[Admin Alert] New Member Registered: {memberName} ({memberCode})";
            await SendToMultipleAsync(adminEmails, adminSubject, body);
        }

        public async Task SendDonationReceiptEmailAsync(string donorEmail, string donorName, string receiptNumber, decimal amount, string campaignTitle, string receiptUrl)
        {
            string subject = $"Thank You for Your Donation - Receipt #{receiptNumber} | Shriram Janki Trust";
            string body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;'>
                <div style='background-color: #ea5455; color: white; padding: 25px; text-align: center;'>
                    <h2 style='margin: 0;'>Shriram Janki Trust</h2>
                    <p style='margin: 5px 0 0;'>Official Donation Receipt</p>
                </div>
                <div style='padding: 25px;'>
                    <p>Dear <strong>{donorName}</strong>,</p>
                    <p>Thank you deeply for your generous contribution of <strong>₹{amount:N2}</strong> towards <strong>{campaignTitle}</strong>. Your seva supports healthcare, education, and food relief for underprivileged families.</p>
                    <div style='background-color: #f8f9fa; border: 1px dashed #ea5455; border-radius: 6px; padding: 15px; margin: 20px 0;'>
                        <p style='margin: 0;'><strong>Receipt Number:</strong> {receiptNumber}</p>
                        <p style='margin: 5px 0 0;'><strong>Amount Contributed:</strong> ₹{amount:N2}</p>
                        <p style='margin: 5px 0 0;'><strong>Cause:</strong> {campaignTitle}</p>
                        <p style='margin: 5px 0 0;'><strong>Status:</strong> <span style='color: green; font-weight: bold;'>Confirmed Successful</span></p>
                    </div>
                    <div style='text-align: center; margin: 25px 0;'>
                        <a href='{receiptUrl}' style='background-color: #ea5455; color: white; padding: 12px 25px; text-decoration: none; border-radius: 4px; font-weight: bold;'>View & Download Official Receipt PDF</a>
                    </div>
                    <p style='color: #666; font-size: 12px;'>Shriram Janki Trust is deeply grateful for your support.<br/>May your seva bring peace, prosperity, and blessings.</p>
                </div>
            </div>";

            if (!string.IsNullOrWhiteSpace(donorEmail))
            {
                await SendEmailAsync(donorEmail, subject, body);
            }
        }

        public async Task SendAdminDonationAlertAsync(string donorName, string donorPhone, decimal amount, string receiptNumber, string campaignTitle)
        {
            string adminEmails = _config["EmailSettings:AdminNotificationEmails"] ?? "";
            string safeDonorName = AntiSpamValidator.SanitizeSubject(donorName);
            string subject = $"[Donation Confirmed] ₹{amount:N0} from {safeDonorName} - Receipt #{receiptNumber}";
            string istTime = DateTime.UtcNow.AddMinutes(330).ToString("dd MMM yyyy, hh:mm tt") + " IST";

            string body = $@"
<!DOCTYPE html>
<html>
<head><meta charset='UTF-8'><meta name='viewport' content='width=device-width, initial-scale=1.0'></head>
<body style='margin:0; padding:20px 0; background-color:#f4f6f9; font-family:""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;'>
  <div style='max-width:600px; margin:0 auto; background:#ffffff; border-radius:12px; overflow:hidden; box-shadow:0 4px 15px rgba(0,0,0,0.08); border:1px solid #ffd8a8;'>
    <div style='background:linear-gradient(135deg, #1b5e20 0%, #2e7d32 100%); padding:22px; text-align:center; color:#ffffff;'>
      <div style='font-size:18px; font-weight:700;'>श्रीराम जानकी मंदिर सेवा ट्रस्ट</div>
      <div style='font-size:12px; letter-spacing:1px; opacity:0.95;'>SHRI RAM JANKI MANDIR SEWA TRUST &bull; AYODHYA DHAM</div>
      <div style='margin-top:10px; display:inline-block; background:rgba(255,255,255,0.2); padding:4px 14px; border-radius:16px; font-size:12px; font-weight:600;'>
        &#128176; Online Donation Confirmed
      </div>
    </div>
    <div style='padding:24px;'>
      <div style='background:#f1f8e9; border:1px dashed #4caf50; border-radius:8px; padding:16px; margin-bottom:20px; text-align:center;'>
        <div style='font-size:13px; color:#558b2f; font-weight:600;'>AMOUNT RECEIVED</div>
        <div style='font-size:28px; font-weight:800; color:#2e7d32; margin:4px 0;'>₹{amount:N2}</div>
        <div style='font-size:12px; color:#689f38;'>Receipt Number: <strong>{receiptNumber}</strong></div>
      </div>
      <table style='width:100%; border-collapse:collapse; margin-bottom:15px; font-size:14px;'>
        <tr><td style='padding:8px; color:#666; width:35%;'>Donor:</td><td style='padding:8px; font-weight:bold; color:#222;'>{WebUtility.HtmlEncode(donorName)}</td></tr>
        <tr><td style='padding:8px; color:#666;'>Mobile:</td><td style='padding:8px; color:#222;'>{WebUtility.HtmlEncode(donorPhone)}</td></tr>
        <tr><td style='padding:8px; color:#666;'>Cause:</td><td style='padding:8px; color:#2e7d32; font-weight:600;'>{WebUtility.HtmlEncode(campaignTitle)}</td></tr>
        <tr><td style='padding:8px; color:#666;'>Time:</td><td style='padding:8px; color:#555;'>{istTime}</td></tr>
      </table>
    </div>
    <div style='background:#fafafa; border-top:1px solid #eee; padding:14px; text-align:center; font-size:11px; color:#888;'>
      श्रीराम जानकी मंदिर सेवा ट्रस्ट &bull; Ayodhya Dham, UP &bull; Helpline: +91 9918953452
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

            string emailSubject = $"[New Inquiry] Shriram Janki Trust - {safeName}";

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
    
    <!-- Top Branded Header -->
    <div style='background:linear-gradient(135deg, #d32f2f 0%, #b71c1c 100%); padding:26px 24px; text-align:center; color:#ffffff;'>
      <div style='font-size:20px; font-weight:700; letter-spacing:0.5px; margin-bottom:4px;'>श्रीराम जानकी मंदिर सेवा ट्रस्ट</div>
      <div style='font-size:12px; text-transform:uppercase; letter-spacing:1.5px; opacity:0.95; margin-bottom:12px;'>SHRI RAM JANKI MANDIR SEWA TRUST &bull; AYODHYA DHAM, UP</div>
      <div style='display:inline-block; background:rgba(255,255,255,0.2); border:1px solid rgba(255,255,255,0.4); padding:5px 16px; border-radius:20px; font-size:12px; font-weight:600; text-transform:uppercase; letter-spacing:1px;'>
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

            string emailSubject = $"[Volunteer Application] Shriram Janki Trust - {safeName}";

            string encodedName = WebUtility.HtmlEncode(name ?? "");
            string encodedEmail = WebUtility.HtmlEncode(email ?? "");
            string encodedPhone = WebUtility.HtmlEncode(phone ?? "");
            string cleanPhone = AntiSpamValidator.SanitizePhone(phone ?? "");
            string encodedInterest = WebUtility.HtmlEncode(interest ?? "General Seva");
            string istTime = DateTime.UtcNow.AddMinutes(330).ToString("dd MMM yyyy, hh:mm tt") + " IST";

            string body = $@"
<!DOCTYPE html>
<html>
<head><meta charset='UTF-8'><meta name='viewport' content='width=device-width, initial-scale=1.0'></head>
<body style='margin:0; padding:20px 0; background-color:#f4f6f9; font-family:""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;'>
  <div style='max-width:600px; margin:0 auto; background:#ffffff; border-radius:12px; overflow:hidden; box-shadow:0 4px 15px rgba(0,0,0,0.08); border:1px solid #ffd8a8;'>
    <div style='background:linear-gradient(135deg, #e65100 0%, #f57c00 100%); padding:22px; text-align:center; color:#ffffff;'>
      <div style='font-size:18px; font-weight:700;'>श्रीराम जानकी मंदिर सेवा ट्रस्ट</div>
      <div style='font-size:12px; letter-spacing:1px; opacity:0.95;'>SHRI RAM JANKI MANDIR SEWA TRUST &bull; AYODHYA DHAM</div>
      <div style='margin-top:10px; display:inline-block; background:rgba(255,255,255,0.2); padding:4px 14px; border-radius:16px; font-size:12px; font-weight:600;'>
        &#129309; New Volunteer Application
      </div>
    </div>
    <div style='padding:24px;'>
      <table style='width:100%; border-collapse:collapse; margin-bottom:15px; font-size:14px;'>
        <tr><td style='padding:8px; color:#666; width:35%;'>Applicant Name:</td><td style='padding:8px; font-weight:bold; color:#222;'>{encodedName}</td></tr>
        <tr><td style='padding:8px; color:#666;'>Email:</td><td style='padding:8px; color:#2b6cb0;'><a href='mailto:{encodedEmail}' style='color:#2b6cb0; text-decoration:none;'>{encodedEmail}</a></td></tr>
        <tr><td style='padding:8px; color:#666;'>Phone:</td><td style='padding:8px; color:#222;'><a href='tel:{cleanPhone}' style='color:#e65100; text-decoration:none; font-weight:bold;'>+91 {cleanPhone}</a></td></tr>
        <tr><td style='padding:8px; color:#666;'>Area of Seva:</td><td style='padding:8px; color:#e65100; font-weight:600;'>{encodedInterest}</td></tr>
        <tr><td style='padding:8px; color:#666;'>Received At:</td><td style='padding:8px; color:#555;'>{istTime}</td></tr>
      </table>
      <div style='text-align:center; margin-top:20px;'>
        <a href='mailto:{encodedEmail}?subject=Regarding%20Your%20Volunteer%20Application%20-%20Shriram%20Janki%20Trust' style='display:inline-block; background:#e65100; color:#fff; padding:10px 20px; text-decoration:none; border-radius:6px; font-weight:bold;'>Contact Applicant</a>
      </div>
    </div>
    <div style='background:#fafafa; border-top:1px solid #eee; padding:14px; text-align:center; font-size:11px; color:#888;'>
      श्रीराम जानकी मंदिर सेवा ट्रस्ट &bull; Ayodhya Dham, UP &bull; Helpline: +91 9918953452
    </div>
  </div>
</body>
</html>";

            await SendToMultipleAsync(adminEmails, emailSubject, body);
        }

        public async Task SendPasswordResetOtpEmailAsync(string toEmail, string userName, string otp)
        {
            string subject = $"Password Reset OTP: {otp} - Shri Ram Janki Trust";
            string body = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; border: 1px solid #ffd8a8; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.08);'>
    <div style='background: linear-gradient(135deg, #d32f2f, #e65100); color: #fff; padding: 24px; text-align: center;'>
        <h2 style='margin: 0; font-size: 22px;'>श्रीराम जानकी मंदिर सेवा ट्रस्ट</h2>
        <p style='margin: 5px 0 0; opacity: 0.9; font-size: 13px;'>SHRI RAM JANKI MANDIR SEWA TRUST &bull; Member Portal</p>
    </div>
    <div style='padding: 28px; background: #fff;'>
        <h3 style='color: #222; margin-top: 0;'>Password Reset Verification Code</h3>
        <p style='color: #555; font-size: 14px;'>Namaste <strong>{userName}</strong>,</p>
        <p style='color: #555; font-size: 14px;'>You requested to reset your Member Portal password. Please use the One-Time Password (OTP) below to proceed:</p>
        <div style='text-align: center; margin: 30px 0;'>
            <span style='display: inline-block; background: #fff3e0; border: 2px dashed #e65100; color: #e65100; font-size: 32px; font-weight: bold; letter-spacing: 8px; padding: 14px 28px; border-radius: 8px;'>{otp}</span>
        </div>
        <p style='color: #d32f2f; font-weight: bold; font-size: 13px; text-align: center;'>âš ï¸ Note: This OTP is strictly valid for only 2 minutes (120 seconds).</p>
        <p style='color: #777; font-size: 12px; margin-top: 25px;'>If you did not request a password reset, please ignore this email or contact trust administration immediately.</p>
    </div>
    <div style='background: #fafafa; border-top: 1px solid #eee; padding: 15px; text-align: center; font-size: 11px; color: #888;'>
        करनपुर, स्टेशन रोड, बभनान - गोण्डा, उ.प्र., भारत - 271313 (निकट राम मंदिर, अयोध्या धाम) &bull; Phone: +91 9918953452, +91 9250931408
    </div>
</div>";
            await SendEmailAsync(toEmail, subject, body);
        }
    }
}
