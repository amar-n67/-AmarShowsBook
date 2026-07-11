using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models.Admin
{
    [Table("activity_logs")]
    public class ActivityLog
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int? UserId { get; set; }

        [Column("action")]
        public string Action { get; set; } = "";

        [Column("module")]
        public string Module { get; set; } = "";

        [Column("entity_type")]
        public string EntityType { get; set; } = "";

        [Column("entity_id")]
        public int? EntityId { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("request_method")]
        public string? RequestMethod { get; set; }

        [Column("endpoint")]
        public string? Endpoint { get; set; }

        [Column("ip_address")]
        public string? IpAddress { get; set; }

        [Column("user_agent")]
        public string? UserAgent { get; set; }

        [Column("status")]
        public string Status { get; set; } = "";

        [Column("is_error")]
        public int IsError { get; set; }

        [Column("error_code")]
        public string? ErrorCode { get; set; }

        [Column("error_message")]
        public string? ErrorMessage { get; set; }

        [Column("error_source")]
        public string? ErrorSource { get; set; }

        [Column("stack_trace")]
        public string? StackTrace { get; set; }

        [Column("old_value")]
        public string? OldValue { get; set; }

        [Column("new_value")]
        public string? NewValue { get; set; }

        [Column("metadata")]
        public string? Metadata { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}