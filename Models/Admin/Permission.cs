namespace AmarShowsBook.Models.Admin
{
    // Human Comment:
    // Stores permission master list

    public class Permission
    {
        public int Id { get; set; }

        public string Module { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}