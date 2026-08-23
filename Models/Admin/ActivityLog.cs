using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models.Admin
{
    public class ActivityLog
    {
        [Column("id")]
        public long Id { get; set; }

        [Column("user_id")]
        public long? UserId { get; set; }

        [Column("action")]
        public string Action { get; set; }

        [Column("module")]
        public string Module { get; set; }

        [Column("entity_type")]
        public string EntityType { get; set; }

        [Column("entity_id")]
        public long? EntityId { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("status")]
        public string Status { get; set; }

        [Column("is_error")]
        public int IsError { get; set; }

        [Column("error_code")]
        public string ErrorCode { get; set; }

        [Column("error_message")]
        public string ErrorMessage { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
