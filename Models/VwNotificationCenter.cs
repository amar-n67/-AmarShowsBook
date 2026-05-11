using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
namespace AmarShowsBook.Models
{
    // Notification analytics SQL view
    [Keyless]
    public class VwNotificationCenter
    {
        public int NotificationId { get; set; }

        public string Status { get; set; }

        [Column("is_error")]
        public int IsError { get; set; }
    }
}