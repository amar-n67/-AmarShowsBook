using AmarShowsBook.Data;
using AmarShowsBook.Models.ViewModels;
using AmarShowsBook.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace AmarShowsBook.Controllers
{
    public class DeveloperController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly RbacService _rbacService;
        private readonly IWebHostEnvironment _environment;

        public DeveloperController(
            ApplicationDbContext context,
            RbacService rbacService,
            IWebHostEnvironment environment)
        {
            _context = context;
            _rbacService = rbacService;
            _environment = environment;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(Profile));
        }

        public IActionResult Profile()
        {
            EnsureDeveloperProfileStore();

            var developer =
            _context.DeveloperProfiles?
            .FirstOrDefault();

            ViewBag.CanEditDeveloperProfile = CanEditDeveloperProfile();

            return View("Index",
                developer ??
                new DeveloperVM
                {
                    FullName="Amar",
                    Bio="Developer Profile",
                    Email="example@gmail.com",
                    ExperienceYears=0
                });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(DeveloperVM model, IFormFile? profilePhoto)
        {
            if (!CanEditDeveloperProfile())
            {
                TempData["Error"] = "Only developer mode role can edit this profile.";
                return RedirectToAction(nameof(Profile));
            }

            EnsureDeveloperProfileStore();

            if (profilePhoto != null && profilePhoto.Length > 0)
            {
                var imagePath = await SaveProfilePhoto(profilePhoto);
                if (string.IsNullOrWhiteSpace(imagePath))
                {
                    TempData["Error"] = "Scene cut: upload a valid JPG, PNG, WEBP, or GIF profile photo.";
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
    updated_at = CURRENT_TIMESTAMP;
");

            TempData["Success"] = "Profile reel updated. The new developer scene is live.";
            return RedirectToAction(nameof(Profile));
        }

        private bool CanEditDeveloperProfile()
        {
            var userIdText =
                HttpContext.Session.GetString("UserId");

            return int.TryParse(userIdText, out var userId) &&
                (_rbacService.HasAnyActiveRole(userId, "AMAR_SUPER_ADMIN", "AMAR_DEVELOPER") ||
                 _rbacService.HasPermission(userId, "DEVELOPER", "EDIT"));
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
    updated_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT ck_developer_profiles_single_row CHECK (developer_id = 1)
);

INSERT INTO public.developer_profiles
(
    developer_id,
    full_name,
    email,
    bio,
    experience_years
)
SELECT
    1,
    'Amar',
    'example@gmail.com',
    'Developer Profile',
    0
WHERE NOT EXISTS
(
    SELECT 1
    FROM public.developer_profiles
    WHERE developer_id = 1
);

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
    website_url AS ""WebsiteUrl""
FROM public.developer_profiles;
");
        }
    }
}
