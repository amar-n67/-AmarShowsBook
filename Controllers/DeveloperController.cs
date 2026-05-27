using AmarShowsBook.Data;
using AmarShowsBook.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AmarShowsBook.Controllers
{
    public class DeveloperController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DeveloperController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var developer =
            _context.DeveloperProfiles?
            .FirstOrDefault();

            return View(
                developer ??
                new DeveloperVM
                {
                    FullName="Amar",
                    Bio="Developer Profile",
                    Email="example@gmail.com",
                    ExperienceYears=0
                });
        }
    }
}