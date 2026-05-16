namespace AmarShowsBook.Models.ViewModels
{
    public class SeatVM
    {
        public long SeatId
        {get;set;}

        public string Row
        {get;set;}

        public string Number
        {get;set;}

        public decimal Price
        {get;set;}

        public string Category
        {get;set;}

        public bool IsBooked
        {get;set;}

        public bool IsLocked
        {get;set;}
    }
}