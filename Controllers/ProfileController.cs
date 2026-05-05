using Microsoft.AspNetCore.Mvc;
using AmarShowsBook.Data;
using AmarShowsBook.Models;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public class ProfileController : Controller
{
    private readonly ApplicationDbContext _context;
    private static readonly Regex EmailRegex = new(@"^[a-zA-Z0-9._%+-]+@(gmail\.com|outlook\.com)$", RegexOptions.Compiled);
    private static readonly Regex MobileRegex = new(@"^[0-9]{10}$", RegexOptions.Compiled);

    public ProfileController(ApplicationDbContext context)
    {
        _context = context;
    }

    // LOAD PROFILE
    public IActionResult Index()
    {
        return RedirectToAction("MyProfile");
    }

    public IActionResult MyProfile()
    {
        var email = HttpContext.Session.GetString("UserEmail");

        if (string.IsNullOrWhiteSpace(email))
        {
            return RedirectToAction("Login", "Auth");
        }

        var user = _context.Users.FirstOrDefault(u => u.Email == email);

        if (user == null)
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Auth");
        }

        if (user != null)
        {
            HttpContext.Session.SetString("ProfileImage", user.ProfileImagePath ?? "");
            HttpContext.Session.SetString("UserName", user.Name ?? user.Email);
            HttpContext.Session.SetString("UserGenre", user.Genre ?? "Dramatic");
            HttpContext.Session.SetString("UserLanguage", user.Language ?? "English");
        }

        return View(user);
    }

    // SAVE PROFILE
    [HttpPost]
    public IActionResult Index(User model, IFormFile profileImage)
    {
        return MyProfile(model, profileImage);
    }

    [HttpPost]
    public IActionResult MyProfile(User model, IFormFile profileImage)
    {
        try
        {
        var email = HttpContext.Session.GetString("UserEmail");

        var user = _context.Users.FirstOrDefault(u => u.Email == email);

        if (user == null) return RedirectToAction("Login", "Auth");

        var newEmail = model.Email?.Trim().ToLower();
        var newMobile = model.Mobile?.Trim();
        var newName = model.Name?.Trim();
        var newAddress = model.Address?.Trim();
        var newCountry = model.Country ?? user.Country;
        var newState = model.State ?? user.State;
        var newDistrict = model.District ?? user.District;
        var newPincode = model.Pincode ?? user.Pincode;
        var newGenre = model.Genre ?? user.Genre;
        var newLanguage = model.Language ?? user.Language;

        if (string.IsNullOrWhiteSpace(newEmail) || string.IsNullOrWhiteSpace(newMobile) || string.IsNullOrWhiteSpace(newName))
        {
            TempData["Error"] = "Stage name, email, and mobile are required.";
            return View("MyProfile", user);
        }

        if (!EmailRegex.IsMatch(newEmail))
        {
            TempData["Error"] = "Only Gmail or Outlook email is allowed.";
            return View("MyProfile", user);
        }

        if (!MobileRegex.IsMatch(newMobile))
        {
            TempData["Error"] = "Mobile must be exactly 10 digits.";
            return View("MyProfile", user);
        }

        var emailChanged = !string.Equals(newEmail, user.Email, StringComparison.Ordinal);
        var mobileChanged = !string.Equals(newMobile, user.Mobile, StringComparison.Ordinal);
        var isChanged =
            !string.Equals(newName, user.Name, StringComparison.Ordinal) ||
            emailChanged ||
            mobileChanged ||
            !string.Equals(newAddress, user.Address, StringComparison.Ordinal) ||
            !string.Equals(newCountry, user.Country, StringComparison.Ordinal) ||
            !string.Equals(newState, user.State, StringComparison.Ordinal) ||
            !string.Equals(newDistrict, user.District, StringComparison.Ordinal) ||
            !string.Equals(newPincode, user.Pincode, StringComparison.Ordinal) ||
            !string.Equals(newGenre, user.Genre, StringComparison.Ordinal) ||
            !string.Equals(newLanguage, user.Language, StringComparison.Ordinal) ||
            (profileImage != null && profileImage.Length > 0);

        if (!isChanged)
        {
            TempData["Error"] = "There is nothing to update.";
            return View("MyProfile", user);
        }

        if (emailChanged && HttpContext.Session.GetString("VerifiedEmailForProfile") != newEmail)
        {
            TempData["Error"] = "Please verify the new email OTP before saving.";
            return View("MyProfile", user);
        }

        if (mobileChanged && HttpContext.Session.GetString("VerifiedMobileForProfile") != newMobile)
        {
            TempData["Error"] = "Please verify the new mobile OTP before saving.";
            return View("MyProfile", user);
        }

        // ================= EMAIL UNIQUE CHECK =================
        if (_context.Users.Any(u => u.Email == newEmail && u.Id != user.Id))
        {
            TempData["Error"] = "Email already exists";
            return View("MyProfile", user);
        }

        // ================= MOBILE UNIQUE CHECK =================
        if (_context.Users.Any(u => u.Mobile == newMobile && u.Id != user.Id))
        {
            TempData["Error"] = "Mobile already exists";
            return View("MyProfile", user);
        }

        // ================= UPDATE FIELDS =================
        user.Email = newEmail;
        user.Mobile = newMobile;
        user.Name = newName;
        user.Address = newAddress;

user.Country = newCountry;
user.State = newState;
user.District = newDistrict;
user.Pincode = newPincode;
        user.Genre = newGenre;
        user.Language = newLanguage;

        var imageChanged = false;

        // ================= IMAGE UPLOAD =================
       if (profileImage != null && profileImage.Length > 0)
{
    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");

    if (!Directory.Exists(uploadsFolder))
        Directory.CreateDirectory(uploadsFolder);

    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(profileImage.FileName);

    var filePath = Path.Combine(uploadsFolder, fileName);

    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        profileImage.CopyTo(stream);
    }

    user.ProfileImagePath = "/uploads/" + fileName;
    imageChanged = true;
}
        // ================= AUDIT =================
var currentUser = HttpContext.Session.GetString("UserEmail");

user.UpdatedAt = DateTime.UtcNow;
user.UpdatedBy = currentUser ?? "System";

        _context.SaveChanges();

        if (emailChanged)
        {
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.Remove("VerifiedEmailForProfile");
        }

        if (mobileChanged)
        {
            HttpContext.Session.Remove("VerifiedMobileForProfile");
        }

        if (imageChanged)
        {
            HttpContext.Session.SetString("ProfileImage", user.ProfileImagePath ?? "");
        }

        HttpContext.Session.SetString("UserName", user.Name ?? user.Email);
        HttpContext.Session.SetString("UserGenre", user.Genre ?? "Dramatic");
        HttpContext.Session.SetString("UserLanguage", user.Language ?? "English");

        TempData["Success"] = "Profile updated successfully";

        return RedirectToAction("MyProfile");
        }
        catch
        {
            TempData["Error"] = "The profile scene could not be saved. Please try again.";
            return RedirectToAction("MyProfile");
        }
    }

    [HttpPost]
    public IActionResult ChangePassword(string verifiedEmail, string newPassword, string confirmPassword)
    {
        try
        {
            var sessionEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrWhiteSpace(sessionEmail))
            {
                return RedirectToAction("Login", "Auth");
            }

            verifiedEmail = verifiedEmail?.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(verifiedEmail) || !EmailRegex.IsMatch(verifiedEmail))
            {
                TempData["Error"] = "Enter a valid Gmail or Outlook email before changing password.";
                return RedirectToAction("MyProfile");
            }

            if (!string.Equals(verifiedEmail, sessionEmail, StringComparison.OrdinalIgnoreCase) ||
                HttpContext.Session.GetString("VerifiedEmailForProfile") != verifiedEmail)
            {
                TempData["Error"] = "Verify your current email first, then change the password.";
                return RedirectToAction("MyProfile");
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            {
                TempData["Error"] = "New password must be minimum 8 characters.";
                return RedirectToAction("MyProfile");
            }

            if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
            {
                TempData["Error"] = "Both new password fields must match.";
                return RedirectToAction("MyProfile");
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == sessionEmail);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = sessionEmail;
            _context.SaveChanges();

            HttpContext.Session.Remove("VerifiedEmailForProfile");
            TempData["Success"] = "Password changed. Your next login has a fresh script.";
            return RedirectToAction("MyProfile");
        }
        catch
        {
            TempData["Error"] = "Password change could not be completed. Please try again.";
            return RedirectToAction("MyProfile");
        }
    }
}
