

public class Post : BaseEntity
{
    public Guid TripId { get; set; }
    public Guid UserId { get; set; }
    public string? ImageUrl { get; set; }
    public string? Caption { get; set; }
    public string? LocationTag { get; set; }
    public Guid[] Likes { get; set; } = Array.Empty<Guid>();

    // Navigation properties
    public virtual Trip Trip { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
