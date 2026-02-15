namespace TripTogether.Domain.Entities;

public class GroupInvite : BaseEntity
{
    public Guid GroupId { get; set; }
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }

    // Navigation properties
    public virtual Group Group { get; set; } = null!;
}
