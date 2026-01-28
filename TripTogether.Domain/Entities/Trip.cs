

using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using TripTogether.Domain.Enums;

public class Trip : BaseEntity
{
    public Guid GroupId { get; set; }
    public string Title { get; set; } = null!;
    public TripStatus Status { get; set; }

    // Step 1 Planning Context
    public DateOnly? PlanningRangeStart { get; set; }
    public DateOnly? PlanningRangeEnd { get; set; }

    // Final confirmed dates (Result of Step 3)
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    // Stores WiFi, Contacts, Packing settings as JSON
    public string? Settings { get; set; }

    // Navigation properties
    public virtual Group Group { get; set; } = null!;
    public virtual ICollection<TripInvite> Invites { get; set; } = new List<TripInvite>();
    public virtual ICollection<Poll> Polls { get; set; } = new List<Poll>();
    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
    public virtual ICollection<PackingItem> PackingItems { get; set; } = new List<PackingItem>();
    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public virtual ICollection<Settlement> Settlements { get; set; } = new List<Settlement>();
    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
    public virtual ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();

    // Helper
    [NotMapped]
    public TripSettings? SettingsDetails
    {
        get => Settings == null ? null : JsonSerializer.Deserialize<TripSettings>(Settings);
        set => Settings = value == null ? null : JsonSerializer.Serialize(value);
    }
}

public class TripSettings
{
    public List<Contact>? EmergencyContacts { get; set; }
}
public class Contact { public string Name { get; set; } public string Number { get; set; } }