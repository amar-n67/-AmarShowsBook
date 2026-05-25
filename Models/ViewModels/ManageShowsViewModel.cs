namespace AmarShowsBook.Models.ViewModels;

public class ManageShowsViewModel
{
    public List<ShowSchedule> Schedules { get; set; } = new();
    public List<Location> Locations { get; set; } = new();
    public List<Venue> Venues { get; set; } = new();
    public List<Screen> Screens { get; set; } = new();
}

public class ManageShowCreateViewModel
{
    public string Type { get; set; } = "Movie";
    public string Title { get; set; } = string.Empty;
    public string? SecondaryName { get; set; }
    public int Duration { get; set; } = 120;
    public DateTime StartTime { get; set; } = DateTime.Now.AddDays(1);
    public int LocationId { get; set; }
    public long VenueId { get; set; }
    public long ScreenId { get; set; }
    public int TotalSeats { get; set; } = 70;
}
