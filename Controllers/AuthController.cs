using Microsoft.AspNetCore.Mvc;
using AmarShowsBook.Data;
using AmarShowsBook.Models;
using System.Linq;

namespace AmarShowsBook.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;

        // OTP temporary storage (dev only)
        private static string generatedOTP;
        private static string resetEmail;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================= LOGIN =================

        public IActionResult Login()
        {
            return View();
        }

       [HttpPost]
public IActionResult Login(string email, string password)
{
    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
    {
        ViewBag.Error = "🎭 Missing credentials. The show can't start!";
        return View();
    }

    // normalize email
    email = email.Trim().ToLower();

    var user = _context.Users.FirstOrDefault(u => u.Email.ToLower() == email);

    if (user == null)
    {
        ViewBag.Error = "🎭 No performer found with this email.";
        return View();
    }

    bool isValid = BCrypt.Net.BCrypt.Verify(password, user.Password);

    if (isValid)
    {
        HttpContext.Session.SetString("UserEmail", user.Email);

        // ✅ ADD THIS LINE RIGHT HERE
        HttpContext.Session.SetString("ProfileImage", user.ProfileImagePath ?? "");

        return RedirectToAction("Index", "Home");
    }

    ViewBag.Error = "🎭 Wrong script! Password didn't match.";
    return View();
}

        // ================= SIGNUP =================

        public IActionResult Signup()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Signup(User user)
        {
            if (!ModelState.IsValid)
            {
                return View(user);
            }

            if (_context.Users.Any(u => u.Mobile == user.Mobile))
            {
                ViewBag.Error = "Mobile number already exists";
                return View(user);
            }

            if (_context.Users.Any(u => u.Email == user.Email))
            {
                ViewBag.Error = "Email already registered";
                return View(user);
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

        // ================= FORGOT PASSWORD =================

        public IActionResult ForgotPassword()
        {
            return View();
        }

        // STEP 1: Send OTP
        [HttpPost]
        public IActionResult SendOTP(string email)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                ViewBag.Error = "No account found with this email";
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
        public IActionResult ResetPassword(string password)
        {
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