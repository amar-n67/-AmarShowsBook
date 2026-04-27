using Microsoft.AspNetCore.Mvc;

namespace AmarShowsBook.Controllers
{
    public class OtpController : Controller
    {
        private static Dictionary<string, string> emailOtps = new();
        private static Dictionary<string, string> mobileOtps = new();

        // EMAIL OTP
        [HttpPost]
        public IActionResult SendEmailOtp(string email)
        {
            var otp = new Random().Next(100000, 999999).ToString();
            emailOtps[email] = otp;

            Console.WriteLine($"Email OTP for {email}: {otp}");

            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult VerifyEmailOtp(string email, string otp)
        {
            if (emailOtps.ContainsKey(email) && emailOtps[email] == otp)
                return Json(new { success = true });

            return Json(new { success = false });
        }

        // MOBILE OTP
        [HttpPost]
        public IActionResult SendMobileOtp(string mobile)
        {
            var otp = new Random().Next(100000, 999999).ToString();
            mobileOtps[mobile] = otp;

            Console.WriteLine($"Mobile OTP for {mobile}: {otp}");

            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult VerifyMobileOtp(string mobile, string otp)
        {
            if (mobileOtps.ContainsKey(mobile) && mobileOtps[mobile] == otp)
                return Json(new { success = true });

            return Json(new { success = false });
        }
    }
}