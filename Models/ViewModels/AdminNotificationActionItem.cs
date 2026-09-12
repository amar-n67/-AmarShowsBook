namespace AmarShowsBook.Models.ViewModels;

public class AdminNotificationActionItem
{
    public string Id { get; set; } = string.Empty;

    public DateTime Time { get; set; }

    public string Category { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Priority { get; set; } = "NORMAL";

    public string UserName { get; set; } = string.Empty;

    public string UserEmail { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;

    public string ActionText { get; set; } = "Open";

    public string ActionUrl { get; set; } = string.Empty;

    public bool RequiresAction { get; set; }
}
