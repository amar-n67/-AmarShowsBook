using AmarShowsBook.Services;
using Npgsql;
using Microsoft.AspNetCore.Mvc;             
using AmarShowsBook.Data;               
using AmarShowsBook.Helpers;
using AmarShowsBook.Models;                             
using System.IO;
using System.Text.RegularExpressions;                   

// Profile pages only work with the signed-in user's row and update session values after saved changes.
public class ProfileController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProfileController> _logger;
    private readonly IActivityLogger _activityLogger;
    private static readonly Regex EmailRegex = new(@"^[a-zA-Z0-9._%+-]+@(gmail\.com|outlook\.com)$", RegexOptions.Compiled);
    private static readonly Regex MobileRegex = new(@"^[0-9]{10}$", RegexOptions.Compiled);
    private static readonly Regex PasswordRegex = new(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*[^a-zA-Z0-9]).{8,}$", RegexOptions.Compiled);
    private const string PasswordRuleMessage = "New password must be at least 8 characters and include uppercase, lowercase, and special character.";

    public ProfileController(
        ILogger<ProfileController> logger,
        ApplicationDbContext context,
        IActivityLogger activityLogger)
        {
            _logger = logger;
            _context = context;
            _activityLogger = activityLogger;
        }
    public async Task<IActionResult> Index()
    {
        return RedirectToAction("MyProfile");
    }

    public async Task<IActionResult> MyProfile()
{
    try
    {
        var userEmail = HttpContext.Session.GetString("UserEmail");

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

    [HttpPost]
    public async Task<IActionResult> MyProfile(User model, IFormFile profileImage)
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

        if (_context.Users.Any(u => u.Email == newEmail && u.Id != user.Id))
        {
            TempData["Error"] = "Email already exists";
            return View("MyProfile", user);
        }

        if (_context.Users.Any(u => u.Mobile == newMobile && u.Id != user.Id))
        {
            TempData["Error"] = "Mobile already exists";
            return View("MyProfile", user);
        }

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
var currentUser = HttpContext.Session.GetString("UserEmail");

user.UpdatedAt = DateTime.UtcNow;
user.UpdatedBy = currentUser ?? "System";

        _context.SaveChanges();
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

    // Previous wording: "The profile scene could not be saved. Please try again."
    TempData["Error"] =
        "We could not save your profile. Please try again.";

    return RedirectToAction("MyProfile");
}
    }

    [HttpPost]
   
   public async Task<IActionResult> ChangePassword(string verifiedEmail, string newPassword, string confirmPassword)
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

            HttpContext.Session.Remove("VerifiedEmailForProfile");
            TempData["Success"] = "Password changed. Your next login has a fresh script.";
            return RedirectToAction("MyProfile");
        }
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
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccount(string password, string confirmationText, string deletionReason)
    {
        try
        {
            var sessionEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrWhiteSpace(sessionEmail))
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == sessionEmail);
            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Auth");
            }

            if (HttpContext.Session.GetString("VerifiedEmailForProfile") != sessionEmail)
            {
                TempData["Error"] = "Verify your email OTP before deleting the account.";
                return RedirectToAction("MyProfile");
            }

            if (string.IsNullOrWhiteSpace(password) ||
                !BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                TempData["Error"] = "Password validation failed.";
                return RedirectToAction("MyProfile");
            }

            if (!string.Equals((confirmationText ?? string.Empty).Trim(), "DELETE MY ACCOUNT", StringComparison.Ordinal))
            {
                TempData["Error"] = "Type DELETE MY ACCOUNT to confirm account deletion.";
                return RedirectToAction("MyProfile");
            }

            var archiveId = await ArchiveUserAccount(user.Id, sessionEmail, deletionReason);

            await _activityLogger.LogAsync(
                userId: user.Id,
                action: "DELETE_ACCOUNT_REQUEST",
                module: "PROFILE",
                entityType: "USER",
                entityId: user.Id,
                description: "User deleted account after OTP and password validation",
                status: "SUCCESS",
                isError: 0,
                metadata: new
                {
                    ArchiveId = archiveId,
                    RecoverUntilDays = 30,
                    PurgeAfterMonths = 3
                });

            TempData["Success"] = "Your account was deleted. You can recover it within 30 days by logging in again.";
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Auth");
        }
        catch (PostgresException ex)
        {
            await _activityLogger.LogAsync(
                action: "DELETE_ACCOUNT_REQUEST",
                module: "PROFILE",
                entityType: "USER",
                description: "Account deletion stopped by database validation",
                status: "FAILURE",
                errorCode: ex.SqlState,
                errorMessage: ex.MessageText,
                errorSource: ex.TableName ?? ex.ConstraintName ?? "PostgreSQL",
                stackTrace: ex.StackTrace,
                isError: 2);

            TempData["Error"] = BuildDeleteAccountErrorMessage(ex);
            return RedirectToAction("MyProfile");
        }
        catch (NpgsqlException ex)
        {
            await _activityLogger.LogAsync(
                action: "DELETE_ACCOUNT_REQUEST",
                module: "PROFILE",
                entityType: "USER",
                description: "Account deletion stopped by database connection or command error",
                status: "FAILURE",
                errorCode: "DB_COMMAND_ERROR",
                errorMessage: ex.Message,
                errorSource: "PostgreSQL",
                stackTrace: ex.StackTrace,
                isError: 2);

            TempData["Error"] = "Account deletion did not proceed because the archive database command could not finish. Please try again after the database connection is stable.";
            return RedirectToAction("MyProfile");
        }
        catch (Exception ex)
        {
            await _activityLogger.LogAsync(
                action: "DELETE_ACCOUNT_REQUEST",
                module: "PROFILE",
                entityType: "USER",
                description: "Account deletion failed",
                status: "FAILURE",
                errorCode: "APP500",
                errorMessage: ex.Message,
                errorSource: "Application",
                stackTrace: ex.StackTrace,
                isError: 1);

            TempData["Error"] = "Account deletion did not proceed because the application hit an unexpected error after validation. Your account is still active; please try again.";
            return RedirectToAction("MyProfile");
        }
    }

    private async Task<long> ArchiveUserAccount(int userId, string deletedBy, string? deletionReason)
    {
        var connectionString =
            DatabaseConnectionStringResolver
                .GetDatabaseConnectionString(HttpContext.RequestServices.GetRequiredService<IConfiguration>());

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            "SELECT public.fn_archive_user_account(@user_id, @deleted_by, @reason);",
            connection);
        command.CommandTimeout = 120;

        command.Parameters.AddWithValue("@user_id", userId);
        command.Parameters.AddWithValue("@deleted_by", deletedBy);
        command.Parameters.AddWithValue("@reason", (object?)deletionReason ?? DBNull.Value);

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    private static string BuildDeleteAccountErrorMessage(PostgresException ex)
    {
        var relation =
            !string.IsNullOrWhiteSpace(ex.TableName)
                ? ex.TableName
                : ex.ConstraintName;

        return ex.SqlState switch
        {
            PostgresErrorCodes.ForeignKeyViolation =>
                $"Account deletion did not proceed because related data in {relation ?? "another table"} must be archived first. No account data was deleted.",
            PostgresErrorCodes.UniqueViolation =>
                "Account deletion did not proceed because an active deletion archive already exists for this account. Try logging in again to recover, or contact support.",
            PostgresErrorCodes.UndefinedTable =>
                $"Account deletion did not proceed because the archive process could not find required table {relation ?? "in the database"}.",
            PostgresErrorCodes.UndefinedColumn =>
                $"Account deletion did not proceed because a required archive column is missing in {relation ?? "the database"}.",
            PostgresErrorCodes.RaiseException =>
                $"Account deletion did not proceed because the archive procedure stopped with: {ex.MessageText}",
            _ =>
                $"Account deletion did not proceed because the database stopped the archive step: {ex.MessageText}"
        };
    }
}
