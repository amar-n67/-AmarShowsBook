using System.ComponentModel.DataAnnotations.Schema;
namespace AmarShowsBook.Models
{
    [Table("refunds")]
    public class Refund
    {
        [Column("id")]
        public long id { get; set; }

        [Column("booking_id")]
        public long booking_id { get; set; }

        [Column("transaction_id")]
        public long transaction_id { get; set; }

        [Column("user_id")]
        public long user_id { get; set; }

        [Column("refund_ref")]
        public string refund_ref { get; set; }

        [Column("refund_amount")]
        public decimal refund_amount { get; set; }

        [Column("refund_reason")]
        public string refund_reason { get; set; }

        [Column("refund_status")]
        public string refund_status { get; set; }

        [Column("refund_method")]
        public string refund_method { get; set; }

        [Column("gateway_refund_id")]
        public string? gateway_refund_id { get; set; }

        [Column("failure_reason")]
        public string? failure_reason { get; set; }

        [Column("requested_at")]
        public DateTime requested_at { get; set; }

        [Column("processed_at")]
        public DateTime? processed_at { get; set; }

        [Column("created_at")]
        public DateTime created_at { get; set; }

        [Column("updated_at")]
        public DateTime? updated_at { get; set; }


        [Column("workflow_action")]
        public string? workflow_action { get; set; }

        [Column("approved_by")]
        public string? approved_by { get; set; }

        [Column("approved_at")]
        public DateTime? approved_at { get; set; }

        [Column("rejected_by")]
        public string? rejected_by { get; set; }

        [Column("rejected_at")]
        public DateTime? rejected_at { get; set; }

        [Column("retried_by")]
        public string? retried_by { get; set; }

        [Column("retried_at")]
        public DateTime? retried_at { get; set; }

        [Column("admin_notes")]
        public string? admin_notes { get; set; }
    }
}