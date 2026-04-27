using AmarShowsBook.Models;
using System;
using System.Linq;

namespace AmarShowsBook.Data
{
    public static class DbSeeder
    {
        public static void Seed(ApplicationDbContext context)
        {
            // ================= MOVIES =================
            if (!context.Movies.Any())
            {
                context.Movies.AddRange(
                    new Movie { Title = "Inception", Director = "Christopher Nolan", Producer = "Emma Thomas", Cast = "Leonardo DiCaprio", Duration = 148 },
                    new Movie { Title = "Interstellar", Director = "Christopher Nolan", Producer = "Emma Thomas", Cast = "Matthew McConaughey", Duration = 169 },
                    new Movie { Title = "Avengers Endgame", Director = "Russo Brothers", Producer = "Marvel", Cast = "Robert Downey Jr.", Duration = 181 },
                    new Movie { Title = "Joker", Director = "Todd Phillips", Producer = "Warner Bros", Cast = "Joaquin Phoenix", Duration = 122 },
                    new Movie { Title = "Titanic", Director = "James Cameron", Producer = "Fox", Cast = "Leonardo DiCaprio", Duration = 195 },
                    new Movie { Title = "RRR", Director = "S.S Rajamouli", Producer = "DVV", Cast = "NTR, Ram Charan", Duration = 182 },
                    new Movie { Title = "Bahubali", Director = "S.S Rajamouli", Producer = "Arka Media", Cast = "Prabhas", Duration = 159 },
                    new Movie { Title = "Dangal", Director = "Nitesh Tiwari", Producer = "Aamir Khan", Cast = "Aamir Khan", Duration = 161 },
                    new Movie { Title = "3 Idiots", Director = "Rajkumar Hirani", Producer = "Vinod Chopra", Cast = "Aamir Khan", Duration = 170 },
                    new Movie { Title = "Jawan", Director = "Atlee", Producer = "Red Chillies", Cast = "Shah Rukh Khan", Duration = 169 }
                );
            }

            // ================= STANDUP =================
            if (!context.StandupShows.Any())
            {
                context.StandupShows.AddRange(
                    new StandupShow { Title = "Zakir Live", Comedian = "Zakir Khan", Duration = 90 },
                    new StandupShow { Title = "Biswa Special", Comedian = "Biswa Kalyan", Duration = 75 },
                    new StandupShow { Title = "Bassi Live", Comedian = "Anubhav Singh Bassi", Duration = 90 },
                    new StandupShow { Title = "Harsh Live", Comedian = "Harsh Gujral", Duration = 80 }
                );
            }

            // ================= LIVE STREAM =================
            if (!context.LiveStreams.Any())
            {
                for (int i = 1; i <= 20; i++)
                {
                    context.LiveStreams.Add(new LiveStream
                    {
                        Title = "Live Event " + i,
                        Host = "Host " + i,
                        Duration = 120
                    });
                }
            }

            // ================= LOCATION (MANDATORY) =================
            if (!context.Locations.Any())
            {
                context.Locations.Add(new Location
                {
                    Country = "India",
                    State = "Delhi",
                    Area = "New Delhi"
                });
            }

            context.SaveChanges();

            // ================= AUTO GENERATE 100+ SCHEDULES =================
if (!context.ShowSchedules.Any())
{
    var movies = context.Movies.ToList();
    var standups = context.StandupShows.ToList();
    var lives = context.LiveStreams.ToList();

    var location = context.Locations.First();

    DateTime baseTime = DateTime.UtcNow.Date.AddHours(9); // 9 AM start

    int totalShows = 0;

    // 🎬 MOVIES (repeat across day)
    foreach (var movie in movies)
    {
        for (int i = 0; i < 2; i++) // repeat each movie twice
        {
            context.ShowSchedules.Add(new ShowSchedule
            {
                Type = "Movie",
                MovieId = movie.Id,
                LocationId = location.Id,
                StartTime = baseTime,
                EndTime = baseTime.AddMinutes(movie.Duration)
            });

            baseTime = baseTime.AddMinutes(movie.Duration + 15);
            totalShows++;
        }
    }

    // 🎤 STANDUP (NO OVERLAP)
    foreach (var standup in standups)
    {
        context.ShowSchedules.Add(new ShowSchedule
        {
            Type = "Standup",
            StandupShowId = standup.Id,
            LocationId = location.Id,
            StartTime = baseTime,
            EndTime = baseTime.AddMinutes(standup.Duration)
        });

        baseTime = baseTime.AddMinutes(standup.Duration + 30);
        totalShows++;
    }

    // 📡 LIVE (parallel allowed)
    for (int i = 0; i < 100; i++)
    {
        var live = lives[i % lives.Count];

        context.ShowSchedules.Add(new ShowSchedule
        {
            Type = "Live",
            LiveStreamId = live.Id,
            LocationId = location.Id,
            StartTime = DateTime.UtcNow.AddMinutes(i * 10),
            EndTime = DateTime.UtcNow.AddMinutes(i * 10 + live.Duration)
        });

        totalShows++;
    }

    Console.WriteLine($"✅ Generated {totalShows} shows");

    context.SaveChanges();
}
// COUNTRIES
if (!context.Countries.Any())
{
    context.Countries.AddRange(
        new Country { Code = "IN", Name = "India" },
        new Country { Code = "US", Name = "United States" }
    );
    context.SaveChanges();
}

// STATES
if (!context.States.Any())
{
    var india = context.Countries.First(c => c.Code == "IN");

    context.States.AddRange(
        new State { Name = "Delhi", CountryId = india.Id },
        new State { Name = "Maharashtra", CountryId = india.Id }
    );
    context.SaveChanges();
}

// DISTRICTS
if (!context.Districts.Any())
{
    var delhi = context.States.First(s => s.Name == "Delhi");
    var mh = context.States.First(s => s.Name == "Maharashtra");

    context.Districts.AddRange(
        new District { Name = "New Delhi", StateId = delhi.Id },
        new District { Name = "Dwarka", StateId = delhi.Id },

        new District { Name = "Mumbai", StateId = mh.Id },
        new District { Name = "Pune", StateId = mh.Id }
    );
    context.SaveChanges();
}
        }
    }
}