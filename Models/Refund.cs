using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models
{
    [Table("refunds")]
    public class Refund
    {
        public long id { get; set; }

        public long booking_id { get; set; }

        public long transaction_id { get; set; }

        public long user_id { get; set; }

        public string refund_ref { get; set; }

        public decimal refund_amount { get; set; }

        public string refund_reason { get; set; }

        public string refund_status { get; set; }

        public string refund_method { get; set; }

        public string? gateway_refund_id { get; set; }

        public string? failure_reason { get; set; }

        public DateTime requested_at { get; set; }

        public DateTime? processed_at { get; set; }

        public DateTime created_at { get; set; }

        public DateTime? updated_at { get; set; }
    }
}