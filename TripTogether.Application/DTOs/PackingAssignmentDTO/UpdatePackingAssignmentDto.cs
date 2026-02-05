using System.ComponentModel.DataAnnotations;

namespace TripTogether.Application.DTOs.PackingAssignmentDTO;

public class UpdatePackingAssignmentDto
{
    [Range(1, 1000, ErrorMessage = "Quantity must be between 1 and 1000")]
    public int? Quantity { get; set; }

    public bool? IsChecked { get; set; }
}
