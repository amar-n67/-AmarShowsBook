using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mail;
using System.Collections.Concurrent;

public class OtpController : Controller
{
    private static ConcurrentDictionary<string, string> emailOtps = new();

    [HttpPost]
    public IActionResult SendEmailOtp(string email)
    {
        string otp = new Random().Next(100000, 999999).ToString();

        emailOtps[email] = otp;
        HttpContext.Session.Remove("VerifiedEmailForProfile");

        try
        {
            var smtp = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("YOUR_EMAIL@gmail.com", "YOUR_APP_PASSWORD"),
                EnableSsl = true
            };

            var mail = new MailMessage
            {
                From = new MailAddress("YOUR_EMAIL@gmail.com"),
                Subject = "🎬 AmarShowsBook OTP Verification",
                Body = $"Your OTP is {otp}\n\nValid for 5 minutes.\n\nEnjoy your show 🍿",
                IsBodyHtml = false
            };

            mail.To.Add(email);

            smtp.Send(mail);

            return Json(new { success = true });
        }
        catch
        {
            return Json(new { success = false });
        }
    }

    [HttpPost]
    public IActionResult VerifyEmailOtp(string email, string otp)
    {
        if (emailOtps.ContainsKey(email) && emailOtps[email] == otp)
        {
            emailOtps.TryRemove(email, out _);
            HttpContext.Session.SetString("VerifiedEmailForProfile", email);
            return Json(new { success = true });
        }

        return Json(new { success = false });
    }
    [HttpPost]
public async Task<IActionResult> SendMobileOtp(string mobile)
{
    string otp = new Random().Next(100000, 999999).ToString();

    emailOtps[mobile] = otp;
    HttpContext.Session.Remove("VerifiedMobileForProfile");

    var client = new HttpClient();

    var request = new HttpRequestMessage(HttpMethod.Get,
        $"https://www.fast2sms.com/dev/bulkV2?authorization=YOUR_API_KEY&route=otp&variables_values={otp}&flash=0&numbers={mobile}");

    var response = await client.SendAsync(request);

    if (response.IsSuccessStatusCode)
        return Json(new { success = true });

    return Json(new { success = false });
}

[HttpPost]
public IActionResult VerifyMobileOtp(string mobile, string otp)
{
    if (emailOtps.ContainsKey(mobile) && emailOtps[mobile] == otp)
    {
        emailOtps.TryRemove(mobile, out _);
        HttpContext.Session.SetString("VerifiedMobileForProfile", mobile);
        return Json(new { success = true });
    }

    return Json(new { success = false });
}
}
