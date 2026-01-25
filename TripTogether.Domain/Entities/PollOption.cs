public class PollOption : BaseEntity
{
    public Guid PollId { get; set; }
    public string? TextValue { get; set; }
    public string? MediaUrl { get; set; }
    public string? Metadata { get; set; } // JSON for budget ranges or specific dates

    // Date Voting Details
    public DateOnly? DateStart { get; set; }
    public DateOnly? DateEnd { get; set; }
    public string? TimeOfDay { get; set; } // 'morning', 'afternoon', 'evening'

    // Navigation properties
    public virtual Poll Poll { get; set; } = null!;
    public virtual ICollection<Vote> Votes { get; set; } = new List<Vote>();
}
