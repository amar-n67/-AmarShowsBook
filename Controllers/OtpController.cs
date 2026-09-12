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
    private readonly IActivityLogger _activityLogger;
    private static readonly Regex EmailRegex = new(@"^[a-zA-Z0-9._%+-]+@(gmail\.com|outlook\.com)$", RegexOptions.Compiled);
    private static readonly Regex MobileRegex = new(@"^[0-9]{10}$", RegexOptions.Compiled);
    private static readonly ConcurrentDictionary<string, OtpEntry> otps = new();

    public OtpController(
        OtpDeliveryService otpDeliveryService,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        IActivityLogger activityLogger)
    {
        _otpDeliveryService = otpDeliveryService;
        _configuration = configuration;
        _environment = environment;
        _activityLogger = activityLogger;
    }

    // Starts email verification and stores only the short-lived OTP in memory.
    [HttpPost]
    public async Task<IActionResult> SendEmailOtp(string email, string purpose = "email verification")
    {
        email = email?.Trim().ToLower() ?? "";

        if (!EmailRegex.IsMatch(email))
        {
            await LogOtpEvent("SEND_EMAIL_OTP", "FAILED", purpose, "Invalid email format.", email: email);
            return Json(new { success = false, message = "Enter a valid Gmail or Outlook email." });
        }

        var otp = CreateOtp();
        var cleanPurpose = CleanPurpose(purpose);
        otps[EmailKey(email)] = new OtpEntry(otp, DateTime.UtcNow.AddMinutes(GetExpiryMinutes()), cleanPurpose);
        HttpContext.Session.Remove("VerifiedEmailForProfile");

        var result = await _otpDeliveryService.SendEmailOtpAsync(email, otp, cleanPurpose);
        if (result.Success)
        {
            await LogOtpEvent("SEND_EMAIL_OTP", "SUCCESS", cleanPurpose, "Email OTP sent.", email: email);
            return Json(new { success = true, message = result.Message });
        }

        if (CanExposeDevOtp(result))
        {
            await LogOtpEvent("SEND_EMAIL_OTP", "SUCCESS", cleanPurpose, "Development email OTP generated.", email: email);
            System.Diagnostics.Debug.WriteLine($"Email OTP for {email} ({cleanPurpose}): {otp}");
            return Json(new { success = true, message = "Development OTP generated.", devOtp = otp });
        }

        otps.TryRemove(EmailKey(email), out _);
        await LogOtpEvent("SEND_EMAIL_OTP", "FAILED", cleanPurpose, result.Message, email: email);
        return Json(new { success = false, message = result.Message });
    }

    // Marks the verified email in session after matching the submitted OTP.
    [HttpPost]
    public async Task<IActionResult> VerifyEmailOtp(string email, string otp)
    {
        email = email?.Trim().ToLower() ?? "";
        otp = otp?.Trim() ?? "";

        if (TryVerify(EmailKey(email), otp))
        {
            HttpContext.Session.SetString("VerifiedEmailForProfile", email);
            await LogOtpEvent("VERIFY_EMAIL_OTP", "SUCCESS", "email verification", "Email OTP verified.", email: email);
            return Json(new { success = true });
        }

        await LogOtpEvent("VERIFY_EMAIL_OTP", "FAILED", "email verification", "Email OTP verification failed.", email: email);
        return Json(new { success = false });
    }

    // Starts mobile verification and stores only the short-lived OTP in memory.
    [HttpPost]
    public async Task<IActionResult> SendMobileOtp(string mobile, string purpose = "mobile verification")
    {
        mobile = mobile?.Trim() ?? "";

        if (!MobileRegex.IsMatch(mobile))
        {
            await LogOtpEvent("SEND_MOBILE_OTP", "FAILED", purpose, "Invalid mobile number format.", mobile: mobile);
            return Json(new { success = false, message = "Mobile must be exactly 10 digits." });
        }

        var otp = CreateOtp();
        var cleanPurpose = CleanPurpose(purpose);
        otps[MobileKey(mobile)] = new OtpEntry(otp, DateTime.UtcNow.AddMinutes(GetExpiryMinutes()), cleanPurpose);
        HttpContext.Session.Remove("VerifiedMobileForProfile");

        var result = await _otpDeliveryService.SendMobileOtpAsync(mobile, otp, cleanPurpose);
        if (result.Success)
        {
            await LogOtpEvent("SEND_MOBILE_OTP", "SUCCESS", cleanPurpose, "Mobile OTP sent.", mobile: mobile);
            return Json(new { success = true, message = result.Message });
        }

        if (CanExposeDevOtp(result))
        {
            await LogOtpEvent("SEND_MOBILE_OTP", "SUCCESS", cleanPurpose, "Development mobile OTP generated.", mobile: mobile);
            System.Diagnostics.Debug.WriteLine($"Mobile OTP for {mobile} ({cleanPurpose}): {otp}");
            return Json(new { success = true, message = "Development OTP generated.", devOtp = otp });
        }

        otps.TryRemove(MobileKey(mobile), out _);
        await LogOtpEvent("SEND_MOBILE_OTP", "FAILED", cleanPurpose, result.Message, mobile: mobile);
        return Json(new { success = false, message = result.Message });
    }

    // Marks the verified mobile number in session after matching the submitted OTP.
    [HttpPost]
    public async Task<IActionResult> VerifyMobileOtp(string mobile, string otp)
    {
        mobile = mobile?.Trim() ?? "";
        otp = otp?.Trim() ?? "";

        if (TryVerify(MobileKey(mobile), otp))
        {
            HttpContext.Session.SetString("VerifiedMobileForProfile", mobile);
            await LogOtpEvent("VERIFY_MOBILE_OTP", "SUCCESS", "mobile verification", "Mobile OTP verified.", mobile: mobile);
            return Json(new { success = true });
        }

        await LogOtpEvent("VERIFY_MOBILE_OTP", "FAILED", "mobile verification", "Mobile OTP verification failed.", mobile: mobile);
        return Json(new { success = false });
    }

    // Deletes used or expired OTPs so an old code cannot be replayed.
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

    // Development fallback is visible only when delivery is not configured and the setting allows it.
    private bool CanExposeDevOtp(OtpDeliveryResult result)
    {
        return !result.IsConfigured &&
            _environment.IsDevelopment() &&
            _configuration.GetValue("Otp:ExposeDevOtp", true);
    }

    // Keep the timeout configurable, but never let it become zero or negative.
    private int GetExpiryMinutes()
    {
        return Math.Max(1, _configuration.GetValue("Otp:ExpiryMinutes", 5));
    }

    // Six digits keeps the code familiar while avoiding predictable Random usage.
    private static string CreateOtp()
    {
        return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
    }

    // The purpose is displayed in OTP messages, so keep it short and safe for UI text.
    private static string CleanPurpose(string purpose)
    {
        purpose = string.IsNullOrWhiteSpace(purpose) ? "verification" : purpose.Trim();
        return purpose.Length > 60 ? purpose[..60] : purpose;
    }

    private async Task LogOtpEvent(
        string action,
        string status,
        string purpose,
        string description,
        string? email = null,
        string? mobile = null)
    {
        await _activityLogger.LogAsync(
            userId: GetCurrentUserId(),
            action: action,
            module: "OTP",
            entityType: email != null ? "EMAIL_OTP" : "MOBILE_OTP",
            description: description,
            status: status,
            isError: status.Equals("FAILED", StringComparison.OrdinalIgnoreCase) ? 1 : 0,
            metadata: new
            {
                email,
                mobile,
                purpose = CleanPurpose(purpose)
            });
    }

    private int? GetCurrentUserId()
    {
        return int.TryParse(HttpContext.Session.GetString("UserId"), out var userId)
            ? userId
            : null;
    }

    private static string EmailKey(string email) => $"email:{email}";

    private static string MobileKey(string mobile) => $"mobile:{mobile}";
}
