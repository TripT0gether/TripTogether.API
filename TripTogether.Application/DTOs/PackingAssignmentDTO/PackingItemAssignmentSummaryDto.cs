namespace TripTogether.Application.DTOs.PackingAssignmentDTO;

public class PackingItemAssignmentSummaryDto
{
    public Guid PackingItemId { get; set; }
    public string PackingItemName { get; set; } = null!;
    public string? Category { get; set; }
    public bool IsShared { get; set; }
    public int QuantityNeeded { get; set; }
    public int TotalAssigned { get; set; }
    public int Remaining { get; set; }
    public bool IsFullyAssigned { get; set; }
    public int AssignmentCount { get; set; }
    public List<PackingAssignmentDto> Assignments { get; set; } = new();
}
