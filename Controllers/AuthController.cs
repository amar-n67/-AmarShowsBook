using Microsoft.AspNetCore.Mvc;
using AmarShowsBook.Data;
using AmarShowsBook.Helpers;
using AmarShowsBook.Models;
using AmarShowsBook.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace AmarShowsBook.Controllers
{
    // This controller creates the session identity that the rest of the app trusts for RBAC and user-owned data.
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly OtpDeliveryService _otpDeliveryService;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly IActivityLogger _activityLogger;
        private static readonly Regex EmailRegex = new(@"^[a-zA-Z0-9._%+-]+@(gmail\.com|outlook\.com)$", RegexOptions.Compiled);
        private static readonly Regex MobileRegex = new(@"^[0-9]{10}$", RegexOptions.Compiled);
        private static readonly Regex PasswordRegex = new(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*[^a-zA-Z0-9]).{8,}$", RegexOptions.Compiled);
        private const string PasswordRuleMessage = "Password must be at least 8 characters and include uppercase, lowercase, and special character.";

        private static string? generatedOTP;
        private static DateTime resetOtpExpiresAtUtc;
        private static string? resetEmail;

        public AuthController(
    ApplicationDbContext context,
    IHostApplicationLifetime applicationLifetime,
    OtpDeliveryService otpDeliveryService,
    IConfiguration configuration,
    IWebHostEnvironment environment,
    IActivityLogger activityLogger)
        {
            _context = context;
            _applicationLifetime = applicationLifetime;
            _otpDeliveryService = otpDeliveryService;
            _configuration = configuration;
            _environment = environment;
            _activityLogger = activityLogger;
        }

        public IActionResult Login()
        {
            return View();
        }

       [HttpPost]
public async Task<IActionResult> Login(string email, string password)
{
    try
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            // Previous wording: "Missing credentials. The show cannot start without email and password."
            ViewBag.Error = "Email and password are required.";
            return View();
        }

        if (!EmailRegex.IsMatch(email.Trim().ToLower()))
        {
            ViewBag.Error = "Only Gmail or Outlook email is allowed.";
            return View();
        }

        if (!PasswordRegex.IsMatch(password))
        {
            ViewBag.Error = PasswordRuleMessage;
            return View();
        }

        email = email.Trim().ToLower();

        var user = _context.Users.FirstOrDefault(u => u.Email.ToLower() == email);

        if (user == null)
        {
            var archivedAccount = await FindRecoverableArchive(email);
            if (archivedAccount != null)
            {
                if (IsArchivePasswordValid(password, archivedAccount.PasswordHash))
                {
                    PrepareRecoveryView(archivedAccount, email);
                    await _activityLogger.LogAsync(
                        userId: archivedAccount.UserId > int.MaxValue ? null : (int)archivedAccount.UserId,
                        action: "DELETED_ACCOUNT_LOGIN",
                        module: "AUTH",
                        entityType: "USER_ACCOUNT_ARCHIVE",
                        entityId: archivedAccount.ArchiveId > int.MaxValue ? null : (int)archivedAccount.ArchiveId,
                        description: "Deleted account login requested recovery decision",
                        status: "SUCCESS",
                        isError: 0);
                    return View();
                }

                await _activityLogger.LogAsync(
                    userId: archivedAccount.UserId > int.MaxValue ? null : (int)archivedAccount.UserId,
                    action: "DELETED_ACCOUNT_LOGIN",
                    module: "AUTH",
                    entityType: "USER_ACCOUNT_ARCHIVE",
                    entityId: archivedAccount.ArchiveId > int.MaxValue ? null : (int)archivedAccount.ArchiveId,
                    description: "Deleted account login recovery password did not match",
                    status: "FAILURE",
                    errorCode: "RECOVERY_PASSWORD_INVALID",
                    errorMessage: "Archived account found, but the password did not match",
                    errorSource: "AuthController.Login",
                    isError: 1);

                ViewBag.Error = "This account is deleted, but the password did not match. Enter the password used before deletion to recover it.";
                return View();
            }

            ViewBag.Error = "No account found with this email.";
            return View();
        }

if (!user.is_active || user.is_deleted)
{
    var archivedAccount = await FindRecoverableArchive(email);
    if (user.is_deleted && archivedAccount != null)
    {
        if (IsArchivePasswordValid(password, archivedAccount.PasswordHash))
        {
            PrepareRecoveryView(archivedAccount, email);
            await _activityLogger.LogAsync(
                userId: user.Id,
                action: "DELETED_ACCOUNT_LOGIN",
                module: "AUTH",
                entityType: "USER_ACCOUNT_ARCHIVE",
                entityId: archivedAccount.ArchiveId > int.MaxValue ? null : (int)archivedAccount.ArchiveId,
                description: "Deleted account login requested recovery decision",
                status: "SUCCESS",
                isError: 0);
            return View();
        }

        await _activityLogger.LogAsync(
            userId: user.Id,
            action: "DELETED_ACCOUNT_LOGIN",
            module: "AUTH",
            entityType: "USER_ACCOUNT_ARCHIVE",
            entityId: archivedAccount.ArchiveId > int.MaxValue ? null : (int)archivedAccount.ArchiveId,
            description: "Deleted account login recovery password did not match",
            status: "FAILURE",
            errorCode: "RECOVERY_PASSWORD_INVALID",
            errorMessage: "Archived account found, but the password did not match",
            errorSource: "AuthController.Login",
            isError: 1);

        ViewBag.Error = "This account is deleted, but the password did not match. Enter the password used before deletion to recover it.";
        return View();
    }

    ViewBag.Error =
        user.is_deleted
            ? "This account is deleted and cannot be recovered after the recovery window."
            : "Your account has been disabled by admin.";

    return View();
}

        bool isValid = BCrypt.Net.BCrypt.Verify(password, user.Password);

        if (isValid)
        {
HttpContext.Session.SetString(
    "UserId",
    user.Id.ToString()
);
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("UserName", user.Name ?? user.Email);
            HttpContext.Session.SetString("UserGenre", user.Genre ?? "Dramatic");
            HttpContext.Session.SetString("UserLanguage", user.Language ?? "English");
            HttpContext.Session.SetString("ProfileImage", user.ProfileImagePath ?? "");

            await CreditFirstLoginWalletBonus(user.Id,user.Email);

            await _activityLogger.LogAsync(
                userId: user.Id,
                action: "LOGIN",
                module: "AUTH",
                entityType: "USER",
                entityId: user.Id,
                description: "User logged in successfully",
                status: "SUCCESS",
                isError: 0
            );

            return RedirectToAction("ShowTime", "Home");
        }
        else
        {
            await _activityLogger.LogAsync(
                userId: user.Id,
                action: "FAILED_LOGIN",
                module: "AUTH",
                entityType: "USER",
                entityId: user.Id,
                description: "User failed to log in with incorrect password",
                status: "FAILURE"
            );

            // Previous wording: "Wrong script. Password did not match."
            ViewBag.Error = "Incorrect password. Please try again.";
            return View();
        }
    }
    catch (PostgresException ex)
    {
        await _activityLogger.LogAsync(
            action: "LOGIN",
            module: "AUTH",
            entityType: "USER_ACCOUNT_ARCHIVE",
            description: "Login stopped by database validation while checking deleted account recovery",
            status: "FAILURE",
            errorCode: ex.SqlState,
            errorMessage: ex.MessageText,
            errorSource: ex.TableName ?? ex.ConstraintName ?? "PostgreSQL",
            stackTrace: ex.StackTrace,
            isError: 2);

        ViewBag.Error = $"Login did not proceed because the recovery database check failed: {ex.MessageText}";
        return View();
    }
    catch (NpgsqlException ex)
    {
        await _activityLogger.LogAsync(
            action: "LOGIN",
            module: "AUTH",
            entityType: "USER_ACCOUNT_ARCHIVE",
            description: "Login stopped by database connection or command error while checking deleted account recovery",
            status: "FAILURE",
            errorCode: "DB_COMMAND_ERROR",
            errorMessage: ex.Message,
            errorSource: "PostgreSQL",
            stackTrace: ex.StackTrace,
            isError: 2);

        ViewBag.Error = "Login did not proceed because the recovery database check could not finish. Please try again after the database connection is stable.";
        return View();
    }
    catch (Exception ex)
    {
        await _activityLogger.LogAsync(
            action: "LOGIN",
            module: "AUTH",
            entityType: "USER",
            description: "Login stopped by application error",
            status: "FAILURE",
            errorCode: "APP500",
            errorMessage: ex.Message,
            errorSource: ex.GetType().Name,
            stackTrace: ex.StackTrace,
            isError: 1);

        ViewBag.Error = $"Login did not proceed because the application hit {ex.GetType().Name}: {ex.Message}";
        return View();
    }
	}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecoverDeletedAccount(long archiveId, string recoveryChoice)
        {
            try
            {
                recoveryChoice = recoveryChoice?.Trim() ?? "";
                var pendingArchiveId = HttpContext.Session.GetString("PendingRecoveryArchiveId");
                var pendingEmail = HttpContext.Session.GetString("PendingRecoveryEmail");
                var pendingExpiresRaw = HttpContext.Session.GetString("PendingRecoveryExpiresUtc");

                if (string.Equals(recoveryChoice, "cancel", StringComparison.OrdinalIgnoreCase))
                {
                    ClearPendingRecovery();
                    ViewBag.Error = "Recovery did not proceed because you selected Cancel. Your account will remain deleted.";
                    return View("Login");
                }

                if (!string.Equals(recoveryChoice, "recover", StringComparison.OrdinalIgnoreCase))
                {
                    ViewBag.Error = "Recovery did not proceed because the selected action was missing or invalid. Please click Recover Account again.";
                    return View("Login");
                }

                if (!long.TryParse(pendingArchiveId, out var sessionArchiveId) ||
                    sessionArchiveId != archiveId ||
                    string.IsNullOrWhiteSpace(pendingEmail) ||
                    !DateTime.TryParse(pendingExpiresRaw, null, System.Globalization.DateTimeStyles.RoundtripKind, out var pendingExpiresUtc) ||
                    pendingExpiresUtc < DateTime.UtcNow)
                {
                    ClearPendingRecovery();
                    ViewBag.Error = "Recovery did not proceed because the recovery approval expired or no longer matches this account. Please log in again to restart recovery.";
                    return View("Login");
                }

                var archivedAccount = await FindRecoverableArchive(pendingEmail, archiveId);
                if (archivedAccount == null)
                {
                    ClearPendingRecovery();
                    ViewBag.Error = "Recovery did not proceed because this account is outside the 30 day recovery window.";
                    return View("Login");
                }

                var recovered = await RecoverUserAccount(archiveId, pendingEmail);
                if (!recovered)
                {
                    ClearPendingRecovery();
                    ViewBag.Error = "Recovery did not proceed because this account is outside the 30 day recovery window.";
                    return View("Login");
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == archivedAccount.UserId);
                if (user == null || user.is_deleted || !user.is_active)
                {
                    ViewBag.Error = "Recovery finished, but login did not start because the restored user row is not active yet. Please log in again.";
                    return View("Login");
                }

                HttpContext.Session.SetString("UserId", user.Id.ToString());
                HttpContext.Session.SetString("UserEmail", user.Email);
                HttpContext.Session.SetString("UserName", user.Name ?? user.Email);
                HttpContext.Session.SetString("UserGenre", user.Genre ?? "Dramatic");
                HttpContext.Session.SetString("UserLanguage", user.Language ?? "English");
                HttpContext.Session.SetString("ProfileImage", user.ProfileImagePath ?? "");
                ClearPendingRecovery();

                await _activityLogger.LogAsync(
                    userId: user.Id,
                    action: "RECOVER_DELETED_ACCOUNT",
                    module: "AUTH",
                    entityType: "USER_ACCOUNT_ARCHIVE",
                    entityId: archiveId > int.MaxValue ? null : (int)archiveId,
                    description: "Deleted user account recovered within recovery window",
                    status: "SUCCESS",
                    isError: 0);

                TempData["Success"] = "Account recovered. Your saved data is available again.";
                return RedirectToAction("MyProfile", "Profile");
            }
            catch (PostgresException ex)
            {
                await _activityLogger.LogAsync(
                    action: "RECOVER_DELETED_ACCOUNT",
                    module: "AUTH",
                    entityType: "USER_ACCOUNT_ARCHIVE",
                    entityId: archiveId > int.MaxValue ? null : (int)archiveId,
                    description: "Deleted account recovery stopped by database validation",
                    status: "FAILURE",
                    errorCode: ex.SqlState,
                    errorMessage: ex.MessageText,
                    errorSource: ex.TableName ?? ex.ConstraintName ?? "PostgreSQL",
                    stackTrace: ex.StackTrace,
                    isError: 2);

                ViewBag.Error = BuildRecoveryErrorMessage(ex);
                return View("Login");
            }
            catch (NpgsqlException ex)
            {
                await _activityLogger.LogAsync(
                    action: "RECOVER_DELETED_ACCOUNT",
                    module: "AUTH",
                    entityType: "USER_ACCOUNT_ARCHIVE",
                    entityId: archiveId > int.MaxValue ? null : (int)archiveId,
                    description: "Deleted account recovery stopped by database connection or command error",
                    status: "FAILURE",
                    errorCode: "DB_COMMAND_ERROR",
                    errorMessage: ex.Message,
                    errorSource: "PostgreSQL",
                    stackTrace: ex.StackTrace,
                    isError: 2);

                ViewBag.Error = "Recovery did not proceed because the archive database command could not finish. Please try again after the database connection is stable.";
                return View("Login");
            }
            catch (Exception ex)
            {
                await _activityLogger.LogAsync(
                    action: "RECOVER_DELETED_ACCOUNT",
                    module: "AUTH",
                    entityType: "USER_ACCOUNT_ARCHIVE",
                    entityId: archiveId > int.MaxValue ? null : (int)archiveId,
                    description: "Deleted account recovery failed",
                    status: "FAILURE",
                    errorCode: "APP500",
                    errorMessage: ex.Message,
                    errorSource: "Application",
                    stackTrace: ex.StackTrace,
                    isError: 1);

                ViewBag.Error = "Recovery did not proceed because the application hit an unexpected error. Your archived data was not purged; please try again.";
                return View("Login");
            }
        }

        private void PrepareRecoveryView(RecoverableAccount archivedAccount, string email)
        {
            ViewBag.RecoverArchiveId = archivedAccount.ArchiveId;
            ViewBag.RecoverEmail = email;
            ViewBag.RecoverUntil = archivedAccount.RecoverUntil.ToLocalTime().ToString("dd MMM yyyy hh:mm tt");
            HttpContext.Session.SetString("PendingRecoveryArchiveId", archivedAccount.ArchiveId.ToString());
            HttpContext.Session.SetString("PendingRecoveryEmail", email);
            HttpContext.Session.SetString("PendingRecoveryExpiresUtc", DateTime.UtcNow.AddMinutes(10).ToString("O"));
        }

        private void ClearPendingRecovery()
        {
            HttpContext.Session.Remove("PendingRecoveryArchiveId");
            HttpContext.Session.Remove("PendingRecoveryEmail");
            HttpContext.Session.Remove("PendingRecoveryExpiresUtc");
        }

        private static bool IsArchivePasswordValid(string password, string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
            {
                return false;
            }

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, passwordHash);
            }
            catch
            {
                return false;
            }
        }

        private static string BuildRecoveryErrorMessage(PostgresException ex)
        {
            var relation =
                !string.IsNullOrWhiteSpace(ex.TableName)
                    ? ex.TableName
                    : ex.ConstraintName;

            return ex.SqlState switch
            {
                PostgresErrorCodes.ForeignKeyViolation =>
                    $"Recovery did not proceed because related data in {relation ?? "another table"} must be restored in order first. Your archive is still available.",
                PostgresErrorCodes.UniqueViolation =>
                    $"Recovery did not proceed because matching data already exists in {relation ?? "the active tables"}. Your archive is still available.",
                PostgresErrorCodes.UndefinedTable =>
                    $"Recovery did not proceed because required table {relation ?? "in the database"} was not found.",
                PostgresErrorCodes.UndefinedColumn =>
                    $"Recovery did not proceed because a required archive column is missing in {relation ?? "the database"}.",
                PostgresErrorCodes.RaiseException =>
                    $"Recovery did not proceed because the archive procedure stopped with: {ex.MessageText}",
                _ =>
                    $"Recovery did not proceed because the database stopped the restore step: {ex.MessageText}"
            };
        }

        private async Task<RecoverableAccount?> FindRecoverableArchive(string email, long? archiveId = null)
        {
            var connectionString =
                DatabaseConnectionStringResolver
                .GetDatabaseConnectionString(_configuration);

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            var sql = @"
SELECT
    id,
    original_user_id,
    password_hash,
    recover_until
FROM public.user_account_archives
WHERE lower(email) = lower(@email)
  AND status = 'DELETED'
  AND recover_until >= CURRENT_TIMESTAMP
  AND (@archive_id IS NULL OR id = @archive_id)
ORDER BY deleted_at DESC
LIMIT 1;";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@email", NpgsqlDbType.Text, email);
            command.Parameters.Add("@archive_id", NpgsqlDbType.Bigint).Value = (object?)archiveId ?? DBNull.Value;

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return new RecoverableAccount(
                reader.GetInt64(0),
                reader.GetInt64(1),
                reader.IsDBNull(2) ? "" : reader.GetString(2),
                reader.GetDateTime(3));
        }

        private async Task<bool> RecoverUserAccount(long archiveId, string recoveredBy)
        {
            var connectionString =
                DatabaseConnectionStringResolver
                .GetDatabaseConnectionString(_configuration);

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(
                "SELECT public.fn_recover_user_account(@archive_id, @recovered_by);",
                connection);
            command.CommandTimeout = 120;
            command.Parameters.AddWithValue("@archive_id", archiveId);
            command.Parameters.AddWithValue("@recovered_by", recoveredBy);

            var result = await command.ExecuteScalarAsync();
            return result is bool recovered && recovered;
        }

        private record RecoverableAccount(
            long ArchiveId,
            long UserId,
            string PasswordHash,
            DateTime RecoverUntil);

        private async Task CreditFirstLoginWalletBonus(
        int userId,
        string userEmail)
        {
            // The welcome wallet credit is idempotent, so repeated logins do not duplicate the bonus.
            var reference =
            $"FIRSTLOGIN-10000-{userId}";

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO user_wallets
(
    user_id,
    wallet_balance,
    blocked_balance,
    loyalty_points,
    wallet_status,
    created_at,
    updated_at
)
VALUES
(
    {userId},
    0,
    0,
    0,
    'ACTIVE',
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP
)
ON CONFLICT (user_id) DO NOTHING;

INSERT INTO wallet_transactions
(
    wallet_id,
    user_id,
    transaction_ref,
    transaction_type,
    entry_type,
    amount,
    opening_balance,
    closing_balance,
    remarks,
    transaction_status,
    created_at,
    created_by,
    description,
    status,
    reference_type,
    reference_id,
    balance_before,
    balance_after,
    payment_method,
    gateway_name,
    gateway_reference,
    is_deleted
)
SELECT
    uw.id,
    {userId},
    {reference},
    'BONUS',
    'CREDIT',
    10000,
    uw.wallet_balance,
    uw.wallet_balance + 10000,
    'First login welcome bonus',
    'SUCCESS',
    CURRENT_TIMESTAMP,
    {userEmail},
    'Automatic first login wallet credit',
    'SUCCESS',
    'USER',
    {userId},
    uw.wallet_balance,
    uw.wallet_balance + 10000,
    'SYSTEM',
    'SYSTEM',
    {reference},
    false
FROM user_wallets uw
WHERE uw.user_id = {userId}
  AND NOT EXISTS
  (
      SELECT 1
      FROM wallet_transactions wt
      WHERE wt.transaction_ref = {reference}
  );");
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

            if (string.IsNullOrWhiteSpace(user.Password) || !PasswordRegex.IsMatch(user.Password))
            {
                ModelState.AddModelError("Password", PasswordRuleMessage);
            }

            return ModelState.IsValid;
        }

        [NonAction]
        private IActionResult SignupError(User user, string message)
        {
            ViewBag.Error = message;
            return View("Signup", user);
        }


        public IActionResult Signup()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Signup(User user)
        {
            try
            {
            // Signup stores a valid customer account, then AssignDefaultUserRole gives it AMAR_USER.
            if (!ValidateSignup(user))
            {
                return View(user);
            }

            if (_context.Users.Any(u => u.Mobile == user.Mobile))
            {
                // Previous wording: "This mobile number already has a ticket in our records."
                return SignupError(user, "This mobile number is already registered.");
            }

            if (_context.Users.Any(u => u.Email.ToLower() == user.Email.ToLower()))
            {
                return SignupError(user, "This email is already registered. Try login or use another email.");
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
            user.CreatedAt = DateTime.UtcNow;


user.is_active = true;

user.is_deleted = false;
user.CreatedBy = user.Email;
if (string.IsNullOrEmpty(user.Genre))
    user.Genre = "Dramatic";

if (string.IsNullOrEmpty(user.Language))
    user.Language = "English";


            _context.Users.Add(user);

await _context.SaveChangesAsync();

await AssignDefaultUserRole(user.Id);

await _activityLogger.LogAsync(
    userId: user.Id,
    action: "SIGNUP",
    module: "AUTH",
    entityType: "USER",
    entityId: user.Id,
    description: "New user account created",
    newValue: new
    {
        user.Id,
        user.Name,
        user.Email,
        user.Mobile
    }
);

return RedirectToAction("Login");
            }
            catch
            {
                // Previous wording: "The signup scene could not be saved. Please try again."
                return SignupError(user, "We could not create your account. Please try again.");
            }
        }

        private async Task AssignDefaultUserRole(int userId)
        {
            var userRole =
            await _context.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(x=>
                x.RoleCode=="AMAR_USER" &&
                x.IsActive);

            if(userRole==null)
            {
                return;
            }

            var hasMapping =
            await _context.UserRoleMappings
            .AnyAsync(x=>
                x.UserId==userId &&
                x.RoleId==userRole.Id);

            if(!hasMapping)
            {
                _context.UserRoleMappings.Add(new UserRoleMapping
                {
                    UserId=userId,
                    RoleId=userRole.Id,
                    AssignedAt=DateTime.UtcNow,
                    AssignedBy=null,
                    IsActive=true
                });

                await _context.SaveChangesAsync();
            }

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
CREATE TABLE IF NOT EXISTS public.user_roles
(
    id bigint GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    user_id bigint NOT NULL,
    role_id bigint NOT NULL,
    assigned_by bigint,
    assigned_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    is_active boolean DEFAULT true
);

INSERT INTO public.user_roles (user_id, role_id, assigned_by, assigned_at, is_active)
VALUES ({userId}, {userRole.Id}, NULL, CURRENT_TIMESTAMP, true)
ON CONFLICT DO NOTHING;

UPDATE public.user_roles
SET is_active = true,
    assigned_at = CURRENT_TIMESTAMP
WHERE user_id = {userId}
  AND role_id = {userRole.Id};");
        }


        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendOTP(string email)
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

            generatedOTP = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            resetOtpExpiresAtUtc = DateTime.UtcNow.AddMinutes(Math.Max(1, _configuration.GetValue("Otp:ExpiryMinutes", 5)));
            resetEmail = email;

            var result = await _otpDeliveryService.SendEmailOtpAsync(email, generatedOTP, "reset password");
            if (!result.Success)
            {
                if (!result.IsConfigured && _environment.IsDevelopment() && _configuration.GetValue("Otp:ExposeDevOtp", true))
                {
                    System.Diagnostics.Debug.WriteLine($"Reset password OTP for {email}: {generatedOTP}");
                    ViewBag.Message = "OTP sent. Check the popup for the development code.";
                    ViewBag.DevOtp = generatedOTP;
                    return View("VerifyOTP");
                }

                ViewBag.Error = result.Message;
                return View("ForgotPassword");
            }

            ViewBag.Message = "OTP sent to your email for reset password.";
            if (_environment.IsDevelopment() && _configuration.GetValue("Otp:ExposeDevOtp", true))
            {
                ViewBag.DevOtp = generatedOTP;
            }
            return View("VerifyOTP");
        }

        [HttpPost]
        public IActionResult VerifyOTP(string otp)
        {
            if (!string.IsNullOrWhiteSpace(generatedOTP) &&
                resetOtpExpiresAtUtc >= DateTime.UtcNow &&
                otp == generatedOTP)
            {
                return View("ResetPassword");
            }

            ViewBag.Error = resetOtpExpiresAtUtc < DateTime.UtcNow ? "OTP expired. Send a new code." : "Invalid OTP";
            return View("VerifyOTP");
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(password) || !PasswordRegex.IsMatch(password))
            {
                ViewBag.Error = PasswordRuleMessage;
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

    await _activityLogger.LogAsync(
        userId: user.Id,
        action: "RESET_PASSWORD",
        module: "AUTH",
        entityType: "USER",
        entityId: user.Id,
        description: "User password reset successfully"
    );
}

            return RedirectToAction("Login");
        }


        public async Task<IActionResult> Logout()
{
    var email = HttpContext.Session.GetString("UserEmail");

    var user = _context.Users.FirstOrDefault(u => u.Email == email);

    if (user != null)
    {
        await _activityLogger.LogAsync(
            userId: user.Id,
            action: "LOGOUT",
            module: "AUTH",
            entityType: "USER",
            entityId: user.Id,
            description: "User logged out successfully"
        );
    }

    HttpContext.Session.Clear();

    return RedirectToAction("Login");
}
        
    }
}
