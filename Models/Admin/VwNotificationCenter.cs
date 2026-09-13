using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models.Admin
{
    public class VwNotificationCenter
    {
        [Column("notification_id")]
        public long NotificationId { get; set; }

        [Column("user_name")]
        public string? UserName { get; set; }

        [Column("user_email")]
        public string? UserEmail { get; set; }

        [Column("template_code")]
        public string? TemplateCode { get; set; }

        [Column("template_name")]
        public string? TemplateName { get; set; }

        [Column("notification_type")]
        public string? NotificationType { get; set; }

        [Column("title")]
        public string? Title { get; set; }

        [Column("message")]
        public string? Message { get; set; }

        [Column("status")]
        public string? Status { get; set; }

        [Column("priority")]
        public string? Priority { get; set; }

        [Column("sent_at")]
        public DateTime? SentAt { get; set; }

        [Column("delivered_at")]
        public DateTime? DeliveredAt { get; set; }

        [Column("read_at")]
        public DateTime? ReadAt { get; set; }

        [Column("retry_count")]
        public int RetryCount { get; set; }

        [Column("failure_reason")]
        public string? FailureReason { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("is_error")]
        public int IsError { get; set; }
    }
}
