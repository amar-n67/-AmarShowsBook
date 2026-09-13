namespace AmarShowsBook.Models.ViewModels;

public class AdminPaginationViewModel
{
    public int CurrentPage { get; set; }

    public int TotalPages { get; set; }

    public string ActionUrl { get; set; } = string.Empty;
}
