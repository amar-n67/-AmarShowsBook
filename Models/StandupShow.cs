namespace AmarShowsBook.Models
{
    public class StandupShow
    {
        public int Id { get; set; }

        public string Title { get; set; }
        public string Comedian { get; set; }

        public int Duration { get; set; }
        public string? Description { get; set; }
        public string? PosterUrl { get; set; }
        public string? Images { get; set; }
        public string? TrailerUrl { get; set; }
    }
}
