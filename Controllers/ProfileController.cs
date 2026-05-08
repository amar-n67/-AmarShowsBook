using AmarShowsBook.Services; // Added for activity logging
using Npgsql;                 // Added for PostgreSQL exception handling
using Microsoft.AspNetCore.Mvc;             
using AmarShowsBook.Data;               
using AmarShowsBook.Models;                             
using System.IO;        // Added for file handling
using System.Linq;      // Added for LINQ queries
using System.Text.RegularExpressions;                   

public class ProfileController : Controller
{
    private readonly ApplicationDbContext _context;
    // ====================== added logger + activity logger ======================
    private readonly ILogger<ProfileController> _logger;
    private readonly IActivityLogger _activityLogger;
    // ====================== End of added logger + activity logger ======================
    private static readonly Regex EmailRegex = new(@"^[a-zA-Z0-9._%+-]+@(gmail\.com|outlook\.com)$", RegexOptions.Compiled);
    private static readonly Regex MobileRegex = new(@"^[0-9]{10}$", RegexOptions.Compiled);
    private static readonly Regex PasswordRegex = new(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*[^a-zA-Z0-9]).{8,}$", RegexOptions.Compiled);
    private const string PasswordRuleMessage = "New password must be at least 8 characters and include uppercase, lowercase, and special character.";

    // ====================== commented out old constructor ======================
    // public ProfileController(ApplicationDbContext context)
    // {
    //     _context = context;
    // }
    // ====================== Updated constructor to include activity logger ======================
    public ProfileController(
        ILogger<ProfileController> logger,
        ApplicationDbContext context,
        IActivityLogger activityLogger)
        {
            _logger = logger;
            _context = context;
            _activityLogger = activityLogger;
        }
    // ====================== End of updated constructor ======================
    // LOAD PROFILE
    //public IActionResult Index() //==== commented out old Index action to reuse for saving profile
    public async Task<IActionResult> Index() // Updated to async for future activity logging
    {
        return RedirectToAction("MyProfile");
    }

    //public IActionResult MyProfile() //commented out old MyProfile action to reuse for saving profile
    public async Task<IActionResult> MyProfile()
{
    try
    {
        var userEmail = HttpContext.Session.GetString("UserEmail");

        // ====================== Unauthorized access logging ======================
        if (string.IsNullOrWhiteSpace(userEmail))
        {
            await _activityLogger.LogAsync(
                action: "UNAUTHORIZED_PROFILE_ACCESS",
                module: "PROFILE",
                entityType: "USER",
                description: "Unauthorized access attempt to profile page",
                status: "FAILURE",
                isError: 4
            );

            return RedirectToAction("Login", "Auth");
        }

        var user = _context.Users
            .FirstOrDefault(u => u.Email == userEmail);

        if (user == null)
        {
            await _activityLogger.LogAsync(
                action: "PROFILE_USER_NOT_FOUND",
                module: "PROFILE",
                entityType: "USER",
                description: "Profile user not found in database",
                status: "FAILURE",
                isError: 1
            );

            HttpContext.Session.Clear();

            return RedirectToAction("Login", "Auth");
        }

        HttpContext.Session.SetString("ProfileImage",
            user.ProfileImagePath ?? "");

        HttpContext.Session.SetString("UserName",
            user.Name ?? user.Email);

        HttpContext.Session.SetString("UserGenre",
            user.Genre ?? "Dramatic");

        HttpContext.Session.SetString("UserLanguage",
            user.Language ?? "English");

        // ====================== Success activity log ======================
        await _activityLogger.LogAsync(
            userId: user.Id,
            action: "VIEW_PROFILE",
            module: "PROFILE",
            entityType: "USER",
            entityId: user.Id,
            description: "User viewed profile page",
            status: "SUCCESS",
            isError: 0
        );

        return View(user);
    }
    catch (PostgresException ex)
    {
        await _activityLogger.LogAsync(
            action: "VIEW_PROFILE",
            module: "PROFILE",
            entityType: "USER",
            description: "Database error while loading profile",
            status: "FAILURE",
            errorCode: ex.SqlState,
            errorMessage: ex.Message,
            errorSource: "PostgreSQL",
            stackTrace: ex.StackTrace,
            isError: 2
        );

        throw;
    }
    catch (Exception ex)
    {
        await _activityLogger.LogAsync(
            action: "VIEW_PROFILE",
            module: "PROFILE",
            entityType: "USER",
            description: "Unexpected error while loading profile",
            status: "FAILURE",
            errorCode: "APP500",
            errorMessage: ex.Message,
            errorSource: "Application",
            stackTrace: ex.StackTrace,
            isError: 1
        );

        throw;
    }
}
//update above profile action
    // SAVE PROFILE
    //[HttpPost]
    //public IActionResult MyProfile(User model, IFormFile profileImage) //commented out old MyProfile action to reuse for saving profile

