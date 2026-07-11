using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models.Admin
{
    [Table("activity_logs")]
    public class ActivityLog
    {
        public long Id { get; set; }

        public long? UserId { get; set; }

        public string Action { get; set; }

        public string Module { get; set; }

        public string EntityType { get; set; }

        public long? EntityId { get; set; }

        public string Description { get; set; }

        public string RequestMethod { get; set; }

        public string Endpoint { get; set; }

        public string IpAddress { get; set; }

        public string UserAgent { get; set; }

        public string Status { get; set; }

        public int IsError { get; set; }

        public string ErrorCode { get; set; }

        public string ErrorMessage { get; set; }

        public string ErrorSource { get; set; }

        public string StackTrace { get; set; }

        public string OldValue { get; set; }

        public string NewValue { get; set; }

        public string Metadata { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}