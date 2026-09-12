public class Screen
{
    public long Id { get; set; }

    public long VenueId { get; set; }

    public string ScreenCode { get; set; }

    public string ScreenName { get; set; }

    public int TotalSeats { get; set; }

    public string ScreenType { get; set; }

    public string AudioSystem { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}