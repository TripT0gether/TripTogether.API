

using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
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

    // Helper
    [NotMapped]
    public BadgeCriteria? CriteriaDetails
    {
        get => Criteria == null ? null : JsonSerializer.Deserialize<BadgeCriteria>(Criteria);
        set => Criteria = value == null ? null : JsonSerializer.Serialize(value);
    }
}
public class BadgeCriteria
{
    public string Metric { get; set; } // e.g., "photo_count"
    public int Threshold { get; set; } // e.g., 50
}

