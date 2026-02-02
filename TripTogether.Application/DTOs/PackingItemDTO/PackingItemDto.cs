namespace TripTogether.Application.DTOs.PackingItemDTO;

public class PackingItemDto
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public string Name { get; set; } = null!;
    public string? Category { get; set; }
    public bool IsShared { get; set; }
    public int QuantityNeeded { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
