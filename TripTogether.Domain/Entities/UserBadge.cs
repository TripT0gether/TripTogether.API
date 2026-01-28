





public class UserBadge : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid BadgeId { get; set; }
    public Guid TripId { get; set; }
    public DateTime EarnedAt { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Badge Badge { get; set; } = null!;
    public virtual Trip Trip { get; set; } = null!;
}
