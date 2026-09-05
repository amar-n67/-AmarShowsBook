using AmarShowsBook.Data;
using AmarShowsBook.Models.ViewModels;
using AmarShowsBook.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace AmarShowsBook.Controllers
{
    public class DeveloperController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly RbacService _rbacService;
        private readonly IWebHostEnvironment _environment;
        private readonly IActivityLogger _activityLogger;
        private static readonly Regex SupportPhoneRegex = new(@"^\+?[0-9][0-9\s-]{8,18}$", RegexOptions.Compiled);
        private static readonly Regex SupportEmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        public DeveloperController(
            ApplicationDbContext context,
            RbacService rbacService,
            IWebHostEnvironment environment,
            IActivityLogger activityLogger)
        {
            _context = context;
            _rbacService = rbacService;
            _environment = environment;
            _activityLogger = activityLogger;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Profile));
        }

        public async Task<IActionResult> Profile()
        {
            EnsureDeveloperProfileStore();
            ApplyAnnualExperienceIncrement();

            var developer =
            _context.DeveloperProfiles?
            .FirstOrDefault();

            ViewBag.CanEditDeveloperProfile = CanEditDeveloperProfile();

            await _activityLogger.LogAsync(
                userId: GetCurrentUserId(),
                action: "VIEW_DEVELOPER_PROFILE",
                module: "DEVELOPER",
                entityType: "DEVELOPER_PROFILE",
                entityId: developer?.DeveloperId,
                description: "Viewed developer profile page",
                status: "SUCCESS",
                metadata: new
                {
                    CanEdit = ViewBag.CanEditDeveloperProfile
                });

            return View("Index",
                developer ??
                new DeveloperVM
                {
                    FullName="showTime Team",
                    Bio="Developer Profile",
                    Email="example@gmail.com",
                    ExperienceYears=0,
                    TwitterUrl="",
                    SupportPhone="+91 9651698863",
                    SupportWhatsAppPhone="+91 9651698863",
                    IsSupportWhatsAppSameAsPhone=true,
                    DeveloperWhatsAppPhone="+91 9651698863",
                    IsDeveloperWhatsAppSameAsPhone=true,
                    DeveloperEmail="example@gmail.com",
                    DeveloperEmailSubject="showTime Developer Contact",
                    DeveloperEmailText="Hi showTime Team, I'm {user}. I would like to connect with the developer.",
                    SupportEmail="support@showtime.com",
                    TopWhatsAppText="Hi showTime Team, I'm {user}. I visited showTime and would like to connect with you.",
                    SupportWhatsAppText="Hi showTime Team, I'm {user}. I need support. Please help me with my request.",
                    SupportEmailSubject="showTime Support Request",
                    SupportEmailText="Hi showTime Team, I'm {user}. I need support. Please help me with my request."
                });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(DeveloperVM model, IFormFile? profilePhoto)
        {
            if (!CanEditDeveloperProfile())
            {
                TempData["Error"] = "Only developer mode role can edit this profile.";
                await _activityLogger.LogAsync(
                    userId: GetCurrentUserId(),
                    action: "UPDATE_DEVELOPER_PROFILE",
                    module: "DEVELOPER",
                    entityType: "DEVELOPER_PROFILE",
                    entityId: model.DeveloperId,
                    description: "Blocked developer profile update because user cannot edit developer profile",
                    status: "FAILURE",
                    errorCode: "DEV403",
                    errorMessage: TempData["Error"]?.ToString(),
                    errorSource: nameof(DeveloperController),
                    isError: 1);
                return RedirectToAction(nameof(Profile));
            }

            EnsureDeveloperProfileStore();
            NormalizeAndValidateSupportDetails(model);
            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
                await _activityLogger.LogAsync(
                    userId: GetCurrentUserId(),
                    action: "UPDATE_DEVELOPER_PROFILE",
                    module: "DEVELOPER",
                    entityType: "DEVELOPER_PROFILE",
                    entityId: model.DeveloperId,
                    description: "Rejected developer profile update because support details are invalid",
                    status: "FAILURE",
                    errorCode: "DEV_SUPPORT_VALIDATION",
                    errorMessage: TempData["Error"]?.ToString(),
                    errorSource: nameof(DeveloperController),
                    isError: 1,
                    newValue: model);
                return RedirectToAction(nameof(Profile));
            }

            var oldProfile = _context.DeveloperProfiles?
                .AsNoTracking()
                .FirstOrDefault();

            if (profilePhoto != null && profilePhoto.Length > 0)
            {
                var imagePath = await SaveProfilePhoto(profilePhoto);
                if (string.IsNullOrWhiteSpace(imagePath))
                {
                    TempData["Error"] = "Scene cut: upload a valid JPG, PNG, WEBP, or GIF profile photo.";
                    await _activityLogger.LogAsync(
                        userId: GetCurrentUserId(),
                        action: "UPLOAD_DEVELOPER_PHOTO",
                        module: "DEVELOPER",
                        entityType: "DEVELOPER_PROFILE",
                        entityId: model.DeveloperId,
                        description: "Rejected invalid developer profile photo upload",
                        status: "FAILURE",
                        errorCode: "DEV_UPLOAD_INVALID",
                        errorMessage: TempData["Error"]?.ToString(),
                        errorSource: nameof(DeveloperController),
                        isError: 1,
                        metadata: new
                        {
                            profilePhoto.FileName,
                            profilePhoto.ContentType,
                            profilePhoto.Length
                        });
                    return RedirectToAction(nameof(Profile));
                }

                model.ProfileImage = imagePath;
            }

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO public.developer_profiles
(
    developer_id,
    full_name,
    email,
    phone,
    bio,
    address,
    experience_years,
    experience_last_increment_year,
    skills,
    education,
    projects,
    technologies,
    achievements,
    resume_url,
    profile_image,
    github_url,
    linked_in_url,
    twitter_url,
    instagram_url,
    facebook_url,
    youtube_url,
    website_url,
    support_phone,
    support_email,
    support_whatsapp_text,
    support_whatsapp_phone,
	    is_support_whatsapp_same_as_phone,
	    developer_whatsapp_phone,
	    is_developer_whatsapp_same_as_phone,
	    developer_email,
	    developer_email_subject,
	    developer_email_text,
	    top_whatsapp_text,
    support_email_subject,
    support_email_text,
    updated_at
)
VALUES
(
    1,
    {model.FullName},
    {model.Email},
    {model.Phone},
    {model.Bio},
    {model.Address},
    {Math.Max(0, model.ExperienceYears)},
    {GetCurrentExperienceAnniversaryYear()},
    {model.Skills},
    {model.Education},
    {model.Projects},
    {model.Technologies},
    {model.Achievements},
    {model.ResumeUrl},
    {model.ProfileImage},
    {model.GitHubUrl},
    {model.LinkedInUrl},
    {model.TwitterUrl},
    {model.InstagramUrl},
    {model.FacebookUrl},
    {model.YoutubeUrl},
    {model.WebsiteUrl},
    {model.SupportPhone},
    {model.SupportEmail},
    {model.SupportWhatsAppText},
    {model.SupportWhatsAppPhone},
	    {model.IsSupportWhatsAppSameAsPhone},
	    {model.DeveloperWhatsAppPhone},
	    {model.IsDeveloperWhatsAppSameAsPhone},
	    {model.DeveloperEmail},
	    {model.DeveloperEmailSubject},
	    {model.DeveloperEmailText},
	    {model.TopWhatsAppText},
    {model.SupportEmailSubject},
    {model.SupportEmailText},
    CURRENT_TIMESTAMP
)
ON CONFLICT (developer_id)
DO UPDATE SET
    full_name = EXCLUDED.full_name,
    email = EXCLUDED.email,
    phone = EXCLUDED.phone,
    bio = EXCLUDED.bio,
    address = EXCLUDED.address,
    experience_years = EXCLUDED.experience_years,
    experience_last_increment_year = EXCLUDED.experience_last_increment_year,
    skills = EXCLUDED.skills,
    education = EXCLUDED.education,
    projects = EXCLUDED.projects,
    technologies = EXCLUDED.technologies,
    achievements = EXCLUDED.achievements,
    resume_url = EXCLUDED.resume_url,
    profile_image = EXCLUDED.profile_image,
    github_url = EXCLUDED.github_url,
    linked_in_url = EXCLUDED.linked_in_url,
    twitter_url = EXCLUDED.twitter_url,
    instagram_url = EXCLUDED.instagram_url,
    facebook_url = EXCLUDED.facebook_url,
    youtube_url = EXCLUDED.youtube_url,
    website_url = EXCLUDED.website_url,
    support_phone = EXCLUDED.support_phone,
    support_email = EXCLUDED.support_email,
    support_whatsapp_text = EXCLUDED.support_whatsapp_text,
    support_whatsapp_phone = EXCLUDED.support_whatsapp_phone,
	    is_support_whatsapp_same_as_phone = EXCLUDED.is_support_whatsapp_same_as_phone,
	    developer_whatsapp_phone = EXCLUDED.developer_whatsapp_phone,
	    is_developer_whatsapp_same_as_phone = EXCLUDED.is_developer_whatsapp_same_as_phone,
	    developer_email = EXCLUDED.developer_email,
	    developer_email_subject = EXCLUDED.developer_email_subject,
	    developer_email_text = EXCLUDED.developer_email_text,
	    top_whatsapp_text = EXCLUDED.top_whatsapp_text,
    support_email_subject = EXCLUDED.support_email_subject,
    support_email_text = EXCLUDED.support_email_text,
    updated_at = CURRENT_TIMESTAMP;
");

            await _activityLogger.LogAsync(
                userId: GetCurrentUserId(),
                action: "UPDATE_DEVELOPER_PROFILE",
                module: "DEVELOPER",
                entityType: "DEVELOPER_PROFILE",
                entityId: 1,
                description: "Updated developer profile",
                oldValue: oldProfile ?? new DeveloperVM { DeveloperId = 1 },
                newValue: model,
                status: "SUCCESS",
                metadata: new
                {
                    HasUploadedPhoto = profilePhoto != null && profilePhoto.Length > 0
                });

            // Previous wording: "Profile reel updated. The new developer scene is live."
            TempData["Success"] = "Developer profile updated successfully.";
            return RedirectToAction(nameof(Profile));
        }

        private int? GetCurrentUserId()
        {
            return int.TryParse(HttpContext.Session.GetString("UserId"), out var userId)
                ? userId
                : null;
        }

        private void NormalizeAndValidateSupportDetails(DeveloperVM model)
        {
            model.SupportPhone = string.IsNullOrWhiteSpace(model.SupportPhone)
                ? "+91 9651698863"
                : model.SupportPhone.Trim();
            model.SupportEmail = string.IsNullOrWhiteSpace(model.SupportEmail)
                ? "support@showtime.com"
                : model.SupportEmail.Trim();

            if (model.IsSupportWhatsAppSameAsPhone)
            {
                model.SupportWhatsAppPhone = model.SupportPhone;
            }
            else
            {
                model.SupportWhatsAppPhone = string.IsNullOrWhiteSpace(model.SupportWhatsAppPhone)
                    ? model.SupportPhone
                    : model.SupportWhatsAppPhone.Trim();
            }

            if (model.IsDeveloperWhatsAppSameAsPhone)
            {
                model.DeveloperWhatsAppPhone = model.SupportPhone;
            }
            else
            {
                model.DeveloperWhatsAppPhone = string.IsNullOrWhiteSpace(model.DeveloperWhatsAppPhone)
                    ? model.SupportPhone
                    : model.DeveloperWhatsAppPhone.Trim();
            }

            model.TopWhatsAppText = string.IsNullOrWhiteSpace(model.TopWhatsAppText)
                ? "Hi showTime Team, I'm {user}. I visited showTime and would like to connect with you."
                : model.TopWhatsAppText.Trim();
            model.DeveloperEmail = string.IsNullOrWhiteSpace(model.DeveloperEmail)
                ? model.SupportEmail
                : model.DeveloperEmail.Trim();
            model.DeveloperEmailSubject = string.IsNullOrWhiteSpace(model.DeveloperEmailSubject)
                ? "showTime Developer Contact"
                : model.DeveloperEmailSubject.Trim();
            model.DeveloperEmailText = string.IsNullOrWhiteSpace(model.DeveloperEmailText)
                ? "Hi showTime Team, I'm {user}. I would like to connect with the developer."
                : model.DeveloperEmailText.Trim();
            model.SupportWhatsAppText = string.IsNullOrWhiteSpace(model.SupportWhatsAppText)
                ? "Hi showTime Team, I'm {user}. I need support. Please help me with my request."
                : model.SupportWhatsAppText.Trim();
            model.SupportEmailSubject = string.IsNullOrWhiteSpace(model.SupportEmailSubject)
                ? "showTime Support Request"
                : model.SupportEmailSubject.Trim();
            model.SupportEmailText = string.IsNullOrWhiteSpace(model.SupportEmailText)
                ? "Hi showTime Team, I'm {user}. I need support. Please help me with my request."
                : model.SupportEmailText.Trim();

            if (!SupportPhoneRegex.IsMatch(model.SupportPhone))
            {
                ModelState.AddModelError(nameof(model.SupportPhone), "Support phone must be a valid phone number.");
            }

            if (!SupportPhoneRegex.IsMatch(model.SupportWhatsAppPhone ?? string.Empty))
            {
                ModelState.AddModelError(nameof(model.SupportWhatsAppPhone), "Support WhatsApp number must be a valid phone number.");
            }

            if (!SupportPhoneRegex.IsMatch(model.DeveloperWhatsAppPhone ?? string.Empty))
            {
                ModelState.AddModelError(nameof(model.DeveloperWhatsAppPhone), "Developer WhatsApp number must be a valid phone number.");
            }

            if (!SupportEmailRegex.IsMatch(model.SupportEmail))
            {
                ModelState.AddModelError(nameof(model.SupportEmail), "Support email must be a valid email address.");
            }

            if (!SupportEmailRegex.IsMatch(model.DeveloperEmail))
            {
                ModelState.AddModelError(nameof(model.DeveloperEmail), "Developer email must be a valid email address.");
            }
        }

        private bool CanEditDeveloperProfile()
        {
            var userIdText =
                HttpContext.Session.GetString("UserId");

            return int.TryParse(userIdText, out var userId) &&
                (_rbacService.HasAnyActiveRole(userId, "AMAR_SUPER_ADMIN", "AMAR_DEVELOPER") ||
                 _rbacService.HasPermission(userId, "DEVELOPER", "EDIT"));
        }

        private void ApplyAnnualExperienceIncrement()
        {
            var today = DateTime.Today;
            var anniversary = new DateTime(today.Year, 5, 16);

            if (today < anniversary)
            {
                return;
            }

            _context.Database.ExecuteSqlInterpolated($@"
UPDATE public.developer_profiles
SET
    experience_years = GREATEST(0, experience_years) + 1,
    experience_last_increment_year = {today.Year},
    updated_at = CURRENT_TIMESTAMP
WHERE developer_id = 1
  AND COALESCE(experience_last_increment_year, {today.Year - 1}) < {today.Year};
");
        }

        private static int GetCurrentExperienceAnniversaryYear()
        {
            var today = DateTime.Today;
            var anniversary = new DateTime(today.Year, 5, 16);

            return today >= anniversary
                ? today.Year
                : today.Year - 1;
        }

        private async Task<string?> SaveProfilePhoto(IFormFile profilePhoto)
        {
            if (!profilePhoto.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var extension = Path.GetExtension(profilePhoto.FileName).ToLowerInvariant();
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp",
                ".gif"
            };

            if (!allowedExtensions.Contains(extension))
            {
                return null;
            }

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            await using var stream = System.IO.File.Create(filePath);
            await profilePhoto.CopyToAsync(stream);

            return $"/uploads/{fileName}";
        }

        private void EnsureDeveloperProfileStore()
        {
            _context.Database.ExecuteSqlRaw(@"
CREATE TABLE IF NOT EXISTS public.developer_profiles
(
    developer_id integer PRIMARY KEY DEFAULT 1,
    full_name text,
    email text,
    phone text,
    bio text,
    address text,
    experience_years integer NOT NULL DEFAULT 0,
    experience_last_increment_year integer,
    skills text,
    education text,
    projects text,
    technologies text,
    achievements text,
    resume_url text,
    profile_image text,
    github_url text,
    linked_in_url text,
    twitter_url text,
    instagram_url text,
    facebook_url text,
    youtube_url text,
    website_url text,
    support_phone text,
    support_email text,
    support_whatsapp_text text,
    support_whatsapp_phone text,
	    is_support_whatsapp_same_as_phone boolean NOT NULL DEFAULT true,
	    developer_whatsapp_phone text,
	    is_developer_whatsapp_same_as_phone boolean NOT NULL DEFAULT true,
	    developer_email text,
	    developer_email_subject text,
	    developer_email_text text,
	    top_whatsapp_text text,
    support_email_subject text,
    support_email_text text,
    updated_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT ck_developer_profiles_single_row CHECK (developer_id = 1)
);

ALTER TABLE public.developer_profiles
ADD COLUMN IF NOT EXISTS support_phone text;

ALTER TABLE public.developer_profiles
ADD COLUMN IF NOT EXISTS experience_last_increment_year integer;

ALTER TABLE public.developer_profiles
ADD COLUMN IF NOT EXISTS support_email text;

ALTER TABLE public.developer_profiles
ADD COLUMN IF NOT EXISTS support_whatsapp_text text;

ALTER TABLE public.developer_profiles
ADD COLUMN IF NOT EXISTS support_whatsapp_phone text;

ALTER TABLE public.developer_profiles
ADD COLUMN IF NOT EXISTS is_support_whatsapp_same_as_phone boolean NOT NULL DEFAULT true;

ALTER TABLE public.developer_profiles
ADD COLUMN IF NOT EXISTS developer_whatsapp_phone text;

	ALTER TABLE public.developer_profiles
	ADD COLUMN IF NOT EXISTS is_developer_whatsapp_same_as_phone boolean NOT NULL DEFAULT true;

	ALTER TABLE public.developer_profiles
	ADD COLUMN IF NOT EXISTS developer_email text;

	ALTER TABLE public.developer_profiles
	ADD COLUMN IF NOT EXISTS developer_email_subject text;

	ALTER TABLE public.developer_profiles
	ADD COLUMN IF NOT EXISTS developer_email_text text;

	ALTER TABLE public.developer_profiles
	ADD COLUMN IF NOT EXISTS top_whatsapp_text text;

ALTER TABLE public.developer_profiles
ADD COLUMN IF NOT EXISTS support_email_subject text;

ALTER TABLE public.developer_profiles
ADD COLUMN IF NOT EXISTS support_email_text text;

INSERT INTO public.developer_profiles
(
    developer_id,
    full_name,
    email,
    bio,
    experience_years,
    twitter_url,
    support_phone,
    support_email,
    support_whatsapp_text,
    support_whatsapp_phone,
	    is_support_whatsapp_same_as_phone,
	    developer_whatsapp_phone,
	    is_developer_whatsapp_same_as_phone,
	    developer_email,
	    developer_email_subject,
	    developer_email_text,
	    top_whatsapp_text,
    support_email_subject,
    support_email_text
)
SELECT
    1,
    'showTime Team',
    'example@gmail.com',
    'Developer Profile',
    0,
    '',
    '+91 9651698863',
    'support@showtime.com',
    'Hi showTime Team, I''m {{user}}. I need support. Please help me with my request.',
    '+91 9651698863',
	    true,
	    '+91 9651698863',
	    true,
	    'example@gmail.com',
	    'showTime Developer Contact',
	    'Hi showTime Team, I''m {{user}}. I would like to connect with the developer.',
	    'Hi showTime Team, I''m {{user}}. I visited showTime and would like to connect with you.',
    'showTime Support Request',
    'Hi showTime Team, I''m {{user}}. I need support. Please help me with my request.'
WHERE NOT EXISTS
(
    SELECT 1
    FROM public.developer_profiles
    WHERE developer_id = 1
);

UPDATE public.developer_profiles
SET
    twitter_url = COALESCE(NULLIF(twitter_url, ''), ''),
    support_phone = COALESCE(NULLIF(support_phone, ''), '+91 9651698863'),
    support_email = COALESCE(NULLIF(support_email, ''), 'support@showtime.com'),
	    support_whatsapp_text = COALESCE(NULLIF(support_whatsapp_text, ''), 'Hi showTime Team, I''m {{user}}. I need support. Please help me with my request.'),
	    support_whatsapp_phone = COALESCE(NULLIF(support_whatsapp_phone, ''), NULLIF(support_phone, ''), '+91 9651698863'),
	    developer_whatsapp_phone = COALESCE(NULLIF(developer_whatsapp_phone, ''), NULLIF(support_phone, ''), '+91 9651698863'),
	    developer_email = COALESCE(NULLIF(developer_email, ''), NULLIF(email, ''), NULLIF(support_email, ''), 'example@gmail.com'),
	    developer_email_subject = COALESCE(NULLIF(developer_email_subject, ''), 'showTime Developer Contact'),
	    developer_email_text = COALESCE(NULLIF(developer_email_text, ''), 'Hi showTime Team, I''m {{user}}. I would like to connect with the developer.'),
	    top_whatsapp_text = COALESCE(NULLIF(top_whatsapp_text, ''), 'Hi showTime Team, I''m {{user}}. I visited showTime and would like to connect with you.'),
    support_email_subject = COALESCE(NULLIF(support_email_subject, ''), 'showTime Support Request'),
    support_email_text = COALESCE(NULLIF(support_email_text, ''), 'Hi showTime Team, I''m {{user}}. I need support. Please help me with my request.')
WHERE developer_id = 1;

UPDATE public.developer_profiles
SET experience_last_increment_year =
    CASE
        WHEN CURRENT_DATE >= make_date(EXTRACT(YEAR FROM CURRENT_DATE)::integer, 5, 16)
            THEN EXTRACT(YEAR FROM CURRENT_DATE)::integer
        ELSE EXTRACT(YEAR FROM CURRENT_DATE)::integer - 1
    END
WHERE developer_id = 1
  AND experience_last_increment_year IS NULL;

CREATE OR REPLACE VIEW public.""vwDeveloperProfile"" AS
SELECT
    developer_id AS ""DeveloperId"",
    full_name AS ""FullName"",
    email AS ""Email"",
    phone AS ""Phone"",
    bio AS ""Bio"",
    address AS ""Address"",
    experience_years AS ""ExperienceYears"",
    skills AS ""Skills"",
    education AS ""Education"",
    projects AS ""Projects"",
    technologies AS ""Technologies"",
    achievements AS ""Achievements"",
    resume_url AS ""ResumeUrl"",
    profile_image AS ""ProfileImage"",
    github_url AS ""GitHubUrl"",
    linked_in_url AS ""LinkedInUrl"",
    twitter_url AS ""TwitterUrl"",
    instagram_url AS ""InstagramUrl"",
    facebook_url AS ""FacebookUrl"",
    youtube_url AS ""YoutubeUrl"",
    website_url AS ""WebsiteUrl"",
    support_phone AS ""SupportPhone"",
    support_email AS ""SupportEmail"",
    support_whatsapp_text AS ""SupportWhatsAppText"",
    support_whatsapp_phone AS ""SupportWhatsAppPhone"",
    is_support_whatsapp_same_as_phone AS ""IsSupportWhatsAppSameAsPhone"",
    top_whatsapp_text AS ""TopWhatsAppText"",
	    support_email_subject AS ""SupportEmailSubject"",
	    support_email_text AS ""SupportEmailText"",
	    developer_whatsapp_phone AS ""DeveloperWhatsAppPhone"",
	    is_developer_whatsapp_same_as_phone AS ""IsDeveloperWhatsAppSameAsPhone"",
	    developer_email AS ""DeveloperEmail"",
	    developer_email_subject AS ""DeveloperEmailSubject"",
	    developer_email_text AS ""DeveloperEmailText""
FROM public.developer_profiles;
");
        }
    }
}
