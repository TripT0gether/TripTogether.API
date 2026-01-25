

public class Poll : BaseEntity
{
    public Guid TripId { get; set; }
    public string Type { get; set; } = null!; // 'date', 'destination', 'budget'
    public string Title { get; set; } = null!;
    public string Status { get; set; } = null!; // 'open', 'closed'

    // Navigation properties
    public virtual Trip Trip { get; set; } = null!;
    public virtual User Creator { get; set; } = null!;
    public virtual ICollection<PollOption> Options { get; set; } = new List<PollOption>();
}
