namespace AmarShowsBook.Models
{
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

        // =====================================================
        // ADMIN WORKFLOW TRACKING
        // =====================================================

        public string? workflow_action { get; set; }

        public string? approved_by { get; set; }

        public DateTime? approved_at { get; set; }

        public string? rejected_by { get; set; }

        public DateTime? rejected_at { get; set; }

        public string? retried_by { get; set; }

        public DateTime? retried_at { get; set; }

        public string? admin_notes { get; set; }
    }
}