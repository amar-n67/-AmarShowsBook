using Microsoft.EntityFrameworkCore;

namespace AmarShowsBook.Models
{
    // Invoice analytics SQL view
    [Keyless]
    public class VwInvoiceSummary
    {
        public int InvoiceId { get; set; }

        public string InvoiceStatus { get; set; }

        public decimal TotalAmount { get; set; }

        public int IsInvoiceError { get; set; }
    }
}