using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models.Admin
{
    [Table("refund_action_logs")]
    public class RefundActionLog
    {
        public long id { get; set; }

        public long refund_id { get; set; }

        public string? refund_ref { get; set; }

        public string action_name { get; set; }

        public string? action_by { get; set; }

        public DateTime action_time { get; set; }

        public string? action_notes { get; set; }

        public string? ip_address { get; set; }

        public DateTime created_at { get; set; }
    }
}