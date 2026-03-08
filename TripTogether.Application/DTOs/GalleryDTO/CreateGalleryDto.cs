using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TripTogether.Application.DTOs.GalleryDTO;

public class CreateGalleryDto
{
    public required Guid TripId { get; set; }
    public Guid? ActivityId { get; set; }

    [Required(ErrorMessage = "Image file is required")]
    public IFormFile ImageFile { get; set; } = null!;

    [MaxLength(500, ErrorMessage = "Caption cannot exceed 500 characters")]
    public string? Caption { get; set; }
}
