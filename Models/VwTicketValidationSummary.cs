using Microsoft.EntityFrameworkCore;

namespace AmarShowsBook.Models
{
    // Ticket security analytics SQL view
    [Keyless]
    public class VwTicketValidationSummary
    {
        public int ValidationLogId { get; set; }

        public string ValidationResult { get; set; }

        public int IsSecurityIssue { get; set; }
    }
}