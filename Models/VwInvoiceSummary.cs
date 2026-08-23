using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models
{
    // Invoice analytics SQL view
    [Keyless]
    public class VwInvoiceSummary
    {
        [Column("invoice_id")]
        public long InvoiceId { get; set; }

        [Column("invoice_number")]
        public string? InvoiceNumber { get; set; }

        [Column("invoice_type")]
        public string? InvoiceType { get; set; }

        [Column("invoice_status")]
        public string? InvoiceStatus { get; set; }

        [Column("invoice_date")]
        public DateTime? InvoiceDate { get; set; }

        [Column("booking_ref")]
        public string? BookingRef { get; set; }

        [Column("transaction_ref")]
        public string? TransactionRef { get; set; }

        [Column("customer_name")]
        public string? CustomerName { get; set; }

        [Column("customer_email")]
        public string? CustomerEmail { get; set; }

        [Column("subtotal_amount")]
        public decimal? SubtotalAmount { get; set; }

        [Column("cgst_amount")]
        public decimal? CgstAmount { get; set; }

        [Column("sgst_amount")]
        public decimal? SgstAmount { get; set; }

        [Column("igst_amount")]
        public decimal? IgstAmount { get; set; }

        [Column("discount_amount")]
        public decimal? DiscountAmount { get; set; }

        [Column("total_amount")]
        public decimal? TotalAmount { get; set; }

        [Column("currency")]
        public string? Currency { get; set; }

        [Column("gstin")]
        public string? Gstin { get; set; }

        [Column("invoice_pdf_url")]
        public string? InvoicePdfUrl { get; set; }

        [Column("is_invoice_error")]
        public int IsInvoiceError { get; set; }
    }
}
