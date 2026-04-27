using System;

namespace AmarShowsBook.Models
{
    public class ShowSchedule
    {
        public int Id { get; set; }

        public int? MovieId { get; set; }
        public Movie Movie { get; set; }

        public int? StandupShowId { get; set; }
        public StandupShow StandupShow { get; set; }

        public int? LiveStreamId { get; set; }
        public LiveStream LiveStream { get; set; }

        public int LocationId { get; set; }
        public Location Location { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Type { get; set; } // Movie / Standup / Live
    }
}