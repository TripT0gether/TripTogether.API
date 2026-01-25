

public class Badge : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public string? Category { get; set; } // 'social', 'planning', 'budget'
    public string? Criteria { get; set; } // JSON for flexible rules

    // Navigation properties
    public virtual ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();
}
