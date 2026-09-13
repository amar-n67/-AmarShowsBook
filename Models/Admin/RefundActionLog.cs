using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models.Admin
{
    [Table("refund_action_logs")]
    public class RefundActionLog
    {
        [Column("id")]
        public long id { get; set; }

        [Column("refund_id")]
        public long refund_id { get; set; }

        [Column("refund_ref")]
        public string? refund_ref { get; set; }

        [Column("action_name")]
        public string action_name { get; set; } = string.Empty;

        [Column("action_by")]
        public string? action_by { get; set; }

        [Column("action_time")]
        public DateTime action_time { get; set; }

        [Column("action_notes")]
        public string? action_notes { get; set; }

        [Column("ip_address")]
        public string? ip_address { get; set; }

        [Column("created_at")]
        public DateTime created_at { get; set; }
    }
}
