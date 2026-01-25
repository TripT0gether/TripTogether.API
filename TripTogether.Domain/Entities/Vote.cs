

public class Vote
{
    public Guid PollOptionId { get; set; }
    public Guid UserId { get; set; }

    // Navigation properties
    public virtual PollOption PollOption { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
