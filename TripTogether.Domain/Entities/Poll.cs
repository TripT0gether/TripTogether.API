



using TripTogether.Domain.Enums;

public class Poll : BaseEntity
{
    public Guid TripId { get; set; }
    public Guid? ActivityId { get; set; }
    public PollType Type { get; set; }
    public string Title { get; set; } = null!;
    public PollStatus Status { get; set; }

    // Navigation properties
    public virtual Trip Trip { get; set; } = null!;
    public virtual Activity? Activity { get; set; }
    public virtual ICollection<PollOption> Options { get; set; } = new List<PollOption>();
}
