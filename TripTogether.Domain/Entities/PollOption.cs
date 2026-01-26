using TripTogether.Domain.Enums;

public class PollOption : BaseEntity
{
    public Guid PollId { get; set; }
    public string? TextValue { get; set; }
    public string? MediaUrl { get; set; }
    public string? Metadata { get; set; } // JSON for budget ranges or specific dates

    // Date Voting Details
    public DateOnly? DateStart { get; set; }
    public DateOnly? DateEnd { get; set; }
    public TimeSlot? TimeOfDay { get; set; }

    // Navigation properties
    public virtual Poll Poll { get; set; } = null!;
    public virtual ICollection<Vote> Votes { get; set; } = new List<Vote>();
}
