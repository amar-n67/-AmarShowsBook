// Human Comment:
// Model for vw_notification_center database view

namespace AmarShowsBook.Models.Admin
{
    public class VwNotificationCenter
    {
        public long NotificationId { get; set; }

        public string UserName { get; set; }

        public string UserEmail { get; set; }

        public string TemplateCode { get; set; }

        public string TemplateName { get; set; }

        public string NotificationType { get; set; }

        public string Title { get; set; }

        public string Message { get; set; }

        public string Status { get; set; }

        public string Priority { get; set; }

        public DateTime? SentAt { get; set; }

        public DateTime? DeliveredAt { get; set; }

        public DateTime? ReadAt { get; set; }

        public int RetryCount { get; set; }

        public string FailureReason { get; set; }

        public DateTime CreatedAt { get; set; }

        public int IsError { get; set; }
    }
}