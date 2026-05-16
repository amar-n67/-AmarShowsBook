using Microsoft.AspNetCore.Mvc;
using AmarShowsBook.Data;
using AmarShowsBook.Models;
using System;
using System.Linq;

namespace AmarShowsBook.Controllers
{
    public class ScheduleController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ScheduleController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Create()
        {
            LoadCreateLookups();

            return View();
        }

        [HttpPost]
        public IActionResult Create(string type, int itemId, int locationId, DateTime startTime)
        {
            LoadCreateLookups();

            if (itemId <= 0 || locationId <= 0 || startTime == default)
            {
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
                ViewBag.Error = "Selected show was not found.";
                return View();
            }

            DateTime endTime = startTime.AddMinutes(duration);

            // ❗ CLASH CHECK (ONLY FOR STANDUP)
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
                    ViewBag.Error = "🎤 This stage is already booked for another performance!";
                    return View();
                }
            }

            var schedule = new ShowSchedule
            {
                Type = type,
                LocationId = locationId,
                StartTime = startTime,
                EndTime = endTime
            };

            if (type == "Movie") schedule.MovieId = itemId;
            if (type == "Standup") schedule.StandupShowId = itemId;
            if (type == "Live") schedule.LiveStreamId = itemId;

            _context.ShowSchedules.Add(schedule);
            _context.SaveChanges();

            ViewBag.Success = "🎬 Show scheduled successfully!";
            return View();
        }

        private void LoadCreateLookups()
        {
            ViewBag.Movies = _context.Movies.ToList();
            ViewBag.Standups = _context.StandupShows.ToList();
            ViewBag.Lives = _context.LiveStreams.ToList();
            ViewBag.Locations = _context.Locations.ToList();
        }
    }
}
