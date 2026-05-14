namespace AmarShowsBook.Models.Admin;

public class VwEnterpriseActivityLog
{
    public long Id { get; set; }

    public DateTime ActivityTime { get; set; }

    public string Module { get; set; } = "NA";

    public string ActionType { get; set; } = "NA";

    public string Description { get; set; } = "NA";

    public string UserName { get; set; } = "NA";

    public string UserEmail { get; set; } = "NA";

    public string Status { get; set; } = "NA";

    public string IpAddress { get; set; } = "NA";

    public string ErrorMessage { get; set; } = "NA";
}