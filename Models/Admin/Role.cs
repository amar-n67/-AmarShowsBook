namespace AmarShowsBook.Models.Admin
{
    // Human Comment:
    // Stores admin/user role definitions

    public class Role
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}