using NpgsqlTypes;
using TripTogether.Domain.Enums;

public class Activity : BaseEntity
{
    public Guid TripId { get; set; }
    public ActivityStatus Status { get; set; }
    public string Title { get; set; } = null!;
    public ActivityCategory? Category { get; set; }

    // Scheduling details (Null if status is 'idea')
    public DateOnly? Date { get; set; }

    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public int? ScheduleDayIndex { get; set; }

    public string? LocationName { get; set; }
    public NpgsqlPoint? GeoCoordinates { get; set; }

    // Metadata
    public string? LinkUrl { get; set; }
    public string? ImageUrl { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public virtual Trip Trip { get; set; } = null!;
}
