using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmarShowsBook.Models;

[Table("news_broadcast_slots")]
public class NewsBroadcastSlot
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("channel_id")]
    public long ChannelId { get; set; }

    public NewsChannel? Channel { get; set; }

    [Column("program_title")]
    public string ProgramTitle { get; set; } = string.Empty;

    [Column("program_type")]
    public string ProgramType { get; set; } = string.Empty;

    [Column("starts_at")]
    public DateTime StartsAt { get; set; }

    [Column("ends_at")]
    public DateTime EndsAt { get; set; }

    [Column("is_live")]
    public bool IsLive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
