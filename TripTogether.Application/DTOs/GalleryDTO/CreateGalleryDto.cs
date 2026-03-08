using System.ComponentModel.DataAnnotations;

namespace TripTogether.Application.DTOs.GalleryDTO;

public class CreateGalleryDto
{
    public required Guid TripId { get; set; }
    public Guid? ActivityId { get; set; }

    [MaxLength(500, ErrorMessage = "Caption cannot exceed 500 characters")]
    public string? Caption { get; set; }
}
