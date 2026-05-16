namespace AmarShowsBook.Models.ViewModels
{
    public class SeatLockRequest
    {
        public int ScheduleId { get; set; }

        public List<long> SeatIds { get; set; }
            = new();
             public decimal TotalAmount { get; set; }
    }
}