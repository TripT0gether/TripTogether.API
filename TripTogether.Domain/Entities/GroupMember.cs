

public class GroupMember
{
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = null!; // 'leader', 'member'
    public string Status { get; set; } = null!; // 'pending', 'active'

    // Navigation properties
    public virtual Group Group { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
