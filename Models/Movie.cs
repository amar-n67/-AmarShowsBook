using System.ComponentModel.DataAnnotations;

namespace AmarShowsBook.Models
{
    public class Movie
    {
        public int Id { get; set; }

        [Required]
        //public string Title { get; set; } //comment to handle nullability in the database
        public string Title { get; set; } = string.Empty;
        //public string Director { get; set; }

        // Director can be empty initially
        public string? Director { get; set; }
        public string Producer { get; set; }
        public string Cast { get; set; }

        // Duration in minutes
        public int Duration { get; set; }

        public string? Description { get; set; }
        public string? PosterUrl { get; set; }
        public string? Images { get; set; }
        public string? TrailerUrl { get; set; }
        public decimal? ImdbRating { get; set; }
    }
}
