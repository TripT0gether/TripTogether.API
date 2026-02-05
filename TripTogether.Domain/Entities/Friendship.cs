



using TripTogether.Domain.Enums;

public class Friendship : BaseEntity
{
    public Guid AddresseeId { get; set; }
    public FriendshipStatus Status { get; set; }

    // Navigation properties
    public virtual User Requester { get; set; } = null!;
    public virtual User Addressee { get; set; } = null!;
}
