using Microsoft.AspNetCore.Mvc;
using AmarShowsBook.Data;
using AmarShowsBook.Models;
using AmarShowsBook.Services;
using System.Globalization;

namespace AmarShowsBook.Controllers
{
    // Legacy scheduling page for adding a single show time; admin show-management pages handle richer edits.
    public class ScheduleController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogger _activityLogger;

        public ScheduleController(
            ApplicationDbContext context,
            IActivityLogger activityLogger)
        {
            _context = context;
            _activityLogger = activityLogger;
        }

        // Loads lookup lists used by the create form.
        public IActionResult Create()
        {
            LoadCreateLookups();

            return View();
        }

        // Validates the chosen item and location before creating the schedule row.
        [HttpPost]
        public async Task<IActionResult> Create(string type, int itemId, int locationId, DateTime startTime)
        {
            LoadCreateLookups();

            if (itemId <= 0 || locationId <= 0 || startTime == default)
            {
                await LogScheduleAttempt(
                    type,
                    itemId,
                    locationId,
                    startTime,
                    "FAILED",
                    "Schedule form was submitted with missing item, location, or start time.");

                ViewBag.Error = "Please select show, country/state/region, and start time.";
                return View();
            }

            int duration = 0;

            if (type == "Movie")
                duration = _context.Movies.Find(itemId)?.Duration ?? 0;

            if (type == "Standup")
                duration = _context.StandupShows.Find(itemId)?.Duration ?? 0;

            if (type == "Live")
                duration = _context.LiveStreams.Find(itemId)?.Duration ?? 0;

            if (duration <= 0)
            {
                await LogScheduleAttempt(
                    type,
                    itemId,
                    locationId,
                    startTime,
                    "FAILED",
                    "Selected show was not found while creating a schedule.");

                ViewBag.Error = "Selected show was not found.";
                return View();
            }

            DateTime endTime = startTime.AddMinutes(duration);

            if (type == "Standup")
            {
                bool clash = _context.ShowSchedules.Any(s =>
                    s.LocationId == locationId &&
                    s.Type == "Standup" &&
                    startTime < s.EndTime &&
                    endTime > s.StartTime
                );

                if (clash)
                {
                    await LogScheduleAttempt(
                        type,
                        itemId,
                        locationId,
                        startTime,
                        "FAILED",
                        "Schedule creation blocked because another standup already uses the location.");

                    ViewBag.Error = "This location is already booked for another performance at the selected time.";
                    return View();
                }
            }

            var schedule = new ShowSchedule
            {
                Type = type,
                LocationId = locationId,
                StartTime = startTime,
                EndTime = endTime,
                ShowDay = startTime.ToString("dddd", CultureInfo.InvariantCulture)
            };

            if (type == "Movie") schedule.MovieId = itemId;
            if (type == "Standup") schedule.StandupShowId = itemId;
            if (type == "Live") schedule.LiveStreamId = itemId;

            _context.ShowSchedules.Add(schedule);
            await _context.SaveChangesAsync();

            await _activityLogger.LogAsync(
                userId: GetCurrentUserId(),
                action: "CREATE_SCHEDULE",
                module: "SCHEDULE",
                entityType: "SHOW_SCHEDULE",
                entityId: schedule.Id,
                description: "Show schedule created",
                status: "SUCCESS",
                newValue: schedule,
                metadata: new
                {
                    type,
                    itemId,
                    locationId,
                    startTime,
                    endTime
                });

            ViewBag.Success = "Show scheduled successfully!";
            return View();
        }

        // Rebuilds dropdown data every time the page returns after validation.
        private void LoadCreateLookups()
        {
            ViewBag.Movies = _context.Movies.ToList();
            ViewBag.Standups = _context.StandupShows.ToList();
            ViewBag.Lives = _context.LiveStreams.ToList();
            ViewBag.Locations = _context.Locations.ToList();
        }

        private async Task LogScheduleAttempt(
            string type,
            int itemId,
            int locationId,
            DateTime startTime,
            string status,
            string description)
        {
            await _activityLogger.LogAsync(
                userId: GetCurrentUserId(),
                action: "CREATE_SCHEDULE",
                module: "SCHEDULE",
                entityType: "SHOW_SCHEDULE",
                description: description,
                status: status,
                isError: status.Equals("FAILED", StringComparison.OrdinalIgnoreCase) ? 1 : 0,
                metadata: new
                {
                    type,
                    itemId,
                    locationId,
                    startTime
                });
        }

        private int? GetCurrentUserId()
        {
            return int.TryParse(HttpContext.Session.GetString("UserId"), out var userId)
                ? userId
                : null;
        }
    }
}
