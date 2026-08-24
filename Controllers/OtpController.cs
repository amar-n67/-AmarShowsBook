using Microsoft.AspNetCore.Mvc;
using AmarShowsBook.Services;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

public class OtpController : Controller
{
    private record OtpEntry(string Code, DateTime ExpiresAtUtc, string Purpose);

    private readonly OtpDeliveryService _otpDeliveryService;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private static readonly Regex EmailRegex = new(@"^[a-zA-Z0-9._%+-]+@(gmail\.com|outlook\.com)$", RegexOptions.Compiled);
    private static readonly Regex MobileRegex = new(@"^[0-9]{10}$", RegexOptions.Compiled);
    private static readonly ConcurrentDictionary<string, OtpEntry> otps = new();

    public OtpController(OtpDeliveryService otpDeliveryService, IConfiguration configuration, IWebHostEnvironment environment)
    {
        _otpDeliveryService = otpDeliveryService;
        _configuration = configuration;
        _environment = environment;
    }

    [HttpPost]
    public async Task<IActionResult> SendEmailOtp(string email, string purpose = "email verification")
    {
        email = email?.Trim().ToLower() ?? "";

        if (!EmailRegex.IsMatch(email))
        {
            return Json(new { success = false, message = "Enter a valid Gmail or Outlook email." });
        }

        var otp = CreateOtp();
        var cleanPurpose = CleanPurpose(purpose);
        otps[EmailKey(email)] = new OtpEntry(otp, DateTime.UtcNow.AddMinutes(GetExpiryMinutes()), cleanPurpose);
        HttpContext.Session.Remove("VerifiedEmailForProfile");

        var result = await _otpDeliveryService.SendEmailOtpAsync(email, otp, cleanPurpose);
        if (result.Success)
        {
            return Json(new { success = true, message = result.Message });
        }

        if (CanExposeDevOtp(result))
        {
            System.Diagnostics.Debug.WriteLine($"Email OTP for {email} ({cleanPurpose}): {otp}");
            return Json(new { success = true, message = "Development OTP generated.", devOtp = otp });
        }

        otps.TryRemove(EmailKey(email), out _);
        return Json(new { success = false, message = result.Message });
    }

    [HttpPost]
    public IActionResult VerifyEmailOtp(string email, string otp)
    {
        email = email?.Trim().ToLower() ?? "";
        otp = otp?.Trim() ?? "";

        if (TryVerify(EmailKey(email), otp))
        {
            HttpContext.Session.SetString("VerifiedEmailForProfile", email);
            return Json(new { success = true });
        }

    return Json(new { success = false });
    }
    [HttpPost]
    public async Task<IActionResult> SendMobileOtp(string mobile, string purpose = "mobile verification")
    {
        mobile = mobile?.Trim() ?? "";

        if (!MobileRegex.IsMatch(mobile))
        {
            return Json(new { success = false, message = "Mobile must be exactly 10 digits." });
        }

        var otp = CreateOtp();
        var cleanPurpose = CleanPurpose(purpose);
        otps[MobileKey(mobile)] = new OtpEntry(otp, DateTime.UtcNow.AddMinutes(GetExpiryMinutes()), cleanPurpose);
        HttpContext.Session.Remove("VerifiedMobileForProfile");

        var result = await _otpDeliveryService.SendMobileOtpAsync(mobile, otp, cleanPurpose);
        if (result.Success)
        {
            return Json(new { success = true, message = result.Message });
        }

        if (CanExposeDevOtp(result))
        {
            System.Diagnostics.Debug.WriteLine($"Mobile OTP for {mobile} ({cleanPurpose}): {otp}");
            return Json(new { success = true, message = "Development OTP generated.", devOtp = otp });
        }

        otps.TryRemove(MobileKey(mobile), out _);
        return Json(new { success = false, message = result.Message });
    }

[HttpPost]
public IActionResult VerifyMobileOtp(string mobile, string otp)
{
    mobile = mobile?.Trim() ?? "";
    otp = otp?.Trim() ?? "";

    if (TryVerify(MobileKey(mobile), otp))
    {
        HttpContext.Session.SetString("VerifiedMobileForProfile", mobile);
        return Json(new { success = true });
    }

    return Json(new { success = false });
}

    private bool TryVerify(string key, string otp)
    {
        if (!otps.TryGetValue(key, out var entry))
        {
            return false;
        }

        if (entry.ExpiresAtUtc < DateTime.UtcNow)
        {
            otps.TryRemove(key, out _);
            return false;
        }

        if (!string.Equals(entry.Code, otp, StringComparison.Ordinal))
        {
            return false;
        }

        otps.TryRemove(key, out _);
        return true;
    }

    private bool CanExposeDevOtp(OtpDeliveryResult result)
    {
        return !result.IsConfigured &&
            _environment.IsDevelopment() &&
            _configuration.GetValue("Otp:ExposeDevOtp", true);
    }

    private int GetExpiryMinutes()
    {
        return Math.Max(1, _configuration.GetValue("Otp:ExpiryMinutes", 5));
    }

    private static string CreateOtp()
    {
        return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
    }

    private static string CleanPurpose(string purpose)
    {
        purpose = string.IsNullOrWhiteSpace(purpose) ? "verification" : purpose.Trim();
        return purpose.Length > 60 ? purpose[..60] : purpose;
    }

    private static string EmailKey(string email) => $"email:{email}";

    private static string MobileKey(string mobile) => $"mobile:{mobile}";
}
