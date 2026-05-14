namespace AmarShowsBook.Models.Admin;

public class VwEnterpriseActivityLog
{
    public long EntityId { get; set; }

    public string? Module { get; set; }

    public string? Action { get; set; }

    public string? UserName { get; set; }

    public string? UserEmail { get; set; }

    public string? ReferenceNo { get; set; }

    public string? Status { get; set; }

    public string? Description { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime ActivityTime { get; set; }
}