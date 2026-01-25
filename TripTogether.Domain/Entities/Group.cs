

public class Group : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? CoverPhotoUrl { get; set; }

    // Navigation properties
    public virtual User Creator { get; set; } = null!;
    public virtual ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
    public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
}
