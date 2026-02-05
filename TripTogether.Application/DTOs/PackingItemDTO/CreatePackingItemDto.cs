using System.ComponentModel.DataAnnotations;

namespace TripTogether.Application.DTOs.PackingItemDTO;

public class CreatePackingItemDto
{
    [Required(ErrorMessage = "Trip ID is required")]
    public Guid TripId { get; set; }

    [Required(ErrorMessage = "Item name is required")]
    [MaxLength(200, ErrorMessage = "Item name cannot exceed 200 characters")]
    public string Name { get; set; } = null!;

    [MaxLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
    public string? Category { get; set; }

    public bool IsShared { get; set; } = false;

    [Range(1, 1000, ErrorMessage = "Quantity needed must be between 1 and 1000")]
    public int QuantityNeeded { get; set; } = 1;
}
