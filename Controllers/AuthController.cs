using Microsoft.AspNetCore.Mvc;
using AmarShowsBook.Data;
using AmarShowsBook.Models;
using System.Linq;
using System.Text.RegularExpressions;

namespace AmarShowsBook.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHostApplicationLifetime _applicationLifetime;

        private static readonly Regex EmailRegex = new(@"^[a-zA-Z0-9._%+-]+@(gmail\.com|outlook\.com)$", RegexOptions.Compiled);
        private static readonly Regex MobileRegex = new(@"^[0-9]{10}$", RegexOptions.Compiled);

        // OTP temporary storage (dev only)
        private static string generatedOTP;
        private static string resetEmail;

        public AuthController(ApplicationDbContext context, IHostApplicationLifetime applicationLifetime)
        {
            _context = context;
            _applicationLifetime = applicationLifetime;
        }

        // ================= LOGIN =================

        public IActionResult Login()
        {
            return View();
        }

       [HttpPost]
public IActionResult Login(string email, string password)
{
    try
    {
    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
    {
        ViewBag.Error = "Missing credentials. The show cannot start without email and password.";
        return View();
    }

    if (!EmailRegex.IsMatch(email.Trim().ToLower()))
    {
        ViewBag.Error = "Only Gmail or Outlook email is allowed.";
        return View();
    }

    if (password.Length < 8)
    {
        ViewBag.Error = "Password must be minimum 8 characters.";
        return View();
    }

    // normalize email
    email = email.Trim().ToLower();

    var user = _context.Users.FirstOrDefault(u => u.Email.ToLower() == email);

    if (user == null)
    {
        ViewBag.Error = "No performer found with this email.";
        return View();
    }

    bool isValid = BCrypt.Net.BCrypt.Verify(password, user.Password);

    if (isValid)
    {
        HttpContext.Session.SetString("UserEmail", user.Email);
        HttpContext.Session.SetString("UserName", user.Name ?? user.Email);
        HttpContext.Session.SetString("UserGenre", user.Genre ?? "Dramatic");
        HttpContext.Session.SetString("UserLanguage", user.Language ?? "English");

        HttpContext.Session.SetString("ProfileImage", user.ProfileImagePath ?? "");

        return RedirectToAction("Index", "Home");
    }

    ViewBag.Error = "Wrong script. Password did not match.";
    return View();
    }
    catch
    {
        ViewBag.Error = "The projector had a technical pause. Please try login again.";
        return View();
    }
}

        [HttpPost]
        public IActionResult CloseApplication()
        {
            Task.Run(async () =>
            {
                await Task.Delay(500);
                _applicationLifetime.StopApplication();
            });

            return Json(new { success = true, message = "Application closing." });
        }

        [NonAction]
        private void PrepareSignupDefaults(User user)
        {
            user.Email = user.Email?.Trim().ToLower();
            user.Mobile = user.Mobile?.Trim();
            user.Name = user.Name?.Trim();
        }

        [NonAction]
        private bool ValidateSignup(User user)
        {
            PrepareSignupDefaults(user);

            if (string.IsNullOrWhiteSpace(user.Email) || !EmailRegex.IsMatch(user.Email))
            {
                ModelState.AddModelError("Email", "Only Gmail or Outlook email is allowed.");
            }

            if (string.IsNullOrWhiteSpace(user.Mobile) || !MobileRegex.IsMatch(user.Mobile))
            {
                ModelState.AddModelError("Mobile", "Mobile must be exactly 10 digits.");
            }

            if (string.IsNullOrWhiteSpace(user.Password) || user.Password.Length < 8)
            {
                ModelState.AddModelError("Password", "Password must be minimum 8 characters.");
            }

            return ModelState.IsValid;
        }

        [NonAction]
        private IActionResult SignupError(User user, string message)
        {
            ViewBag.Error = message;
            return View("Signup", user);
        }

        // ================= SIGNUP =================

        public IActionResult Signup()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Signup(User user)
        {
            try
            {
            if (!ValidateSignup(user))
            {
                return View(user);
            }

            if (_context.Users.Any(u => u.Mobile == user.Mobile))
            {
                return SignupError(user, "This mobile number already has a ticket in our records.");
            }

            if (_context.Users.Any(u => u.Email.ToLower() == user.Email.ToLower()))
            {
                return SignupError(user, "This email is already registered. Try login or use another email.");
            }

            // Hash password
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
            user.CreatedAt = DateTime.UtcNow;
user.CreatedBy = user.Email;
if (string.IsNullOrEmpty(user.Genre))
    user.Genre = "Dramatic";

if (string.IsNullOrEmpty(user.Language))
    user.Language = "English";

            _context.Users.Add(user);
            _context.SaveChanges();

            return RedirectToAction("Login");
            }
            catch
            {
                return SignupError(user, "The signup scene could not be saved. Please try again.");
            }
        }

        // ================= FORGOT PASSWORD =================

        public IActionResult ForgotPassword()
        {
            return View();
        }

        // STEP 1: Send OTP
        [HttpPost]
        public IActionResult SendOTP(string email)
        {
            email = email?.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(email) || !EmailRegex.IsMatch(email))
            {
                ViewBag.Error = "Enter a valid Gmail or Outlook email.";
                return View("ForgotPassword");
            }

            var user = _context.Users.FirstOrDefault(u => u.Email.ToLower() == email);

            if (user == null)
            {
                ViewBag.Error = "No account found with this email.";
                return View("ForgotPassword");
            }

            var rand = new Random();
            generatedOTP = rand.Next(100000, 999999).ToString();
            resetEmail = email;

            // DEV ONLY (prints in terminal)
            Console.WriteLine("OTP: " + generatedOTP);

            ViewBag.Message = "OTP sent (check terminal)";
            return View("VerifyOTP");
        }

        // STEP 2: Verify OTP
        [HttpPost]
        public IActionResult VerifyOTP(string otp)
        {
            if (otp == generatedOTP)
            {
                return View("ResetPassword");
            }

            ViewBag.Error = "Invalid OTP";
            return View("VerifyOTP");
        }

        // STEP 3: Reset Password
        [HttpPost]
        public IActionResult ResetPassword(string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            {
                ViewBag.Error = "Password must be minimum 8 characters.";
                return View("ResetPassword");
            }

            if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
            {
                ViewBag.Error = "Both password fields must match.";
                return View("ResetPassword");
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == resetEmail);

            if (user != null)
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(password);
                _context.SaveChanges();
            }

            return RedirectToAction("Login");
        }

        // ================= LOGOUT =================

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
