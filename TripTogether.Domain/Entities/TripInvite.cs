

public class TripInvite : BaseEntity
{
    public Guid TripId { get; set; }
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }

    // Navigation properties
    public virtual Trip Trip { get; set; } = null!;
    public virtual User Creator { get; set; } = null!;
}
