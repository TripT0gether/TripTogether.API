

public class Friendship
{
    public Guid RequesterId { get; set; }
    public Guid AddresseeId { get; set; }
    public string Status { get; set; } = null!; // 'pending', 'accepted', 'blocked'
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public virtual User Requester { get; set; } = null!;
    public virtual User Addressee { get; set; } = null!;
}
