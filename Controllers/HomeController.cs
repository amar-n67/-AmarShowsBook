using AmarShowsBook.Services; // Added for activity logging
using Npgsql;                 // Added for PostgreSQL exception handling
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AmarShowsBook.Data;
using AmarShowsBook.Models;
using AmarShowsBook.Models.ViewModels;
using System.Globalization;

namespace AmarShowsBook.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogger _activityLogger; //Added for logging activities

        // ====================== commented out old constructor ======================
        // ✅ Inject BOTH logger + DB context
        // public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        // {
        //     _logger = logger;
        //     _context = context;
        // }
        // ====================== Updated constructor to include activity logger ======================
        public HomeController(
        ILogger<HomeController> logger,
        ApplicationDbContext context,
        IActivityLogger activityLogger)
        {
            _logger = logger;
            _context = context;
            _activityLogger = activityLogger;
        }
        // ====================== End of updated constructor ======================

        //=================== commented out old Index action ======================
        // ================= HOME =================

        // public IActionResult Index(string type = "Movie")
        // {
        //     // 🔐 Protect page (only logged in users)
        //     if (HttpContext.Session.GetString("UserEmail") == null)
        //     {
        //         return RedirectToAction("Login", "Auth");
        //     }

        //     // 📦 Fetch schedules based on type
        //     var schedules = _context.ShowSchedules
        //         .Include(s => s.Movie)
        //         .Include(s => s.StandupShow)
        //         .Include(s => s.LiveStream)
        //         .Include(s => s.Location)
        //         .Where(s => s.Type == type)
        //         .OrderBy(s => s.StartTime)
        //         .ToList();

        //     var vm = new HomeViewModel
        //     {
        //         Schedules = schedules
        //     };

        //     return View(vm);
        // }
        // ====================== Updated Index action to log activity ======================
public IActionResult Index(string type = "Movie")
{
    return RedirectToAction(nameof(ShowTime), new { type });
}

