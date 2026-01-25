

public class PackingItem : BaseEntity
{
    public Guid TripId { get; set; }
    public string Name { get; set; } = null!;
    public string? Category { get; set; }
    public bool IsShared { get; set; } // true = Group, false = Personal
    public int QuantityNeeded { get; set; } = 1;

    // Navigation properties
    public virtual Trip Trip { get; set; } = null!;
    public virtual User Creator { get; set; } = null!;
    public virtual ICollection<PackingAssignment> Assignments { get; set; } = new List<PackingAssignment>();
}
