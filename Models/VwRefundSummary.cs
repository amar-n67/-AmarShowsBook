using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
namespace AmarShowsBook.Models
{
    // Refund analytics SQL view
    [Keyless]
    public class VwRefundSummary
    {
        public int RefundId { get; set; }

        public string RefundStatus { get; set; }

        public decimal RefundAmount { get; set; }

        [Column("is_refund_error")]
        public int IsRefundError { get; set; }
    }
}