public async Task<IActionResult> ShowTime(string type = "Movie")
{
    try
    {
        var userEmail = HttpContext.Session.GetString("UserEmail");

        var user = string.IsNullOrWhiteSpace(userEmail)
            ? null
            : _context.Users.FirstOrDefault(u => u.Email == userEmail);

        // var schedules = _context.ShowSchedules
        //     .Include(s => s.Movie)
        //     .Include(s => s.StandupShow)
        //     .Include(s => s.LiveStream)
        //     .Include(s => s.Location)
        //     .Where(s => s.Type == type)
        //     .OrderBy(s => s.StartTime)
        //     .ToList();
        string dbType = type switch
{
    "Movie" => "Movie",
    "Standup" => "StandupShow",
    "Live" => "LiveStream",
    _ => "Movie"
};

// var schedules = await _context.HomeShows
//     .Where(x => x.ShowType == type)
//     .Where(x => x.StartTime >= DateTime.UtcNow)
//     .OrderByDescending(x => x.StartTime)
//     .ToListAsync();

// var vm = new HomeViewModel
// {
//     HomeShows = schedules
// };

// var schedules = await _context.HomeShows
//     .Where(x => x.ShowType == type)
//     .Where(x => x.StartTime >= DateTime.UtcNow)
//     .OrderByDescending(x => x.StartTime)
//     .Select(x => new HomeShowViewModel
//     {
//         ScheduleId = x.ScheduleId,

//         Title = x.Title,
//         Description = x.Description,

//         PosterUrl = x.PosterUrl,
//         Images = x.Images,
//         TrailerUrl = x.TrailerUrl,

//         StartTime = x.StartTime,
//         EndTime = x.EndTime,

//         Location = x.Location,
//         State = x.State,
//         Country = x.Country,

//         ShowType = x.ShowType
//     })
//     .ToListAsync();

await EnsureHomeShowListingView();

var schedules = await _context.HomeShows
.Where(x => x.ShowType == type)
.Where(x => x.StartTime >= DateTime.UtcNow)
.OrderBy(x => x.StartTime)
.Select(x => new HomeShowViewModel
{
    ScheduleId = x.ScheduleId, // REQUIRED

    ShowId = x.ShowId,

    ShowType = x.ShowType,

    Title = x.Title,

    Description = x.Description,

    PosterUrl = x.PosterUrl,

    Images = x.Images,

    TrailerUrl = x.TrailerUrl,

    StartTime = x.StartTime,

    EndTime = x.EndTime,

    Location = x.Location,

    State = x.State,

    Country = x.Country
    ,
    Director = x.Director,
    Cast = x.Cast,
    ImdbRating = x.ImdbRating,
    VenueName = x.VenueName,
    ScreenName = x.ScreenName
})
.ToListAsync();

var vm = new HomeViewModel
{
    HomeShows = schedules
};

var scheduleIds = schedules.Select(x => x.ScheduleId).ToList();
var theaterLookup = await
(
    from schedule in _context.ShowSchedules.AsNoTracking()
    join screen in _context.Screens.AsNoTracking()
        on schedule.ScreenId equals screen.Id into screenGroup
    from screen in screenGroup.DefaultIfEmpty()
    join venue in _context.Venues.AsNoTracking()
        on screen.VenueId equals venue.Id into venueGroup
    from venue in venueGroup.DefaultIfEmpty()
    where scheduleIds.Contains(schedule.Id)
    select new
    {
        schedule.Id,
        TheaterDetails = venue == null
            ? null
            : string.Join(" / ", new[] { venue.VenueName, screen.ScreenName, venue.Address, venue.City }
                .Where(value => !string.IsNullOrWhiteSpace(value)))
    }
).ToDictionaryAsync(x => x.Id, x => x.TheaterDetails);

foreach (var show in schedules)
{
    if (theaterLookup.TryGetValue(show.ScheduleId, out var theaterDetails))
    {
        show.TheaterDetails = theaterDetails;
    }

    if (!string.IsNullOrWhiteSpace(show.VenueName) || !string.IsNullOrWhiteSpace(show.ScreenName))
    {
        show.TheaterDetails = string.Join(" / ", new[] { show.VenueName, show.ScreenName, show.TheaterDetails }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
    }
}

        await _activityLogger.LogAsync(
            userId: user?.Id,
            action: "VIEW_HOME",
            module: "HOME",
            entityType: "SHOW_SCHEDULE",
            description: string.IsNullOrWhiteSpace(userEmail)
                ? $"Guest viewed {type} schedules"
                : $"Viewed {type} schedules",
            status: "SUCCESS",
            isError: 0,
            metadata: new
            {
                Type = type,
                Count = schedules.Count
            }
        );

        return View("Index", vm);
    }
    catch (PostgresException ex)
    {
        await _activityLogger.LogAsync(
            action: "VIEW_HOME",
            module: "HOME",
            entityType: "SHOW_SCHEDULE",
            description: "Database error while loading home page",
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
            action: "VIEW_HOME",
            module: "HOME",
            entityType: "SHOW_SCHEDULE",
            description: "Unexpected error while loading home page",
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
// ====================== End of updated Index action ======================
        private async Task EnsureHomeShowListingView()
        {
            await _context.Database.ExecuteSqlRawAsync(@"
ALTER TABLE public.""Movies"" ADD COLUMN IF NOT EXISTS ""Description"" text;
ALTER TABLE public.""Movies"" ADD COLUMN IF NOT EXISTS ""PosterUrl"" text;
ALTER TABLE public.""Movies"" ADD COLUMN IF NOT EXISTS ""Images"" text;
ALTER TABLE public.""Movies"" ADD COLUMN IF NOT EXISTS ""TrailerUrl"" text;
ALTER TABLE public.""Movies"" ADD COLUMN IF NOT EXISTS ""ImdbRating"" numeric(3,1);
ALTER TABLE public.""StandupShows"" ADD COLUMN IF NOT EXISTS ""Description"" text;
ALTER TABLE public.""StandupShows"" ADD COLUMN IF NOT EXISTS ""PosterUrl"" text;
ALTER TABLE public.""StandupShows"" ADD COLUMN IF NOT EXISTS ""Images"" text;
ALTER TABLE public.""StandupShows"" ADD COLUMN IF NOT EXISTS ""TrailerUrl"" text;
ALTER TABLE public.""LiveStreams"" ADD COLUMN IF NOT EXISTS ""Description"" text;
ALTER TABLE public.""LiveStreams"" ADD COLUMN IF NOT EXISTS ""PosterUrl"" text;
ALTER TABLE public.""LiveStreams"" ADD COLUMN IF NOT EXISTS ""Images"" text;
ALTER TABLE public.""LiveStreams"" ADD COLUMN IF NOT EXISTS ""TrailerUrl"" text;

CREATE OR REPLACE VIEW public.vw_home_show_listing AS
SELECT
    s.""Id"" AS schedule_id,
    CASE
        WHEN s.""MovieId"" IS NOT NULL THEN 'Movie'
        WHEN s.""StandupShowId"" IS NOT NULL THEN 'Standup'
        WHEN s.""LiveStreamId"" IS NOT NULL THEN 'Live'
        ELSE COALESCE(NULLIF(s.""Type"", ''), 'Movie')
    END AS show_type,
    COALESCE(s.""MovieId"", s.""StandupShowId"", s.""LiveStreamId"", 0) AS show_id,
    COALESCE(m.""Title"", st.""Title"", ls.""Title"", 'Untitled Show') AS title,
    COALESCE(m.""Description"", st.""Description"", ls.""Description"",
        CASE
            WHEN m.""Id"" IS NOT NULL THEN concat_ws(' | ', NULLIF(m.""Director"", ''), NULLIF(m.""Producer"", ''), NULLIF(m.""Cast"", ''))
            WHEN st.""Id"" IS NOT NULL THEN 'Comedian: ' || st.""Comedian""
            WHEN ls.""Id"" IS NOT NULL THEN 'Host: ' || ls.""Host""
            ELSE ''
        END) AS ""Description"",
    COALESCE(m.""PosterUrl"", st.""PosterUrl"", ls.""PosterUrl"") AS ""PosterUrl"",
    COALESCE(m.""Images"", st.""Images"", ls.""Images"") AS ""Images"",
    COALESCE(m.""TrailerUrl"", st.""TrailerUrl"", ls.""TrailerUrl"") AS ""TrailerUrl"",
    COALESCE(m.""Director"", st.""Comedian"", ls.""Host"") AS director,
    m.""Cast"" AS cast,
    m.""ImdbRating"" AS imdb_rating,
    v.venue_name,
    sc.screen_name,
    s.""StartTime"" AS start_time,
    s.""EndTime"" AS end_time,
    COALESCE(l.""Area"", '') AS location,
    COALESCE(l.""State"", '') AS state,
    COALESCE(l.""Country"", '') AS country
FROM public.""ShowSchedules"" s
LEFT JOIN public.""Movies"" m ON s.""MovieId"" = m.""Id""
LEFT JOIN public.""StandupShows"" st ON s.""StandupShowId"" = st.""Id""
LEFT JOIN public.""LiveStreams"" ls ON s.""LiveStreamId"" = ls.""Id""
LEFT JOIN public.""Locations"" l ON s.""LocationId"" = l.""Id""
LEFT JOIN public.screens sc ON s.screen_id = sc.id
LEFT JOIN public.venues v ON sc.venue_id = v.id;
");
        }

        // ================= PRIVACY =================

        public IActionResult Privacy()
        {
            return View();
        }

        // ================= ERROR =================

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
        [HttpGet]
        public async Task<IActionResult> GetCountries()
        {
            try
            {
                var dataCountries = await _context.Countries
                    .AsNoTracking()
                    .Select(c => new { id = (int?)c.Id, name = c.Name })
                    .ToListAsync();

                var locationCountries = await _context.Locations
                    .AsNoTracking()
                    .Where(l => l.Country != null && l.Country.Trim() != "")
                    .Select(l => new { id = (int?)null, name = l.Country })
                    .ToListAsync();

                var countries = dataCountries
                    .Concat(locationCountries)
                    .Where(c => !string.IsNullOrWhiteSpace(c.name))
                    .GroupBy(c => c.name.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(g => new
                    {
                        id = g.FirstOrDefault(x => x.id.HasValue)?.id,
                        name = g.Key
                    })
                    .OrderBy(c => c.name)
                    .ToList();

                return Json(countries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load countries for home location dropdown.");
                return StatusCode(500, new { message = "Failed to load countries." });
            }
        }
public async Task<IActionResult> ShowDates(
int id,
string type)
{
    var dates = await _context.HomeShows
        .Where(x =>
            x.ShowId == id &&
            x.ShowType == type)
        .OrderBy(x=>x.StartTime)
        .ToListAsync();

    return View(dates);
}
        [HttpGet]
        public async Task<IActionResult> GetStates(int? countryId, string? countryName)
        {
            try
            {
                var stateRows = countryId.HasValue
                    ? await _context.States
                    .AsNoTracking()
                    .Where(s => s.CountryId == countryId.Value)
                    .Select(s => new { id = (int?)s.Id, name = s.Name })
                    .ToListAsync()
                    : new List<object>().Select(_ => new { id = (int?)null, name = string.Empty }).ToList();

                var locationStates = await _context.Locations
                    .AsNoTracking()
                    .Where(l =>
                        l.State != null &&
                        l.State.Trim() != "" &&
                        (string.IsNullOrWhiteSpace(countryName) || l.Country.ToLower() == countryName.ToLower()))
                    .Select(l => new { id = (int?)null, name = l.State })
                    .ToListAsync();

                var states = stateRows
                    .Concat(locationStates)
                    .Where(s => !string.IsNullOrWhiteSpace(s.name))
                    .GroupBy(s => s.name.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(g => new
                    {
                        id = g.FirstOrDefault(x => x.id.HasValue)?.id,
                        name = g.Key
                    })
                    .OrderBy(s => s.name)
                    .ToList();

                return Json(states);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load states for country {CountryId} / {CountryName}.", countryId, countryName);
                return StatusCode(500, new { message = "Failed to load states." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDistricts(int? stateId, string? stateName, string? countryName)
        {
            try
            {
                var districtRows = stateId.HasValue
                    ? await _context.Districts
                    .AsNoTracking()
                    .Where(d => d.StateId == stateId.Value)
                    .Select(d => new { id = (int?)d.Id, name = d.Name })
                    .ToListAsync()
                    : new List<object>().Select(_ => new { id = (int?)null, name = string.Empty }).ToList();

                var locationRegions = await _context.Locations
                    .AsNoTracking()
                    .Where(l =>
                        l.Area != null &&
                        l.Area.Trim() != "" &&
                        (string.IsNullOrWhiteSpace(countryName) || l.Country.ToLower() == countryName.ToLower()) &&
                        (string.IsNullOrWhiteSpace(stateName) || l.State.ToLower() == stateName.ToLower()))
                    .Select(l => new { id = (int?)null, name = l.Area })
                    .ToListAsync();

                var districts = districtRows
                    .Concat(locationRegions)
                    .Where(d => !string.IsNullOrWhiteSpace(d.name))
                    .GroupBy(d => d.name.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(g => new
                    {
                        id = g.FirstOrDefault(x => x.id.HasValue)?.id,
                        name = g.Key
                    })
                    .OrderBy(d => d.name)
                    .ToList();

                return Json(districts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load districts for state {StateId} / {StateName}.", stateId, stateName);
                return StatusCode(500, new { message = "Failed to load regions." });
            }
        }
    }
}
