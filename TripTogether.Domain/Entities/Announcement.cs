using TripTogether.Domain.Enums;

public class Announcement : BaseEntity
{
    public AnnouncementType Type { get; set; }
    public required string Message { get; set; }

    // Context relationships
    public Guid? GroupId { get; set; }
    public Guid? TripId { get; set; }

    // Related entity references (nullable - depends on announcement type)
    public Guid? ActivityId { get; set; }
    public Guid? PollId { get; set; }
    public Guid? PackingItemId { get; set; }
    public Guid? FriendshipId { get; set; }
    public Guid? GroupInviteId { get; set; }

    // Target user (if announcement is for a specific user, null means all group/trip members)
    public Guid? TargetUserId { get; set; }

    // Sender/Creator of the announcement (who triggered the action)
    public Guid? FromUserId { get; set; }

    // Read status
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    // Navigation properties
    public virtual User? TargetUser { get; set; }
    public virtual User? FromUser { get; set; }
    public virtual Group? Group { get; set; }
    public virtual Trip? Trip { get; set; }
    public virtual Activity? Activity { get; set; }
    public virtual Poll? Poll { get; set; }
    public virtual PackingItem? PackingItem { get; set; }
    public virtual Friendship? Friendship { get; set; }
    public virtual GroupInvite? GroupInvite { get; set; }
}
