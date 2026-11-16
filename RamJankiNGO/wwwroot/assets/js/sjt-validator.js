/**
 * Shriram Janki Trust - Client-Side Unified Form & Domain Validator (v4)
 * Validates Email Domains (catches typos like gmil.com, gail.com, disposable emails), Names, Mobile numbers, and Messages.
 */
(function (global) {
    'use strict';

    function levenshteinDistance(s1, s2) {
        if (!s1) return s2 ? s2.length : 0;
        if (!s2) return s1.length;
        var m = s1.length, n = s2.length;
        var d = [];
        for (var i = 0; i <= m; i++) d[i] = [i];
        for (var j = 0; j <= n; j++) d[0][j] = j;
        for (var j = 1; j <= n; j++) {
            for (var i = 1; i <= m; i++) {
                if (s1[i - 1] === s2[j - 1]) d[i][j] = d[i - 1][j - 1];
                else d[i][j] = Math.min(d[i - 1][j] + 1, d[i][j - 1] + 1, d[i - 1][j - 1] + 1);
            }
        }
        return d[m][n];
    }

    var domainTypos = {
        'gail.com': 'gmail.com',
        'gmil.com': 'gmail.com',
        'gmial.com': 'gmail.com',
        'gamil.com': 'gmail.com',
        'gmai.com': 'gmail.com',
        'gmal.com': 'gmail.com',
        'gmaill.com': 'gmail.com',
        'gmaik.com': 'gmail.com',
        'gmeil.com': 'gmail.com',
        'gmaii.com': 'gmail.com',
        'gimail.com': 'gmail.com',
        'gamail.com': 'gmail.com',
        'gmaail.com': 'gmail.com',
        'gmailll.com': 'gmail.com',
        'gmaul.com': 'gmail.com',
        'gnail.com': 'gmail.com',
        'hmail.com': 'gmail.com',
        'fmail.com': 'gmail.com',
        'gemail.com': 'gmail.com',
        'g-mail.com': 'gmail.com',
        'gmail.co': 'gmail.com',
        'gmail.con': 'gmail.com',
        'gmail.cpm': 'gmail.com',
        'gmail.cm': 'gmail.com',
        'gmail.om': 'gmail.com',
        'gmail.in': 'gmail.com',
        'gmail.co.in': 'gmail.com',
        'gmai.co.in': 'gmail.com',
        'gamil.co.in': 'gmail.com',
        'gmali.com': 'gmail.com',
        'gail.co.in': 'gmail.com',
        'gail.in': 'gmail.com',
        'gmil.in': 'gmail.com',
        'gmai.in': 'gmail.com',
        'yaho.com': 'yahoo.com',
        'yaho.co.in': 'yahoo.co.in',
        'yahooo.com': 'yahoo.com',
        'yhaoo.com': 'yahoo.com',
        'yaho.in': 'yahoo.in',
        'yahoo.co': 'yahoo.com',
        'yahoo.con': 'yahoo.com',
        'yahoo.cm': 'yahoo.com',
        'yohoo.com': 'yahoo.com',
        'yaboo.com': 'yahoo.com',
        'yaoo.com': 'yahoo.com',
        'hotmial.com': 'hotmail.com',
        'hotmai.com': 'hotmail.com',
        'hotmale.com': 'hotmail.com',
        'hotmil.com': 'hotmail.com',
        'hotmaill.com': 'hotmail.com',
        'hotamail.com': 'hotmail.com',
        'hotmali.com': 'hotmail.com',
        'homail.com': 'hotmail.com',
        'hitmail.com': 'hotmail.com',
        'outlok.com': 'outlook.com',
        'outloo.com': 'outlook.com',
        'outllok.com': 'outlook.com',
        'outlool.com': 'outlook.com',
        'outloook.com': 'outlook.com',
        'outlook.co': 'outlook.com',
        'outlook.con': 'outlook.com',
        'outliok.com': 'outlook.com',
        'autlook.com': 'outlook.com',
        'redifmail.com': 'rediffmail.com',
        'rediff.com': 'rediffmail.com',
        'redif.com': 'rediffmail.com',
        'rediffmial.com': 'rediffmail.com',
        'radiffmail.com': 'rediffmail.com',
        'rediffmal.com': 'rediffmail.com',
        'icould.com': 'icloud.com',
        'iclod.com': 'icloud.com',
        'icluod.com': 'icloud.com',
        'icloud.co': 'icloud.com'
    };

    var majorTargets = [
        { target: 'gmail.com', sld: 'gmail', label: 'gmail.com' },
        { target: 'yahoo.com', sld: 'yahoo', label: 'yahoo.com' },
        { target: 'hotmail.com', sld: 'hotmail', label: 'hotmail.com' },
        { target: 'outlook.com', sld: 'outlook', label: 'outlook.com' },
        { target: 'rediffmail.com', sld: 'rediffmail', label: 'rediffmail.com' },
        { target: 'icloud.com', sld: 'icloud', label: 'icloud.com' }
    ];

    var disposableDomains = [
        'merepost.com', 'mailinator.com', 'tempmail.com', '10minutemail.com', 'guerrillamail.com',
        'sharklasers.com', 'trashmail.com', 'dispostable.com', 'yopmail.com', 'fakeinbox.com',
        'mytemp.email', 'throwawaymail.com', 'getairmail.com', 'fakemailgenerator.com', 'generator.email',
        'crazymailing.com', 'nada.ltd', 'temp-mail.org', 'burnermail.io', 'mohmal.com',
        'inboxkitten.com', 'trashmail.net', 'tempinbox.com', 'maildrop.cc', 'mytempemail.com',
        'temp-mail.io', 'guerrillamailblock.com', 'pokemail.net', 'spam4.me', 'grr.la',
        'guerrillamail.biz', 'guerrillamail.de', 'guerrillamail.net', 'guerrillamail.org',
        'armyspy.com', 'cuvox.de', 'dayrep.com', 'einrot.com', 'fleckens.hu', 'gustr.com',
        'jourrapide.com', 'rhyta.com', 'superrito.com', 'teleworm.us', 'mytempmail.com',
        'emailondeck.com', 'throwawayemail.com', 'tempail.com', 'getnada.com', 'dropmail.me'
    ];

    var botTlds = ['xyz', 'top', 'cc', 'ru', 'click', 'link', 'work', 'bid', 'cn'];

    function isGibberish(text) {
        if (!text) return true;
        var t = text.trim();
        if (t.length < 8) return true;

        // Count unique alphanumeric characters
        var uniqueChars = {};
        for (var i = 0; i < t.length; i++) {
            var ch = t[i].toLowerCase();
            if (/[a-z0-9]/.test(ch)) uniqueChars[ch] = true;
        }
        var count = Object.keys(uniqueChars).length;
        if (count <= 3 && t.length >= 8) {
            return true; // Gibberish repetition like DASDASDAD, aDASDASDASDASD, asdfasdf, aaaaaa
        }

        // Check for repetitive short chunks (e.g. "DAS" repeated 3+ times)
        if (/^(.{2,4})\1{2,}$/i.test(t)) {
            return true;
        }

        // Common keyboard mashing strings
        var mashes = ['asdfghjkl', 'qwertyuiop', 'zxcvbnm', 'asdfasdf', 'dasdas', 'sdasda', '12345678'];
        var lower = t.toLowerCase();
        for (var k = 0; k < mashes.length; k++) {
            if (lower.indexOf(mashes[k]) !== -1) return true;
        }

        return false;
    }

    var SJTValidator = {
        domainTypos: domainTypos,
        disposableDomains: disposableDomains,

        detectTypo: function (domain) {
            if (!domain) return null;
            var d = domain.trim().toLowerCase();

            // 1. Exact dictionary match
            if (domainTypos[d]) {
                return domainTypos[d];
            }

            // 2. Pattern based matching for gmail typos (e.g. gail.com, gmil.com, gmeil.com)
            if (d !== 'gmail.com' && d !== 'googlemail.com') {
                if (/^g[a-z]{2,5}l\.(com|co|in|co\.in)$/.test(d)) return 'gmail.com';
                if (/^g.*mail\.(com|co|in)$/.test(d)) return 'gmail.com';
                if (/^gm.*l\.(com|co|in)$/.test(d)) return 'gmail.com';
            }

            // Pattern for yahoo typos
            if (d !== 'yahoo.com' && d !== 'yahoo.co.in' && d !== 'yahoo.in') {
                if (/^y.*hoo\.(com|co|in)$/.test(d)) return 'yahoo.com';
            }

            // Pattern for hotmail typos
            if (d !== 'hotmail.com') {
                if (/^hot.*m.*l\.(com|co|in)$/.test(d)) return 'hotmail.com';
            }

            // Pattern for outlook typos
            if (d !== 'outlook.com') {
                if (/^out.*l.*k\.(com|co|in)$/.test(d)) return 'outlook.com';
            }

            // Pattern for rediffmail typos
            if (d !== 'rediffmail.com') {
                if (/^redif.*mail\.(com|co|in)$/.test(d)) return 'rediffmail.com';
            }

            // 3. Levenshtein Distance matching against major targets
            for (var i = 0; i < majorTargets.length; i++) {
                var item = majorTargets[i];
                if (d === item.target) continue;

                var distFull = levenshteinDistance(d, item.target);
                if (distFull <= 2) {
                    return item.label;
                }

                // Check Second-Level Domain (e.g. "gail" in "gail.com")
                var sld = d.split('.')[0];
                if (sld && sld !== item.sld) {
                    var distSld = levenshteinDistance(sld, item.sld);
                    if (distSld <= 2 && distSld > 0) {
                        return item.label;
                    }
                }
            }

            return null;
        },

        validateEmail: function (email) {
            if (!email || email.trim().length === 0) {
                return { valid: false, message: 'ईमेल पता आवश्यक है (Email address is required).' };
            }
            var val = email.trim().toLowerCase();
            if (val.length < 5 || val.length > 254) {
                return { valid: false, message: 'ईमेल 5 से 254 अक्षरों के बीच होना चाहिए।' };
            }
            if (val.indexOf('~') !== -1 || val.indexOf(' ') !== -1 || val.indexOf('..') !== -1 || val.indexOf(',') !== -1 || val.indexOf(';') !== -1) {
                return { valid: false, message: 'ईमेल में अमान्य वर्ण (characters) हैं।' };
            }
            var emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,24}$/;
            if (!emailRegex.test(val)) {
                return { valid: false, message: 'अमान्य प्रारूप! सही ईमेल दर्ज करें (जैसे: name@gmail.com)।' };
            }
            var parts = val.split('@');
            if (parts.length !== 2) {
                return { valid: false, message: 'अमान्य ईमेल संरचना।' };
            }
            var username = parts[0];
            var domain = parts[1];

            if (username.length < 2) {
                return { valid: false, message: 'ईमेल यूजरनेम बहुत छोटा है।' };
            }

            // 1. Detect misspelled domain typos (e.g. gmil.com, gail.com -> gmail.com)
            var typoSuggestion = this.detectTypo(domain);
            if (typoSuggestion) {
                return {
                    valid: false,
                    isTypo: true,
                    suggested: typoSuggestion,
                    message: "गलत डोमेन '" + domain + "'! क्या आपका मतलब @" + typoSuggestion + " है?"
                };
            }

            // 2. Reject disposable domains
            if (disposableDomains.indexOf(domain) !== -1) {
                return { valid: false, message: 'अस्थायी / डिस्पोजेबल ईमेल स्वीकार्य नहीं हैं। कृपया अपना वास्तविक ईमेल दें।' };
            }

            // 3. TLD checks
            var dotIndex = domain.lastIndexOf('.');
            if (dotIndex === -1) {
                return { valid: false, message: 'डोमेन में .com या .in जैसा एक्सटेंशन होना चाहिए।' };
            }
            var tld = domain.substring(dotIndex + 1);
            if (tld.length < 2) {
                return { valid: false, message: 'अमान्य टॉप-लेवल डोमेन।' };
            }

            // 4. Reject bot TLDs
            if (botTlds.indexOf(tld) !== -1) {
                return { valid: false, message: '.' + tld + ' डोमेन स्वीकार्य नहीं हैं। कृपया .com, .in, .org ईमेल का उपयोग करें।' };
            }

            return { valid: true, clean: val, message: 'मान्य ईमेल प्रारूप' };
        },

        validateName: function (name) {
            if (!name || name.trim().length === 0) {
                return { valid: false, message: 'पूरा नाम आवश्यक है (Full name is required).' };
            }
            var val = name.trim();
            if (val.length < 2) {
                return { valid: false, message: 'नाम कम से कम 2 अक्षरों का होना चाहिए।' };
            }
            if (val.length > 60) {
                return { valid: false, message: 'नाम 60 अक्षरों से अधिक नहीं हो सकता।' };
            }
            var spamChars = /http|https|\.cc|\.com|\*|~|<|>|\$|{|}|\[|\]/i;
            if (spamChars.test(val)) {
                return { valid: false, message: 'नाम में विशेष चिह्न, ब्रैकेट या लिंक की अनुमति नहीं है।' };
            }
            var nameRegex = /^[a-zA-Z\u0900-\u097F\s\.\'\-]{2,60}$/;
            if (!nameRegex.test(val)) {
                return { valid: false, message: 'कृपया केवल वास्तविक नाम (अक्षर और स्पेस) दर्ज करें।' };
            }
            return { valid: true, clean: val, message: 'मान्य नाम' };
        },

        validateMobile: function (phone, isRequired) {
            if (!phone || phone.trim().length === 0) {
                if (isRequired) {
                    return { valid: false, message: 'मोबाइल नंबर आवश्यक है (Mobile number is required).' };
                }
                return { valid: true, clean: '', message: '' };
            }
            var clean = phone.replace(/[^0-9]/g, '');
            if (clean.length === 12 && clean.indexOf('91') === 0) {
                clean = clean.substring(2);
            } else if (clean.length === 11 && clean.indexOf('0') === 0) {
                clean = clean.substring(1);
            }

            if (clean.length !== 10) {
                return { valid: false, message: 'कृपया 10 अंकों का मोबाइल नंबर दर्ज करें।' };
            }
            if (!/^[6-9]/.test(clean)) {
                return { valid: false, message: 'भारतीय मोबाइल नंबर 6, 7, 8 या 9 से शुरू होना चाहिए।' };
            }
            if (/^(\d)\1{9}$/.test(clean) || clean === '1234567890') {
                return { valid: false, message: 'कृपया एक वैध एवं सक्रिय मोबाइल नंबर दर्ज करें।' };
            }
            return { valid: true, clean: clean, message: 'मान्य 10 अंकों का मोबाइल नंबर' };
        },

        validateMessage: function (msg) {
            if (!msg || msg.trim().length === 0) {
                return { valid: false, message: 'संदेश लिखना आवश्यक है (Message is required).' };
            }
            var val = msg.trim();
            if (val.length < 8) {
                return { valid: false, message: 'संदेश बहुत छोटा है (कम से कम 8 अक्षर लिखें)।' };
            }
            if (val.length > 2000) {
                return { valid: false, message: 'संदेश बहुत लंबा है (अधिकतम 2000 अक्षर)।' };
            }
            var urlMatches = (val.match(/(https?:\/\/|www\.)/gi) || []).length;
            if (urlMatches > 1) {
                return { valid: false, message: 'संदेश में प्रचारक लिंक या एक से अधिक URL की अनुमति नहीं है।' };
            }
            if (isGibberish(val)) {
                return { valid: false, message: 'कृपया एक सार्थक संदेश लिखें (Please enter a meaningful inquiry message).' };
            }
            return { valid: true, clean: val, message: 'मान्य संदेश' };
        },

        validateSubject: function (sub) {
            if (!sub || sub.trim().length === 0) {
                return { valid: true, clean: 'General Inquiry' };
            }
            var val = sub.trim().replace(/\[/g, '').replace(/\]/g, '').trim();
            if (val.length > 80) val = val.substring(0, 80);
            if (isGibberish(val)) {
                return { valid: false, message: 'कृपया एक सार्थक विषय लिखें (Please enter a meaningful subject).' };
            }
            return { valid: true, clean: val };
        }
    };

    global.SJTValidator = SJTValidator;
})(typeof window !== 'undefined' ? window : this);
