

public class PackingAssignment : BaseEntity
{
    public Guid PackingItemId { get; set; }
    public Guid UserId { get; set; }
    public int Quantity { get; set; } = 1;
    public bool IsChecked { get; set; }

    // Navigation properties
    public virtual PackingItem PackingItem { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
