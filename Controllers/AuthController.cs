using Microsoft.AspNetCore.Mvc;
using AmarShowsBook.Data;
using AmarShowsBook.Models;
using AmarShowsBook.Services;
using Microsoft.EntityFrameworkCore;
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
            ViewBag.Error = "Missing credentials. The show cannot start without email and password.";
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
            ViewBag.Error = "No performer found with this email.";
            return View();
        }

if (!user.is_active || user.is_deleted)
{
    ViewBag.Error =
        "Your account has been disabled by admin.";

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

            ViewBag.Error = "Wrong script. Password did not match.";
            return View();
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine(ex);

        ViewBag.Error = "The projector had a technical pause. Please try login again.";
        return View();
    }
	}

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
                return SignupError(user, "This mobile number already has a ticket in our records.");
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
                return SignupError(user, "The signup scene could not be saved. Please try again.");
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
