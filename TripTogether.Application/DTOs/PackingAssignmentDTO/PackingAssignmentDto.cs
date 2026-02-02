namespace TripTogether.Application.DTOs.PackingAssignmentDTO;

public class PackingAssignmentDto
{
    public Guid Id { get; set; }
    public Guid PackingItemId { get; set; }
    public string PackingItemName { get; set; } = null!;
    public Guid UserId { get; set; }
    public string UserName { get; set; } = null!;
    public string? UserAvatarUrl { get; set; }
    public int Quantity { get; set; }
    public bool IsChecked { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
