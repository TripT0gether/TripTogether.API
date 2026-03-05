namespace TripTogether.Application.DTOs.GalleryDTO;

public class GalleryQueryDto
{
    public required Guid TripId { get; set; }
    public Guid? ActivityId { get; set; }
    public string? SearchTerm { get; set; }
}
