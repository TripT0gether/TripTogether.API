public class Gallery : BaseEntity
{
    public Guid? TripId { get; set; }
    public Guid? ActivityId { get; set; }
    public string ImageUrl { get; set; } = null!;
    public string? Caption { get; set; }
    public int DisplayOrder { get; set; }

    // Navigation properties
    public virtual Trip? Trip { get; set; }
    public virtual Activity? Activity { get; set; }
}
