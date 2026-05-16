using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models
{
    [Table("booking_transactions")]
    public class BookingTransaction
    {
        [Key]
        public long Id { get; set; }

        public long BookingId { get; set; }

        public string TransactionRef { get; set; } = "";

        public string PaymentMethod { get; set; } = "";

        public decimal Amount { get; set; }

        public string PaymentStatus { get; set; } = "";

        public DateTime CreatedAt { get; set; }

        public DateTime? PaidAt { get; set; }
    }
}