namespace TripTogether.Application.DTOs.TripInviteDTO;

public class CreateTripInviteDto
{
    public Guid TripId { get; set; }
    public int ExpiresInHours { get; set; } = 168; // Default 7 days
}
