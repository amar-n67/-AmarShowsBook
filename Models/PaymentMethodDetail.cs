using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models
{
    [Table("payment_method_details")]
    public class PaymentMethodDetail
    {
        [Key]
        public long Id { get; set; }

        public long BookingId { get; set; }

        public long BookingDraftId { get; set; }

        public long UserId { get; set; }

        public string PaymentMethod { get; set; } = "";

        public string? UpiName { get; set; }

        public string? UpiId { get; set; }

        public string? UpiHandle { get; set; }

        public string? CardLast4 { get; set; }

        public string? CardExpiry { get; set; }

        public string? CardNetwork { get; set; }

        public string? NetBank { get; set; }

        public string? NetBankUserIdMasked { get; set; }

        public bool OtpVerified { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
