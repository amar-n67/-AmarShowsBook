namespace AmarShowsBook.Models
{
    public class DummyCard
    {
        public long Id { get; set; }

        public string CardNo { get; set; }="";

        public string HolderName { get; set; }="";

        public string CVV { get; set; }="";

        public string Expiry { get; set; }="";
    }
}