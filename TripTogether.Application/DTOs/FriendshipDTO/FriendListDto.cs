namespace TripTogether.Application.DTOs.FriendshipDTO;

public class FriendListDto
{
    public Guid FriendshipId { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public DateTime FriendsSince { get; set; }
}
