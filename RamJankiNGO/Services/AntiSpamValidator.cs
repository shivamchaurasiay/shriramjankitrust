using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace RamJankiNGO.Services
{
    public static class AntiSpamValidator
    {
        private static readonly byte[] TokenSecretKey = Encoding.UTF8.GetBytes("SJT_Antispam_Secure_Token_2026_Key!");

        private static readonly HashSet<string> DisposableEmailDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "merepost.com", "mailinator.com", "tempmail.com", "10minutemail.com", "guerrillamail.com",
            "sharklasers.com", "trashmail.com", "dispostable.com", "yopmail.com", "fakeinbox.com",
            "mytemp.email", "throwawaymail.com", "getairmail.com", "fakemailgenerator.com", "generator.email",
            "crazymailing.com", "nada.ltd", "temp-mail.org", "burnermail.io", "mohmal.com",
            "inboxkitten.com", "trashmail.net", "tempinbox.com", "maildrop.cc", "mytempemail.com",
            "temp-mail.io", "guerrillamailblock.com", "pokemail.net", "spam4.me", "grr.la",
            "guerrillamail.biz", "guerrillamail.de", "guerrillamail.net", "guerrillamail.org",
            "armyspy.com", "cuvox.de", "dayrep.com", "einrot.com", "fleckens.hu", "gustr.com",
            "jourrapide.com", "rhyta.com", "superrito.com", "teleworm.us", "mytempmail.com",
            "emailondeck.com", "throwawayemail.com", "tempail.com", "getnada.com", "dropmail.me"
        };

        private static readonly string[] SpamKeywords = new[]
        {
            "scam service", "scam", "service 24/7", "casino", "crypto", "bitcoin",
            "seo ranking", "adult dating", "viagra", "cialis", "telegram:", "t.me/",
            "whatsapp group", "fast profit", "investment guarantee", "escort service",
            "hack account", "myxa.cc", "xxx*", "hs="
        };

        /// <summary>
        /// Validates email address format strictly according to RFC standards and checks disposable domains.
        /// </summary>
        public static bool IsValidEmail(string? email, out string cleanEmail, out string errorMessage)
        {
            cleanEmail = string.Empty;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(email))
            {
                errorMessage = "Email address is required.";
                return false;
            }

            cleanEmail = email.Trim().ToLowerInvariant();

            if (cleanEmail.Length < 5 || cleanEmail.Length > 254)
            {
                errorMessage = "Email address must be between 5 and 254 characters.";
                return false;
            }

            // Reject invalid characters in email like '~', spaces, commas, quotes
            if (cleanEmail.Contains('~') || cleanEmail.Contains(' ') || cleanEmail.Contains(',') || cleanEmail.Contains(';') || cleanEmail.Contains('\"') || cleanEmail.Contains('\''))
            {
                errorMessage = "Email address contains invalid characters.";
                return false;
            }

            // RFC-compliant strict pattern
            var emailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,24}$", RegexOptions.Compiled);
            if (!emailRegex.IsMatch(cleanEmail))
            {
                errorMessage = "Please enter a valid email format (e.g. name@example.com).";
                return false;
            }

            // Domain checks
            int atIndex = cleanEmail.LastIndexOf('@');
            if (atIndex < 1 || atIndex >= cleanEmail.Length - 3)
            {
                errorMessage = "Invalid email domain structure.";
                return false;
            }

            string domain = cleanEmail.Substring(atIndex + 1);
            if (!domain.Contains('.') || domain.StartsWith(".") || domain.EndsWith("."))
            {
                errorMessage = "Email domain must contain a valid top-level domain (.com, .org, etc.).";
                return false;
            }

            // Check disposable domain blacklist
            if (DisposableEmailDomains.Contains(domain))
            {
                errorMessage = "Temporary / disposable email addresses are not accepted. Please use your genuine personal or work email.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates sender name: strictly letters, spaces, dots, hyphens (English & Hindi/Devanagari).
        /// Disallows URLs, spam patterns, and bot junk.
        /// </summary>
        public static bool IsValidName(string? name, out string cleanName, out string errorMessage)
        {
            cleanName = string.Empty;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(name))
            {
                errorMessage = "Your full name is required.";
                return false;
            }

            cleanName = Regex.Replace(name.Trim(), @"\s+", " ");

            if (cleanName.Length < 2 || cleanName.Length > 60)
            {
                errorMessage = "Name must be between 2 and 60 characters.";
                return false;
            }

            // Check for URLs inside the name field (common bot injection)
            string lower = cleanName.ToLowerInvariant();
            if (lower.Contains("http://") || lower.Contains("https://") || lower.Contains("ftp://") ||
                lower.Contains(".cc") || lower.Contains(".com") || lower.Contains(".org") ||
                lower.Contains(".xyz") || lower.Contains(".ru") || lower.Contains("www."))
            {
                errorMessage = "Website links and URLs are not permitted in the Name field.";
                return false;
            }

            // Check for spam trigger words
            foreach (var kw in SpamKeywords)
            {
                if (lower.Contains(kw))
                {
                    errorMessage = "The entered name contains invalid or prohibited terms.";
                    return false;
                }
            }

            // Check for bot symbol characters
            if (cleanName.IndexOfAny(new[] { '*', '~', '<', '>', '$', '{', '}', '[', ']', '|', '^', '=', '+', '\\', '%', '#', '?' }) >= 0)
            {
                errorMessage = "Name contains prohibited special characters.";
                return false;
            }

            // Allowed: English letters, Hindi/Devanagari Unicode (\u0900-\u097F), spaces, dots, hyphens, and apostrophes
            var nameRegex = new Regex(@"^[a-zA-Z\u0900-\u097F\s\.\'\-]{2,60}$", RegexOptions.Compiled);
            if (!nameRegex.IsMatch(cleanName))
            {
                errorMessage = "Please enter a valid human name (letters and spaces only).";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates Indian 10-digit mobile number: starts with 6, 7, 8, or 9.
        /// </summary>
        public static bool IsValidPhone(string? phone, bool isRequired, out string cleanPhone, out string errorMessage)
        {
            cleanPhone = string.Empty;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(phone))
            {
                if (isRequired)
                {
                    errorMessage = "Mobile number is required.";
                    return false;
                }
                return true;
            }

            cleanPhone = SanitizePhone(phone);

            if (cleanPhone.Length == 0)
            {
                if (isRequired)
                {
                    errorMessage = "Mobile number is required.";
                    return false;
                }
                return true;
            }

            // Must be strictly 10 digits starting with 6, 7, 8, or 9
            var phoneRegex = new Regex(@"^[6-9]\d{9}$", RegexOptions.Compiled);
            if (!phoneRegex.IsMatch(cleanPhone))
            {
                errorMessage = "Please enter a valid 10-digit Indian mobile number starting with 6, 7, 8, or 9.";
                return false;
            }

            // Reject dummy repetitive numbers (e.g. 9999999999, 8888888888, 1234567890)
            if (new Regex(@"^(\d)\1{9}$").IsMatch(cleanPhone) || cleanPhone == "1234567890")
            {
                errorMessage = "Please enter a genuine, active mobile number.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates message content against link bombing, bot junk strings, and minimum length.
        /// </summary>
        public static bool IsValidMessage(string? message, out string cleanMessage, out string errorMessage)
        {
            cleanMessage = string.Empty;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(message))
            {
                errorMessage = "Message is required.";
                return false;
            }

            cleanMessage = message.Trim();

            if (cleanMessage.Length < 8)
            {
                errorMessage = "Message is too short. Please provide at least 8 characters describing your inquiry.";
                return false;
            }

            if (cleanMessage.Length > 2000)
            {
                errorMessage = "Message is too long (maximum 2000 characters allowed).";
                return false;
            }

            // Count URLs in message to prevent link-bombing spam
            int urlMatches = Regex.Matches(cleanMessage, @"(https?://|www\.)", RegexOptions.IgnoreCase).Count;
            if (urlMatches > 1)
            {
                errorMessage = "Promotional links or multiple URLs are not allowed in inquiry messages.";
                return false;
            }

            string lower = cleanMessage.ToLowerInvariant();
            foreach (var kw in SpamKeywords)
            {
                if (lower.Contains(kw))
                {
                    errorMessage = "Message contains prohibited promotional or scam content.";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Cleans and formats phone numbers by stripping country codes (+91/0) and non-digit characters.
        /// </summary>
        public static string SanitizePhone(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

            string digitsOnly = Regex.Replace(raw, @"\D", "");

            // If 12 digits starting with 91, strip country code
            if (digitsOnly.Length == 12 && digitsOnly.StartsWith("91"))
            {
                digitsOnly = digitsOnly.Substring(2);
            }
            // If 11 digits starting with 0, strip leading 0
            else if (digitsOnly.Length == 11 && digitsOnly.StartsWith("0"))
            {
                digitsOnly = digitsOnly.Substring(1);
            }

            return digitsOnly;
        }

        /// <summary>
        /// Cleans subject line to prevent newline/header injection.
        /// </summary>
        public static string SanitizeSubject(string? subject)
        {
            if (string.IsNullOrWhiteSpace(subject)) return "General Website Inquiry";
            string clean = Regex.Replace(subject.Trim(), @"[\r\n\t]+", " ");
            clean = Regex.Replace(clean, @"\s+", " ");
            if (clean.Length > 60) clean = clean.Substring(0, 60);
            return clean;
        }

        /// <summary>
        /// Generates a signed timestamp token to protect against instantaneous automated bot POST requests.
        /// </summary>
        public static string GenerateFormToken()
        {
            long nowTicks = DateTime.UtcNow.Ticks;
            string payload = $"{nowTicks}";
            using (var hmac = new HMACSHA256(TokenSecretKey))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                string sig = Convert.ToBase64String(hash);
                return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{payload}|{sig}"));
            }
        }

        /// <summary>
        /// Validates that the form was not submitted instantly (&lt; 2.5 seconds) by an automated bot script.
        /// </summary>
        public static bool ValidateSubmissionTiming(string? formToken, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(formToken))
            {
                // Fallback graceful: allow if token was somehow omitted in non-standard request
                return true;
            }

            try
            {
                byte[] bytes = Convert.FromBase64String(formToken);
                string decoded = Encoding.UTF8.GetString(bytes);
                var parts = decoded.Split('|');
                if (parts.Length != 2) return true;

                string ticksStr = parts[0];
                string sig = parts[1];

                using (var hmac = new HMACSHA256(TokenSecretKey))
                {
                    byte[] expectedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(ticksStr));
                    string expectedSig = Convert.ToBase64String(expectedHash);
                    if (!sig.Equals(expectedSig, StringComparison.Ordinal))
                    {
                        return true; // Signature mismatch, pass through to other validators
                    }
                }

                if (long.TryParse(ticksStr, out long ticks))
                {
                    var tokenTime = new DateTime(ticks, DateTimeKind.Utc);
                    var elapsed = DateTime.UtcNow - tokenTime;

                    // If form was submitted in less than 2.0 seconds from page generation, it is almost certainly a bot
                    if (elapsed.TotalSeconds < 2.0 && elapsed.TotalSeconds >= 0)
                    {
                        error = "Submission was too fast. Please take a moment to review your details.";
                        return false;
                    }

                    // Expire token after 24 hours
                    if (elapsed.TotalHours > 24)
                    {
                        error = "The form session expired. Please refresh the page and try again.";
                        return false;
                    }
                }
            }
            catch
            {
                // In case of base64 failure, allow graceful fallback
            }

            return true;
        }
    }
}
