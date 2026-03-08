namespace TripTogether.Application.DTOs.GalleryDTO;

public class GalleryDto
{
    public Guid Id { get; set; }
    public Guid? TripId { get; set; }
    public Guid? ActivityId { get; set; }
    public string ImageUrl { get; set; } = null!;
    public string? Caption { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
}
