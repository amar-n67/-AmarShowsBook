using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
namespace AmarShowsBook.Models
{
    // Ticket security analytics SQL view
    [Keyless]
    public class VwTicketValidationSummary
    {
        public int ValidationLogId { get; set; }

        public string ValidationResult { get; set; }

        [Column("is_security_issue")]
        public int IsSecurityIssue { get; set; }
    }
}