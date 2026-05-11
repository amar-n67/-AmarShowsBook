using Microsoft.EntityFrameworkCore;

namespace AmarShowsBook.Models
{
    // Notification analytics SQL view
    [Keyless]
    public class VwNotificationCenter
    {
        public int NotificationId { get; set; }

        public string Status { get; set; }

        public int IsError { get; set; }
    }
}