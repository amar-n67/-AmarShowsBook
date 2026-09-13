namespace AmarShowsBook.Models.ViewModels
{
    public class HomeViewModel
    {
        public List<HomeShowViewModel> HomeShows { get; set; }
            = new();
            public string Description { get; set; }
public string PosterUrl { get; set; }
    }
}