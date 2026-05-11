using Microsoft.EntityFrameworkCore;

namespace AmarShowsBook.Models
{
    // Refund analytics SQL view
    [Keyless]
    public class VwRefundSummary
    {
        public int RefundId { get; set; }

        public string RefundStatus { get; set; }

        public decimal RefundAmount { get; set; }

        public int IsRefundError { get; set; }
    }
}