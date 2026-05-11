namespace AmarShowsBook.Models.Admin
{
    public class VwNotificationCenter
    {
        public long NotificationId { get; set; }

        public string UserName { get; set; }

        public string Title { get; set; }

        public string Status { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}