namespace AmarShowsBook.Models.Admin;

public class DeletedUser
{
    public long DeletedUserId { get; set; }

    public long OriginalUserId { get; set; }

    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? Mobile { get; set; }

    public string? Password { get; set; }

    public string? Language { get; set; }

    public string? Genre { get; set; }

    public string? Country { get; set; }

    public string? State { get; set; }

    public string? District { get; set; }

    public string? Address { get; set; }

    public string? Pincode { get; set; }

    public string? ProfileImagePath { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public long? DeletedBy { get; set; }

    public string? DeleteReason { get; set; }

    public DateTime? RevokeAt { get; set; }

    public long? RevokedBy { get; set; }

    public bool IsRevoked { get; set; }
}