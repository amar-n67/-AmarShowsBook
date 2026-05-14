namespace AmarShowsBook.Models.Admin
{
    public class VwEnterpriseActivityLog
    {
        public long entity_id { get; set; }

        public string module { get; set; }

        public string action { get; set; }

        public string? user_name { get; set; }

        public string? user_email { get; set; }

        public string? reference_no { get; set; }

        public string status { get; set; }

        public string description { get; set; }

        public string? error_message { get; set; }

        public DateTime activity_time { get; set; }
    }
}