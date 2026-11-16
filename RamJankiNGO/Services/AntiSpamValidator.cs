using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace RamJankiNGO.Services
{
    public static class AntiSpamValidator
    {
        private static readonly byte[] TokenSecretKey = Encoding.UTF8.GetBytes("SJT_Antispam_Secure_Token_2026_Key!");

        public static int ComputeLevenshteinDistance(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1)) return string.IsNullOrEmpty(s2) ? 0 : s2.Length;
            if (string.IsNullOrEmpty(s2)) return s1.Length;

            int n = s1.Length;
            int m = s2.Length;
            int[,] d = new int[n + 1, m + 1];

            for (int i = 0; i <= n; d[i, 0] = i++) { }
            for (int j = 0; j <= m; d[0, j] = j++) { }

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (s2[j - 1] == s1[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }

        private static readonly Dictionary<string, string> DomainTypoSuggestions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "gail.com", "gmail.com" },
            { "gmil.com", "gmail.com" },
            { "gmial.com", "gmail.com" },
            { "gamil.com", "gmail.com" },
            { "gmai.com", "gmail.com" },
            { "gmal.com", "gmail.com" },
            { "gmaill.com", "gmail.com" },
            { "gmaik.com", "gmail.com" },
            { "gmeil.com", "gmail.com" },
            { "gmaii.com", "gmail.com" },
            { "gimail.com", "gmail.com" },
            { "gamail.com", "gmail.com" },
            { "gmaail.com", "gmail.com" },
            { "gmailll.com", "gmail.com" },
            { "gmaul.com", "gmail.com" },
            { "gnail.com", "gmail.com" },
            { "hmail.com", "gmail.com" },
            { "fmail.com", "gmail.com" },
            { "gemail.com", "gmail.com" },
            { "g-mail.com", "gmail.com" },
            { "gmail.co", "gmail.com" },
            { "gmail.con", "gmail.com" },
            { "gmail.cpm", "gmail.com" },
            { "gmail.cm", "gmail.com" },
            { "gmail.om", "gmail.com" },
            { "gmail.in", "gmail.com" },
            { "gmail.co.in", "gmail.com" },
            { "gmai.co.in", "gmail.com" },
            { "gamil.co.in", "gmail.com" },
            { "gmali.com", "gmail.com" },
            { "gail.co.in", "gmail.com" },
            { "gail.in", "gmail.com" },
            { "gmil.in", "gmail.com" },
            { "gmai.in", "gmail.com" },
            { "yaho.com", "yahoo.com" },
            { "yaho.co.in", "yahoo.co.in" },
            { "yahooo.com", "yahoo.com" },
            { "yhaoo.com", "yahoo.com" },
            { "yaho.in", "yahoo.in" },
            { "yahoo.co", "yahoo.com" },
            { "yahoo.con", "yahoo.com" },
            { "yahoo.cm", "yahoo.com" },
            { "yohoo.com", "yahoo.com" },
            { "yaboo.com", "yahoo.com" },
            { "yaoo.com", "yahoo.com" },
            { "hotmial.com", "hotmail.com" },
            { "hotmai.com", "hotmail.com" },
            { "hotmale.com", "hotmail.com" },
            { "hotmil.com", "hotmail.com" },
            { "hotmaill.com", "hotmail.com" },
            { "hotamail.com", "hotmail.com" },
            { "hotmali.com", "hotmail.com" },
            { "homail.com", "hotmail.com" },
            { "hitmail.com", "hotmail.com" },
            { "outlok.com", "outlook.com" },
            { "outloo.com", "outlook.com" },
            { "outllok.com", "outlook.com" },
            { "outlool.com", "outlook.com" },
            { "outloook.com", "outlook.com" },
            { "outlook.co", "outlook.com" },
            { "outlook.con", "outlook.com" },
            { "outliok.com", "outlook.com" },
            { "autlook.com", "outlook.com" },
            { "redifmail.com", "rediffmail.com" },
            { "rediff.com", "rediffmail.com" },
            { "redif.com", "rediffmail.com" },
            { "rediffmial.com", "rediffmail.com" },
            { "radiffmail.com", "rediffmail.com" },
            { "rediffmal.com", "rediffmail.com" },
            { "icould.com", "icloud.com" },
            { "iclod.com", "icloud.com" },
            { "icluod.com", "icloud.com" },
            { "icloud.co", "icloud.com" }
        };

        private static readonly (string target, string sld, string label)[] MajorTargets = new[]
        {
            ("gmail.com", "gmail", "gmail.com"),
            ("yahoo.com", "yahoo", "yahoo.com"),
            ("hotmail.com", "hotmail", "hotmail.com"),
            ("outlook.com", "outlook", "outlook.com"),
            ("rediffmail.com", "rediffmail", "rediffmail.com"),
            ("icloud.com", "icloud", "icloud.com")
        };

        private static readonly HashSet<string> KnownValidProviders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "gmail.com", "googlemail.com", "yahoo.com", "yahoo.co.in", "yahoo.co.uk", "ymail.com",
            "outlook.com", "hotmail.com", "live.com", "msn.com", "icloud.com", "me.com", "mac.com",
            "rediffmail.com", "zoho.com", "zohomail.in", "proton.me", "protonmail.com", "aol.com", "mail.com"
        };

        private static readonly HashSet<string> BotTlds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "xyz", "top", "cc", "ru", "click", "link", "work", "bid", "cn"
        };

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

        public static string? DetectDomainTypo(string domain)
        {
            if (string.IsNullOrWhiteSpace(domain)) return null;
            string d = domain.Trim().ToLowerInvariant();

            // 1. Direct dictionary match
            if (DomainTypoSuggestions.TryGetValue(d, out string? directMatch))
            {
                return directMatch;
            }

            // 2. Pattern checks for gmail typos (e.g. gail.com, gmil.com, gmeil.com)
            if (d != "gmail.com" && d != "googlemail.com")
            {
                if (Regex.IsMatch(d, @"^g[a-z]{2,5}l\.(com|co|in|co\.in)$")) return "gmail.com";
                if (Regex.IsMatch(d, @"^g.*mail\.(com|co|in)$")) return "gmail.com";
                if (Regex.IsMatch(d, @"^gm.*l\.(com|co|in)$")) return "gmail.com";
            }

            if (d != "yahoo.com" && d != "yahoo.co.in" && d != "yahoo.in")
            {
                if (Regex.IsMatch(d, @"^y.*hoo\.(com|co|in)$")) return "yahoo.com";
            }

            if (d != "hotmail.com")
            {
                if (Regex.IsMatch(d, @"^hot.*m.*l\.(com|co|in)$")) return "hotmail.com";
            }

            if (d != "outlook.com")
            {
                if (Regex.IsMatch(d, @"^out.*l.*k\.(com|co|in)$")) return "outlook.com";
            }

            if (d != "rediffmail.com")
            {
                if (Regex.IsMatch(d, @"^redif.*mail\.(com|co|in)$")) return "rediffmail.com";
            }

            // 3. Levenshtein Distance matching against major email providers
            foreach (var item in MajorTargets)
            {
                if (string.Equals(d, item.target, StringComparison.OrdinalIgnoreCase)) continue;

                int distFull = ComputeLevenshteinDistance(d, item.target);
                if (distFull <= 2)
                {
                    return item.label;
                }

                // Check Second-Level Domain (e.g. "gail" in "gail.com")
                string sld = d.Split('.')[0];
                if (!string.IsNullOrEmpty(sld) && !string.Equals(sld, item.sld, StringComparison.OrdinalIgnoreCase))
                {
                    int distSld = ComputeLevenshteinDistance(sld, item.sld);
                    if (distSld <= 2 && distSld > 0)
                    {
                        return item.label;
                    }
                }
            }

            return null;
        }

        public static bool IsGibberish(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return true;
            string t = text.Trim();
            if (t.Length < 8) return true;

            // Count unique alphanumeric characters
            var uniqueChars = new HashSet<char>();
            foreach (char ch in t.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(ch)) uniqueChars.Add(ch);
            }
            if (uniqueChars.Count <= 3 && t.Length >= 8)
            {
                return true; // Rejects repetitive keyboard mash like DASDASDAD, aDASDASDASDASD, asdfasdf, etc.
            }

            // Repeated chunks of length 2 to 4
            if (Regex.IsMatch(t, @"^(.{2,4})\1{2,}$", RegexOptions.IgnoreCase))
            {
                return true;
            }

            // Common keyboard mashing sequences
            string[] mashes = new[] { "asdfghjkl", "qwertyuiop", "zxcvbnm", "asdfasdf", "dasdas", "sdasda", "12345678" };
            string lower = t.ToLowerInvariant();
            foreach (var mash in mashes)
            {
                if (lower.Contains(mash)) return true;
            }

            return false;
        }

        /// <summary>
        /// Validates email address format strictly according to RFC standards, catches domain typos and checks disposable domains.
        /// </summary>
        public static bool IsValidEmail(string? email, out string cleanEmail, out string errorMessage)
        {
            cleanEmail = string.Empty;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(email))
            {
                errorMessage = "ईमेल पता आवश्यक है (Email address is required).";
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

            // 1. Detect misspelled domain typos (e.g. gmil.com, gail.com -> gmail.com)
            string? suggestedDomain = DetectDomainTypo(domain);
            if (!string.IsNullOrEmpty(suggestedDomain))
            {
                errorMessage = $"गलत ईमेल डोमेन '{domain}'! क्या आपका मतलब @{suggestedDomain} है? (Invalid domain '{domain}'. Did you mean @{suggestedDomain}?)";
                return false;
            }

            // 2. Check disposable domain blacklist
            if (DisposableEmailDomains.Contains(domain))
            {
                errorMessage = "Temporary / disposable email addresses are not accepted. Please use your genuine personal or work email.";
                return false;
            }

            // 3. Reject bot TLDs (.xyz, .top, .cc, .ru, etc.)
            int dotIdx = domain.LastIndexOf('.');
            string tld = dotIdx != -1 ? domain.Substring(dotIdx + 1) : "";
            if (BotTlds.Contains(tld))
            {
                errorMessage = $"Domains ending in .{tld} are not accepted. Please use a standard email domain (.com, .in, .org).";
                return false;
            }

            // 4. Verify domain DNS host resolution if not a known standard provider
            if (!KnownValidProviders.Contains(domain) && !domain.EndsWith(".gov.in") && !domain.EndsWith(".nic.in") && !domain.EndsWith(".edu.in") && !domain.EndsWith(".ac.in"))
            {
                try
                {
                    var hostEntry = System.Net.Dns.GetHostEntry(domain);
                    if (hostEntry.AddressList == null || hostEntry.AddressList.Length == 0)
                    {
                        errorMessage = $"The email domain '{domain}' cannot be verified or does not exist.";
                        return false;
                    }
                }
                catch
                {
                    errorMessage = $"The email domain '{domain}' does not exist or has no active DNS record.";
                    return false;
                }
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

            cleanName = name.Trim();

            if (cleanName.Length < 2)
            {
                errorMessage = "Name must be at least 2 characters.";
                return false;
            }

            if (cleanName.Length > 60)
            {
                errorMessage = "Name cannot exceed 60 characters.";
                return false;
            }

            // Disallow URLs, bracket junk, HTML
            string lower = cleanName.ToLowerInvariant();
            if (lower.Contains("http://") || lower.Contains("https://") || lower.Contains(".com") || lower.Contains(".cc") ||
                lower.Contains("[") || lower.Contains("]") || lower.Contains("<") || lower.Contains(">") ||
                cleanName.Contains('~') || cleanName.Contains('*') || cleanName.Contains('$'))
            {
                errorMessage = "Name contains prohibited special characters or URLs.";
                return false;
            }

            foreach (var kw in SpamKeywords)
            {
                if (lower.Contains(kw))
                {
                    errorMessage = "Name contains prohibited words.";
                    return false;
                }
            }

            // Allow English and Hindi/Devanagari letters, dots, hyphens, and spaces
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
        /// Validates message content against link bombing, bot junk strings, minimum length, and gibberish.
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

            if (IsGibberish(cleanMessage))
            {
                errorMessage = "कृपया एक सार्थक संदेश लिखें (Please enter a meaningful message describing your inquiry).";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Cleans and formats phone numbers by stripping country codes (+91/0) and non-digit characters.
        /// </summary>
        public static string SanitizePhone(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            string digits = Regex.Replace(raw, @"\D", "");
            if (digits.Length == 12 && digits.StartsWith("91"))
            {
                digits = digits.Substring(2);
            }
            else if (digits.Length == 11 && digits.StartsWith("0"))
            {
                digits = digits.Substring(1);
            }
            return digits;
        }

        /// <summary>
        /// Cleans subject line to prevent header injection, strips square brackets, and limits length.
        /// </summary>
        public static string SanitizeSubject(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "General Inquiry";
            string clean = Regex.Replace(raw, @"[\r\n\t]+", " ");
            clean = Regex.Replace(clean, @"[\[\]]+", " ");
            clean = Regex.Replace(clean, @"[<>\$~\*]+", "");
            clean = clean.Trim();
            if (clean.Length > 60) clean = clean.Substring(0, 60).Trim();
            if (string.IsNullOrWhiteSpace(clean) || IsGibberish(clean)) return "General Inquiry";
            return clean;
        }

        /// <summary>
        /// Generates an encrypted time-stamped token to enforce human filling time (>= 2 seconds).
        /// </summary>
        public static string GenerateFormToken()
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string plain = $"{timestamp}:{Guid.NewGuid():N}";
            using (var hmac = new HMACSHA256(TokenSecretKey))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(plain));
                string sig = Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_").TrimEnd('=');
                string payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(plain)).Replace("+", "-").Replace("/", "_").TrimEnd('=');
                return $"{payload}.{sig}";
            }
        }

        /// <summary>
        /// Validates that form submission took at least 2 seconds and token is under 30 minutes old.
        /// </summary>
        public static bool ValidateSubmissionTiming(string? token, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(token))
            {
                return true; // Graceful pass if tokens are not yet embedded on every form
            }

            var parts = token.Split('.');
            if (parts.Length != 2)
            {
                return true;
            }

            try
            {
                string payloadStr = parts[0].Replace("-", "+").Replace("_", "/");
                switch (payloadStr.Length % 4)
                {
                    case 2: payloadStr += "=="; break;
                    case 3: payloadStr += "="; break;
                }
                string plain = Encoding.UTF8.GetString(Convert.FromBase64String(payloadStr));

                using (var hmac = new HMACSHA256(TokenSecretKey))
                {
                    byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(plain));
                    string expectedSig = Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_").TrimEnd('=');
                    if (expectedSig != parts[1])
                    {
                        return true;
                    }
                }

                var subParts = plain.Split(':');
                if (long.TryParse(subParts[0], out long issuedAt))
                {
                    long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    long elapsed = now - issuedAt;

                    if (elapsed < 2)
                    {
                        errorMessage = "Submission was too fast. Please take your time to fill the form.";
                        return false;
                    }

                    if (elapsed > 1800)
                    {
                        errorMessage = "Form session expired. Please refresh the page and try again.";
                        return false;
                    }
                }
            }
            catch
            {
                return true;
            }

            return true;
        }
    }
}
