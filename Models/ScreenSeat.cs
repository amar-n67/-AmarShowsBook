namespace AmarShowsBook.Models
{
    public class ScreenSeat
    {
        public long Id { get; set; }

        public long ScreenId { get; set; }
        public Screen Screen { get; set; }

        public int ScheduleId { get; set; }

        public ShowSchedule Schedule { get; set; }

        public string SeatRow { get; set; }

        public string SeatNumber { get; set; }

        public string SeatCategory { get; set; }

        public decimal SeatPrice { get; set; }

        public bool IsActive { get; set; }
    }
}
