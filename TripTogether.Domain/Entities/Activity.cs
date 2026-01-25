using NpgsqlTypes;


public class Activity : BaseEntity
{
    public Guid TripId { get; set; }
    public string Status { get; set; } = null!; // 'idea', 'scheduled'
    public string Title { get; set; } = null!;
    public string? Category { get; set; } // 'flight', 'hotel', 'food', 'attraction'

    // Scheduling details (Null if status is 'idea')
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }

    // Step 4 Scheduling
    public int? ScheduleDayIndex { get; set; }
    public string? ScheduleSlot { get; set; } // 'morning', 'afternoon', 'evening'

    public string? LocationName { get; set; }
    public NpgsqlPoint? GeoCoordinates { get; set; }

    // Metadata
    public string? LinkUrl { get; set; }
    public string? ImageUrl { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public virtual Trip Trip { get; set; } = null!;
    public virtual User Creator { get; set; } = null!;
}
