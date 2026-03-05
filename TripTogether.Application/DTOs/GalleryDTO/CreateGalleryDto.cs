using System.ComponentModel.DataAnnotations;

namespace TripTogether.Application.DTOs.GalleryDTO;

public class CreateGalleryDto
{
    public required Guid TripId { get; set; }
    public Guid? ActivityId { get; set; }

    [Required(ErrorMessage = "Image URL is required")]
    [Url(ErrorMessage = "Invalid URL format")]
    public string ImageUrl { get; set; } = null!;

    [MaxLength(500, ErrorMessage = "Caption cannot exceed 500 characters")]
    public string? Caption { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Display order must be a positive number")]
    public int DisplayOrder { get; set; }
}
