namespace AmarShowsBook.Models.Admin
{
    public class ActivityLog
    {
        public long Id { get; set; }

        public int? UserId { get; set; }

        public string Action { get; set; }

        public string Module { get; set; }

        public string EntityType { get; set; }

        public int? EntityId { get; set; }

        public string Description { get; set; }

        public string Status { get; set; }

        public string? ErrorCode { get; set; }

        public string? ErrorMessage { get; set; }

        public int IsError { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}