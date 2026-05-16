using System.Collections.Generic;

namespace AmarShowsBook.Models.ViewModels
{
    public class HomeViewModel
    {
        //public List<ShowSchedule> Schedules { get; set; } //comment to handle nullability in the database
        public List<ShowSchedule> Schedules { get; set; } = new();
    }
}