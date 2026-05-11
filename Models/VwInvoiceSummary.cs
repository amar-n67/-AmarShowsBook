using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
namespace AmarShowsBook.Models
{
    // Invoice analytics SQL view
    [Keyless]
    public class VwInvoiceSummary
    {
        public int InvoiceId { get; set; }

        public string InvoiceStatus { get; set; }

        public decimal TotalAmount { get; set; }
        [Column("is_invoice_error")]    
        public int IsInvoiceError { get; set; }
    }
}