    [HttpPost]
    //public IActionResult MyProfile(User model, IFormFile profileImage) //commented out old MyProfile action to reuse for saving profile
    public async Task<IActionResult> MyProfile(User model, IFormFile profileImage) //added async for future activity logging
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
        // ============ added activity log for profile update ============
        await _activityLogger.LogAsync(
    userId: user.Id,
    action: "UPDATE_PROFILE",
    module: "PROFILE",
    entityType: "USER",
    entityId: user.Id,
    description: "User updated profile successfully",
    status: "SUCCESS",
    isError: 0,
    metadata: new
    {
        user.Name,
        user.Email,
        user.Mobile,
        ImageUpdated = imageChanged
    }
);
        //==========end of added activity log for profile update ===========

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
        //==============commented out old catch block to reuse for saving profile with activity logging =================
        // catch
        // {
        //     TempData["Error"] = "The profile scene could not be saved. Please try again.";
        //     return RedirectToAction("MyProfile");
        // }
        //==================================updated catch blocks to log profile update failure =================
        catch (Exception ex)
{
    await _activityLogger.LogAsync(
        action: "UPDATE_PROFILE",
        module: "PROFILE",
        entityType: "USER",
        description: "Profile update failed",
        status: "FAILURE",
        errorCode: "APP500",
        errorMessage: ex.Message,
        errorSource: "Application",
        stackTrace: ex.StackTrace,
        isError: 1
    );

    TempData["Error"] =
        "The profile scene could not be saved. Please try again.";

    return RedirectToAction("MyProfile");
}
    //=======================================end of added activity log for profile update failure ===
    }

    [HttpPost]
    //public IActionResult ChangePassword(string verifiedEmail, string newPassword, string confirmPassword)//commented out old ChangePassword action to reuse for password change with activity logging
   
   public async Task<IActionResult> ChangePassword(string verifiedEmail, string newPassword, string confirmPassword) //added async for future activity logging
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

            if (string.IsNullOrWhiteSpace(newPassword) || !PasswordRegex.IsMatch(newPassword))
            {
                TempData["Error"] = PasswordRuleMessage;
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
            // ============ added activity log for password change ============
            await _activityLogger.LogAsync(
    userId: user.Id,
    action: "CHANGE_PASSWORD",
    module: "PROFILE",
    entityType: "USER",
    entityId: user.Id,
    description: "User changed password successfully",
    status: "SUCCESS",
    isError: 0
);
//==========end of added activity log for password change ===========

            HttpContext.Session.Remove("VerifiedEmailForProfile");
            TempData["Success"] = "Password changed. Your next login has a fresh script.";
            return RedirectToAction("MyProfile");
        }
        // catch
        // {
        //     TempData["Error"] = "Password change could not be completed. Please try again.";
        //     return RedirectToAction("MyProfile");
        // }
        //===================updated catch block to log password change failure =================
        catch (Exception ex)
{
    await _activityLogger.LogAsync(
        action: "CHANGE_PASSWORD",
        module: "PROFILE",
        entityType: "USER",
        description: "Password change failed",
        status: "FAILURE",
        errorCode: "APP500",
        errorMessage: ex.Message,
        errorSource: "Application",
        stackTrace: ex.StackTrace,
        isError: 1
    );

    TempData["Error"] =
        "Password change could not be completed. Please try again.";

    return RedirectToAction("MyProfile");
}
//=======================================end of added activity log for password change failure =================
    }
}
