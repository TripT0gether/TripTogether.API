

using TripTogether.Domain.Enums;

public class Badge : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public BadgeCategory? Category { get; set; }
    public string? Criteria { get; set; } // JSON for flexible rules

    // Navigation properties
    public virtual ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();
}
