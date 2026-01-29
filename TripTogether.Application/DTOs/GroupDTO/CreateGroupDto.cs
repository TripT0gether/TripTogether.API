using System.ComponentModel.DataAnnotations;

namespace TripTogether.Application.DTOs.GroupDTO;

public class CreateGroupDto
{
    [Required(ErrorMessage = "Group name is required")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Group name must be between 3 and 100 characters")]
    public string Name { get; set; } = null!;
}