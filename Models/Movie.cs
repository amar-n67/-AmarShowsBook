using System.ComponentModel.DataAnnotations;

namespace AmarShowsBook.Models
{
    public class Movie
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        public string Director { get; set; }
        public string Producer { get; set; }
        public string Cast { get; set; }

        // Duration in minutes
        public int Duration { get; set; }
    }
}