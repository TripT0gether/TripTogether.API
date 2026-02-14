namespace TripTogether.Application.DTOs.TripDTO;

public class CreateTripDto
{
    public Guid GroupId { get; set; }
    public string Title { get; set; } = null!;
}
