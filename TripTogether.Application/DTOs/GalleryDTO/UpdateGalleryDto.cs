using System.ComponentModel.DataAnnotations;

namespace TripTogether.Application.DTOs.GalleryDTO;

public class UpdateGalleryDto
{
    [MaxLength(500, ErrorMessage = "Caption cannot exceed 500 characters")]
    public string? Caption { get; set; }
}
