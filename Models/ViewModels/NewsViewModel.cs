namespace AmarShowsBook.Models.ViewModels;

public class NewsViewModel
{
    public List<NewsChannel> Channels { get; set; } = new();
    public List<NewsBroadcastSlot> Slots { get; set; } = new();
    public List<string> Languages { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public List<string> Countries { get; set; } = new();
    public List<string> States { get; set; } = new();
    public List<string> Cities { get; set; } = new();
}
