using System.ComponentModel.DataAnnotations;

namespace TripTogether.Application.DTOs.PackingAssignmentDTO;

public class CreatePackingAssignmentDto
{
    [Required(ErrorMessage = "Packing item ID is required")]
    public Guid PackingItemId { get; set; }

    public Guid? UserId { get; set; }

    [Range(1, 1000, ErrorMessage = "Quantity must be between 1 and 1000")]
    public int Quantity { get; set; } = 1;
}
