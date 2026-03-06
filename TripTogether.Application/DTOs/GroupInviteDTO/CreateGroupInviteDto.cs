namespace TripTogether.Application.DTOs.GroupInviteDTO;

public class CreateGroupInviteDto
{
    public Guid GroupId { get; set; }
    public int ExpiresInHours { get; set; } = 168; // Default 7 days
}
