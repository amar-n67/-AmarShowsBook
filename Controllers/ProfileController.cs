using Microsoft.AspNetCore.Mvc;
using AmarShowsBook.Data;
using AmarShowsBook.Models;
using System.IO;
using System.Linq;

public class ProfileController : Controller
{
    private readonly ApplicationDbContext _context;

    public ProfileController(ApplicationDbContext context)
    {
        _context = context;
    }

    // LOAD PROFILE
    public IActionResult Index()
    {
        var email = HttpContext.Session.GetString("UserEmail");

        var user = _context.Users.FirstOrDefault(u => u.Email == email);

        return View(user);
    }

    // SAVE PROFILE
    [HttpPost]
    public IActionResult Index(User model, IFormFile profileImage)
    {
        var email = HttpContext.Session.GetString("UserEmail");

        var user = _context.Users.FirstOrDefault(u => u.Email == email);

        if (user == null) return RedirectToAction("Login", "Auth");

        // ================= EMAIL UNIQUE CHECK =================
        if (_context.Users.Any(u => u.Email == model.Email && u.Id != user.Id))
        {
            TempData["Error"] = "Email already exists";
            return View(user);
        }

        // ================= MOBILE UNIQUE CHECK =================
        if (_context.Users.Any(u => u.Mobile == model.Mobile && u.Id != user.Id))
        {
            TempData["Error"] = "Mobile already exists";
            return View(user);
        }

        // ================= UPDATE FIELDS =================
        user.Email = model.Email;
        user.Mobile = model.Mobile;
        user.Address = model.Address;

user.Country = model.Country;
user.State = model.State;
user.District = model.District;
user.Pincode = model.Pincode;
        user.Genre = model.Genre;
        user.Language = model.Language;

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
}
        // ================= AUDIT =================
var currentUser = HttpContext.Session.GetString("UserEmail");

user.UpdatedAt = DateTime.UtcNow;
user.UpdatedBy = currentUser ?? "System";

        _context.SaveChanges();

        TempData["Success"] = "Profile updated successfully";

        return RedirectToAction("Index");
    }
}