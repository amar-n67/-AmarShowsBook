namespace AmarShowsBook.Models.ViewModels
{
    public class HomeShowViewModel
    {
        public int ScheduleId { get; set; }

        public string ShowType { get; set; }
        public string? PosterUrl { get; set; }

public string? Images { get; set; }

public string? TrailerUrl { get; set; }

        public int ShowId { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public string Location { get; set; }

        public string State { get; set; }

        public string Country { get; set; }
        public string? TheaterDetails { get; set; }
    }
}
