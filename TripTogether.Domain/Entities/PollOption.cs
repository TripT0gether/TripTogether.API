using TripTogether.Domain.Enums;

public class PollOption : BaseEntity
{
    public Guid PollId { get; set; }
    public string? TextValue { get; set; }
    public string? MediaUrl { get; set; }
    public decimal? Budget { get; set; }

    // Date Voting Details
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    // Time Voting Details
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }

    // Time of Day Category (optional categorization)
    public TimeSlot? TimeOfDay { get; set; }

    // Navigation properties
    public virtual Poll Poll { get; set; } = null!;
    public virtual ICollection<Vote> Votes { get; set; } = new List<Vote>();
}
