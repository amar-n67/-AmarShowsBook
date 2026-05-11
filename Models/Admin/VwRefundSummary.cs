namespace AmarShowsBook.Models.Admin
{
    public class VwRefundSummary
    {
        public long RefundId { get; set; }

        public string RefundRef { get; set; }

        public string UserName { get; set; }

        public decimal RefundAmount { get; set; }

        public string RefundStatus { get; set; }
    }
}